using DemiurgeLib.Common;
using System;
using System.Collections.Generic;

namespace DemiurgeLib
{
    public class SplineTree
    {
        // Values stored in spline control points are (in order) X, Y, altitude
        private List<CenCatRomSpline> splines;
        private IField2d<float> altitudes;
        private Random random;

        private float alpha = 0.5f;

        public SplineTree(TreeNode<Point2d> tree, IField2d<float> altitudes, Random random)
        {
            this.splines = new List<CenCatRomSpline>();
            this.altitudes = altitudes;
            this.random = random;

            var lastList = BuildSplinesRecursively(tree, null);
            lastList.Add(GetParentPoint(lastList));
            this.splines.Add(new CenCatRomSpline(lastList.ToArray(), this.alpha));

            this.altitudes = null;
            this.random = null;
        }

        private List<vFloat> BuildSplinesRecursively(TreeNode<Point2d> node, vFloat parentPoint)
        {
            vFloat herePoint = new vFloat(node.value.x + (float)this.random.NextDouble(), node.value.y + (float)this.random.NextDouble(), this.altitudes[node.value.y, node.value.x]);

            if (node.children.Count == 0)
            {
                // Idiotic case, tree has only one element and a spline is rediculous.
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

                int maxIdx = 0;
                foreach (var n in node.children)
                {
                    lsts.Add(BuildSplinesRecursively(n, herePoint));
                    lsts[lsts.Count - 1].Add(herePoint);

                    if (lsts[lsts.Count - 1].Count > lsts[maxIdx].Count)
                    {
                        maxIdx = lsts.Count - 1;
                    }
                }
                
                if (parentPoint == null)
                {
                    parentPoint = GetParentPoint(lsts[maxIdx]);
                }

                for (int idx = 0; idx < lsts.Count; idx++)
                {
                    if (idx != maxIdx)
                    {
                        lsts[idx].Add(parentPoint);
                        this.splines.Add(new CenCatRomSpline(lsts[idx].ToArray(), this.alpha));
                    }
                }

                return lsts[maxIdx];
            }
        }

        private vFloat GetParentPoint(List<vFloat> pts)
        {
            return 2f * pts[pts.Count - 1] - pts[pts.Count - 2];
        }

        public IEnumerable<vFloat> GetSamples(int resolution = 100)
        {
            foreach (var spline in splines)
                foreach (var sample in spline.GetSamples(resolution))
                    yield return sample;
        }
    }
}
