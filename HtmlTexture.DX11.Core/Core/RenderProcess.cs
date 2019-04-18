using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Chromium;
using Chromium.Remote;
using Chromium.Remote.Event;
using VVVV.DX11.Nodes;

namespace VVVV.HtmlTexture.DX11.Core
{
    class App
    {
        private static Process _parent;
        private static Process _current;

        private static void InitProcessListener(string[] args)
        {
            try
            {
                var pidargraw = args.ToList().Find(a => a.Contains("--vvvv-pid"));
                var pidargregex = new Regex(@"--vvvv-pid=(?<pid>\d*?)$");
                var pidarg = pidargregex.Match(pidargraw).Groups["pid"].Value;

                if (!int.TryParse(pidarg, out var parentpid)) return;

                _parent = Process.GetProcessById(parentpid);
                _current = Process.GetCurrentProcess();

                Task.Run(() =>
                {
                    while (true)
                    {
                        if (_parent.HasExited) _current.Kill();
                        Thread.Sleep(1000);
                    }
                });
            }
            catch { }
        }

        public static int Main(string[] args)
        {
            InitProcessListener(args);
            return CfxRuntime.ExecuteProcess(null);
        }
    }

    public class RenderProcess
    {
        static List<RenderProcess> renderProcess = new List<RenderProcess>(); // prevent from compiler's optimization

        public static int RenderProcessMain()
        {
            try
            {
                RenderProcess rp = new RenderProcess();

                renderProcess.Add(rp);
                int retval = rp.Start();
                renderProcess.Remove(rp);

                return retval;
            }
            catch
            {
                return 0;
            }
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
