using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace HtmlTexture.DX11.Nodes
{
    [PluginInfo(
        Name = "Scroll",
        Category = "HtmlTexture.Operation",
        Tags = "Vanadium",
        Author = "MESO, microdee",
        Help = "Scroll on any element in a HTML document"
    )]
    public class ScrollOperationNode : PersistentOperationNode<ScrollOperation>
    {
        [Input("Scroll", Order = 10, BinOrder = 11)]
        public IDiffSpread<Vector2D> FScroll;
        [Input("Normalized", Order = 12, BinOrder = 13)]
        public IDiffSpread<bool> FNorm;
        [Input("Element Selector", Order = 14, BinOrder = 15)]
        public IDiffSpread<string> FElement;

        protected override int SliceCount()
        {
            return SpreadUtils.SpreadMax(FScroll, FElement, FNorm);
        }

        protected override void UpdateOps(ref ScrollOperation ops, int i)
        {
            ops.ElementSelector = FElement[i];
            ops.ScrollPos = FScroll[i];
            ops.Normalized = FNorm[i];
            ops.Execute = SpreadUtils.AnyChanged(FScroll, FNorm, FElement);
        }
    }
}
