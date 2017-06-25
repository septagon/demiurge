using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemiurgeLib.Common
{
    public class ReResField : IField2d<float>
    {
        private IField2d<float> source;
        private float scale;
        public int Height { get; private set; }
        public int Width { get; private set; }

        public ReResField(IField2d<float> src, float scale)
        {
            this.Width = (int)(src.Width * scale);
            this.Height = (int)(src.Height * scale);
            this.scale = (float)this.Width / src.Width;
            this.source = src;
        }

        /// <summary>
        /// Uses a rather naive algorithm, particularly for downres.  Lazy evaluation is memory optimal.
        /// </summary>
        public float this[int y, int x]
        {
            get
            {
                float i = x / this.scale - 0.5f;
                float j = y / this.scale - 0.5f;

                int iMin = Math.Max(0, (int)Math.Floor(i));
                int jMin = Math.Max(0, (int)Math.Floor(j));
                int iMax = Math.Min(iMin + 1, this.source.Width - 1);
                int jMax = Math.Min(jMin + 1, this.source.Height - 1);

                float ul = this.source[jMin, iMin];
                float ur = this.source[jMin, iMax];
                float ll = this.source[jMax, iMin];
                float lr = this.source[jMax, iMax];

                float tx = 1f - (i - iMin);
                float ty = 1f - (j - jMin);
                
                float xLerp = ((ul + ll) * tx + (ur + lr) * (1f - tx)) / 2f;
                float yLerp = ((ul + ur) * ty + (ll + lr) * (1f - ty)) / 2f;
                return (xLerp + yLerp) / 2f;
            }
        }
    }
}
