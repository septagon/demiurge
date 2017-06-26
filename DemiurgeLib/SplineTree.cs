using DemiurgeLib.Common;
using System;
using System.Collections.Generic;

namespace DemiurgeLib
{
    class SplineTree
    {
        private List<CenCatRomSpline> splines;
        private Random random;

        public SplineTree(TreeNode<Point2d> tree, Random random)
        {
            this.splines = new List<CenCatRomSpline>();
            this.random = random;

            BuildSplinesRecursively(tree, null);
        }

        private List<vFloat> BuildSplinesRecursively(TreeNode<Point2d> node, vFloat parentPoint)
        {
            vFloat herePoint = new vFloat(node.value.x + (float)this.random.NextDouble(), node.value.y + (float)this.random.NextDouble());

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
                List<vFloat> lst = BuildSplinesRecursively(node.children.GetEnumerator().Current, herePoint);
                lst.Add(new vFloat(node.value.x + (float)this.random.NextDouble(), node.value.y + (float)this.random.NextDouble()));
                return lst;
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
                    parentPoint = 2f * lsts[maxIdx][lsts[maxIdx].Count - 1] - lsts[maxIdx][lsts[maxIdx].Count - 2];
                }

                for (int idx = 0; idx < lsts.Count; idx++)
                {
                    if (node.parent == null || idx != maxIdx)
                    {
                        lsts[idx].Add(parentPoint);
                        this.splines.Add(new CenCatRomSpline(lsts[idx].ToArray(), 0.5f));
                    }
                }

                if (node.parent == null)
                {
                    return null;
                }
                else
                {
                    return lsts[maxIdx];
                }
            }
        }

        private IEnumerable<vFloat> GetSamples(int resolution = 100)
        {
            foreach (var spline in splines)
                foreach (var sample in spline.GetSamples(resolution))
                    yield return sample;
        }
    }
}
