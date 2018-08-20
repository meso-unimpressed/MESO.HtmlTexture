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
        Tags = "Modular",
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

    public abstract class MesoHtmlTextureNode : HtmlTextureInputOutputNode, IPluginEvaluate
    {
        private bool _init;

        public void Evaluate(int SpreadMax)
        {
            if (!_init)
            {
                FWrapperOutput.SliceCount = 0;
            }

            int slc = SliceCount();
            FWrapperOutput.Resize(slc, CreateWrapper, wrapper => wrapper.Dispose());
            SetOutputsSliceCount(slc);

            // Going backwards to avoid automatic slice duplication
            for (int i = slc - 1; i >= 0; i--)
            {
                var wrapper = FWrapperOutput[i];
                if (wrapper == null)
                {
                    wrapper = CreateWrapper(i);
                    FWrapperOutput[i] = wrapper;
                }
                else if (FWrapperOutput.Count(w => w != null && w.BrowserId == wrapper.BrowserId) > 1)
                {
                    wrapper = CreateWrapper(i);
                    FWrapperOutput[i] = wrapper;
                }
                UpdateWrapper(wrapper, i);
                wrapper.Mainloop(0);
                FillOuptuts(wrapper, i);
            }

            FWrapperOutput.Stream.IsChanged = true;
            _init = true;
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
