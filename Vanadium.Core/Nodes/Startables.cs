using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vanadium.Core;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;

namespace Vanadium.Nodes
{
    [Startable(Lazy = false)]
    public class HtmlPluginStartable : IStartable
    {
        public void Start()
        {
            //MessageBox.Show("test startable");
            HtmlTextureStartable.Start();
        }

        public void Shutdown()
        {
            HtmlTextureStartable.Shutdown();
        }
    }
}
