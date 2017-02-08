using System;
using System.Linq;
using System.Collections.Generic;

namespace DemiurgeLib.Common
{
    public class ContiguousSets
    {
        public static Dictionary<T, HashSet<PointSet2d>> FindContiguousSets<T>(IField2d<T> field)
        {
            Dictionary<T, HashSet<PointSet2d>> categoryToSets = new Dictionary<T, HashSet<PointSet2d>>();

            // At the beginning, all points are unaffiliated.
            PointSet2d unaffiliated = new PointSet2d();
            for (int x = 0, y = 0; y < field.Height; y += ++x / field.Width, x %= field.Width)
            {
                unaffiliated.Add(new Point2d(x, y));
            }

            Point2d[] neighbors = new Point2d[8];

            for (int x = 0, y = 0; y < field.Height; y += ++x / field.Width, x %= field.Width)
            {
                Point2d p = new Point2d(x, y);
                if (unaffiliated.Contains(p))
                {
                    // Start a new contiguous set.
                    PointSet2d contiguousSet = new PointSet2d();

                    T key = field[p.y, p.x];

                    // Only points which (1) are unaffiliated and (2) have not been considered
                    // for this set are allowed to be candidates.
                    Queue<Point2d> candidates = new Queue<Point2d>();
                    PointSet2d considered = new PointSet2d();

                    candidates.Enqueue(p);
                    considered.Add(p);

                    while (candidates.Count > 0)
                    {
                        p = candidates.Dequeue();

                        if (Object.Equals(field[p.y, p.x], key))
                        {
                            unaffiliated.Remove(p);
                            contiguousSet.Add(p);

                            Utils.SetNeighbors(p, ref neighbors);
                            for (int idx = 0; idx < neighbors.Length; idx++)
                            {
                                p = neighbors[idx];
                                if (unaffiliated.Contains(p) && !considered.Contains(p))
                                {
                                    candidates.Enqueue(p);
                                    considered.Add(p);
                                }
                            }
                        }
                    }

                    HashSet<PointSet2d> sets;
                    if (!categoryToSets.TryGetValue(key, out sets))
                    {
                        sets = new HashSet<PointSet2d>();
                        categoryToSets.Add(key, sets);
                    }
                    sets.Add(contiguousSet);
                }
            }

            return categoryToSets;
        }

        public static TreeNode<Point2d> MakeTreeFromContiguousSet(PointSet2d pointSet, Func<Point2d, bool> isRoot)
        {
            Point2d? rootPt = null;
            foreach (var pt in pointSet)
            {
                if (isRoot(pt))
                {
                    rootPt = pt;
                }
            }

            if (!rootPt.HasValue)
            {
                return null;
            }
            else
            {
                return Utils.MakeBfsTree(rootPt.Value, pt => pointSet.Contains(pt));
            }
        }
    }
}
