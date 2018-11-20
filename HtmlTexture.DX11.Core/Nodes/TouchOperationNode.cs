﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Coding;
using md.stdl.Interaction;
using md.stdl.Mathematics;
using Notui;
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
        public abstract IEnumerable<HtmlTextureTouch> GetTouches(int i);

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
        Tags = "Vanadium",
        Author = "MESO, microdee",
        Help = "TouchContainer provider for HtmlTexture"
    )]
    public class TouchContainerTouchOperationNode : TouchOperationNode
    {
        [Input("Touches", Order = 10, BinOrder = 11)]
        public ISpread<ISpread<TouchContainer>> FTouches;

        public override bool IsChanged() => true;

        protected override int SliceCount()
        {
            return SpreadUtils.SpreadMax(FTouches, FOpsIn);
        }

        public override IEnumerable<HtmlTextureTouch> GetTouches(int i)
        {
            return FTouches[i].Where(t=> t != null).Select(t => new HtmlTextureTouch(t.Id, t.Point.X, t.Point.Y, t.Force, 5.0f, 0.0f));
        }
    }

    [PluginInfo(
        Name = "SendTouch",
        Category = "HtmlTexture.Operation",
        Version = "Notui.Element",
        Tags = "Vanadium",
        Author = "MESO, microdee",
        Help = "Provides touches for HtmlTexture from a Notui element"
    )]
    public class NotuiElementTouchOperationNode : TouchOperationNode
    {
        [Input("Element", Order = 10)]
        public ISpread<NotuiElement> ElementIn;
        [Input("Use Element Space", Order = 11)]
        public ISpread<bool> UseElementSpaceIn;

        public override bool IsChanged() => true;

        protected override int SliceCount()
        {
            return SpreadUtils.SpreadMax(ElementIn, FOpsIn);
        }

        public override IEnumerable<HtmlTextureTouch> GetTouches(int i)
        {
            if (ElementIn[i] == null) return Enumerable.Empty<HtmlTextureTouch>();
            return ElementIn[i].Touching.Values.Select(t => new HtmlTextureTouch(
                t.Touch.Id,
                UseElementSpaceIn[i] ? t.ElementSpace.X : t.SurfaceSpace.X,
                UseElementSpaceIn[i] ? t.ElementSpace.Y : t.SurfaceSpace.Y,
                t.Touch.Force,
                5.0f,
                0.0f)
            );
        }
    }

    [PluginInfo(
        Name = "SendTouch",
        Category = "HtmlTexture.Operation",
        Version = "Value",
        Tags = "Vanadium",
        Author = "MESO, microdee",
        Help = "Simple touch provider for HtmlTexture"
    )]
    public class ValueTouchOperationNode : TouchOperationNode
    {

        [Input("Points", Order = 10, BinOrder = 11)]
        public IDiffSpread<ISpread<Vector2D>> FPoints;
        [Input("Force", Order = 12, BinOrder = 13, DefaultValue = 1.0)]
        public IDiffSpread<ISpread<float>> FForce;
        [Input("Radius", Order = 14, BinOrder = 15, DefaultValue = 5.0)]
        public IDiffSpread<ISpread<float>> FRadius;
        [Input("Rotation", Order = 16, BinOrder = 17)]
        public IDiffSpread<ISpread<float>> FRotation;
        [Input("Id", Order = 18, BinOrder = 19)]
        public IDiffSpread<ISpread<int>> FIds;

        protected override int SliceCount()
        {
            return FIds.SliceCount;
        }

        public override bool IsChanged() => true;

        public override IEnumerable<HtmlTextureTouch> GetTouches(int i)
        {
            for (int j = 0; j < FIds[i].SliceCount; j++)
            {
                yield return new HtmlTextureTouch(
                    FIds[i][j],
                    (float) FPoints[i][j].x,
                    (float) FPoints[i][j].y,
                    FForce[i][j],
                    FRadius[i][j],
                    FRotation[i][j]
                );
            }
        }
    }

    [PluginInfo(
        Name = "SendTouch",
        Category = "HtmlTexture.Operation",
        Version = "DX11",
        Tags = "Vanadium",
        Author = "MESO, microdee",
        Help = "Touch provider for HtmlTexture from a DX11 Renderer window"
    )]
    public class Dx11TouchOperationNode : TouchOperationNode
    {

        [Input("Touches", Order = 10, BinOrder = 11)]
        public Pin<TouchData> FPoints;

        public override bool IsChanged() => true;

        protected override int SliceCount()
        {
            return Math.Max(1, FOpsIn.SliceCount);
        }

        public override IEnumerable<HtmlTextureTouch> GetTouches(int i)
        {
            foreach (var touch in FPoints)
            {
                if(touch == null) continue;
                yield return new HtmlTextureTouch(
                    touch.Id,
                    touch.Pos.X,
                    touch.Pos.Y,
                    1.0f,
                    5.0f,
                    0.0f
                );
            }
        }
    }
}
 