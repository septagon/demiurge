using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemiurgeLib
{
    public class ContinuousField : IField2d<float>, IContinuum2d<float>
    {
        private IField2d<float> source;

        public ContinuousField(IField2d<float> source)
        {
            this.source = source;
        }
        
        public float this[float y, float x]
        {
            get
            {
                // This is based on the naive algorithm for the re-res field.
                int xMin = Math.Max(0, (int)Math.Floor(x));
                int yMin = Math.Max(0, (int)Math.Floor(y));
                int xMax = Math.Min(xMin + 1, this.source.Width - 1);
                int yMax = Math.Min(yMin + 1, this.source.Height - 1);

                float ul = this.source[yMin, xMin];
                float ur = this.source[yMin, xMax];
                float ll = this.source[yMax, xMin];
                float lr = this.source[yMax, xMax];

                float tx = 1f - (x - xMin);
                float ty = 1f - (y - yMin);

                float xLerp = ((ul + ll) * tx + (ur + lr) * (1f - tx)) / 2f;
                float yLerp = ((ul + ur) * ty + (ll + lr) * (1f - ty)) / 2f;
                return (xLerp + yLerp) / 2f;
            }
        }

        public float this[int y, int x]
        {
            get
            {
                return this.source[y, x];
            }
        }

        public int Height
        {
            get
            {
                return this.source.Height;
            }
        }

        public int Width
        {
            get
            {
                return this.source.Width;
            }
        }
    }
}
