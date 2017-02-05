using DemiurgeLib.Common;
using System;

namespace DemiurgeLib
{
    public class HydrologicalField : Transformation2d<BrownianTree.Availability, HydrologicalField.LandType>
    {
        public enum LandType
        {
            Land,
            Shore,
            Ocean,
        }

        public HydrologicalField(IField2d<BrownianTree.Availability> field, int sensitivity = 7, float shoreThreshold = 0.5f)
            : base(field, (x, y, val) => Classify(field, x, y, sensitivity, shoreThreshold)) { }

        private static LandType Classify(IField2d<BrownianTree.Availability> field, int x, int y, int sensitivity, float shoreThreshold)
        {
            if (field[y, x] == BrownianTree.Availability.Available)
            {
                return LandType.Land;
            }

            Point2d pos = new Point2d(x, y);
            float landNum = 0f;
            float totalNum = 0f;

            for (int j = (int)Math.Max(0, y - sensitivity); j < Math.Min(field.Height, y + sensitivity + 1); j++)
            {
                for (int i = (int)Math.Max(0, x - sensitivity); i < Math.Min(field.Width, x + sensitivity + 1); i++)
                {
                    if (Point2d.SqDist(new Point2d(i, j), pos) < sensitivity * sensitivity)
                    {
                        if (field[j, i] == BrownianTree.Availability.Available)
                        {
                            landNum++;
                        }
                        totalNum++;
                    }
                }
            }

            if (landNum / totalNum > shoreThreshold)
            {
                return LandType.Shore;
            }

            return LandType.Ocean;
        }
    }
}
