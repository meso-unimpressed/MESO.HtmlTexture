using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chromium;
using Chromium.Remote;
using md.stdl.Coding;
using md.stdl.Interfaces;
using VVVV.Core.Logging;

namespace VVVV.HtmlTexture.DX11.Core
{
    public partial class HtmlTextureWrapper : IMainlooping, IDisposable
    {
        public void Reload()
        {
            if (Closed) return;
            Browser.ReloadIgnoreCache();
        }

        public void UpdateDom()
        {
            if (Closed) return;
            Browser.MainFrame.GetSource(Visitor);
        }

        public void LoadUrl(string url)
        {
            if (Closed) return;
            if (Browser == null) return;
            Browser.MainFrame.LoadUrl(url);
        }

        public void LoadString(string content, string dummyurl)
        {
            if (Closed) return;
            if (Browser == null) return;
            Browser.MainFrame.LoadUrl("about:blank");
            _customContent = content;
            _customContentUrl = dummyurl;
        }

        public void ScrollTo(double h, double v, string elementSelector = "", bool normalized = false)
        {
            if (Closed) return;
            var usewindow = string.IsNullOrWhiteSpace(elementSelector);
            var norm = normalized ? "true" : "false";
            var code = !usewindow ? string.Format(CultureInfo.InvariantCulture,
                    "vvvvUtils.elementScroll({0}, {1}, \"{2}\", {3})", h, v, elementSelector, norm)
                : string.Format(CultureInfo.InvariantCulture,
                    "vvvvUtils.windowScroll({0}, {1}, {2})", h, v, norm);
            ExecuteJavascript(code);
        }

        public void SendTouches(IEnumerable<HtmlTextureTouch> touches)
        {
            if (Closed) return;
            foreach (var touch in touches)
            {
                SubmittedTouches.UpdateGeneric(touch.Id, touch);
            }
        }

        public bool ExecuteJavascript(string code)
        {
            if (Closed) return false;
            if (Browser == null) return false;
            Browser.MainFrame.ExecuteJavaScript(code, null, 0);
            return true;
        }

        public void ShowDevTool()
        {
            if (Closed) return;
            Browser.Host.ShowDevTools(new CfxWindowInfo()
            {
                Style = WindowStyle.WS_OVERLAPPEDWINDOW | WindowStyle.WS_CLIPCHILDREN | WindowStyle.WS_CLIPSIBLINGS | WindowStyle.WS_VISIBLE,
                ParentWindow = IntPtr.Zero,
                WindowName = "DevTools",
                X = 200,
                Y = 200,
                Width = 800,
                Height = 600
            }, new CfxClient(), new CfxBrowserSettings(), null);
        }

        public bool EvaluateJavascript(string code, Action<CfrV8Value, CfrV8Exception> callback)
        {
            if (Closed) return false;
            if (RemoteBrowser == null)
                return false;
            try
            {
                var remoteCallContext = RemoteBrowser.CreateRemoteCallContext();
                remoteCallContext.Enter();
                try
                {
                    var taskrunner = CfrTaskRunner.GetForThread((CfxThreadId)6);
                    if (taskrunner == null)
                    {
                        LogError("Not evaluating javascript, TaskRunner is null, wrong CEF thread ID?");
                        return false;
                    }
                    taskrunner.PostTask(new EvaluateTask(RemoteBrowser, code, callback));
                    return true;
                }
                finally
                {
                    remoteCallContext.Exit();
                }
            }
            catch (IOException ex)
            {
                LogError(ex.Message);
                VvvvLogger?.Log(ex);
                return false;
            }
        }

        public bool BindObject(JsBinding binding)
        {
            if (Closed) return false;
            if (RemoteBrowser == null)
                return false;
            try
            {
                var jsBinding = binding;
                if (JsBindings.ContainsKey(binding.Name))
                {
                    jsBinding = JsBindings[binding.Name];
                    jsBinding.MigrateFunctions(binding);
                }
                else JsBindings.Add(binding.Name, binding);

                var remoteCallContext = RemoteBrowser.CreateRemoteCallContext();
                remoteCallContext.Enter();
                try
                {
                    V8Handler = new CfrV8Handler();
                    jsBinding.Bind(this, V8Handler);
                    var taskrunner = CfrTaskRunner.GetForThread((CfxThreadId)6);
                    if (taskrunner == null)
                    {
                        LogError("Unsuccessful Binding, TaskRunner is null");
                        remoteCallContext.Exit();
                        return false;
                    }
                    taskrunner.PostTask(new BindFunctionsTask(RemoteBrowser, jsBinding.Name, jsBinding.Functions.Keys.ToArray(), V8Handler));
                    remoteCallContext.Exit();
                    return true;
                }
                catch (Exception ex)
                {
                    remoteCallContext.Exit();
                    LogError(ex.Message);
                    VvvvLogger?.Log(ex);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                VvvvLogger?.Log(ex);
                return false;
            }
        }
    }
}
