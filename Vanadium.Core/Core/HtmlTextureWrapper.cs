// Decompiled with JetBrains decompiler
// Type: Vanadium.Core.HtmlTextureWrapper
// Assembly: Vanadium.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 57C678C6-0611-48DA-B8C5-441FD4527177
// Assembly location: D:\local\vvvv-gp\packs\Vanadium\nodes\plugins\Vanadium.Core.exe

using Chromium;
using Chromium.Event;
using Chromium.Remote;
using Chromium.Remote.Event;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using md.stdl.Interaction;
using md.stdl.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;
using md.stdl.Coding;
using VVVV.Core.Logging;
using VVVV.DX11;
using VVVV.DX11.Lib.Effects.Pins.RenderSemantics;
using VVVV.Utils.IO;
using VVVV.Utils.VMath;

namespace Vanadium.Core
{
    public partial class HtmlTextureWrapper : IMainlooping, IDisposable
    {
        public static Dictionary<int, HtmlTextureWrapper> Instances = new Dictionary<int, HtmlTextureWrapper>();
        public static readonly (int w, int h) DefaultSize = (3840, 2160);

        public const int DefaultWidth = 3840;
        public const int DefaultHeight = 2160;

        //private readonly object _lockingObject = new object();

        private bool _allowInitFrameRequest = false;
        private bool _browserReadyFrame = false;
        private bool _loadedFrame = false;
        private bool _invalidateAccFrame = false;
        private readonly Dictionary<DX11RenderContext, bool> _newAccFrameReady = new Dictionary<DX11RenderContext, bool>();
        private IntPtr _newAccFramePtr;
        private string _customContent = "";
        private string _customContentUrl = "";

        private WrapperTextureSettings _prevTextureSettings;
        private WrapperBrowserSettings _prevBrowserSettings;
        private Subscription<Mouse, MouseNotification> _mouseSubscription;
        private Subscription<Keyboard, KeyNotification> _keyboardSubscription;
        private Keyboard _keyboard;

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

        public bool Enabled { get; set; }

        public WrapperBrowserSettings BrowserSettings { get; set; }
        public WrapperTextureSettings TextureSettings { get; set; }
        public WrapperInitSettings InitSettings { get; }

        public JsBinding OnLoadBinding { get; private set; }
        public ResizeNotificationFunction ResizeNotification { get; private set; }
        public DocSizeBaseSelector DocumentSizeBaseSelector { get; private set; }
        public Dictionary<string, JsBinding> JsBindings { get; } = new Dictionary<string, JsBinding>();

        public IEnumerable<HtmlTextureOperation> Operations { get; set; }
        public Dictionary<HtmlTextureOperation, object> OperationResults { get; } = new Dictionary<HtmlTextureOperation, object>();

        public List<string> UrlFilter { get; } = new List<string>();

        public ILogger VvvvLogger { get; set; }
        
        public int BrowserId { get; private set; }
        
        public (int w, int h) DocumentSize { get; private set; }
        public (int w, int h) TextureSize { get; private set; }

        public CfxMouseEvent MouseEvent { get; set; }
        public CfxBrowser Browser { get; private set; }
        public CfrBrowser RemoteBrowser { get; set; }
        public CfxStringVisitor Visitor { get; private set; }
        public CfxClient Client { get; private set; }
        public CfxContextMenuHandler ContextMenuHandler { get; private set; }
        public CfxLifeSpanHandler LifeSpanHandler { get; private set; }
        public CfxLoadHandler LoadHandler { get; private set; }
        public CfxRenderHandler RenderHandler { get; private set; }
        public CfxRequestHandler RequestHandler { get; private set; }
        public CfxDisplayHandler DisplayHandler { get; private set; }
        public CfxBrowserSettings Settings { get; private set; }
        public CfrV8Handler V8Handler { get; private set; }

        public bool IsDocumentReady { get; set; }
        public bool Loading { get; set; }
        public bool LoadedFrame { get; private set; }
        public bool CreatedFrame { get; private set; }
        //public bool IsImageReady { get; private set; }
        public bool IsTextureValid { get; private set; }
        public bool LivePageActive { get; private set; }

        public double Progress { get; private set; }

        //public byte[] RawImage { get; private set; }

        public string LastError { get; private set; }
        public string LastConsole { get; private set; }
        public string CurrentUrl { get; private set; }
        public string LastStatusMessage { get; private set; }

        public XElement RootElement { get; private set; }
        public XDocument Dom { get; private set; }

        public DX11Resource<DX11Texture2D> DX11Texture { get; set; } = new DX11Resource<DX11Texture2D>();

        public event EventHandler OnBrowserReady;

        public Mouse Mouse
        {
            set
            {
                if (_mouseSubscription == null)
                {
                    _mouseSubscription = new Subscription<Mouse, MouseNotification>(mouse => mouse.MouseNotifications, (mouse, n) =>
                    {
                        if (!Enabled || Browser == null) return;

                        var cfxMouseEvent = new CfxMouseEvent
                        {
                            X = (int) VMath.Map(n.Position.X, 0.0, n.ClientArea.Width, 0.0, TextureSize.w, 0),
                            Y = (int) VMath.Map(n.Position.Y, 0.0, n.ClientArea.Height, 0.0, TextureSize.h, 0)
                        };

                        var type = CfxMouseButtonType.Left;
                        if (n is MouseButtonNotification buttonNotification && (n.Kind == MouseNotificationKind.MouseUp || n.Kind == MouseNotificationKind.MouseDown))
                        {
                            switch (buttonNotification.Buttons)
                            {
                                case MouseButtons.Left:
                                    type = CfxMouseButtonType.Left;
                                    break;
                                case MouseButtons.Right:
                                    type = CfxMouseButtonType.Right;
                                    break;
                                case MouseButtons.Middle:
                                    type = CfxMouseButtonType.Middle;
                                    break;
                            }
                        }

                        var invvwheel = BrowserSettings.InvertScrollWheel ? -1 : 1;
                        var invhwheel = BrowserSettings.InvertHorizontalScrollWheel ? -1 : 1;
                        switch (n.Kind)
                        {
                            case MouseNotificationKind.MouseDown:
                                Browser.Host.SendMouseClickEvent(cfxMouseEvent, type, false, 1);
                                break;
                            case MouseNotificationKind.MouseUp:
                                Browser.Host.SendMouseClickEvent(cfxMouseEvent, type, true, 1);
                                break;
                            case MouseNotificationKind.MouseMove:
                                Browser.Host.SendMouseMoveEvent(cfxMouseEvent, false);
                                break;
                            case MouseNotificationKind.MouseWheel:
                                if (!(n is MouseWheelNotification wvn)) break;
                                //Browser.Host.SetFocus(true);
                                Browser.Host.SendMouseWheelEvent(cfxMouseEvent, 0, wvn.WheelDelta * invvwheel);
                                break;
                            case MouseNotificationKind.MouseHorizontalWheel:
                                if (!(n is MouseHorizontalWheelNotification whn)) break;
                                //Browser.Host.SetFocus(true);
                                Browser.Host.SendMouseWheelEvent(cfxMouseEvent, whn.WheelDelta * invhwheel, 0);
                                break;
                        }
                    });
                }
                _mouseSubscription.Update(value);
            }
        }

        public Keyboard Keyboard
        {
            set
            {
                if (_keyboardSubscription == null)
                {
                    _keyboardSubscription = new Subscription<Keyboard, KeyNotification>(
                        keyboard => keyboard.KeyNotifications,
                        (keyboard, n) =>
                        {
                            if (!Enabled || Browser == null)
                                return;
                            var keyevents = new CfxKeyEvent
                            {
                                Modifiers = (uint) _keyboard.Modifiers >> 15
                            };
                            switch (n.Kind)
                            {
                                case KeyNotificationKind.KeyDown:
                                    if (!(n is KeyDownNotification downNotification)) break;

                                    keyevents.Type = CfxKeyEventType.Keydown;
                                    keyevents.WindowsKeyCode = (int) downNotification.KeyCode;
                                    keyevents.NativeKeyCode = (int) downNotification.KeyCode;

                                    break;

                                case KeyNotificationKind.KeyPress:
                                    if (!(n is KeyPressNotification pressNotification)) break;

                                    keyevents.Type = CfxKeyEventType.Char;
                                    keyevents.Character = checked((short) pressNotification.KeyChar);
                                    keyevents.UnmodifiedCharacter = checked((short) pressNotification.KeyChar);
                                    keyevents.WindowsKeyCode = (int) pressNotification.KeyChar;
                                    keyevents.NativeKeyCode = (int) pressNotification.KeyChar;

                                    break;

                                case KeyNotificationKind.KeyUp:
                                    if (!(n is KeyUpNotification keyUpNotification)) break;

                                    keyevents.Type = CfxKeyEventType.Keyup;
                                    keyevents.WindowsKeyCode = (int) keyUpNotification.KeyCode;
                                    keyevents.NativeKeyCode = (int) keyUpNotification.KeyCode;

                                    break;
                            }

                            Browser.Host.SendKeyEvent(keyevents);
                        }
                    );
                }
                _keyboard = value;
                _keyboardSubscription.Update(value);
            }
        }

        public HtmlTextureWrapper(WrapperInitSettings initSettings)
        {
            if (!HtmlTextureStartable.Started)
            {
                HtmlTextureStartable.Start();
            }

            InitSettings = initSettings;
        }

        public void Initialize()
        {
            Settings = new CfxBrowserSettings()
            {
                WindowlessFrameRate = InitSettings.Fps,
                Webgl = CfxState.Enabled,
                Plugins = CfxState.Enabled,
                ApplicationCache = CfxState.Enabled,
                Javascript = CfxState.Enabled,
                FileAccessFromFileUrls = CfxState.Enabled,
                UniversalAccessFromFileUrls = CfxState.Enabled,
                WebSecurity = CfxState.Disabled
            };
            Globals.UpdateScripts();

            TextureSize = (DefaultWidth, DefaultHeight);

            LifeSpanHandler = new CfxLifeSpanHandler();
            LifeSpanHandler.OnAfterCreated += (sender, e) =>
            {
                Browser = e.Browser;
                BrowserId = e.Browser.Identifier;
                Instances.UpdateGeneric(BrowserId, this);
                Browser.Host.WasResized();

                if(InitSettings.FrameRequestFromVvvv)
                    Browser.Host.SendExternalBeginFrame();

                _browserReadyFrame = true;
                //_invalidate = true;
            };
            LifeSpanHandler.OnBeforePopup += (sender, e) =>
            {
                if (BrowserSettings.AllowPopups)
                {
                    Browser.MainFrame.LoadUrl(e.TargetUrl);
                    e.SetReturnValue(true);
                }
                else e.SetReturnValue(true);
            };

            RenderHandler = new CfxRenderHandler();
            RenderHandler.GetViewRect += (sender, e) =>
            {
                e.Rect.X = 0;
                e.Rect.Y = 0;
                e.Rect.Width = TextureSize.w;
                e.Rect.Height = TextureSize.h;
                e.SetReturnValue(true);
            };
            RenderHandler.GetRootScreenRect += (sender, e) =>
            {
                e.Rect.X = 0;
                e.Rect.Y = 0;
                e.Rect.Width = TextureSize.w;
                e.Rect.Height = TextureSize.h;
                e.SetReturnValue(true);
            };
            //RenderHandler.OnPaint += HandleRenderPaint;
            RenderHandler.OnAcceleratedPaint += HandleAcceleratedRenderPaint;

            LoadHandler = new CfxLoadHandler();
            LoadHandler.OnLoadStart += (sender, e) => IsDocumentReady = false;
            LoadHandler.OnLoadError += (sender, e) =>
            {
                LogError(e.ErrorCode.ToString() + Environment.NewLine + "    " + e.ErrorText + Environment.NewLine + "    " + e.FailedUrl);
                LastError = e.ErrorText;
            };
            LoadHandler.OnLoadingStateChange += (sender, e) => CurrentUrl = Browser.MainFrame.Url;

            RequestHandler = new CfxRequestHandler();
            RequestHandler.OnBeforeBrowse += (sender, e) =>
            {
                var flag = UrlFilter.Any(u => u == "" || e.Browser.MainFrame.Url.Contains(u));
                e.SetReturnValue(!flag);
            };
            RequestHandler.OnBeforeResourceLoad += HandleBeforeResourceLoad;
            RequestHandler.OnCertificateError += (sender, e) => LogError("Cert error: " + e.CertError.ToString());
            RequestHandler.OnPluginCrashed += (sender, e) => LogError("Plugin Crashed: " + e.PluginPath);
            RequestHandler.OnRenderProcessTerminated += (sender, e) => LogError("Render Process Terminated: " + e.Status.ToString());

            ContextMenuHandler = new CfxContextMenuHandler();
            ContextMenuHandler.OnBeforeContextMenu += (sender, args) => args.Model.Clear();
            DisplayHandler = new CfxDisplayHandler();
            DisplayHandler.OnConsoleMessage += HandleConsoleMessage;
            DisplayHandler.OnLoadingProgressChange += (sender, args) => Progress = args.Progress;

            Client = new CfxClient();
            Client.GetLifeSpanHandler += (sender, e) => e.SetReturnValue(LifeSpanHandler);
            Client.GetRenderHandler += (sender, e) => e.SetReturnValue(RenderHandler);
            Client.GetLoadHandler += (sender, e) => e.SetReturnValue(LoadHandler);
            Client.GetRequestHandler += (sender, e) => e.SetReturnValue(RequestHandler);
            Client.GetDisplayHandler += (sender, e) => e.SetReturnValue(DisplayHandler);
            Client.GetContextMenuHandler += (sender, args) => args.SetReturnValue(ContextMenuHandler);
            Visitor = new CfxStringVisitor();
            Visitor.Visit += (sender, e) =>
            {
                RootElement = HtmlToXElement(e.String);
                Dom = new XDocument(RootElement);
            };

            CreateBrowser();
            //_invalidate = true;
        }

        public void OnLoadEnd()
        {
            IsDocumentReady = true;
            AuxOnLoad();
            UpdateSize();
            if (!string.IsNullOrWhiteSpace(_customContent))
            {
                Browser.MainFrame.LoadString(_customContent, _customContentUrl);
                _customContent = "";
                _customContentUrl = "";
            }
            _loadedFrame = true;
        }

        private void HandleConsoleMessage(object sender, CfxOnConsoleMessageEventArgs e)
        {
            LastConsole = $"{e.Message} ({e.Source}: {e.Line})";
            if (BrowserSettings.ListenConsole)
                VvvvLogger?.Log(LogType.Message, $"CEF: {e.Message} ({e.Source}: {e.Line})");
            e.SetReturnValue(false);
        }

        public void LogError(string message)
        {
            VvvvLogger?.Log(LogType.Error, "CEF Error: " + message);
            LastError = message;
        }

        private void HandleBeforeResourceLoad(object sender, CfxOnBeforeResourceLoadEventArgs e)
        {
            var headerMap = e.Request.GetHeaderMap();
            if (!string.IsNullOrWhiteSpace(BrowserSettings.UserAgent))
            {
                foreach (var strArray in headerMap)
                {
                    if (strArray[0] == "User-Agent") strArray[1] = BrowserSettings.UserAgent;
                }
            }
            e.Request.SetHeaderMap(headerMap);
            e.SetReturnValue(CfxReturnValue.Continue);
        }

        private void HandleAcceleratedRenderPaint(object sender, CfxOnAcceleratedPaintEventArgs e)
        {
            _allowInitFrameRequest = true;
            _invalidateAccFrame = e.SharedHandle != _newAccFramePtr;
            _newAccFramePtr = e.SharedHandle;
        }

        public void UpdateSize()
        {
            TextureSize =
            (
                (TextureSettings.AutoWidth ? DocumentSize.w : TextureSettings.TargetSize.w) + TextureSettings.ExtraSize.w,
                (TextureSettings.AutoHeight ? DocumentSize.h : TextureSettings.TargetSize.h) + TextureSettings.ExtraSize.h
            );

            Browser?.Host.WasResized();
        }

        private void CreateBrowser()
        {
            var windowInfo = new CfxWindowInfo();
            windowInfo.SetAsWindowless(IntPtr.Zero);

            windowInfo.WindowlessRenderingEnabled = true;
            windowInfo.SharedTextureEnabled = true;
            windowInfo.ExternalBeginFrameEnabled = InitSettings.FrameRequestFromVvvv;

            if (Browser != null)
            {
                Browser.Host.CloseBrowser(true);
                Browser = null;
            }
            CfxBrowserHost.CreateBrowser(windowInfo, Client, "", Settings, null);
        }

        private void AuxOnLoad()
        {
            OnLoadBinding = new JsBinding()
            {
                Name = "vvvvUtils"
            };
            DocumentSizeBaseSelector = OnLoadBinding.AddFunction(new DocSizeBaseSelector
            {
                Selector = BrowserSettings.DocumentSizeElementSelector
            });

            ResizeNotification = OnLoadBinding.AddFunction(new ResizeNotificationFunction());
            ResizeNotification.SizeChanged += (sender, args) =>
            {
                if(!TextureSettings.AutoWidth && !TextureSettings.AutoHeight) return;
                UpdateSize();
            };

            BindObject(OnLoadBinding);
            foreach (var code in Globals.ScriptsFromOnloadFolder.Values)
                ExecuteJavascript(code);
        }

        public void SendTouches(IEnumerable<TouchContainer> touches)
        {
            touches.Where(t => t.Id >= 0).ForEach(t =>
            {
                var cfxTouchEventType = CfxTouchEventType.Moved;
                if (t.AgeFrames < 2)
                    cfxTouchEventType = CfxTouchEventType.Pressed;
                if (t.ExpireFrames >= 1)
                    cfxTouchEventType = CfxTouchEventType.Released;
                Browser.Host.SendTouchEvent(new CfxTouchEvent()
                {
                    Type = cfxTouchEventType,
                    Id = t.Id,
                    X = (float) VMath.Map(t.Point.X, -1.0, 1.0, 0.0, TextureSize.w, TMapMode.Float),
                    Y = (float) VMath.Map(t.Point.Y, 1.0, -1.0, 0.0, TextureSize.h, TMapMode.Float),
                    Force = t.Force,
                });
            });
        }

        public static XElement HtmlToXElement(string html)
        {
            var htmlDocument = new HtmlAgilityPack.HtmlDocument()
            {
                OptionOutputAsXml = true
            };
            htmlDocument.LoadHtml(html);

            using (var stringWriter = new StringWriter())
            {
                htmlDocument.Save((TextWriter)stringWriter);
                using (var stringReader = new StringReader(stringWriter.ToString()))
                    return XElement.Load((TextReader)stringReader);
            }
        }

        public bool ExecuteJavascript(string code)
        {
            if (Browser == null) return false;
            Browser.MainFrame.ExecuteJavaScript(code, null, 0);
            return true;
        }

        public bool EvaluateJavascript(string code, Action<CfrV8Value, CfrV8Exception> callback)
        {
            if (RemoteBrowser == null)
                return false;
            try
            {
                var remoteCallContext = RemoteBrowser.CreateRemoteCallContext();
                remoteCallContext.Enter();
                try
                {
                    var taskrunner = CfrTaskRunner.GetForThread((CfxThreadId) 6);
                    if (taskrunner == null)
                    {
                        LogError("Not evaluating javascript, TaskRunner is null, wrong CEF thread ID?");
                        return false;
                    }
                    taskrunner.PostTask(new EvaluateTask(RemoteBrowser, code, callback));
                    return true;
                }
                finally
                {
                    remoteCallContext.Exit();
                }
            }
            catch (IOException ex)
            {
                LogError(ex.Message);
                VvvvLogger?.Log(ex);
                return false;
            }
        }

        public bool BindObject(JsBinding binding)
        {
            if (RemoteBrowser == null)
                return false;
            try
            {
                var jsBinding = binding;
                if (JsBindings.ContainsKey(binding.Name))
                {
                    jsBinding = JsBindings[binding.Name];
                    jsBinding.MigrateFunctions(binding);
                }
                else JsBindings.Add(binding.Name, binding);

                var remoteCallContext = RemoteBrowser.CreateRemoteCallContext();
                remoteCallContext.Enter();
                try
                {
                    if(V8Handler == null) V8Handler = new CfrV8Handler();
                    jsBinding.Bind(this);
                    var taskrunner = CfrTaskRunner.GetForThread((CfxThreadId)6);
                    if (taskrunner == null)
                    {
                        LogError("Unsuccessful Binding, TaskRunner is null");
                        return false;
                    }
                    taskrunner.PostTask(new BindFunctionsTask(RemoteBrowser, jsBinding.Name, jsBinding.Functions.Keys.ToArray(), V8Handler));
                    return true;
                }
                finally
                {
                    remoteCallContext.Exit();
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                VvvvLogger?.Log(ex);
                return false;
            }
        }

        public void ShowDevTool()
        {
            Browser.Host.ShowDevTools(new CfxWindowInfo()
            {
                Style = WindowStyle.WS_OVERLAPPEDWINDOW | WindowStyle.WS_CLIPCHILDREN | WindowStyle.WS_CLIPSIBLINGS | WindowStyle.WS_VISIBLE,
                ParentWindow = IntPtr.Zero,
                WindowName = "DevTools",
                X = 200,
                Y = 200,
                Width = 800,
                Height = 600
            }, new CfxClient(), new CfxBrowserSettings(), null);
        }

        public void Reload()
        {
            Browser.ReloadIgnoreCache();
        }

        public void UpdateDom()
        {
            Browser.MainFrame.GetSource(Visitor);
        }

        public void LoadUrl(string url)
        {
            Browser?.MainFrame?.LoadUrl(url);
        }
        
        public void LoadString(string content, string dummyurl)
        {
            if (Browser == null) return;
            Browser.MainFrame.LoadUrl("about:blank");
            _customContent = content;
            _customContentUrl = dummyurl;
        }

        public void ScrollTo(double h, double v, string elementSelector = "", bool normalized = false)
        {
            var usewindow = string.IsNullOrWhiteSpace(elementSelector);
            var norm = normalized ? "true" : "false";
            var code = !usewindow ? string.Format(CultureInfo.InvariantCulture,
                    "vvvvUtils.elementScroll({0}, {1}, \"{2}\", {3})", h, v, elementSelector, norm)
                : string.Format(CultureInfo.InvariantCulture,
                    "vvvvUtils.windowScroll({0}, {1}, {2})", h, v, norm);
            ExecuteJavascript(code);
        }

        public void Mainloop(float deltatime)
        {
            OnMainLoopBegin?.Invoke(this, EventArgs.Empty);

            if (CreatedFrame) CreatedFrame = false;
            if (_browserReadyFrame)
            {
                OnBrowserReady?.Invoke(this, EventArgs.Empty);
                CreatedFrame = true;
                _browserReadyFrame = false;
            }

            if (Browser == null) return;

            if (LoadedFrame) LoadedFrame = false;
            Loading = Browser.IsLoading;
            IsDocumentReady = !(!IsDocumentReady && Browser.IsLoading);
            if (_loadedFrame)
            {
                IsDocumentReady = false;
                LoadedFrame = true;
                _loadedFrame = false;
            }
            
            //if (!_allowInitFrameRequest) return;

            if (_invalidateAccFrame)
            {
                _newAccFrameReady.Clear();
            }

            if (!TextureSettings.Equals(_prevTextureSettings)) UpdateSize();

            if (BrowserSettings.ZoomLevel != Browser.Host.ZoomLevel)
            {
                Browser.Host.ZoomLevel = BrowserSettings.ZoomLevel;
            }

            if(DocumentSizeBaseSelector != null)
                DocumentSizeBaseSelector.Selector = BrowserSettings.DocumentSizeElementSelector;

            if (LivePageActive != BrowserSettings.UseLivePage)
            {
                ExecuteJavascript(BrowserSettings.UseLivePage ? Globals.LivePageLoader : Globals.LivePageUnloader);
                LivePageActive = BrowserSettings.UseLivePage;
            }

            if (Enabled)
            {
                var operations = Operations;
                operations?.ForEach(o => o?.Invoke(this));

                if(InitSettings.FrameRequestFromVvvv)
                    Browser.Host.SendExternalBeginFrame();
            }
            _prevTextureSettings = TextureSettings;
            _prevBrowserSettings = BrowserSettings;

            OnMainLoopEnd?.Invoke((object)this, EventArgs.Empty);
        }

        public void UpdateDX11Resources(DX11RenderContext context)
        {
            var invalidate = !_newAccFrameReady.ContainsKey(context) || !DX11Texture.Contains(context);
            if(invalidate)
            {
                Browser?.Host.Invalidate(CfxPaintElementType.View);
                UpdateSize();
            }
            if (!invalidate) invalidate = _newAccFrameReady[context];

            if (_newAccFramePtr != IntPtr.Zero && invalidate)
            {
                if(DX11Texture.Contains(context)) DX11Texture.Dispose(context);
                try
                {
                    DX11Texture[context] = DX11Texture2D.FromSharedHandle(context, _newAccFramePtr);
                    IsTextureValid = true;
                }
                catch
                {
                    IsTextureValid = false;
                }
            }

            if (invalidate)
            {
                _newAccFrameReady.UpdateGeneric(context, false);
            }
        }

        public void DestroyDX11Resources(DX11RenderContext context, bool force)
        {
            DX11Texture.Dispose(context);
        }

        public event EventHandler OnMainLoopBegin;

        public event EventHandler OnMainLoopEnd;

        public void Dispose()
        {
            Browser?.Host?.CloseBrowser(true);
            DX11Texture?.Dispose();
            _mouseSubscription?.Dispose();
            _keyboardSubscription?.Dispose();
            MouseEvent?.Dispose();
            Browser?.Dispose();
            RemoteBrowser?.Dispose();
            Visitor?.Dispose();
            Client?.Dispose();
            ContextMenuHandler?.Dispose();
            LifeSpanHandler?.Dispose();
            LoadHandler?.Dispose();
            RenderHandler?.Dispose();
            RequestHandler?.Dispose();
            DisplayHandler?.Dispose();
            Settings?.Dispose();
        }
    }
}
