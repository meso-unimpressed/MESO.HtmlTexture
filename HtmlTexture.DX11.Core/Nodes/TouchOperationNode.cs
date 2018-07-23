using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Coding;
using md.stdl.Interaction;
using md.stdl.Mathematics;
using VVVV.DX11.Nodes.Renderers.Graphics.Touch;
using VVVV.HtmlTexture.DX11.Core;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.SlimDX;
using VVVV.Utils.VMath;

namespace HtmlTexture.DX11.Nodes
{
    public abstract class TouchOperationNode : PersistentOperationNode<SendTouchOperation>
    {
        public abstract bool IsChanged();
        public abstract IEnumerable<TouchContainer> GetTouches(int i);

        protected override void UpdateOps(ref SendTouchOperation ops, int i)
        {
            ops.Touches = GetTouches(i);
            ops.Execute = true;
        }
    }

    [PluginInfo(
        Name = "SendTouch",
        Category = "HtmlTexture.Operation",
        Version = "TouchContainer",
        Author = "MESO, microdee",
        Help = "TouchContainer provider for HtmlTexture"
    )]
    public class TouchContainerTouchOperationNode : TouchOperationNode
    {
        [Input("Touches", Order = 10, BinOrder = 11)]
        public ISpread<ISpread<TouchContainer>> FTouches;

        public override bool IsChanged()
        {
            return true;
        }

        protected override int SliceCount()
        {
            return SpreadUtils.SpreadMax(FTouches, FOpsIn);
        }

        public override IEnumerable<TouchContainer> GetTouches(int i)
        {
            return FTouches[i];
        }
    }

    public abstract class TouchGeneratorTouchOperationNode : TouchOperationNode
    {

        [Import] public IPluginHost2 PluginHost;
        [Import] public IHDEHost Host;

        [Input("Release After Frames", Order = 14, Visibility = PinVisibility.Hidden, DefaultValue = 2)]
        public ISpread<int> FRelease;

        [Output("Touches Out", Order = 10, BinOrder = 11)]
        public ISpread<ISpread<TouchContainer>> FTouches;

        protected Dictionary<int, TouchContainer>[] Touches = new Dictionary<int, TouchContainer>[0];

        protected float _prevFrameTime = 0;

        public override bool IsChanged()
        {
            return true;
        }

        protected abstract int SetSliceCount();
        protected abstract int SetTouchCount(int i);
        protected abstract (Vector2D p, int id) ProvideTouch(int i, int j);

        protected override int SliceCount()
        {
            return FTouches.SliceCount = SetSliceCount();
        }

        protected override void PreEvaluate(int sprmax)
        {

            float dt = (float)Host.FrameTime - _prevFrameTime;
            if (_prevFrameTime <= 0.00001) dt = 0;

            if (Touches.Length != sprmax)
            {
                var prev = Touches.Take(Math.Min(sprmax, Touches.Length)).ToArray();
                Touches = new Dictionary<int, TouchContainer>[sprmax];
                Touches.Fill(prev);
            }

            for (int i = 0; i < sprmax; i++)
            {
                var touches = Touches[i];
                if (touches == null)
                {
                    touches = new Dictionary<int, TouchContainer>();
                    Touches[i] = touches;
                }

                var removables = touches
                    .Where(kvp => kvp.Value.ExpireFrames > FRelease[0])
                    .Select(kvp => kvp.Key)
                    .ToArray();

                foreach (var id in removables)
                {
                    touches.Remove(id);
                }

                foreach (var touch in touches.Values)
                {
                    touch.Mainloop(dt);
                }

                var tsprmax = SetTouchCount(i);

                for (int j = 0; j < tsprmax; j++)
                {
                    var (p, id) = ProvideTouch(i, j);

                    if (touches.ContainsKey(id))
                    {
                        var touch = touches[id];
                        touch.Update(p.AsSystemVector(), dt);
                    }
                    else
                    {
                        var touch = new TouchContainer(id);
                        touch.Update(p.AsSystemVector(), dt);
                        touches.Add(id, touch);
                    }
                }
                FTouches[i].AssignFrom(touches.Values);
            }
        }

        public override IEnumerable<TouchContainer> GetTouches(int i)
        {
            return Touches[i].Values;
        }
    }

    [PluginInfo(
        Name = "SendTouch",
        Category = "HtmlTexture.Operation",
        Version = "Value",
        Author = "MESO, microdee",
        Help = "Simple touch provider for HtmlTexture"
    )]
    public class ValueTouchOperationNode : TouchGeneratorTouchOperationNode
    {

        [Input("Points", Order = 10, BinOrder = 11)]
        public ISpread<ISpread<Vector2D>> FPoints;
        [Input("Id", Order = 12, BinOrder = 13)]
        public ISpread<ISpread<int>> FIds;

        protected override int SetSliceCount()
        {
            return SpreadUtils.SpreadMax(FPoints, FIds);
        }

        protected override int SetTouchCount(int i)
        {
            return SpreadUtils.SpreadMax(FPoints[i], FIds[i]);
        }

        protected override (Vector2D p, int id) ProvideTouch(int i, int j)
        {
            return (FPoints[i][j], FIds[i][j]);
        }
    }

    [PluginInfo(
        Name = "SendTouch",
        Category = "HtmlTexture.Operation",
        Version = "DX11",
        Author = "MESO, microdee",
        Help = "Touch provider for HtmlTexture from a DX11 Renderer window"
    )]
    public class Dx11TouchOperationNode : TouchGeneratorTouchOperationNode
    {

        [Input("Touches", Order = 10, BinOrder = 11)]
        public Pin<TouchData> FPoints;

        protected override int SetSliceCount()
        {
            return 1;
        }

        protected override int SetTouchCount(int i)
        {
            if (!FPoints.IsConnected) return 0;
            return FPoints.SliceCount;
        }

        protected override (Vector2D p, int id) ProvideTouch(int i, int j)
        {
            if (FPoints[j] == null) return (Vector2D.Zero, -1);
            return (FPoints[j].Pos.ToVector2D(), FPoints[j].Id);
        }
    }
}
 