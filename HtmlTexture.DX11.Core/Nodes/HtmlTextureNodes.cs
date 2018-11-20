using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using mp.pddn;
using VVVV.Core.Logging;
using VVVV.DX11;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.IO;
using VVVV.Utils.VMath;

namespace HtmlTexture.DX11.Nodes
{
    public abstract class MesoHtmlTextureNode : HtmlTextureInputOutputNode, IPluginEvaluate, IPluginAwareOfEvaluation
    {
        private bool _init;
        protected bool Evaluating = true;

        public void Evaluate(int SpreadMax)
        {
            if(!Evaluating) return;
            if (!_init)
            {
                WrapperOut.SliceCount = 0;
            }

            int slc = SliceCount();
            WrapperOut.Resize(slc, CreateWrapper, wrapper => wrapper.Dispose());
            SetOutputsSliceCount(slc);

            // Going backwards to avoid automatic slice duplication
            for (int i = slc - 1; i >= 0; i--)
            {
                if ((WrapperOut[i]?.Closed ?? false) && EnabledIn[i])
                {
                    WrapperOut[i]?.Dispose();
                    WrapperOut[i] = null;
                }
                var wrapper = WrapperOut[i];
                if (wrapper == null)
                {
                    wrapper = CreateWrapper(i);
                    WrapperOut[i] = wrapper;
                }
                else if (WrapperOut.Count(w => w != null && w.BrowserId == wrapper.BrowserId) > 1)
                {
                    wrapper = CreateWrapper(i);
                    WrapperOut[i] = wrapper;
                }
                if (wrapper == null) continue;
                if (!EnabledIn[i] && wrapper.Closed) continue;
                if ((!EnabledIn[i] || HardLoadIn[i]) && !wrapper.Closed)
                {
                    wrapper.Close();
                    continue;
                }
                UpdateWrapper(wrapper, i);
                wrapper?.Mainloop(0);
                FillOuptuts(wrapper, i);
            }

            WrapperOut.Stream.IsChanged = true;
            _init = true;
        }

        public void TurnOn()
        {
            for (int i = 0; i < WrapperOut.SliceCount; i++)
            {
                WrapperOut[i]?.Dispose();
                WrapperOut[i] = null;
            }
            Evaluating = true;
        }

        public void TurnOff()
        {
            for (int i = 0; i < WrapperOut.SliceCount; i++)
            {
                WrapperOut[i].Close();
            }
            Evaluating = false;
        }
    }

    [PluginInfo(
        Name = "HtmlTexture",
        Category = "DX11.Texture2D",
        Version = "Url",
        Tags = "Vanadium",
        Author = "MESO, microdee, Gumilastik, Tonfilm",
        Help = "Advanced version of HTMLTexture with more bells and whistles"
    )]
    public class UrlMesoHtmlTextureNode : MesoHtmlTextureNode
    {
        [Input("Url", DefaultString = "about:blank")]
        public IDiffSpread<string> FUrl;

        protected override void LoadContent(HtmlTextureWrapper wrapper, int i)
        {
            wrapper.LoadUrl(FUrl[i]);
        }

        protected override int SliceCount()
        {
            return FUrl.SliceCount > 0 ? Math.Max(base.SliceCount(), FUrl.SliceCount) : 0;
        }
    }

    [PluginInfo(
        Name = "HtmlTexture",
        Category = "DX11.Texture2D",
        Version = "String",
        Tags = "Vanadium",
        Author = "MESO, microdee, Gumilastik, Tonfilm",
        Help = "Advanced version of HTMLTexture with more bells and whistles"
    )]
    public class StringMesoHtmlTextureNode : MesoHtmlTextureNode
    {
        [Input("Content", DefaultString = @"<html><head></head><body bgcolor=""#0000ff""></body></html>")]
        public IDiffSpread<string> FContent;
        [Input("Dummy Url", DefaultString = "about:blank")]
        public IDiffSpread<string> FUrl;

        protected override void LoadContent(HtmlTextureWrapper wrapper, int i)
        {
            wrapper.LoadString(FContent[i], FUrl[i]);
        }

        protected override int SliceCount()
        {
            var cslc = SpreadUtils.SpreadMax(FContent, FUrl);
            return cslc > 0 ? Math.Max(base.SliceCount(), cslc) : 0;
        }
    }

    [PluginInfo(
        Name = "SplitWrapper",
        Category = "HtmlTexture",
        Tags = "Never, Use, This",
        Author = "MESO, microdee",
        Help = "NEVER EVER USE THIS!"
    )]
    public class HtmlWrapperSplitNode : ObjectSplitNode<HtmlTextureWrapper>
    {
        [ImportingConstructor]
        public HtmlWrapperSplitNode()
        {
            MemberBlackList = new StringCollection { "RawImage" };
        }
    }
}
