using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chromium.Event;
using HtmlTexture.DX11.Nodes;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V2;

namespace VVVV.HtmlTexture.DX11.Nodes
{
    [PluginInfo(
        Name = "EventTunneler",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "onloaded, oncreated, Vanadium",
        Help = "Notify events upstream"
    )]
    public class EventsTunnelingNode : PersistentOperationNode<EmptyOperation>
    {
        [Output("On Loaded")]
        public ISpread<int> LoadedOut;
        [Output("On Created")]
        public ISpread<int> CreatedOut;
        [Output("On Loaded Bang", IsBang = true)]
        public ISpread<bool> LoadedBangOut;
        [Output("On Created Bang", IsBang = true)]
        public ISpread<bool> CreatedBangOut;

        protected int _loadedc = -1;
        protected int _createdc = -1;

        protected override int SliceCount(int i) => 1;
        protected override int BinCount() => 1;

        protected override EmptyOperation CreateOps(int i, int j)
        {
            var ops = base.CreateOps(i, j);

            void OpsOnOnBeforeExecute(HtmlTextureOperation operation, HtmlTextureWrapper wrapper)
            {
                LoadedOut[i] += wrapper.LoadedFrame ? 1 : 0;
                CreatedOut[i] += wrapper.CreatedFrame ? 1 : 0;
                //operation.OnBeforeExecute -= OpsOnOnBeforeExecute;
            }
            ops.OnBeforeExecute += OpsOnOnBeforeExecute;
            return ops;
        }

        protected override void PreEvaluate(int sprmax)
        {
            base.PreEvaluate(sprmax);
            LoadedOut.SliceCount = CreatedOut.SliceCount = 1;
            LoadedBangOut[0] = CreatedBangOut[0] = false;

            if (_loadedc != LoadedOut[0])
            {
                LoadedBangOut[0] = true;
            }
            _loadedc = LoadedOut[0];

            if (_createdc != CreatedOut[0])
            {
                CreatedBangOut[0] = true;
            }
            _createdc = CreatedOut[0];
        }

        protected override void UpdateOps(ref EmptyOperation ops, int i, int j)
        {
            ops.ExecuteOnLoad = true;
        }
    }
}
