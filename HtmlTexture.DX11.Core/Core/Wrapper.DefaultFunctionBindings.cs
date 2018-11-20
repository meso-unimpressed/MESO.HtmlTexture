using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chromium.Remote;
using Chromium.Remote.Event;
using md.stdl.Interfaces;

namespace VVVV.HtmlTexture.DX11.Core
{
    public partial class HtmlTextureWrapper : IMainlooping, IDisposable
    {
        public class ResizeNotificationFunction : JsBindingFunction
        {
            public int Width { get; private set; }
            public int Height { get; private set; }

            public event JsFunctionEventHandler SizeChanged;

            public ResizeNotificationFunction()
            {
                Name = "docResizeNotification";
            }

            protected override CfrV8Value Function(CfrV8HandlerExecuteEventArgs args, JsBinding binding, HtmlTextureWrapper wrapper)
            {
                if(wrapper == null) return CfrV8Value.CreateNull();
                if (Arguments.Length >= 2 && (Arguments[0].IsInt && Arguments[1].IsInt))
                {
                    Width = Arguments[0].IntValue;
                    Height = Arguments[1].IntValue;
                    wrapper.DocumentSize = (Width, Height);
                    SizeChanged?.Invoke(this, wrapper);
                }
                return CfrV8Value.CreateNull();
            }

            public override JsBindingFunction Copy()
            {
                return new ResizeNotificationFunction { Name = Name };
            }
        }

        public class DocSizeBaseSelector : JsBindingFunction
        {
            public string Selector { get; set; } = "body";

            public DocSizeBaseSelector()
            {
                Name = "docSizeBaseSelector";
            }

            protected override CfrV8Value Function(CfrV8HandlerExecuteEventArgs args, JsBinding binding, HtmlTextureWrapper wrapper)
            {
                if (Arguments.Length >= 1 && Arguments[0].IsString)
                    Selector = Arguments[0].StringValue;
                return CfrV8Value.CreateString(Selector);
            }

            public override JsBindingFunction Copy()
            {
                return new DocSizeBaseSelector
                {
                    Name = Name,
                    Selector = Selector
                };
            }
        }
    }
}
