using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mp.pddn;
using Vanadium.Core;
using VVVV.PluginInterfaces.V2;

namespace Vanadium.Nodes
{
    [PluginInfo(
        Name = "DOM",
        Category = "HtmlTexture",
        Tags = "HTML, XElement",
        Author = "MESO, microdee",
        Help = "Get the DOM xml from HtmlTexture"
    )]
    public class GetDomNode : ObjectSplitNode<HtmlTextureWrapper>
    {
        [Input("Update DOM", IsBang = true, Order = 100)]
        public IDiffSpread<bool> FUpdate;

        [ImportingConstructor]
        public GetDomNode()
        {
            MemberWhiteList = new StringCollection
            {
                "RootElement",
                "Dom"
            };
        }

        public override void OnEvaluateBegin()
        {
            for (int i = 0; i < FInput.SliceCount; i++)
            {
                if(FUpdate[i]) FInput[i].UpdateDom();
            }
        }
    }

    [PluginInfo(
        Name = "BrowserId",
        Category = "HtmlTexture",
        Tags = "HTML, XElement",
        Author = "MESO, microdee",
        Help = "Get the DOM xml from HtmlTexture"
    )]
    public class GetBrowserIdNode : ObjectSplitNode<HtmlTextureWrapper>
    {
        [ImportingConstructor]
        public GetBrowserIdNode()
        {
            UseObjectCache = false;
            MemberWhiteList = new StringCollection
            {
                "BrowserId"
            };
        }
    }
}
