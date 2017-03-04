using DemiurgeLib.Common;
using System;
using System.Collections.Generic;

namespace DemiurgeLib.Noise
{
    public class MountainNoise : Field2d<float>
    {
        public MountainNoise(int width, int height, float startingScale) : this(width, height, startingScale, System.DateTime.Now.Ticks) { }

        public MountainNoise(int width, int height, float startingScale, long seed, int offsetX = 0, int offsetY = 0) : base(width, height)
        {
            List<IField2d<float>> octaves = new List<IField2d<float>>();

            for (int idx = 0; idx < 10; idx++)
            {
                float xi = (float)Math.Pow(2, idx);
                octaves.Add(new ScaleTransform(new InvertTransform(new AbsTransform(new Noise.Simplex2D(width, height, startingScale * xi, seed, offsetX, offsetY))), 1f / xi));
            }

            // Note: this does an allocation and a copy that aren't strictly speaking necessary.  While it does
            // prevent the fragmenting of replication behavior in fields, it may be a win overall to just do the
            // replication here directly to avoid unnecessary operations.
            this.Replicate(new Field2d<float>(new NormalizedComposition2d<float>(octaves.ToArray())));
        }
    }
}
