using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chromium;
using md.stdl.Coding;
using md.stdl.Interaction;
using mp.pddn;
using Notui;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.IO;
using VVVV.Utils.VMath;

namespace VVVV.HtmlTexture.DX11.Core
{
    public delegate void OperationExecutedEventHandler(HtmlTextureOperation ops, HtmlTextureWrapper wrapper);

    public class ResultFromJs
    {
        public string Result { get; set; }
        public string[] Results { get; set; }
        public string Error { get; set; }
    }

    public abstract class HtmlTextureOperation : OperationBase
    {
        public ISpread<HtmlTextureOperation> Others { get; set; }
        public bool ExecuteOnLoad { get; set; }
        public bool Execute { get; set; }
        public Dictionary<int, bool> Executed { get; } = new Dictionary<int, bool>();
        public HtmlTextureWrapper Wrapper { get; set; }
        public event OperationExecutedEventHandler OnBeforeExecute;
        public event OperationExecutedEventHandler OnAfterExecute;

        public bool WasExecuted(int bid)
        {
            return Executed.ContainsKey(bid) && Executed[bid];
        }

        public override void Invoke()
        {
            if((Execute || ExecuteOnLoad && Wrapper.LoadedFrame) && !WasExecuted(Wrapper.BrowserId))
            {
                OnBeforeExecute?.Invoke(this, Wrapper);
                Operation(Wrapper);
                Executed.UpdateGeneric(Wrapper.BrowserId, true);
                OnAfterExecute?.Invoke(this, Wrapper);
            }
        }

        protected abstract void Operation(HtmlTextureWrapper wrapper);
    }

    public class HtmlTextureOperationHost : OperationHost<HtmlTextureOperation>
    {
        public void InvokeRecursive(HtmlTextureWrapper wrapper)
        {
            InvokeRecursive(0, (i, j, o) => o.Wrapper = wrapper);
        }
    }

    public class EmptyOperation : HtmlTextureOperation
    {
        protected override void Operation(HtmlTextureWrapper wrapper) { }
    }

    public class DummyResizeOperation : HtmlTextureOperation
    {
        protected override void Operation(HtmlTextureWrapper wrapper)
        {
            void OnMainloopBegin1(object sender1, EventArgs e1)
            {
                var ts = wrapper.TextureSettings;
                ts.ExtraSize = (
                    ts.ExtraSize.w + 1,
                    ts.ExtraSize.h + 1
                );
                wrapper.TextureSettings = ts;

                wrapper.OnMainLoopBegin -= OnMainloopBegin1;

                void OnMainloopBegin2(object sender2, EventArgs e2)
                {
                    var tse = wrapper.TextureSettings;
                    tse.ExtraSize = (
                        tse.ExtraSize.w - 1,
                        tse.ExtraSize.h - 1
                    );
                    wrapper.TextureSettings = tse;

                    wrapper.OnMainLoopBegin -= OnMainloopBegin2;
                }
                wrapper.OnMainLoopBegin += OnMainloopBegin2;
            }
            wrapper.OnMainLoopBegin += OnMainloopBegin1;
        }
    }

    public class ScrollOperation : HtmlTextureOperation
    {
        public Vector2D ScrollPos { get; set; }
        public bool Normalized { get; set; }
        public string ElementSelector { get; set; } = "";

        protected override void Operation(HtmlTextureWrapper wrapper)
        {
            wrapper.ScrollTo(ScrollPos.x, ScrollPos.y, ElementSelector, Normalized);
        }
    }

    public interface IJsOperation
    {
        string Script { get; set; }
    }

    public class ExecuteJsOperation : HtmlTextureOperation, IJsOperation
    {
        public string Script { get; set; } = "";
        protected override void Operation(HtmlTextureWrapper wrapper)
        {
            wrapper.ExecuteJavascript(Script);
        }
    }

    public class EvaluateJsOperation : HtmlTextureOperation, IJsOperation
    {
        public string Script { get; set; } = "";
        protected override void Operation(HtmlTextureWrapper wrapper)
        {
            wrapper.EvaluateJavascript(Script, (value, exception) =>
            {
                wrapper.OperationResults.UpdateGeneric(this, new ResultFromJs
                {
                    Result = value.CfrV8ValueToString(),
                    Results = value.CfrV8ValueToStringArray(),
                    Error = exception.GetExceptionDump()
                });
            });
        }
    }

    public class BindObjectOperation : HtmlTextureOperation
    {
        public string Name { get; set; } = "vvvv";
        public JsBinding Binding { get; set; }

        protected override void Operation(HtmlTextureWrapper wrapper)
        {
            if (Binding != null) wrapper.BindObject(Binding);
        }
    }

    public class SendTouchOperation : HtmlTextureOperation
    {
        public IEnumerable<HtmlTextureTouch> Touches { get; set; }
        protected override void Operation(HtmlTextureWrapper wrapper)
        {
            if(Touches != null) wrapper.SendTouches(Touches);
        }
    }

    public class NavigationOperation : HtmlTextureOperation
    {
        public bool Forward { get; set; }
        public bool Backward { get; set; }
        protected override void Operation(HtmlTextureWrapper wrapper)
        {
            if (Forward && wrapper.Browser.CanGoForward) wrapper.Browser.GoForward();
            if (Backward && wrapper.Browser.CanGoBack) wrapper.Browser.GoBack();
        }
    }

    public struct KeyEvent
    {
        public int KeyCode;
        public string KeyChar;
    }

    public class SendKeyOperation : HtmlTextureOperation
    {
        public List<KeyEvent> Up { get; set; } = new List<KeyEvent>();
        public List<KeyEvent> Down { get; private set; } = new List<KeyEvent>();
        public List<KeyEvent> Pressed { get; private set; } = new List<KeyEvent>();
        public IEnumerable<KeyEvent> Input { get; set; }
        public bool Enabled { get; set; }

        public override void Invoke()
        {
            Up.Clear();
            bool inputexists = false;
            foreach (var key in Input)
            {
                inputexists = true;
                break;
            }
            if(inputexists) Up.AddRange(Pressed);
            else Up.AddRange(Pressed.Where(k => Input.All(kk => k.KeyCode != kk.KeyCode)));

            Down.Clear();
            if(Pressed.Count == 0) Down.AddRange(Input);
            else Down.AddRange(Input.Where(k => Pressed.All(kk => k.KeyCode != kk.KeyCode)));

            Pressed.Clear();
            Pressed.AddRange(Input);

            Execute = Enabled && (Up.Count > 0 || Down.Count > 0);
            base.Invoke();
        }

        protected override void Operation(HtmlTextureWrapper wrapper)
        {
            foreach (var ke in Down)
            {
                var cke = new CfxKeyEvent
                {
                    Type = CfxKeyEventType.Keydown,
                    Character = (short) ke.KeyChar[0],
                    WindowsKeyCode = ke.KeyCode,
                    NativeKeyCode = ke.KeyCode
                };
                wrapper.Browser.Host.SendKeyEvent(cke);
            }
            foreach (var ke in Up)
            {
                var cke = new CfxKeyEvent
                {
                    Type = CfxKeyEventType.Keydown,
                    Character = (short)ke.KeyChar[0],
                    WindowsKeyCode = ke.KeyCode,
                    NativeKeyCode = ke.KeyCode
                };
                wrapper.Browser.Host.SendKeyEvent(cke);
            }
        }
    }
}
