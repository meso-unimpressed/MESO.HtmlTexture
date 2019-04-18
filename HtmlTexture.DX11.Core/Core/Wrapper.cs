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
using System.Numerics;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using md.stdl.Coding;
using Notui;
using VVVV.Core.Logging;
using VVVV.DX11;
using VVVV.DX11.Lib.Effects.Pins.RenderSemantics;
using VVVV.Utils.IO;
using VVVV.Utils.VMath;

namespace VVVV.HtmlTexture.DX11.Core
{
    public partial class HtmlTextureWrapper : IMainlooping, IDisposable
    {
        public static readonly Dictionary<int, HtmlTextureWrapper> Instances = new Dictionary<int, HtmlTextureWrapper>();
        public static readonly (int w, int h) DefaultSize = (3840, 2160);

        public const int DefaultWidth = 3840;
        public const int DefaultHeight = 2160;

        //private readonly object _lockingObject = new object();

        private bool _browserReadyFrame = false;
        private bool _loadedFrame = false;
        private bool _invalidateAccFrame = false;
        private IntPtr _newAccFramePtr;
        private readonly Dictionary<DX11RenderContext, bool> _newAccFrameReady = new Dictionary<DX11RenderContext, bool>();

        private string _customContent = "";
        private string _customContentUrl = "";
        private string _prevUrl = "";

        private WrapperTextureSettings _prevTextureSettings;
        private WrapperBrowserSettings _prevBrowserSettings;

        private Subscription<Mouse, MouseNotification> _mouseSubscription;
        private Subscription<Keyboard, KeyNotification> _keyboardSubscription;
        private Keyboard _keyboard;
        private readonly Dictionary<int, CfxTouchEvent> _touches = new Dictionary<int, CfxTouchEvent>();
        private readonly Dictionary<int, HtmlAudioStream> _audioStreams = new Dictionary<int, HtmlAudioStream>();

        public bool Enabled { get; set; }
        public bool AllowFrameRequest { get; set; } = false;
        public IEnumerable<string> UrlFilters { get; set; }
        public ILogger VvvvLogger { get; set; }
        public HtmlTextureOperationHost Operations { get; set; }
        public WrapperBrowserSettings BrowserSettings { get; set; }
        public WrapperTextureSettings TextureSettings { get; set; }
        public WrapperInitSettings InitSettings { get; }

        public CfxBrowserSettings Settings { get; private set; }

        public JsBinding OnLoadBinding { get; private set; }
        public ResizeNotificationFunction ResizeNotification { get; private set; }
        public DocSizeBaseSelector DocumentSizeBaseSelector { get; private set; }
        public Dictionary<string, JsBinding> JsBindings { get; } = new Dictionary<string, JsBinding>();
        public Dictionary<HtmlTextureOperation, object> OperationResults { get; } = new Dictionary<HtmlTextureOperation, object>();
        public Dictionary<int, HtmlTextureTouch> SubmittedTouches { get; } = new Dictionary<int, HtmlTextureTouch>();

        public IEnumerable<HtmlAudioStream> AudioStreams => _audioStreams.Values;

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
        public CfxJsDialogHandler JsDialogHandler { get; private set; }

        // TODO: Manage Downloads
        //public CfxDownloadHandler DownloadHandler { get; private set; }
        public CfxAudioHandler AudioHandler { get; private set; }
        public CfrV8Handler V8Handler { get; private set; }

        public (int w, int h) DocumentSize { get; private set; }
        public (int w, int h) TextureSize { get; private set; }
        public int BrowserId { get; private set; }
        public bool IsDocumentReady { get; private set; }
        public bool Loading { get; private set; }
        public bool LoadedFrame { get; private set; }
        public bool CreatedFrame { get; private set; }
        public bool IsTextureValid { get; private set; }
        public bool LivePageActive { get; private set; }
        public double Progress { get; private set; }
        public bool Closed { get; private set; }
        public bool HasAudio { get; private set; }

        public string LastJsDialogText { get; private set; }
        public string LastError { get; private set; }
        public string LastConsole { get; private set; }
        public string CurrentUrl { get; private set; }

        public XElement RootElement { get; private set; }
        public XDocument Dom { get; private set; }

        public DX11Resource<DX11Texture2D> DX11Texture { get; } = new DX11Resource<DX11Texture2D>();

        public event EventHandler OnBrowserReady;
        public event EventHandler OnMainLoopBegin;
        public event EventHandler OnMainLoopEnd;

        public HtmlTextureWrapper(WrapperInitSettings initSettings)
        {
            InitSettings = initSettings;
        }

        public void LogError(string message)
        {
            VvvvLogger?.Log(LogType.Error, "CEF Error: " + message);
            LastError = message;
        }

        public void LogError(Exception e)
        { 
            VvvvLogger?.Log(LogType.Error, "CEF Error");
            VvvvLogger?.Log(e);
            LastError = $"CEF Error: {e.Message}\n{e.StackTrace}";
        }

        public void UpdateSize()
        {
            if(Closed) return;
            TextureSize =
            (
                (TextureSettings.AutoWidth ? DocumentSize.w : TextureSettings.TargetSize.w) + TextureSettings.ExtraSize.w,
                (TextureSettings.AutoHeight ? DocumentSize.h : TextureSettings.TargetSize.h) + TextureSettings.ExtraSize.h
            );

            Browser?.Host.WasResized();
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

        public void DestroyDX11Resources(DX11RenderContext context, bool force)
        {
            DX11Texture.Dispose(context);
        }

        public void Close()
        {
            if(Closed) return;
            Closed = true;
            
            Browser?.Host?.CloseBrowser(true);
            _mouseSubscription?.Dispose();
            _keyboardSubscription?.Dispose();

            Browser?.Dispose();
            RemoteBrowser?.Dispose();

            Visitor?.Dispose();
            Client?.Dispose();

            MouseEvent?.Dispose();
            ContextMenuHandler?.Dispose();
            LifeSpanHandler?.Dispose();
            LoadHandler?.Dispose();
            RenderHandler?.Dispose();
            RequestHandler?.Dispose();
            DisplayHandler?.Dispose();
            V8Handler?.Dispose();

            Settings?.Dispose();
            
        }

        public void Dispose()
        {
            if(Instances.ContainsKey(BrowserId))
                Instances.Remove(BrowserId);

            DX11Texture?.Dispose();
            Close();
        }

        ~HtmlTextureWrapper()
        {
            if (Instances.ContainsKey(BrowserId))
                Instances.Remove(BrowserId);
        }
    }
}
