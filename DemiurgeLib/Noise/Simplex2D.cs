using DemiurgeLib.NoiseAlgorithms;

namespace DemiurgeLib.Noise
{
    public class Simplex2D : Field2d<float>
    {
        public Simplex2D(int width, int height, float scale, int offsetX = 0, int offsetY = 0)
            : this(width, height, scale, offsetX, offsetY, new OpenSimplexNoise()) { }

        public Simplex2D(int width, int height, float scale, long seed, int offsetX = 0, int offsetY = 0)
            : this(width, height, scale, offsetX, offsetY, new OpenSimplexNoise(seed)) { }

        private Simplex2D(int width, int height, float scale, int offsetX, int offsetY, OpenSimplexNoise osn)
            : base(width, height)
        {
            for (int x = 0, y = 0; y < height; y += ++x / width, x %= width)
            {
                // OpenSimplexNoise produces doubles int the range (-1, 1); convert to normalized floats.
                double val = osn.Evaluate((x + offsetX) * scale, (y + offsetY) * scale);
                this[y, x] = (float)(0.5 + val / 2.0);
            }
        }
    }
}
