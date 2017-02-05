using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DemiurgeLib.NoiseAlgorithms;

namespace DemiurgeLib.Noise
{
    public class Simplex2D : Field2d<float>
    {
        public Simplex2D(int width, int height, float scale) : this(width, height, scale, new OpenSimplexNoise()) { }
        public Simplex2D(int width, int height, float scale, long seed) : this(width, height, scale, new OpenSimplexNoise(seed)) { }

        private Simplex2D(int width, int height, float scale, OpenSimplexNoise osn)
            : base(width, height)
        {
            for (int x = 0, y = 0; y < height; y += ++x / width, x %= width)
            {
                // OpenSimplexNoise produces doubles int the range (-1, 1); convert to normalized floats.
                double val = osn.Evaluate(x * scale, y * scale);
                this[y, x] = (float)(0.5 + val / 2.0);
            }
        }
    }
}
