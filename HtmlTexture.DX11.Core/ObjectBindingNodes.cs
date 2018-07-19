using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Chromium.Remote;
using mp.pddn;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V2;

namespace HtmlTexture.DX11.Nodes
{
    [PluginInfo(
        Name = "BindObject",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "Bind, javascript",
        Help = "Bind objects with functions which can be called by Javascript with arguments, Get those arguments in vvvv and return an object in Javascript",
        Warnings = "Supply only simple structs and classes with primitive members, Do not supply complex self referencing big objects as return values. It can crash vvvv"
    )]
    public class ObjectBindingNode : PersistentOperationNode<BindObjectOperation>, IPartImportsSatisfiedNotification
    {
        [Import] public IHDEHost Hde;
        [Import] public IPluginHost2 Host;

        private GenericBinSizedInput _input;

        [Input("Object Name", Order = 6, BinOrder = 7)]
        public ISpread<string> FObj;
        [Input("Function Name", Order = 8, BinOrder = 9)]
        public ISpread<ISpread<string>> FFunc;
        [Input("Bind", Order = 12, BinOrder = 13, IsBang = true)]
        public IDiffSpread<bool> FBind;

        protected override int SliceCount()
        {
            return Math.Max(SpreadUtils.SpreadMax(FObj, FFunc, FBind), _input.SliceCount) *
                   _input.SliceCount > 0 ? 1 : 0;
        }

        protected override void UpdateOps(BindObjectOperation ops, int i)
        {
            if (ops.Binding == null) ops.Binding = new JsBinding();
            ops.Binding.Name = FObj[i];
            ops.Execute = FBind[i];
            if (FBind[i])
            {
                ops.Binding.Functions.Clear();
                for (int j = 0; j < FFunc[i].SliceCount; j++)
                {
                    if(string.IsNullOrWhiteSpace(FFunc[i][j])) continue;
                    var func = new SimpleReturnObjectBinding
                    {
                        Name = FFunc[i][j],
                        ReturnObject = _input[i][j]
                    };
                    ops.Binding.AddFunction(func);
                }
            }

            for (int j = 0; j < FFunc[i].SliceCount; j++)
            {
                if(!ops.Binding.Functions.ContainsKey(FFunc[i][j])) continue;
                var func = ops.Binding.Functions[FFunc[i][j]];

                if (func is SimpleReturnObjectBinding sfunc)
                {
                    sfunc.ReturnObject = _input[i][j];
                }
            }
        }

        public void OnImportsSatisfied()
        {
            _input = new GenericBinSizedInput(Host, new InputAttribute("Return Objects")
            {
                Order = 10,
                BinOrder = 11,
            }, Hde.MainLoop);
        }
    }

    [PluginInfo(
        Name = "GetBoundObject",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "Bind, javascript",
        Help = "Get data of bound functions from javascript"
    )]
    public class GetJsBindingNode : IPluginEvaluate
    {
        [Input("Input")]
        public Pin<HtmlTextureWrapper> FWrapper;

        [Input("Object Name")]
        public ISpread<ISpread<string>> FObj;
        [Input("Function Name")]
        public ISpread<ISpread<string>> FFunc;

        [Output("Output")]
        public ISpread<ISpread<JsBindingFunction>> FOut;
        [Output("Valid")]
        public ISpread<ISpread<bool>> FValid;

        public void Evaluate(int SpreadMax)
        {
            if (FWrapper.IsConnected)
            {
                FOut.SliceCount = FValid.SliceCount = FWrapper.SliceCount;
                for (int i = 0; i < FWrapper.SliceCount; i++)
                {
                    var wrapper = FWrapper[i];

                    if (wrapper == null)
                    {
                        FOut[i].SliceCount = FValid[i].SliceCount = 0;
                        continue;
                    }

                    var slc = SpreadUtils.SpreadMax(FObj[i], FFunc[i]);
                    FOut[i].SliceCount = FValid[i].SliceCount = slc;

                    for (int j = 0; j < slc; j++)
                    {
                        var objname = FObj[i][j];
                        var funcname = FFunc[i][j];
                        if (wrapper.JsBindings.ContainsKey(objname))
                        {
                            var obj = wrapper.JsBindings[objname];
                            if (obj.Functions.ContainsKey(funcname))
                            {
                                FOut[i][j] = obj.Functions[funcname];
                                FValid[i][j] = true;
                            }
                            else
                            {
                                FOut[i][j] = null;
                                FValid[i][j] = false;
                            }
                        }
                        else
                        {
                            FOut[i][j] = null;
                            FValid[i][j] = false;
                        }
                    }
                }
            }
            else
            {
                FOut.SliceCount = FValid.SliceCount = 0;
            }
        }
    }

    [PluginInfo(
        Name = "GetBoundFunction",
        Category = "HtmlTexture.Operation",
        Author = "MESO, microdee",
        Tags = "Bind, javascript",
        Help = "Get data of a bound function from javascript"
    )]
    public class SplitJsBindingFuncNode : ObjectSplitNode<JsBindingFunction>
    {
        public override Type TransformType(Type original, MemberInfo member)
        {
            if (original == typeof(CfrV8Value)) return typeof(string);
            return base.TransformType(original, member);
        }

        public override object TransformOutput(object obj, MemberInfo member, int i)
        {
            if (obj is CfrV8Value v8Val) return v8Val.CfrV8ValueToString();
            return base.TransformOutput(obj, member, i);
        }
    }
}
