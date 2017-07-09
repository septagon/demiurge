using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemiurgeLib.Noise
{
    public class ContinuousMountainNoise : IField2d<float>, IContinuum2d<float>
    {
        private const int OCTAVE_COUNT = 10;

        private float[] reciprocalWeights;
        private float totalWeight;
        private IContinuum2d<float>[] octaves;

        public ContinuousMountainNoise(int width, int height, float scale = 0.005f, int offsetX = 0, int offsetY = 0, Func<int, float> getOctaveWeight = null)
            : this(width, height, scale, System.DateTime.UtcNow.Ticks, offsetX, offsetY, getOctaveWeight ?? (o => (float)Math.Pow(2, o))) { }

        public ContinuousMountainNoise(int width, int height, float scale, long seed, int offsetX, int offsetY, Func<int, float> getOctaveWeight)
        {
            this.reciprocalWeights = new float[OCTAVE_COUNT];
            this.totalWeight = 0f;
            this.octaves = new IContinuum2d<float>[OCTAVE_COUNT];

            for (int idx = 0; idx < OCTAVE_COUNT; idx++)
            {
                this.reciprocalWeights[idx] = getOctaveWeight(idx);
                this.totalWeight +=  1f / this.reciprocalWeights[idx];
                this.octaves[idx] = (new Noise.ContinuousSimplexNoise(width, height, scale * this.reciprocalWeights[idx], seed, offsetX, offsetY));
            }
        }

        public float this[float y, float x]
        {
            get
            {
                float value = 0f;
                for (int idx = 0; idx < this.octaves.Length; idx++)
                {
                    value += (1f - Math.Abs(this.octaves[idx][y, x] - 0.5f) * 2f) / this.reciprocalWeights[idx];
                }

                return value / this.totalWeight;
            }
        }

        public float this[int y, int x]
        {
            get
            {
                return this[(float)y, (float)x];
            }
        }

        public int Height
        {
            get
            {
                return this.octaves[0].Height;
            }
        }

        public int Width
        {
            get
            {
                return this.octaves[0].Width;
            }
        }
    }
}
