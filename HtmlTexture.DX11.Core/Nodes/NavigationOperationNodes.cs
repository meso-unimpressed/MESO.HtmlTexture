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
        Author = "MESO, microdee",
        Help = "Go back or forward in browser history"
    )]
    public class NavigationOperationNodes : PersistentOperationNode<NavigationOperation>
    {
        [Input("Back", Order = 10, BinOrder = 11, IsBang = true)]
        public ISpread<bool> FBack;
        [Input("Forward", Order = 12, BinOrder = 13, IsBang = true)]
        public ISpread<bool> FForw;

        protected override int SliceCount()
        {
            return SpreadUtils.SpreadMax(FBack, FForw);
        }

        protected override void UpdateOps(ref NavigationOperation ops, int i)
        {
            ops.Backward = FBack[i];
            ops.Forward = FForw[i];
            ops.Execute = ops.Backward || ops.Forward;
        }
    }
}
