using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chromium;
using md.stdl.Coding;
using md.stdl.Interaction;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.IO;
using VVVV.Utils.VMath;

namespace VVVV.HtmlTexture.DX11.Core
{
    public abstract class HtmlTextureOperation
    {
        public ISpread<HtmlTextureOperation> Others { get; set; }
        public bool Execute { get; set; }

        public virtual void Invoke(HtmlTextureWrapper wrapper)
        {
            if (Others == null) return;
            foreach (var operation in Others)
            {
                operation?.Invoke(wrapper);
            }
            if(Execute) Operation(wrapper);
        }

        protected abstract void Operation(HtmlTextureWrapper wrapper);
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

    public class ExecuteJsOperation : HtmlTextureOperation
    {
        public string Script { get; set; } = "";
        protected override void Operation(HtmlTextureWrapper wrapper)
        {
            wrapper.ExecuteJavascript(Script);
        }
    }

    public class EvaluateJsOperation : HtmlTextureOperation
    {
        public string Script { get; set; } = "";
        protected override void Operation(HtmlTextureWrapper wrapper)
        {
            wrapper.EvaluateJavascript(Script, (value, exception) =>
            {
                wrapper.OperationResults.UpdateGeneric(this, new EvaluateJsOperationResult
                {
                    Result = value.CfrV8ValueToString(),
                    Results = value.CfrV8ValueToStringArray(),
                    Error = exception.GetExceptionDump()
                });
            });
        }
    }

    public class EvaluateJsOperationResult
    {
        public string Result { get; set; }
        public string[] Results { get; set; }
        public string Error { get; set; }
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
        public IEnumerable<TouchContainer> Touches { get; set; }
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

        public override void Invoke(HtmlTextureWrapper wrapper)
        {
            Up.Clear();
            if(Input.IsEmpty()) Up.AddRange(Pressed);
            else Up.AddRange(Pressed.Where(k => Input.All(kk => k.KeyCode != kk.KeyCode)));

            Down.Clear();
            if(Pressed.Count == 0) Down.AddRange(Input);
            else Down.AddRange(Input.Where(k => Pressed.All(kk => k.KeyCode != kk.KeyCode)));

            Pressed.Clear();
            Pressed.AddRange(Input);

            Execute = Enabled && (Up.Count > 0 || Down.Count > 0);
            base.Invoke(wrapper);
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
