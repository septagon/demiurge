using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DemiurgeLib.NoiseAlgorithms;

namespace DemiurgeLib.Noise
{
    public class DiamondSquare2D : Field2d<float>
    {
        public DiamondSquare2D(int width, int height, float roughness = 1f, Func<double, double> decayFunction = null, System.Random random = null)
            : base(width, height)
        {
            DiamondSquareNoise.DiamondSquareArguments args = new DiamondSquareNoise.DiamondSquareArguments();
            args.width = width;
            args.height = height;
            args.roughness = roughness;
            args.decayFunction = decayFunction;
            args.random = random;
            double[,] noise = DiamondSquareNoise.GenerateNoise(args);

            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;

            for (int x = 0, y = 0; y < this.Height; y += ++x / this.Width, x %= this.Width)
            {
                this[y, x] = (float)noise[y, x];

                min = Math.Min(min, this[y, x]);
                max = Math.Max(max, this[y, x]);
            }
        }
    }
}
