using DemiurgeLib;
using DemiurgeLib.Common;
using DemiurgeLib.Noise;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DemiurgeConsole
{
    public partial class Utils
    {
        public static List<Point2d> GetSettlementLocations(WaterTableField wtf, IField2d<float> wateriness,
            float noiseScale = 0.1f, float noiseBlur = 3f, float noiseContribution = 0.15f)
        {
            IField2d<float> noise = new BlurredField(new MountainNoise(wtf.Width, wtf.Height, noiseScale), noiseBlur);
            Transformation2d<float, float, float> combination = new Transformation2d<float, float, float>(wateriness, noise, (w, n) => w + noiseContribution * n);

            List<Point2d> locations = new List<Point2d>();

            for (int y = 1; y < combination.Height - 1; y++)
            {
                for (int x = 1; x < combination.Width - 1; x++)
                {
                    if (wtf[y, x] > 0f &&
                        combination[y - 1, x - 1] < combination[y, x] &&
                        combination[y - 1, x + 0] < combination[y, x] &&
                        combination[y - 1, x + 1] < combination[y, x] &&
                        combination[y + 0, x + 1] < combination[y, x] &&
                        combination[y + 1, x + 1] < combination[y, x] &&
                        combination[y + 1, x + 0] < combination[y, x] &&
                        combination[y + 1, x - 1] < combination[y, x] &&
                        combination[y + 0, x - 1] < combination[y, x])
                    {
                        locations.Add(new Point2d(x, y));
                    }
                }
            }

            return locations;
        }
    }
}
