using System.Collections.Generic;
using Chromium;
using Chromium.Remote;
using Chromium.Remote.Event;

namespace VVVV.Vanadium.Core
{

    class App
    {
        public static int Main(string[] args)
        {
            return CfxRuntime.ExecuteProcess(null);
        }
    }

    public class RenderProcess
    {
        static List<RenderProcess> renderProcess = new List<RenderProcess>(); // prevent from compiler's optimization

        public static int RenderProcessMain()
        {
            RenderProcess rp = new RenderProcess();

            renderProcess.Add(rp);
            int retval = rp.Start();
            renderProcess.Remove(rp);

            return retval;
        }

        private CfrApp app;
        private CfrLoadHandler loadHandler;
        private CfrRenderProcessHandler renderProcessHandler;
        private RenderProcess()
        {

        }

        public int Start()
        {
            try
            {
                app = new CfrApp();

                loadHandler = new CfrLoadHandler();
                loadHandler.OnLoadEnd += loadHandler_OnLoadEnd;
                loadHandler.OnLoadStart += loadHandler_OnLoadStart;

                renderProcessHandler = new CfrRenderProcessHandler();
                renderProcessHandler.GetLoadHandler += (sender, e) => e.SetReturnValue(loadHandler);

                app.GetRenderProcessHandler += (s, e) => e.SetReturnValue(renderProcessHandler);

                var retval = CfrRuntime.ExecuteProcess(app);
                return retval;
            }
            catch
            {
                return 0;
            }
        }

        void loadHandler_OnLoadStart(object sender, CfrOnLoadStartEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                if (HtmlTextureWrapper.Instances.ContainsKey(e.Browser.Identifier))
                {
                    var instance = HtmlTextureWrapper.Instances[e.Browser.Identifier];
                    instance.RemoteBrowser = e.Browser;
                    //instance.IsDocumentReady = false;
                }
            }
        }

        void loadHandler_OnLoadEnd(object sender, CfrOnLoadEndEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                if (HtmlTextureWrapper.Instances.ContainsKey(e.Browser.Identifier))
                {
                    var instance = HtmlTextureWrapper.Instances[e.Browser.Identifier];
                    instance.OnLoadEnd();
                }
            }
        }
    }
}
