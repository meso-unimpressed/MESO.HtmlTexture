using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V2;

namespace HtmlTexture.DX11.Nodes
{
    [PluginInfo(
        Name = "Navigate",
        Category = "HtmlTexture.Operation",
        Tags = "Vanadium",
        Author = "MESO, microdee",
        Help = "Go back or forward in browser history"
    )]
    public class NavigationOperationNodes : PersistentOperationNode<NavigationOperation>
    {
        [Input("Back", Order = 10, IsBang = true)]
        public ISpread<bool> FBack;
        [Input("Forward", Order = 12, IsBang = true)]
        public ISpread<bool> FForw;

        protected override int SliceCount(int i) => 1;

        protected override int BinCount()
        {
            return SpreadUtils.SpreadMax(FBack, FForw);
        }

        protected override void UpdateOps(ref NavigationOperation ops, int i, int j)
        {
            ops.Backward = FBack[i];
            ops.Forward = FForw[i];
            ops.Execute = ops.Backward || ops.Forward;
        }
    }
}
