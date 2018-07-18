using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V2;

namespace HtmlTexture.DX11.Nodes
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
