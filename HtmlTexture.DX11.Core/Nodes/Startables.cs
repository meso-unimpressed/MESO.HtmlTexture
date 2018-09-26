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
