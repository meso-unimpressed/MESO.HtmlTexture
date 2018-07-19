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
        Author = "MESO, microdee",
        Help = "Scroll on any element in a HTML document"
    )]
    public class ScrollOperationNode : AbstractOperationNode<ScrollOperation>, IPluginEvaluate
    {
        [Input("Scroll", Order = 10, BinOrder = 11)]
        public IDiffSpread<Vector2D> FScroll;
        [Input("Normalized", Order = 12, BinOrder = 13)]
        public IDiffSpread<bool> FNorm;
        [Input("Element Selector", Order = 14, BinOrder = 15)]
        public IDiffSpread<string> FElement;

        public void Evaluate(int SpreadMax)
        {
            for (int i = 0; i < FOpsOut.SliceCount; i++)
            {
                if(FOpsOut[i] != null) FOpsOut[i].Execute = false;
            }
            if (SpreadUtils.AnyChanged(FElement, FNorm, FScroll))
            {
                var sprmax = FOpsOut.SliceCount = SpreadUtils.SpreadMax(FElement, FScroll, FNorm, FOpsIn);
                for (int i = 0; i < sprmax; i++)
                {
                    FOpsOut[i] = new ScrollOperation
                    {
                        ScrollPos = FScroll[i],
                        ElementSelector = FElement[i],
                        Normalized = FNorm[i],
                        Execute = true,
                        Others = FOpsIn[i]
                    };
                }
                FOpsOut.Stream.IsChanged = true;
            }
        }
    }
}
