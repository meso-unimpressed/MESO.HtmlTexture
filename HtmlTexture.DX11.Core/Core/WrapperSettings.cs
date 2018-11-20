using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Interfaces;

namespace VVVV.HtmlTexture.DX11.Core
{
    public enum UrlFilterMode
    {
        None,
        BlacklistMatch,
        WhitelistMatch,
        BlacklistContains,
        WhitelistContains,
        BlacklistRegex,
        WhitelistRegex
    }
    public partial class HtmlTextureWrapper : IMainlooping, IDisposable
    {
        public struct WrapperTextureSettings
        {
            public bool AutoWidth;
            public bool AutoHeight;
            public (int w, int h) ExtraSize;
            public (int w, int h) TargetSize;

            public override bool Equals(object obj)
            {
                var obj1 = obj;
                object obj2;
                if (obj1 == null || !((obj2 = obj1) is WrapperTextureSettings))
                    return false;
                var other = (WrapperTextureSettings)obj2;
                return AutoWidth == other.AutoWidth &&
                       AutoHeight == other.AutoHeight &&
                       ExtraSize.w == other.ExtraSize.w &&
                       ExtraSize.h == other.ExtraSize.h &&
                       TargetSize.w == other.TargetSize.w &&
                       TargetSize.h == other.TargetSize.h;
            }
        }

        public struct WrapperBrowserSettings
        {
            public bool UseLivePage;
            public bool AllowPopups;
            public string UserAgent;
            public bool ListenConsole;
            public double ZoomLevel;
            public bool InvertScrollWheel;
            public bool InvertHorizontalScrollWheel;
            public UrlFilterMode UrlFilterMode;
            public string DocumentSizeElementSelector;
        }

        public class WrapperInitSettings
        {
            public int Fps { get; set; } = 60;
            public int ParentHandle { get; set; } = 0;
            public bool FrameRequestFromVvvv = false;
        }
    }
}
