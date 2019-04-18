using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fasterflect;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using mp.pddn;
using VVVV.Core.Logging;
using VVVV.DX11;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.IO;
using VVVV.Utils.VMath;

namespace HtmlTexture.DX11.Nodes
{
    public abstract class AbstractHtmlTextureInputsNode
    {
        [Import]
        public ILogger Logger;

        [Config("Framerate", DefaultValue = 60)]
        public ISpread<int> FpsIn;

        [Input(
            "VVVV Requests Frame",
            DefaultBoolean = false,
            Visibility = PinVisibility.OnlyInspector,
            Order = -11
        )]
        public ISpread<bool> VvvvRequFrameIn;

        [Input(
            "Allow Requesting Frames",
            DefaultBoolean = true,
            Visibility = PinVisibility.OnlyInspector,
            Order = -10
        )]
        public ISpread<bool> AllowDrawIn;

        [Input("Load", IsBang = true)]
        public ISpread<bool> LoadIn;

        [Input(
            "Hard Load",
            IsBang = true,
            Visibility = PinVisibility.Hidden
        )]
        public ISpread<bool> HardLoadIn;

        [Input("Reload Current", IsBang = true)]
        public ISpread<bool> ReloadIn;

        [Input(
            "Size",
            DefaultValues = new double[]
            {
                HtmlTextureWrapper.DefaultWidth,
                HtmlTextureWrapper.DefaultHeight
            }
        )]
        public IDiffSpread<Vector2D> SizeIn;

        [Input(
            "Use Element As Document Size",
            DefaultString = "body",
            Visibility = PinVisibility.OnlyInspector
        )]
        public IDiffSpread<string> DocSizeBaseSelectorIn;

        [Input("Auto Width")]
        public IDiffSpread<bool> AutoWidthIn;
        [Input("Auto Height")]
        public IDiffSpread<bool> AutoHeightIn;
        [Input("Extra Size", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<Vector2D> ExtraSizeIn;

        [Input("Allow Popup Takeover")]
        public IDiffSpread<bool> PopupIn;

        [Input(
            "Allow Cookies",
            Visibility = PinVisibility.Hidden,
            DefaultBoolean = true
        )]
        public IDiffSpread<bool> AllowCookiesIn;

        [Input(
            "Allow Get Cookies",
            Visibility = PinVisibility.OnlyInspector,
            DefaultBoolean = true
        )]
        public IDiffSpread<bool> AllowGetCookiesIn;

        [Input(
            "Allow Set Cookies",
            Visibility = PinVisibility.OnlyInspector,
            DefaultBoolean = true
        )]
        public IDiffSpread<bool> AllowSetCookiesIn;

        [Input(
            "Filter Url",
            Visibility = PinVisibility.OnlyInspector,
            BinVisibility = PinVisibility.OnlyInspector
        )]
        public IDiffSpread<ISpread<string>> FilterUrlIn;

        [Input("Filter Mode", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<UrlFilterMode> FilterModeIn;

        [Input("Zoom")]
        public IDiffSpread<double> ZoomLevelIn;

        [Input("Mouse")]
        public ISpread<Mouse> MouseIn;

        [Input("Invert Vertical Scrolling", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<bool> InvVScrollIn;
        [Input("Invert Horizontal Scrolling", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<bool> InvHScrollIn;

        [Input("Keyboard")]
        public ISpread<Keyboard> KeyboardIn;

        [Input("Show DevTools", IsBang = true)]
        public IDiffSpread<bool> ShowDevToolsIn;

        [Input("Use LivePage")]
        public IDiffSpread<bool> LivePageIn;

        [Input("User-Agent")]
        public IDiffSpread<string> UserAgentIn;

        [Input("Mute Audio")]
        public IDiffSpread<bool> MuteAudioIn;

        [Input("Log to Console")]
        public IDiffSpread<bool> LogToConsoleIn;

        [Input("Operations", IsSingle = true)]
        public ISpread<HtmlTextureOperationHost> OperationsIn;

        [Input(
            "No MouseMove on First Touch",
            Visibility = PinVisibility.OnlyInspector
        )]
        public IDiffSpread<bool> NoMouseMoveOnFirstTouchIn;

        [Input("Enabled", DefaultBoolean = true)]
        public ISpread<bool> EnabledIn;

        [Input(
            "Allow Initialization",
            Visibility = PinVisibility.OnlyInspector,
            DefaultBoolean = true
        )]
        public IDiffSpread<bool> ManInit;

        protected bool CanCreate;

        protected virtual int SliceCount()
        {
            return SpreadUtils.SpreadMax(OperationsIn, LoadIn, HardLoadIn, ReloadIn, SizeIn,
                DocSizeBaseSelectorIn, AutoWidthIn, AutoHeightIn, PopupIn, FilterUrlIn,
                ZoomLevelIn, MouseIn, KeyboardIn, ShowDevToolsIn,
                LivePageIn, UserAgentIn, LogToConsoleIn, NoMouseMoveOnFirstTouchIn, MuteAudioIn, EnabledIn);
        }

        protected abstract void LoadContent(HtmlTextureWrapper wrapper, int i);

        protected virtual HtmlTextureWrapper CreateWrapper(int i)
        {
            if (!CanCreate) return null;
            var wrapper = new HtmlTextureWrapper(new HtmlTextureWrapper.WrapperInitSettings
            {
                Fps = FpsIn[0],
                ParentHandle = 0,
                FrameRequestFromVvvv = VvvvRequFrameIn[0]
            })
            {
                TextureSettings = new HtmlTextureWrapper.WrapperTextureSettings
                {
                    TargetSize = ((int)SizeIn[i].x, (int)SizeIn[i].y),
                    ExtraSize = ((int)ExtraSizeIn[i].x, (int)ExtraSizeIn[i].y),
                    AutoWidth = AutoWidthIn[i],
                    AutoHeight = AutoHeightIn[i]
                },
                BrowserSettings = new HtmlTextureWrapper.WrapperBrowserSettings
                {
                    UseLivePage = LivePageIn[i],
                    AllowPopups = PopupIn[i],
                    UserAgent = UserAgentIn[i],
                    ListenConsole = LogToConsoleIn[i],
                    ZoomLevel = ZoomLevelIn[i],
                    InvertScrollWheel = InvVScrollIn[i],
                    InvertHorizontalScrollWheel = InvHScrollIn[i],
                    UrlFilterMode = FilterModeIn[i],
                    DocumentSizeElementSelector = DocSizeBaseSelectorIn[i],
                    AllowGetCookies = AllowCookiesIn[i] && AllowGetCookiesIn[i],
                    AllowSetCookies = AllowCookiesIn[i] && AllowSetCookiesIn[i],
                    NoMouseMoveOnFirstTouch = NoMouseMoveOnFirstTouchIn[i],
                    MuteAudio = MuteAudioIn[i]
                },
                VvvvLogger = Logger,
                Operations = new HtmlTextureOperationHost(),
                UrlFilters = FilterUrlIn[i],
                Enabled = EnabledIn[i]
            };

            var ops = OperationsIn.TryGetSlice(0);
            ops?.ExtractOperationHost(wrapper.Operations, i);
            
            wrapper.OnBrowserReady += (sender, e) => LoadContent(wrapper, i);

            wrapper.Initialize();
            return wrapper;
        }

        protected virtual void UpdateWrapper(HtmlTextureWrapper wrapper, int i)
        {
            if (wrapper == null) return;
            wrapper.Enabled = EnabledIn[i];
            if (LoadIn[i]) LoadContent(wrapper, i);
            if (ReloadIn[i]) wrapper.Reload();
            if (ShowDevToolsIn[i]) wrapper.ShowDevTool();

            if (SpreadUtils.AnyChanged(LivePageIn, PopupIn, UserAgentIn,
                LogToConsoleIn, ZoomLevelIn, InvVScrollIn,
                InvHScrollIn, DocSizeBaseSelectorIn, FilterModeIn,
                NoMouseMoveOnFirstTouchIn, MuteAudioIn))
            {
                wrapper.BrowserSettings = new HtmlTextureWrapper.WrapperBrowserSettings
                {
                    UseLivePage = LivePageIn[i],
                    AllowPopups = PopupIn[i],
                    UserAgent = UserAgentIn[i],
                    ListenConsole = LogToConsoleIn[i],
                    ZoomLevel = ZoomLevelIn[i],
                    InvertScrollWheel = InvVScrollIn[i],
                    InvertHorizontalScrollWheel = InvHScrollIn[i],
                    UrlFilterMode = FilterModeIn[i],
                    DocumentSizeElementSelector = DocSizeBaseSelectorIn[i],
                    AllowGetCookies = AllowCookiesIn[i] && AllowGetCookiesIn[i],
                    AllowSetCookies = AllowCookiesIn[i] && AllowSetCookiesIn[i],
                    NoMouseMoveOnFirstTouch = NoMouseMoveOnFirstTouchIn[i],
                    MuteAudio = MuteAudioIn[i]
                };
            }

            if (SpreadUtils.AnyChanged(SizeIn, ExtraSizeIn, AutoWidthIn, AutoHeightIn))
            {
                wrapper.TextureSettings = new HtmlTextureWrapper.WrapperTextureSettings
                {
                    TargetSize = ((int)SizeIn[i].x, (int)SizeIn[i].y),
                    ExtraSize = ((int)ExtraSizeIn[i].x, (int)ExtraSizeIn[i].y),
                    AutoWidth = AutoWidthIn[i],
                    AutoHeight = AutoHeightIn[i]
                };
            }

            wrapper.AllowFrameRequest = AllowDrawIn[i];
            wrapper.Mouse = MouseIn[i];
            wrapper.Keyboard = KeyboardIn[i];

            if(wrapper.Operations == null) wrapper.Operations = new HtmlTextureOperationHost();
            var ops = OperationsIn.TryGetSlice(0);
            ops?.ExtractOperationHost(wrapper.Operations, i);

            if (FilterUrlIn.IsChanged)
                wrapper.UrlFilters = FilterUrlIn[i];
        }
    }

    public abstract class HtmlTextureInputOutputNode : AbstractHtmlTextureInputsNode, IDX11ResourceHost, IDisposable
    {
        [Output("Texture Output")]
        public Pin<DX11Resource<DX11Texture2D>> TextureOut;

        [Output("Wrapper Output")]
        public Pin<HtmlTextureWrapper> WrapperOut;

        [Output("Document Size")]
        public ISpread<Vector2D> DocSizeOut;

        [Output("Document Ready")]
        public ISpread<bool> DocReadyOut;
        [Output("Loading")]
        public ISpread<bool> IsLoadingOut;

        [Output("On Created", IsBang = true)]
        public ISpread<bool> CreatedOut;
        [Output("On Loaded", IsBang = true)]
        public ISpread<bool> LoadedOut;
        [Output("Loading Progress")]
        public ISpread<double> LoadingProgressOut;

        [Input("Has Audio")]
        public IDiffSpread<bool> HasAudioOut;

        [Output("Current Url")]
        public ISpread<string> CurrentUrlOut;

        [Output("Texture Valid")]
        public ISpread<bool> TextureValidOut;

        [Output("Error Text")]
        public ISpread<string> ErrorTextOut;

        [Output("Last Js Log")]
        public ISpread<string> JsLogOut;

        protected virtual void SetOutputsSliceCount(int slc)
        {
            TextureOut.SliceCount = WrapperOut.SliceCount = DocSizeOut.SliceCount = IsLoadingOut.SliceCount =
                JsLogOut.SliceCount = CurrentUrlOut.SliceCount = ErrorTextOut.SliceCount = TextureValidOut.SliceCount =
                    LoadedOut.SliceCount = CreatedOut.SliceCount = DocReadyOut.SliceCount = LoadingProgressOut.SliceCount =
                        HasAudioOut.SliceCount = slc;
        }

        protected void FillOuptuts(HtmlTextureWrapper wrapper, int i)
        {
            if (wrapper == null) return;
            TextureOut[i] = wrapper.DX11Texture;
            DocSizeOut[i] = new Vector2D(wrapper.DocumentSize.w, wrapper.DocumentSize.h);
            IsLoadingOut[i] = wrapper.Loading;
            DocReadyOut[i] = wrapper.IsDocumentReady;
            CurrentUrlOut[i] = wrapper.CurrentUrl;
            ErrorTextOut[i] = wrapper.LastError;
            LoadedOut[i] = wrapper.LoadedFrame;
            CreatedOut[i] = wrapper.CreatedFrame;
            TextureValidOut[i] = wrapper.IsTextureValid;
            JsLogOut[i] = wrapper.LastConsole;
            LoadingProgressOut[i] = wrapper.Progress;
            HasAudioOut[i] = wrapper.HasAudio;
        }

        public void Update(DX11RenderContext context)
        {
            CanCreate = TextureOut.IsConnected && (ManInit.SliceCount > 0 && ManInit[0]);
            for (int i = 0; i < WrapperOut.SliceCount; i++)
            {
                var wrapper = WrapperOut[i];
                wrapper?.UpdateDX11Resources(context);
            }
        }

        public void Destroy(DX11RenderContext context, bool force)
        {
            for (int i = 0; i < WrapperOut.SliceCount; i++)
            {
                var wrapper = WrapperOut[i];
                wrapper?.DestroyDX11Resources(context, force);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < WrapperOut.SliceCount; i++)
            {
                var wrapper = WrapperOut[i];
                wrapper?.Dispose();
            }
        }
    }
}
