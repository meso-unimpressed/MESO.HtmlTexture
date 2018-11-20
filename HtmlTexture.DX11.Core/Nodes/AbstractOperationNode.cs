using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mp.pddn;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace HtmlTexture.DX11.Nodes
{
    public class AbstractOperationNode<T> where T : HtmlTextureOperation
    {
        [Input("Operations In", Order = 0, IsSingle = true)]
        public ISpread<HtmlTextureOperationHost> OpsIn;

        [Output("Output", Order = -10)]
        public ISpread<HtmlTextureOperationHost> OpsOut;

        [Output(
            "Individual Operations",
            Order = 9998,
            BinOrder = 9999,
            Visibility = PinVisibility.Hidden,
            BinVisibility = PinVisibility.Hidden
        )]
        public ISpread<ISpread<T>> IndivOpsOut;
    }

    public abstract class PersistentOperationNode<T> : AbstractOperationNode<T>, IPluginEvaluate where T : HtmlTextureOperation, new()
    {
        protected abstract int SliceCount(int i);
        protected abstract int BinCount();
        protected abstract void UpdateOps(ref T ops, int i, int j);
        protected virtual void PreEvaluate(int sprmax) { }

        protected virtual T CreateOps(int i, int j)
        {
            return new T();
        }

        public void Evaluate(int SpreadMax)
        {
            if(OpsOut[0] == null) OpsOut[0] = new HtmlTextureOperationHost();
            var ophost = OpsOut[0];
            ophost.Child = OpsIn.TryGetSlice(0);

            var sprmax = IndivOpsOut.SliceCount = BinCount();
            ophost.SetBinCount(sprmax);

            PreEvaluate(sprmax);

            for (int i = 0 ; i < sprmax; i++)
            {
                var binsprmax = ophost.Operations[i].SliceCount = IndivOpsOut[i].SliceCount = SliceCount(i);

                for (int j = binsprmax - 1; j >= 0; j--)
                {
                    if (IndivOpsOut[i][j] == null) IndivOpsOut[i][j] = CreateOps(i, j);
                    else if (IndivOpsOut[i].Count(o => o?.Equals(IndivOpsOut[i][j]) ?? false) > 1)
                        IndivOpsOut[i][j] = CreateOps(i, j);

                    var op = IndivOpsOut[i][j];
                    UpdateOps(ref op, i, j);
                    op.Executed.Clear();
                    IndivOpsOut[i][j] = op;
                    ophost.Operations[i][j] = op;
                }
            }

            OpsOut.Stream.IsChanged = true;
        }
    }

    public abstract class GetOperationResultsNode<TRes, TOps> : IPluginEvaluate
        where TOps : HtmlTextureOperation
        where TRes : class
    {
        [Input("Input")]
        public Pin<HtmlTextureWrapper> FWrapper;

        [Input("Operation")]
        public ISpread<ISpread<TOps>> FOps;

        [Output("Output")]
        public ISpread<ISpread<TRes>> FOut;

        [Output("Valid")]
        public ISpread<ISpread<bool>> FValid;

        protected virtual void OnValidResult(TRes result, TOps ops, HtmlTextureWrapper wrapper, int i, int j) { }
        protected virtual void OnInvalidResult(TOps ops, HtmlTextureWrapper wrapper, int i, int j) { }
        protected virtual void OnWrapperSliceCount(int slc) { }
        protected virtual void OnOpsSliceCount(int slc, int i) { }

        public void Evaluate(int SpreadMax)
        {
            if (FWrapper.IsConnected)
            {
                var slc = FOut.SliceCount = FValid.SliceCount = FWrapper.SliceCount;
                OnWrapperSliceCount(slc);
                for (int i = 0; i < slc; i++)
                {
                    var wrapper = FWrapper[i];
                    if (wrapper == null)
                    {
                        FOut[i].SliceCount = FValid[i].SliceCount = 0;
                        OnOpsSliceCount(0, i);
                        continue;
                    }
                    var oslc = FOut[i].SliceCount = FValid[i].SliceCount = FOps[i].SliceCount;
                    OnOpsSliceCount(oslc, i);
                    for (int j = 0; j < oslc; j++)
                    {
                        var ops = FOps[i][j];
                        if (ops == null)
                        {
                            FOut[i][j] = null;
                            FValid[i][j] = false;
                            OnInvalidResult(ops, wrapper, i, j);
                            continue;
                        }
                        if (!wrapper.OperationResults.ContainsKey(ops))
                        {
                            FOut[i][j] = null;
                            FValid[i][j] = false;
                            OnInvalidResult(ops, wrapper, i, j);
                            continue;
                        }
                        var res = wrapper.OperationResults[ops];
                        if (res is TRes result)
                        {
                            FOut[i][j] = result;
                            FValid[i][j] = true;
                            OnValidResult(result, ops, wrapper, i, j);
                        }
                        else
                        {
                            FOut[i][j] = null;
                            FValid[i][j] = false;
                            OnInvalidResult(ops, wrapper, i, j);
                        }
                    }
                }
            }
            else
            {
                FOut.SliceCount = FValid.SliceCount = 0;
                OnWrapperSliceCount(0);
            }
        }
    }
}
