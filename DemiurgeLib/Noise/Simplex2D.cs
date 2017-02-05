using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DemiurgeLib.OpenSimplex;

namespace DemiurgeLib.Noise
{
    public class Simplex2D : I2dField<float>
    {
        private float[,] values;

        public Simplex2D(int width, int height, float scale) : this(width, height, scale, new OpenSimplexNoise()) { }
        public Simplex2D(int width, int height, float scale, long seed) : this(width, height, scale, new OpenSimplexNoise(seed)) { }

        private Simplex2D(int width, int height, float scale, OpenSimplexNoise osn)
        {
            this.values = new float[height, width];

            for (int x = 0, y = 0; y < height; y += ++x / width, x %= width)
            {
                // OpenSimplexNoise produces doubles int the range (-1, 1); convert to normalized floats.
                double val = osn.Evaluate(x * scale, y * scale);
                values[y, x] = (float)(0.5 + val / 2.0);
            }
        }

        public float this[int y, int x]
        {
            get
            {
                return values[y, x];
            }
        }

        public int Width
        {
            get
            {
                return values.GetLength(1);
            }
        }

        public int Height
        {
            get
            {
                return values.GetLength(0);
            }
        }
    }
}
