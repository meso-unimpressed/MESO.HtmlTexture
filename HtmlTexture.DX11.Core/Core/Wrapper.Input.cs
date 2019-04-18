using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Chromium;
using md.stdl.Coding;
using md.stdl.Interfaces;
using VVVV.Utils.IO;
using VVVV.Utils.VMath;

namespace VVVV.HtmlTexture.DX11.Core
{
    public struct HtmlTextureTouch
    {
        public int Id;
        public float X;
        public float Y;
        public float Force;
        public float Radius;
        public float Rotation;

        public HtmlTextureTouch(int id, float x, float y, float force, float radius, float rotation)
        {
            Id = id;
            X = x;
            Y = y;
            Force = force;
            Radius = radius;
            Rotation = rotation;
        }

        public void Deconstruct(out int id, out float x, out float y, out float force, out float radius, out float rotation)
        {
            id = Id;
            x = X;
            y = Y;
            force = Force;
            radius = Radius;
            rotation = Rotation;
        }
    }

    public partial class HtmlTextureWrapper : IMainlooping, IDisposable
    {
        private void ProcessTouches()
        {
            if (Closed) return;
            foreach (var touch in _touches.Values.ToList())
            {
                if (touch.Type == CfxTouchEventType.Released || touch.Type == CfxTouchEventType.Cancelled)
                {
                    _touches.Remove(touch.Id);
                    //touch.Dispose();
                }
                touch.Type = CfxTouchEventType.Released;
            }
            foreach (var (id, f, y, force, rad, rot) in SubmittedTouches.Values)
            {
                var ceftouch = new CfxTouchEvent
                {
                    PointerType = CfxPointerType.Touch,
                    Type = _touches.ContainsKey(id) ? CfxTouchEventType.Moved : CfxTouchEventType.Pressed,
                    Id = id,
                    X = (float)VMath.Map(f, -1.0, 1.0, 0.0, TextureSize.w, TMapMode.Float),
                    Y = (float)VMath.Map(y, 1.0, -1.0, 0.0, TextureSize.h, TMapMode.Float),
                    Pressure = force,
                    RadiusX = rad,
                    RadiusY = rad,
                    RotationAngle = rot
                };
                _touches.UpdateGeneric(id, ceftouch);
            }

            foreach (var touch in _touches.Values)
            {
                Browser.Host.SendTouchEvent(touch);
            }

            if(BrowserSettings.NoMouseMoveOnFirstTouch) return;

            var firsttouch = _touches.Values.OrderBy(t => t.Id).FirstOrDefault();
            if (firsttouch != null)
            {
                var me = new CfxMouseEvent
                {
                    X = (int)firsttouch.X,
                    Y = (int)firsttouch.Y
                };
                Browser.Host.SendMouseMoveEvent(me, false);
            }
        }

        public Mouse Mouse
        {
            set
            {
                if (_mouseSubscription == null)
                {
                    _mouseSubscription = new Subscription<Mouse, MouseNotification>(mouse => mouse.MouseNotifications, (mouse, n) =>
                    {
                        if (!Enabled || Browser == null) return;

                        var cfxMouseEvent = new CfxMouseEvent
                        {
                            X = (int)VMath.Map(n.Position.X, 0.0, n.ClientArea.Width, 0.0, TextureSize.w, 0),
                            Y = (int)VMath.Map(n.Position.Y, 0.0, n.ClientArea.Height, 0.0, TextureSize.h, 0)
                        };

                        var type = CfxMouseButtonType.Left;
                        if (n is MouseButtonNotification buttonNotification && (n.Kind == MouseNotificationKind.MouseUp || n.Kind == MouseNotificationKind.MouseDown))
                        {
                            switch (buttonNotification.Buttons)
                            {
                                case MouseButtons.Left:
                                    type = CfxMouseButtonType.Left;
                                    break;
                                case MouseButtons.Right:
                                    type = CfxMouseButtonType.Right;
                                    break;
                                case MouseButtons.Middle:
                                    type = CfxMouseButtonType.Middle;
                                    break;
                            }
                        }

                        var invvwheel = BrowserSettings.InvertScrollWheel ? -1 : 1;
                        var invhwheel = BrowserSettings.InvertHorizontalScrollWheel ? -1 : 1;
                        switch (n.Kind)
                        {
                            case MouseNotificationKind.MouseDown:
                                Browser.Host.SendMouseClickEvent(cfxMouseEvent, type, false, 1);
                                break;
                            case MouseNotificationKind.MouseUp:
                                Browser.Host.SendMouseClickEvent(cfxMouseEvent, type, true, 1);
                                break;
                            case MouseNotificationKind.MouseMove:
                                Browser.Host.SendMouseMoveEvent(cfxMouseEvent, false);
                                break;
                            case MouseNotificationKind.MouseWheel:
                                if (!(n is MouseWheelNotification wvn)) break;
                                //Browser.Host.SetFocus(true);
                                Browser.Host.SendMouseWheelEvent(cfxMouseEvent, 0, wvn.WheelDelta * invvwheel);
                                break;
                            case MouseNotificationKind.MouseHorizontalWheel:
                                if (!(n is MouseHorizontalWheelNotification whn)) break;
                                //Browser.Host.SetFocus(true);
                                Browser.Host.SendMouseWheelEvent(cfxMouseEvent, whn.WheelDelta * invhwheel, 0);
                                break;
                        }
                    });
                }
                _mouseSubscription.Update(value);
            }
        }

        public Keyboard Keyboard
        {
            set
            {
                if (_keyboardSubscription == null)
                {
                    _keyboardSubscription = new Subscription<Keyboard, KeyNotification>(
                        keyboard => keyboard.KeyNotifications,
                        (keyboard, n) =>
                        {
                            if (!Enabled || Browser == null)
                                return;
                            var keyevents = new CfxKeyEvent
                            {
                                Modifiers = (uint)_keyboard.Modifiers >> 15
                            };
                            switch (n.Kind)
                            {
                                case KeyNotificationKind.KeyDown:
                                    if (!(n is KeyDownNotification downNotification)) break;

                                    keyevents.Type = CfxKeyEventType.Keydown;
                                    keyevents.WindowsKeyCode = (int)downNotification.KeyCode;
                                    keyevents.NativeKeyCode = (int)downNotification.KeyCode;

                                    break;

                                case KeyNotificationKind.KeyPress:
                                    if (!(n is KeyPressNotification pressNotification)) break;

                                    keyevents.Type = CfxKeyEventType.Char;
                                    keyevents.Character = checked((short)pressNotification.KeyChar);
                                    keyevents.UnmodifiedCharacter = checked((short)pressNotification.KeyChar);
                                    keyevents.WindowsKeyCode = (int)pressNotification.KeyChar;
                                    keyevents.NativeKeyCode = (int)pressNotification.KeyChar;

                                    break;

                                case KeyNotificationKind.KeyUp:
                                    if (!(n is KeyUpNotification keyUpNotification)) break;

                                    keyevents.Type = CfxKeyEventType.Keyup;
                                    keyevents.WindowsKeyCode = (int)keyUpNotification.KeyCode;
                                    keyevents.NativeKeyCode = (int)keyUpNotification.KeyCode;

                                    break;
                            }

                            Browser.Host.SendKeyEvent(keyevents);
                        }
                    );
                }
                _keyboard = value;
                _keyboardSubscription.Update(value);
            }
        }
    }
}
