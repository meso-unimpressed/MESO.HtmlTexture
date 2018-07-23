using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
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
        public ISpread<int> FFps;
        
        [Input("Load", IsBang = true)]
        public ISpread<bool> FLoad;

        [Input("Reload Current", IsBang = true)]
        public ISpread<bool> FReloadIn;

        [Input("Size", DefaultValues = new double[] { HtmlTextureWrapper.DefaultWidth, HtmlTextureWrapper.DefaultHeight })]
        public IDiffSpread<Vector2D> FSize;

        [Input("Use Element As Document Size", DefaultString = "body", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<string> FDocSizeBaseSelector;

        [Input("Auto Width")]
        public IDiffSpread<bool> FAutoWidth;
        [Input("Auto Height")]
        public IDiffSpread<bool> FAutoHeight;
        [Input("Extra Size", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<Vector2D> FExtraSize;

        [Input("Allow Popup Takeover")]
        public IDiffSpread<bool> FPopupIn;

        [Input("Filter Url")]
        public IDiffSpread<ISpread<string>> FFilterUrlIn;

        [Input("Zoom")]
        public IDiffSpread<double> FZoomLevelIn;

        [Input("Mouse")]
        public ISpread<Mouse> FMouseIn;

        [Input("Invert Vertical Scrolling", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<bool> FInvVScroll;
        [Input("Invert Horizontal Scrolling", Visibility = PinVisibility.OnlyInspector)]
        public IDiffSpread<bool> FInvHScroll;

        [Input("Keyboard")]
        public ISpread<Keyboard> FKeyboardIn;

        [Input("Show DevTools", IsBang = true)]
        public IDiffSpread<bool> FShowDevToolsIn;

        [Input("Use LivePage")]
        public IDiffSpread<bool> FLivePageIn;

        [Input("User-Agent")]
        public IDiffSpread<string> FUserAgentIn;

        [Input("Log to Console")]
        public IDiffSpread<bool> FConsoleIn;

        [Input("Operations", BinVisibility = PinVisibility.Hidden)]
        public ISpread<ISpread<HtmlTextureOperation>> FOperations;

        [Input("Enabled", DefaultBoolean = true)]
        public ISpread<bool> FEnabledIn;

        protected int SliceCount()
        {
            return SpreadUtils.SpreadMax(FOperations, FLoad, FReloadIn, FSize,
                FDocSizeBaseSelector, FAutoWidth, FAutoHeight, FPopupIn, FFilterUrlIn,
                FZoomLevelIn, FMouseIn, FKeyboardIn, FShowDevToolsIn,
                FLivePageIn, FUserAgentIn, FConsoleIn, FEnabledIn);
        }

        protected abstract void LoadContent(HtmlTextureWrapper wrapper, int i);

        protected virtual HtmlTextureWrapper CreateWrapper(int i)
        {
            var wrapper = new HtmlTextureWrapper(new HtmlTextureWrapper.WrapperInitSettings
            {
                Fps = FFps[0],
                ParentHandle = 0
            })
            {
                TextureSettings = new HtmlTextureWrapper.WrapperTextureSettings
                {
                    TargetSize = ((int)FSize[i].x, (int)FSize[i].y),
                    ExtraSize = ((int)FExtraSize[i].x, (int)FExtraSize[i].y),
                    AutoWidth = FAutoWidth[i],
                    AutoHeight = FAutoHeight[i]
                },
                BrowserSettings = new HtmlTextureWrapper.WrapperBrowserSettings
                {
                    UseLivePage = FLivePageIn[i],
                    AllowPopups = FPopupIn[i],
                    UserAgent = FUserAgentIn[i],
                    ListenConsole = FConsoleIn[i],
                    ZoomLevel = FZoomLevelIn[i],
                    InvertScrollWheel = FInvVScroll[i],
                    InvertHorizontalScrollWheel = FInvHScroll[i],
                    DocumentSizeElementSelector = FDocSizeBaseSelector[i]
                },
                VvvvLogger = Logger,
                Operations = FOperations[i],
                Enabled = FEnabledIn[i]
            };

            wrapper.UrlFilter.AddRange(FFilterUrlIn[i]);
            wrapper.OnBrowserReady += (sender, e) => LoadContent(wrapper, i);

            wrapper.Initialize();
            return wrapper;
        }

        protected virtual void UpdateWrapper(HtmlTextureWrapper wrapper, int i)
        {
            wrapper.Enabled = FEnabledIn[i];
            if (FLoad[i]) LoadContent(wrapper, i);
            if (FReloadIn[i]) wrapper.Reload();
            if (FShowDevToolsIn[i]) wrapper.ShowDevTool();

            if (SpreadUtils.AnyChanged(FLivePageIn, FPopupIn, FUserAgentIn, FConsoleIn, FZoomLevelIn, FInvVScroll,
                FInvHScroll, FDocSizeBaseSelector))
            {
                wrapper.BrowserSettings = new HtmlTextureWrapper.WrapperBrowserSettings
                {
                    UseLivePage = FLivePageIn[i],
                    AllowPopups = FPopupIn[i],
                    UserAgent = FUserAgentIn[i],
                    ListenConsole = FConsoleIn[i],
                    ZoomLevel = FZoomLevelIn[i],
                    InvertScrollWheel = FInvVScroll[i],
                    InvertHorizontalScrollWheel = FInvHScroll[i],
                    DocumentSizeElementSelector = FDocSizeBaseSelector[i]
                };
            }

            if (SpreadUtils.AnyChanged(FSize, FExtraSize, FAutoWidth, FAutoHeight))
            {
                wrapper.TextureSettings = new HtmlTextureWrapper.WrapperTextureSettings
                {
                    TargetSize = ((int)FSize[i].x, (int)FSize[i].y),
                    ExtraSize = ((int)FExtraSize[i].x, (int)FExtraSize[i].y),
                    AutoWidth = FAutoWidth[i],
                    AutoHeight = FAutoHeight[i]
                };
            }

            wrapper.Mouse = FMouseIn[i];
            wrapper.Keyboard = FKeyboardIn[i];
            wrapper.Operations = FOperations[i];

            if (FFilterUrlIn.IsChanged)
            {
                wrapper.UrlFilter.Clear();
                wrapper.UrlFilter.AddRange(FFilterUrlIn[i]);
            }
        }
    }

    public abstract class HtmlTextureInputOutputNode : AbstractHtmlTextureInputsNode, IDX11ResourceHost, IDisposable
    {
        [Output("Texture Output")]
        public Pin<DX11Resource<DX11DynamicTexture2D>> FTextureOutput;

        [Output("Wrapper Output")]
        public Pin<HtmlTextureWrapper> FWrapperOutput;

        [Output("Document Size")]
        public ISpread<Vector2D> FDocSizeOut;

        [Output("Document Ready")]
        public ISpread<bool> FDocReady;
        [Output("Loading")]
        public ISpread<bool> FIsLoadingOut;

        [Output("On Created", IsBang = true)]
        public ISpread<bool> FCreated;
        [Output("On Loaded", IsBang = true)]
        public ISpread<bool> FLoaded;

        [Output("Current Url")]
        public ISpread<string> FCurrentUrlOut;

        [Output("Error Text")]
        public ISpread<string> FErrorTextOut;

        [Output("Last Js Log")]
        public ISpread<string> FLog;

        protected void SetOutputsSliceCount(int slc)
        {
            FTextureOutput.SliceCount = FWrapperOutput.SliceCount = FDocSizeOut.SliceCount = FIsLoadingOut.SliceCount =
                FLog.SliceCount = FCurrentUrlOut.SliceCount = FErrorTextOut.SliceCount =
                    FLoaded.SliceCount = FCreated.SliceCount = FDocReady.SliceCount = slc;
        }

        protected void FillOuptuts(HtmlTextureWrapper wrapper, int i)
        {
            FTextureOutput[i] = wrapper.DX11Texture;
            FDocSizeOut[i] = new Vector2D(wrapper.DocumentSize.w, wrapper.DocumentSize.h);
            FIsLoadingOut[i] = wrapper.Loading;
            FDocReady[i] = wrapper.IsDocumentReady;
            FCurrentUrlOut[i] = wrapper.CurrentUrl;
            FErrorTextOut[i] = wrapper.LastError;
            FLoaded[i] = wrapper.LoadedFrame;
            FCreated[i] = wrapper.CreatedFrame;
            FLog[i] = wrapper.LastConsole;
        }

        public void Update(DX11RenderContext context)
        {
            for (int i = 0; i < FWrapperOutput.SliceCount; i++)
            {
                var wrapper = FWrapperOutput[i];
                wrapper?.UpdateDX11Resources(context);
            }
        }

        public void Destroy(DX11RenderContext context, bool force)
        {
            for (int i = 0; i < FWrapperOutput.SliceCount; i++)
            {
                var wrapper = FWrapperOutput[i];
                wrapper?.DestroyDX11Resources(context, force);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < FWrapperOutput.SliceCount; i++)
            {
                var wrapper = FWrapperOutput[i];
                wrapper?.Dispose();
            }
        }
    }

    public abstract class HtmlTextureOutputNode : IDX11ResourceHost, IDisposable
    {
        [Output("Texture Output")]
        public Pin<DX11Resource<DX11DynamicTexture2D>> FTextureOutput;

        [Output("Wrapper Output")]
        public Pin<HtmlTextureWrapper> FWrapperOutput;

        [Output("Document Size")]
        public ISpread<Vector2D> FDocSizeOut;

        [Output("Document Ready")]
        public ISpread<bool> FDocReady;
        [Output("Loading")]
        public ISpread<bool> FIsLoadingOut;

        [Output("On Created", IsBang = true)]
        public ISpread<bool> FCreated;
        [Output("On Loaded", IsBang = true)]
        public ISpread<bool> FLoaded;

        [Output("Current Url")]
        public ISpread<string> FCurrentUrlOut;

        [Output("Error Text")]
        public ISpread<string> FErrorTextOut;

        [Output("Last Js Log")]
        public ISpread<string> FLog;

        protected void SetOutputsSliceCount(int slc)
        {
            FTextureOutput.SliceCount = FWrapperOutput.SliceCount = FDocSizeOut.SliceCount = FIsLoadingOut.SliceCount =
                FLog.SliceCount = FCurrentUrlOut.SliceCount = FErrorTextOut.SliceCount =
                    FLoaded.SliceCount = FCreated.SliceCount = FDocReady.SliceCount = slc;
        }

        protected void FillOuptuts(HtmlTextureWrapper wrapper, int i)
        {
            FTextureOutput[i] = wrapper.DX11Texture;
            FDocSizeOut[i] = new Vector2D(wrapper.DocumentSize.w, wrapper.DocumentSize.h);
            FIsLoadingOut[i] = wrapper.Loading;
            FDocReady[i] = wrapper.IsDocumentReady;
            FCurrentUrlOut[i] = wrapper.CurrentUrl;
            FErrorTextOut[i] = wrapper.LastError;
            FLoaded[i] = wrapper.LoadedFrame;
            FCreated[i] = wrapper.CreatedFrame;
            FLog[i] = wrapper.LastConsole;
        }

        public void Update(DX11RenderContext context)
        {
            for (int i = 0; i < FWrapperOutput.SliceCount; i++)
            {
                var wrapper = FWrapperOutput[i];
                wrapper?.UpdateDX11Resources(context);
            }
        }

        public void Destroy(DX11RenderContext context, bool force)
        {
            for (int i = 0; i < FWrapperOutput.SliceCount; i++)
            {
                var wrapper = FWrapperOutput[i];
                wrapper?.DestroyDX11Resources(context, force);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < FWrapperOutput.SliceCount; i++)
            {
                var wrapper = FWrapperOutput[i];
                wrapper?.Dispose();
            }
        }
    }
}
