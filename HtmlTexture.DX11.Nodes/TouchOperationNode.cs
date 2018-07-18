using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Interaction;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V2;

namespace HtmlTexture.DX11.Nodes
{
    public abstract class TouchOperationNode : AbstractOperationNode, IPluginEvaluate
    {
        public abstract bool IsChanged();
        public abstract IEnumerable<TouchContainer> GetTouches(int i);

        public virtual void PreEvaluate(int sprmax)
        {
            FOpsOut.SliceCount = sprmax;
        }

        public void Evaluate(int SpreadMax)
        {
            PreEvaluate(SpreadMax);

            for (int i = 0; i < FOpsOut.SliceCount; i++)
            {
                var ops = FOpsOut[i];
                if(ops == null) continue;
                ops.Execute = false;
                ops.Others = FOpsIn[i];
            }

            if (IsChanged())
            {
                for (int i = 0; i < SpreadMax; i++)
                {
                    FOpsOut[i] = new SendTouchOperation
                    {
                        Others = FOpsIn[i],
                        Touches = GetTouches(i),
                        Execute = true
                    };
                }
            }
        }
    }

    [PluginInfo(
        Name = "SendTouch",
        Category = "HtmlTexture.Operation",
        Version = "TouchContainer",
        Author = "MESO, microdee"
    )]
    public class TouchContainerTouchOperationNode : TouchOperationNode
    {
        [Input("Touches", Order = 10, BinOrder = 11)]
        public ISpread<ISpread<TouchContainer>> FTouches;

        public override bool IsChanged()
        {
            return true;
        }

        public override void PreEvaluate(int sprmax)
        {
            FOpsOut.SliceCount = SpreadUtils.SpreadMax(FTouches, FOpsIn);
        }

        public override IEnumerable<TouchContainer> GetTouches(int i)
        {
            return FTouches[i];
        }
    }
}
 