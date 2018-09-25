using System.Windows.Forms;
using VVVV.PluginInterfaces.V2;
using VVVV.Vanadium.Core;

namespace VVVV.Vanadium.Nodes
{
    [Startable(Lazy = false)]
    public class HtmlPluginStartable : IStartable
    {
        public void Start()
        {
            MessageBox.Show("test startable");
            HtmlTextureStartable.Start();
        }

        public void Shutdown()
        {
            HtmlTextureStartable.Shutdown();
        }
    }
}
