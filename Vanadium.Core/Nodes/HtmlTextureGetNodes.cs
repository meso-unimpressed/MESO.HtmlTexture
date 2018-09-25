using System.Collections.Specialized;
using System.ComponentModel.Composition;
using mp.pddn;
using VVVV.PluginInterfaces.V2;
using VVVV.Vanadium.Core;

namespace VVVV.Vanadium.Nodes
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
