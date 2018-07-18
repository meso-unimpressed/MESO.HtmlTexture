using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V2;

namespace HtmlTexture.DX11.Nodes
{
    public class AbstractOperationNode
    {
        [Input("Operations In", BinSize = 1, BinVisibility = PinVisibility.Hidden, Order = 0, BinOrder = 1)]
        public ISpread<ISpread<HtmlTextureOperation>> FOpsIn;

        [Output("Output", Order = 0)]
        public ISpread<HtmlTextureOperation> FOpsOut;
    }
}
