using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanadium.Core;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace Vanadium.Nodes
{
    public class AbstractOperationNode<T> where T : HtmlTextureOperation
    {
        [Input("Operations In", BinSize = 1, BinVisibility = PinVisibility.Hidden, Order = 0, BinOrder = 1)]
        public ISpread<ISpread<HtmlTextureOperation>> FOpsIn;

        [Output("Output", Order = -10)]
        public ISpread<T> FOpsOut;
    }
    public abstract class PersistentOperationNode<T> : AbstractOperationNode<T>, IPluginEvaluate where T : HtmlTextureOperation, new()
    {
        protected abstract int SliceCount();
        protected abstract void UpdateOps(ref T ops, int i);
        protected virtual void PreEvaluate(int sprmax) { }

        protected virtual bool IsChanged()
        {
            return false;
        }

        protected virtual T CreateOps(int i)
        {
            return new T();
        }

        private int _oldSlc = -1;

        public void Evaluate(int SpreadMax)
        {
            var sprmax = FOpsOut.SliceCount = Math.Max(FOpsIn.SliceCount, SliceCount());
            var changed = IsChanged() || _oldSlc != sprmax;
            _oldSlc = sprmax;

            PreEvaluate(sprmax);

            for (int i = sprmax - 1; i >= 0; i--)
            {
                if (FOpsOut[i] == null) FOpsOut[i] = CreateOps(i);
                else if (FOpsOut.Count(o => o?.Equals(FOpsOut[i]) ?? false) > 1) FOpsOut[i] = CreateOps(i);
                var opsout = FOpsOut[i];

                UpdateOps(ref opsout, i);

                opsout.Executed.Clear();
                opsout.Others = FOpsIn[i];
                FOpsOut[i] = opsout;
            }

            FOpsOut.Stream.IsChanged = true;
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
