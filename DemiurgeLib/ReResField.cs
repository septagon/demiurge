using System;

namespace DemiurgeLib.Common
{
    public class ReResField : SubContinuum<float>
    {
        public ReResField(IField2d<float> src, float scale)
            : base((int)(src.Width * scale), (int)(src.Height * scale), new ContinuousField(src), new System.Drawing.Rectangle(0, 0, src.Width, src.Height))
        { }
    }
}
