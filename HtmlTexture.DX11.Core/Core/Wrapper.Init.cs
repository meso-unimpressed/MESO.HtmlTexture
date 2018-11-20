using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Chromium;
using Chromium.Event;
using md.stdl.Coding;
using md.stdl.Interfaces;
using VVVV.Core.Logging;

namespace VVVV.HtmlTexture.DX11.Core
{
    public partial class HtmlTextureWrapper : IMainlooping, IDisposable
    {
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
            //RenderHandler.OnPaint += (sender, args) => _paintFrame = true;
            RenderHandler.OnAcceleratedPaint += HandleAcceleratedRenderPaint;

            LoadHandler = new CfxLoadHandler();
            LoadHandler.OnLoadStart += (sender, e) =>
            {
                if (e.Browser.MainFrame.Url != _prevUrl)
                    IsDocumentReady = false;
                _prevUrl = e.Browser.MainFrame.Url;
            };
            LoadHandler.OnLoadError += (sender, e) =>
            {
                LogError(e.ErrorCode.ToString() + Environment.NewLine + "    " + e.ErrorText + Environment.NewLine + "    " + e.FailedUrl);
                LastError = e.ErrorText;
            };
            LoadHandler.OnLoadingStateChange += (sender, e) => CurrentUrl = Browser.MainFrame.Url;

            RequestHandler = new CfxRequestHandler();
            RequestHandler.OnBeforeBrowse += HandleOnBeforeBrowse;
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

        private void HandleOnBeforeBrowse(object sender, CfxOnBeforeBrowseEventArgs e)
        {
            if (UrlFilters == null)
            {
                e.SetReturnValue(false);
                return;
            }

            switch (BrowserSettings.UrlFilterMode)
            {
                case UrlFilterMode.None:
                    e.SetReturnValue(false);
                    return;
                case UrlFilterMode.BlacklistMatch:
                    e.SetReturnValue(UrlFilters.Any(f => e.Browser.MainFrame.Url.Equals(f, StringComparison.InvariantCultureIgnoreCase)));
                    return;
                case UrlFilterMode.WhitelistMatch:
                    e.SetReturnValue(!UrlFilters.Any(f => e.Browser.MainFrame.Url.Equals(f, StringComparison.InvariantCultureIgnoreCase)));
                    return;
                case UrlFilterMode.BlacklistContains:
                    e.SetReturnValue(UrlFilters.Any(f => e.Browser.MainFrame.Url.Contains(f, StringComparison.InvariantCultureIgnoreCase)));
                    return;
                case UrlFilterMode.WhitelistContains:
                    e.SetReturnValue(!UrlFilters.Any(f => e.Browser.MainFrame.Url.Contains(f, StringComparison.InvariantCultureIgnoreCase)));
                    return;
                case UrlFilterMode.BlacklistRegex:
                    var blregex = UrlFilters.Any(f => Regex.IsMatch(e.Browser.MainFrame.Url, f, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline));
                    e.SetReturnValue(blregex);
                    return;
                case UrlFilterMode.WhitelistRegex:
                    var wlregex = UrlFilters.Any(f => Regex.IsMatch(e.Browser.MainFrame.Url, f, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline));
                    e.SetReturnValue(!wlregex);
                    return;
            }

            //e.SetReturnValue(false);
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

        private void HandleConsoleMessage(object sender, CfxOnConsoleMessageEventArgs e)
        {
            LastConsole = $"{e.Message} ({e.Source}: {e.Line})";
            if (BrowserSettings.ListenConsole)
                VvvvLogger?.Log(LogType.Message, $"CEF: {e.Message} ({e.Source}: {e.Line})");
            e.SetReturnValue(false);
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
            _invalidateAccFrame = e.SharedHandle != _newAccFramePtr;
            _newAccFramePtr = e.SharedHandle;
        }

        public void OnLoadEnd()
        {
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
                if (!TextureSettings.AutoWidth && !TextureSettings.AutoHeight) return;
                UpdateSize();
            };

            BindObject(OnLoadBinding);
            foreach (var code in Globals.ScriptsFromOnloadFolder.Values)
                ExecuteJavascript(code);
        }
    }
}
