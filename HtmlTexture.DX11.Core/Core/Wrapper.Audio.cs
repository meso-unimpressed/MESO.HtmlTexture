using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chromium;
using md.stdl.Interfaces;

namespace VVVV.HtmlTexture.DX11.Core
{
    public class HtmlAudioStream
    {
        public int AudioStreamId { get; set; }
        public CfxChannelLayout ChannelLayout { get; set; }
        public int Channels { get; set; }
        public int FramesPerBuffer { get; set; }
        public int Frames { get; set; }
        public int SampleRate { get; set; }
        public long TimeStamp { get; set; }

        // TODO: Expose audio buffers
    }
}
