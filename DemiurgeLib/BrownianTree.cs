using DemiurgeLib.Common;
using System;
using System.Collections.Generic;

namespace DemiurgeLib
{
    public class BrownianTree : Field2d<BrownianTree.Availability>
    {
        public enum Availability
        {
            Available,
            Unavailable,
            Illegal,
        }

        private Random random;

        public BrownianTree(IField2d<BrownianTree.Availability> baseField, Random random = null)
            : base(baseField)
        {
            this.random = random ?? new Random();
        }

        private BrownianTree(int width, int height) : base(width, height) { }

        public static BrownianTree CreateFromOther<T>(IField2d<T> baseField, Func<T, Availability> conversion, Random random = null)
        {
            BrownianTree tree = new BrownianTree(baseField.Width, baseField.Height);

            for (int x = 0, y = 0; y < tree.Height; y += ++x / tree.Width, x %= tree.Width)
            {
                tree[y, x] = conversion(baseField[y, x]);
            }

            tree.random = random ?? new Random();

            return tree;
        }

        // TODO: Porting all this super fast from old code, should remove this ASAP.
        public void RunDefaultTree(int minimumSensitivity = 12)
        {
            int stepSize = Math.Min(this.Width, this.Height);

            while (stepSize > minimumSensitivity)
            {
                float impact = AddBrownianTreePass(stepSize / 2, stepSize / 10 + 1, 5f, stepSize);

                if (impact < 0.05f) stepSize /= 2;
            }
        }

        public float AddBrownianTreePass(int sensitivity = 1, int maxMoveSize = 1, float maxSegment = 1f, int stepSize = 10)
        {
            List<Point2d> positions = new List<Point2d>();
            for (int j = 0; j < this.Height; j += stepSize)
            {
                for (int i = 0; i < this.Width; i += stepSize)
                {
                    positions.Add(new Point2d(i + random.Next(stepSize), j + random.Next(stepSize)));
                }
            }

            float cellCount = positions.Count;
            int impactCount = 0;

            while (positions.Count > 0)
            {
                int idx = random.Next(positions.Count);
                Point2d p = positions[idx];
                positions.RemoveAt(idx);
                Point2d t = Point2d.zero;

                // If we're already close enough to another point that we don't have to move to reach it, don't bother.
                if (ShouldStop(p, sensitivity, random, ref t))
                {
                    continue;
                }
                
                while (IsLegal(p, sensitivity) && !ShouldStop(p, sensitivity, random, ref t))
                {
                    p = MoveOnce(p, maxMoveSize);
                }

                if (!IsLegal(p, sensitivity))
                {
                    continue;
                }

                // Draw a line from the stopping point to the place it "hit"
                for (float f = Math.Max(0f, 1f - maxSegment / Point2d.Distance(t, p)); f <= 1f; f += 0.5f / sensitivity)
                {
                    int i = (int)((1f - f) * p.x + f * t.x);
                    int j = (int)((1f - f) * p.y + f * t.y);
                    this[j, i] = Availability.Unavailable;
                }

                impactCount++;
            }

            return impactCount / cellCount;
        }

        private bool IsLegal(Point2d pos, int sensitivity)
        {
            int x = (int)pos.x;
            int y = (int)pos.y;

            // Determine that we aren't off the field.
            if (x < 0 || y < 0 || x >= this.Width || y >= this.Height)
            {
                return false;
            }

            // Determine that this value is available.
            if (this[y, x] == Availability.Unavailable)
            {
                return false;
            }

            // Determine that we are not too close to an illegal areas, which repel placement.
            // TODO: pre-process map to find illegal placements upfront, then compare against those instead.
            for (int j = (int)Math.Max(0, y - sensitivity); j < Math.Min(this.Height, y + sensitivity + 1); j++)
            {
                for (int i = (int)Math.Max(0, x - sensitivity); i < Math.Min(this.Width, x + sensitivity + 1); i++)
                {
                    if (Point2d.SqDist(new Point2d(i, j), pos) < sensitivity * sensitivity && this[j, i] == Availability.Illegal)
                    {
                        return false;
                    }
                }
            }

            // Placement is legal.
            return true;
        }

        // TODO: This was ported from the old implementation.  It might be the most inefficient thing ever.
        private bool ShouldStop(Point2d pos, int sensitivity, System.Random random, ref Point2d ret)
        {
            List<Point2d> candidates = new List<Point2d>();

            // Assume placement is legal, then check to see if there are any values close enough to draw lines to.
            for (int j = (int)Math.Max(0, pos.y - sensitivity); j < Math.Min(this.Height, pos.y + sensitivity + 1); j++)
            {
                for (int i = (int)Math.Max(0, pos.x - sensitivity); i < Math.Min(this.Width, pos.x + sensitivity + 1); i++)
                {
                    Point2d pt = new Point2d(i, j);
                    if (Point2d.SqDist(pt, pos) < sensitivity * sensitivity && this[j, i] == Availability.Unavailable)
                    {
                        candidates.Add(pt);
                    }
                }
            }

            // If there are many candidates, choose one at random.  Otherwise, return nothing.
            if (candidates.Count > 0)
            {
                ret = candidates[random.Next(candidates.Count)];
                return true;
            }
            return false;
        }

        private Point2d MoveOnce(Point2d pos, int maxStepSize = 1)
        {
            return pos + new Point2d(random.Next(2 * maxStepSize + 1) - maxStepSize, random.Next(2 * maxStepSize + 1) - maxStepSize);
        }
    }
}
