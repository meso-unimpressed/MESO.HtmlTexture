using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanadium.Nodes;
using Vanadium.Core;
using VVVV.PluginInterfaces.V2;

namespace Vanadium.Nodes
{
    [PluginInfo(
        Name = "SendResizeEvent",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Help = "Send a dummy resize event to the wrapper. Useful for layout refreshing."
    )]
    public class DummyResizeOpNode : PersistentOperationNode<DummyResizeOperation>
    {
        [Input("Execute", Order = 12, BinOrder = 13, IsBang = true)]
        public IDiffSpread<bool> FExec;
        [Input("Execute On Load", Order = 14, BinOrder = 15)]
        public IDiffSpread<bool> FExecOnLoad;

        protected override int SliceCount()
        {
            return 1;
        }

        protected override void UpdateOps(ref DummyResizeOperation ops, int i)
        {
            ops.Execute = FExec[i];
            ops.ExecuteOnLoad = FExecOnLoad[i];
        }
    }
}
