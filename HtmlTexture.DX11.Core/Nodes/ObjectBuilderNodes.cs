using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Coding;
using mp.pddn;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.HtmlTexture.DX11.Nodes
{
    public abstract class ObjectBuilderNode : IPluginEvaluate, IPartImportsSatisfiedNotification
    {
        [Import] public IHDEHost Hde;
        [Import] public IPluginHost2 Host;

        protected GenericBinSizedInput Input;

        protected abstract ObjectBuilder CreateObject(int i);
        protected abstract void UpdateObject(ObjectBuilder o, int i);
        protected abstract int SetSliceCount();

        [Output("Output")]
        public ISpread<ObjectBuilder> FOut;

        public void OnImportsSatisfied()
        {
            Input = new GenericBinSizedInput(Host, new InputAttribute("Values")
            {
                Order = 10,
                BinOrder = 11,
                AutoValidate = false
            }, Hde.MainLoop);
        }

        public void Evaluate(int SpreadMax)
        {
            var sprmax = SetSliceCount();

            for (int i = sprmax - 1; i >= 0; i--)
            {
                if (FOut[i] == null) FOut[i] = CreateObject(i);
                else if (FOut.Count(o => o?.Equals(FOut[i]) ?? false) > 1) FOut[i] = CreateObject(i);
                var obj = FOut[i];
                UpdateObject(obj, i);
            }
        }
    }

    [PluginInfo(
        Name = "V8Object",
        Category = "HtmlTexture.V8Object",
        Author = "MESO, microdee",
        Tags = "Bind, javascript, Vanadium",
        Help = "Create an object hierarchy which can be fed to BindObject (HtmlTexture.Operation) node",
        Warnings = "Supply only numbers, strings, V8Objects, simple structs or classes with primitive members, Do not supply complex self referencing big objects as return values. It can crash vvvv"
    )]
    public class ObjectObjectBuilderNode : ObjectBuilderNode
    {
        [Input("Keys")]
        public ISpread<ISpread<string>> FKeys;


        protected override ObjectBuilder CreateObject(int i)
        {
            return new ObjectBuilder
            {
                Type = TokenType.Object
            };
        }

        protected override void UpdateObject(ObjectBuilder o, int i)
        {
            for (int j = 0; j < FKeys[i].SliceCount; j++)
            {
                o.Properties.UpdateGeneric(FKeys[i][j], Input[i]?[VMath.Zmod(j, Input[i].Count)]);
            }
        }

        protected override int SetSliceCount()
        {
            return FKeys.SliceCount * (Input.SliceCount > 0 ? 1 : 0);
        }
    }

    [PluginInfo(
        Name = "V8Array",
        Category = "HtmlTexture.V8Object",
        Author = "MESO, microdee",
        Tags = "Bind, javascript, Vanadium",
        Help = "Create an object hierarchy which can be fed to BindObject (HtmlTexture.Operation) node",
        Warnings = "Supply only numbers, strings, V8Objects, simple structs or classes with primitive members, Do not supply complex self referencing big objects as return values. It can crash vvvv"
    )]
    public class ArrayObjectBuilderNode : ObjectBuilderNode
    {
        protected override ObjectBuilder CreateObject(int i)
        {
            return new ObjectBuilder
            {
                Type = TokenType.Array
            };
        }

        protected override void UpdateObject(ObjectBuilder o, int i)
        {
            if(Input[i] == null) return;
            o.Array = Input[i];
        }

        protected override int SetSliceCount()
        {
            return Input.SliceCount;
        }
    }
}
