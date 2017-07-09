using DemiurgeLib.NoiseAlgorithms;

namespace DemiurgeLib.Noise
{
    class ContinuousSimplexNoise : IField2d<float>, IContinuum2d<float>
    {
        private OpenSimplexNoise noise;
        private float offsetX;
        private float offsetY;
        private float scale;
        private int width;
        private int height;

        public ContinuousSimplexNoise(int width, int height, float scale = 0.005f, float offsetX = 0, float offsetY = 0)
            : this(width, height, scale, System.DateTime.UtcNow.Ticks, offsetX, offsetY) { }

        public ContinuousSimplexNoise(int width, int height, float scale, long seed, float offsetX, float offsetY)
        {
            this.width = width;
            this.height = height;
            this.noise = new OpenSimplexNoise(seed);
            this.offsetX = offsetX;
            this.offsetY = offsetY;
            this.scale = scale;
        }

        public float this[float y, float x]
        {
            get
            {
                double val = noise.Evaluate((x + offsetX) * scale, (y + offsetY) * scale);
                return (float)(0.5 + val / 2.0);
            }
        }

        public float this[int y, int x]
        {
            get
            {
                double val = noise.Evaluate((x + offsetX) * scale, (y + offsetY) * scale);
                return (float)(0.5 + val / 2.0);
            }
        }

        public int Height
        {
            get
            {
                return this.height;
            }
        }

        public int Width
        {
            get
            {
                return this.width;
            }
        }
    }
}
