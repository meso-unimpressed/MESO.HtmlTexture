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
    [PluginInfo(
        Name = "HtmlTexture",
        Category = "DX11.Texture2D",
        Version = "Url",
        Tags = "Modular",
        Author = "MESO, microdee, Gumilastik, Tonfilm"
    )]
    public class UrlMesoHtmlTextureNode : MesoHtmlTextureNode
    {
        [Input("Url", DefaultString = "https://html5test.com")]
        public IDiffSpread<string> FUrl;

        protected override void LoadContent(HtmlTextureWrapper wrapper, int i)
        {
            wrapper.LoadUrl(FUrl[i]);
        }
    }

    [PluginInfo(
        Name = "HtmlTexture",
        Category = "DX11.Texture2D",
        Version = "String",
        Tags = "Modular",
        Author = "MESO, microdee, Gumilastik, Tonfilm"
    )]
    public class StringMesoHtmlTextureNode : MesoHtmlTextureNode
    {
        [Input("Content", DefaultString = @"<html><head></head><body bgcolor=""#0000ff""></body></html>")]
        public IDiffSpread<string> FContent;

        protected override void LoadContent(HtmlTextureWrapper wrapper, int i)
        {
            wrapper.LoadString(FContent[i]);
        }
    }

    public abstract class MesoHtmlTextureNode : IPluginEvaluate, IDX11ResourceHost, IDisposable
    {
        //[Import]
        //public IPluginHost2 Host;

        [Import]
        public ILogger Logger;

        [Config("Framerate", DefaultValue = 60)]
        public ISpread<int> FFps;

        [Input("Operations")]
        public IDiffSpread<ISpread<HtmlTextureOperation>> FOperations;

        [Input("Load", IsBang = true)]
        public ISpread<bool> FLoad;

        [Input("Reload Current", IsBang = true)]
        public ISpread<bool> FReloadIn;

        [Input("Size", DefaultValues = new double[] { HtmlTextureWrapper.DefaultWidth, HtmlTextureWrapper.DefaultHeight })]
        public IDiffSpread<Vector2D> FSize;

        [Input("Auto Size")]
        public IDiffSpread<bool> FAutoSize;

        [Input("Allow Popup Takeover")]
        public IDiffSpread<bool> FPopupIn;

        [Input("Filter Url")]
        public IDiffSpread<ISpread<string>> FFilterUrlIn;

        [Input("Zoom")]
        public IDiffSpread<double> FZoomLevelIn;

        [Input("Mouse")]
        public ISpread<Mouse> FMouseIn;

        [Input("Invert Vertical Scrolling", Visibility = PinVisibility.OnlyInspector)]
        public ISpread<bool> FInvVScroll;
        [Input("Invert Horizontal Scrolling", Visibility = PinVisibility.OnlyInspector)]
        public ISpread<bool> FInvHScroll;

        [Input("Keyboard")]
        public ISpread<Keyboard> FKeyboardIn;

        [Input("Show DevTools", IsBang = true)]
        public IDiffSpread<bool> FShowDevToolsIn;

        [Input("Use LivePage")]
        public IDiffSpread<bool> FLivePageIn;

        [Input("User-Agent")]
        public ISpread<string> FUserAgentIn;

        [Input("Log to Console")]
        public IDiffSpread<bool> FConsoleIn;

        [Input("Enabled", DefaultBoolean = true)]
        public ISpread<bool> FEnabledIn;

        [Output("Texture Output")]
        public Pin<DX11Resource<DX11DynamicTexture2D>> FTextureOutput;

        [Output("Wrapper Output")]
        public Pin<HtmlTextureWrapper> FWrapperOutput;

        [Output("Document Size")]
        public ISpread<Vector2D> FDocSizeOut;

        [Output("Is Loading")]
        public ISpread<bool> FIsLoadingOut;

        [Output("Current Url")]
        public ISpread<string> FCurrentUrlOut;

        [Output("Error Text")]
        public ISpread<string> FErrorTextOut;

        private bool _init;
        private bool _browserReady;

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
                    AutoSize = FAutoSize[i]
                },
                BrowserSettings = new HtmlTextureWrapper.WrapperBrowserSettings
                {
                    UseLivePage = FLivePageIn[i],
                    AllowPopups = FPopupIn[i],
                    UserAgent = FUserAgentIn[i],
                    ListenConsole = FConsoleIn[i],
                    ZoomLevel = FZoomLevelIn[i],
                    InvertScrollWheel = FInvVScroll[i],
                    InvertHorizontalScrollWheel = FInvHScroll[i]
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

        public void Evaluate(int SpreadMax)
        {
            if (!_init)
            {
                FWrapperOutput.SliceCount = 0;
            }
            FWrapperOutput.Resize(SpreadMax, CreateWrapper, wrapper => wrapper.Dispose());
            FTextureOutput.SliceCount = FDocSizeOut.SliceCount = FIsLoadingOut.SliceCount =
                FCurrentUrlOut.SliceCount = FErrorTextOut.SliceCount = SpreadMax;

            // Going backwards to avoid automatic slice duplication
            for (int i = SpreadMax - 1; i >= 0; i--)
            {
                var wrapper = FWrapperOutput[i];
                if (wrapper == null)
                {
                    wrapper = CreateWrapper(i);
                    FWrapperOutput[i] = wrapper;
                }
                else if (FWrapperOutput.Count(w => w.BrowserId == wrapper.BrowserId) > 1)
                {
                    wrapper = CreateWrapper(i);
                    FWrapperOutput[i] = wrapper;
                }

                wrapper.Enabled = FEnabledIn[i];
                if (FLoad[i]) LoadContent(wrapper, i);
                if (FReloadIn[i]) wrapper.Reload();
                if (FShowDevToolsIn[i]) wrapper.ShowDevTool();

                wrapper.BrowserSettings = new HtmlTextureWrapper.WrapperBrowserSettings
                {
                    UseLivePage = FLivePageIn[i],
                    AllowPopups = FPopupIn[i],
                    UserAgent = FUserAgentIn[i],
                    ListenConsole = FConsoleIn[i],
                    ZoomLevel = FZoomLevelIn[i],
                    InvertScrollWheel = FInvVScroll[i],
                    InvertHorizontalScrollWheel = FInvHScroll[i]
                };
                wrapper.TextureSettings = new HtmlTextureWrapper.WrapperTextureSettings
                {
                    TargetSize = ((int) FSize[i].x, (int) FSize[i].y),
                    AutoSize = FAutoSize[i]
                };

                wrapper.Mouse = FMouseIn[i];
                wrapper.Keyboard = FKeyboardIn[i];
                wrapper.Operations = FOperations[i];

                if (FFilterUrlIn.IsChanged)
                {
                    wrapper.UrlFilter.Clear();
                    wrapper.UrlFilter.AddRange(FFilterUrlIn[i]);
                }

                wrapper.Mainloop(0);

                FTextureOutput[i] = wrapper.DX11Texture;
                FDocSizeOut[i] = new Vector2D(wrapper.DocumentSize.w, wrapper.DocumentSize.h);
                FIsLoadingOut[i] = !wrapper.IsDocumentReady;
                FCurrentUrlOut[i] = wrapper.CurrentUrl;
                FErrorTextOut[i] = wrapper.LastError;
            }
            _init = true;
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
