using DemiurgeLib.Common;
using System;
using System.Collections.Generic;

namespace DemiurgeLib
{
    public class SplineTree
    {
        // Values stored in spline control points are (in order) X, Y, altitude, size
        private List<CenCatRomSpline> splines;
        private IField2d<float> altitudes;
        private Random random;

        private int minSizeForFork;
        private float alpha;

        public const float CAPACITY_DIVISOR = 10000f;

        public SplineTree(TreeNode<Point2d> tree, IField2d<float> altitudes, Random random, int minSizeForFork = 3, float alpha = 0.5f)
        {
            this.splines = new List<CenCatRomSpline>();
            this.altitudes = altitudes;
            this.random = random;

            this.minSizeForFork = minSizeForFork;
            this.alpha = alpha;

            var lastList = BuildSplinesRecursively(tree, null);
            lastList.Add(GetParentPoint(lastList));
            this.splines.Add(new CenCatRomSpline(lastList.ToArray(), this.alpha));

            this.altitudes = null;
            this.random = null;
        }

        private List<vFloat> BuildSplinesRecursively(TreeNode<Point2d> node, vFloat parentPoint)
        {
            vFloat herePoint = new vFloat(
                node.value.x + (float)this.random.NextDouble(), // X position in world coordinates
                node.value.y + (float)this.random.NextDouble(), // Y position in world coordinates
                this.altitudes[node.value.y, node.value.x],     // altitude in whatever units were used by the "altitudes" input
                node.Size() / CAPACITY_DIVISOR);                // river size, meaning the count of pixels above this, divided to avoid affecting the spline

            if (node.children.Count == 0 || (node.children.Count > 1 && node.Depth() <= minSizeForFork))
            {
                // Idiotic case, tree has only one element and a spline is ridiculous.
                if (node.parent == null)
                {
                    throw new ArgumentException("A tree with only one node cannot be made into a spline tree.");
                }

                List<vFloat> lst = new List<vFloat>();
                lst.Add(2 * herePoint - parentPoint);
                lst.Add(herePoint);
                return lst;
            }
            else if (node.children.Count == 1)
            {
                // We know there's only one because we just checked; we just use this syntax to get that one out of the children.
                foreach (var child in node.children)
                {
                    List<vFloat> lst = BuildSplinesRecursively(child, herePoint);
                    lst.Add(herePoint);
                    return lst;
                }
                throw new InvalidOperationException("A non-empty list behaved as if it was empty.  Has a race condition been introduced?");
            }
            else
            {
                List<List<vFloat>> lsts = new List<List<vFloat>>();

                int minIdx = 0;
                foreach (var n in node.children)
                {
                    if (n.Size() < minSizeForFork)
                    {
                        continue;
                    }

                    lsts.Add(BuildSplinesRecursively(n, herePoint));
                    lsts[lsts.Count - 1].Add(herePoint);

                    if (lsts[lsts.Count - 1].Count < lsts[minIdx].Count)
                    {
                        minIdx = lsts.Count - 1;
                    }
                }
                
                if (parentPoint == null)
                {
                    parentPoint = GetParentPoint(lsts[minIdx]);
                }

                for (int idx = 0; idx < lsts.Count; idx++)
                {
                    if (idx != minIdx)
                    {
                        lsts[idx].Add(parentPoint);
                        lsts[idx].Add(2 * lsts[idx][lsts[idx].Count - 2] - parentPoint);
                        this.splines.Add(new CenCatRomSpline(lsts[idx].ToArray(), this.alpha));
                    }
                }

                return lsts[minIdx];
            }
        }

        private static vFloat GetParentPoint(List<vFloat> pts)
        {
            return 2f * pts[pts.Count - 1] - pts[pts.Count - 2];
        }

        public IEnumerable<vFloat> GetSamplesFromAll(int sampleCount = 100)
        {
            foreach (var spline in this.splines)
                foreach (var sample in spline.GetSamples(sampleCount))
                    yield return sample;
        }

        public IEnumerable<vFloat> GetSamplesPerControlPoint(float samplesPerControlPoint = 3f)
        {
            foreach (var spline in this.splines)
                foreach (var sample in spline.GetSamplesPerControlPoint(samplesPerControlPoint))
                    yield return sample;
        }
    }
}
