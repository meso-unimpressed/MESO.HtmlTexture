using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using Chromium;
using Chromium.Remote;

namespace VVVV.HtmlTexture.DX11.Core
{
    public enum TokenType
    {
        Array,
        Object
    }
    public class ObjectBuilder
    {
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();
        public IEnumerable Array { get; set; }
        public TokenType Type { get; set; }

        public CfrV8Value Serialize(HashSet<object> refin = null)
        {
            var propattr = CfxV8PropertyAttribute.DontDelete | CfxV8PropertyAttribute.ReadOnly;

            var referenced = refin;
            if(referenced != null && referenced.Contains(this)) return CfrV8Value.CreateNull();
            else if(referenced == null)
            {
                referenced = new HashSet<object>();
                referenced.Add(this);
            }
            else
            {
                referenced.Add(this);
            }

            switch (Type)
            {
                case TokenType.Array:
                    lock (Array)
                    {
                        var reslist = (from object obj in Array select obj.V8Serialize(referenced)).ToList();
                        var resa = CfrV8Value.CreateArray(reslist.Count);
                        for (int i = 0; i < reslist.Count; i++)
                        {
                            resa.SetValue(i, reslist[i]);
                        }
                        return resa;
                    }
                case TokenType.Object:
                    var reso = CfrV8Value.CreateObject(ClrV8ValueSerializer.Accessor);

                    lock (Properties)
                    {
                        foreach (var prop in Properties.ToArray())
                        {
                            var cobj = prop.Value.V8Serialize(referenced);
                            reso.SetValue(prop.Key, cobj, propattr);
                        }
                    }
                    return reso;
                default:
                    return reso = CfrV8Value.CreateUndefined();
            }
        }
    }
}
