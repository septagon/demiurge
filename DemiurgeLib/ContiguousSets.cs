using System;
using System.Linq;
using System.Collections.Generic;

namespace DemiurgeLib.Common
{
    public class ContiguousSets
    {
        public class TreeNode<T>
        {
            public T value;
            public TreeNode<T> parent;
            public HashSet<TreeNode<T>> children;

            public TreeNode(T value, TreeNode<T> parent = null)
            {
                this.value = value;
                this.parent = parent;
                this.children = new HashSet<TreeNode<T>>();

                if (this.parent != null)
                {
                    this.parent.children.Add(this);
                }
            }

            public void SetParent(TreeNode<T> newParent)
            {
                if (this.parent != null)
                {
                    this.parent.children.Remove(this);
                }

                this.parent = newParent;

                if (this.parent != null)
                {
                    this.parent.children.Add(this);
                }
            }

            public int Size()
            {
                return 1 + this.children.Sum(node => node.Size());
            }

            public int Depth()
            {
                return this.children.Count == 0 ? 0 : 1 + this.children.Select(node => node.Depth()).Max();
            }

            public int ForkRank()
            {
                return this.children.Count <= 1 ? 0 : this.children.Select(node => node.Depth()).Min();
            }

            // TODO: this is WILDLY inefficient without caching results.  Cache results or eliminate this method altogether.
            public int MaxForkRank()
            {
                return this.children.Count == 0 ? 0 : Math.Max(ForkRank(), this.children.Select(node => node.MaxForkRank()).Max());
            }
        }

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

                            SetNeighbors(p, ref neighbors);
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
                TreeNode<Point2d> root = new TreeNode<Point2d>(rootPt.Value);

                PointSet2d alreadyIncluded = new PointSet2d();
                alreadyIncluded.Add(root.value);

                Point2d[] neighbors = new Point2d[8];

                Queue<TreeNode<Point2d>> searchSpace = new Queue<TreeNode<Point2d>>();
                searchSpace.Enqueue(root);

                while (searchSpace.Count > 0)
                {
                    var node = searchSpace.Dequeue();

                    SetNeighbors(node.value, ref neighbors);
                    for (int idx = 0; idx < neighbors.Length; idx++)
                    {
                        Point2d pt = neighbors[idx];
                        if (pointSet.Contains(pt) && !alreadyIncluded.Contains(pt))
                        {
                            searchSpace.Enqueue(new TreeNode<Point2d>(pt, node));
                            alreadyIncluded.Add(pt);
                        }
                    }
                }

                return root;
            }
        }

        private static void SetNeighbors(Point2d center, ref Point2d[] neighbors)
        {
            // Set the eight neighbor positions, clockwise.
            neighbors[0].x = center.x - 1; neighbors[0].y = center.y - 1;
            neighbors[1].x = center.x + 0; neighbors[1].y = center.y - 1;
            neighbors[2].x = center.x + 1; neighbors[2].y = center.y - 1;
            neighbors[3].x = center.x + 1; neighbors[3].y = center.y + 0;
            neighbors[4].x = center.x + 1; neighbors[4].y = center.y + 1;
            neighbors[5].x = center.x + 0; neighbors[5].y = center.y + 1;
            neighbors[6].x = center.x - 1; neighbors[6].y = center.y + 1;
            neighbors[7].x = center.x - 1; neighbors[7].y = center.y + 0;
        }
    }
}
