using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemiurgeLib.Common
{
    public class ReResField : Field2d<float>
    {
        public ReResField(IField2d<float> src, float scale) : base((int)(src.Width * scale), (int)(src.Height * scale))
        {
            scale = (float)this.Width / src.Width;

            for (int x = 0, y = 0; y < this.Height; y += ++x / this.Width, x %= this.Width)
            {
                float i = x / scale;
                float j = y / scale;

                int iMin = (int)Math.Floor(i);
                int jMin = (int)Math.Floor(j);
                int iMax = (int)Math.Min(iMin + 1, src.Width - 1);
                int jMax = (int)Math.Min(jMin + 1, src.Height - 1);

                float ul = src[jMin, iMin];
                float ur = src[jMin, iMax];
                float ll = src[jMax, iMin];
                float lr = src[jMax, iMax];

                float tx = 1f - (i - iMin);
                float ty = 1f - (j - jMin);

                this[y, x] = ((ul + ll) * tx + (ur + lr) * (1f - tx)) / 2f;
            }
        }
    }
}
