using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chromium;
using Chromium.Remote;
using md.stdl.Interfaces;

namespace Vanadium.Core
{
    public partial class HtmlTextureWrapper : IMainlooping, IDisposable
    {
        public class EvaluateTask : CfrTask
        {
            private readonly CfrBrowser _browser;
            private readonly Action<CfrV8Value, CfrV8Exception> _callback;
            private readonly string _code;

            internal EvaluateTask(CfrBrowser browser, string code, Action<CfrV8Value, CfrV8Exception> callback)
            {
                _browser = browser;
                _code = code;
                _callback = callback;
                Execute += (s, e) => TaskExecute(e);
            }

            private void TaskExecute(CfrEventArgs e)
            {
                var evalSucceeded = false;
                try
                {
                    var result = _browser.MainFrame.V8Context.Eval(_code, "", 0, out var retval, out var exception);
                    evalSucceeded = true;
                    if (result)
                        _callback(retval, exception);
                    else
                        _callback(null, null);
                }
                catch
                {
                    if (evalSucceeded)
                        return;
                    _callback(null, null);
                }
            }
        }

        public class BindFunctionsTask : CfrTask
        {
            internal BindFunctionsTask(CfrBrowser remoteBrowser, string objName, string[] funcNames, CfrV8Handler v8Handler)
            {
                Execute += (s, e) =>
                {
                    var v8Context = remoteBrowser.MainFrame.V8Context;
                    v8Context.Enter();
                    var cfrV8Value = (CfrV8Value)null;
                    if (!string.IsNullOrEmpty(objName) && !v8Context.Global.HasValue(objName))
                    {
                        cfrV8Value = CfrV8Value.CreateObject(new CfrV8Accessor());
                        v8Context.Global.SetValue(objName, cfrV8Value,
                            CfxV8PropertyAttribute.ReadOnly | CfxV8PropertyAttribute.DontDelete);
                    }

                    foreach (var funcName in funcNames)
                    {
                        var function = CfrV8Value.CreateFunction(funcName, v8Handler);
                        if (cfrV8Value != null && !cfrV8Value.HasValue(funcName))
                            cfrV8Value.SetValue(funcName, function,
                                CfxV8PropertyAttribute.ReadOnly | CfxV8PropertyAttribute.DontDelete);
                        else if (!v8Context.Global.HasValue(funcName))
                            v8Context.Global.SetValue(funcName, function,
                                CfxV8PropertyAttribute.ReadOnly | CfxV8PropertyAttribute.DontDelete);
                    }

                    v8Context.Exit();
                };
            }
        }
    }
}
