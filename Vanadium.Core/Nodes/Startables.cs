using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanadium.Core;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;

namespace Vanadium.Nodes
{
    [Startable]
    public class HtmlPluginStartable : IStartable
    {
        public void Start()
        {
            HtmlTextureStartable.Start();
        }

        public void Shutdown()
        {
            HtmlTextureStartable.Shutdown();
        }
    }
}
