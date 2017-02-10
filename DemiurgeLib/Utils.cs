using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemiurgeLib.Common
{
    public class TreeNode<T> : IEnumerable<TreeNode<T>>
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

        public T GetDeepestValue()
        {
            int depth = 0;
            return GetDeepestValue(ref depth);
        }

        private T GetDeepestValue(ref int depth)
        {
            int curDepth = depth + 1;
            T ret = this.value, val;

            foreach (var child in this.children)
            {
                val = child.GetDeepestValue(ref curDepth);
                if (curDepth > depth)
                {
                    depth = curDepth;
                    ret = val;
                }
            }

            return ret;
        }
        
        public IEnumerator<TreeNode<T>> GetEnumerator()
        {
            // Don't do this recursively, as doing so is ludicrously
            // inefficient in terms of allocations.
            Queue<TreeNode<T>> nodes = new Queue<TreeNode<T>>();
            nodes.Enqueue(this);

            while (nodes.Count > 0)
            {
                TreeNode<T> node = nodes.Dequeue();
                yield return node;

                foreach (var child in node.children)
                {
                    nodes.Enqueue(child);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    class Utils
    {
        public static Point2d? Bfs(Point2d start, Func<Point2d, bool> isInDomain, Func<Point2d, bool> isSatisfactory)
        {
            PointSet2d alreadyIncluded = new PointSet2d();
            alreadyIncluded.Add(start);

            Point2d[] neighbors = new Point2d[8];

            Queue<Point2d> searchSpace = new Queue<Point2d>();
            searchSpace.Enqueue(start);

            while (searchSpace.Count > 0)
            {
                var current = searchSpace.Dequeue();

                SetNeighbors(current, ref neighbors);
                for (int idx = 0; idx < neighbors.Length; idx++)
                {
                    Point2d pt = neighbors[idx];
                    if (isInDomain(pt) && !alreadyIncluded.Contains(pt))
                    {
                        if (isSatisfactory(pt))
                        {
                            return pt;
                        }
                        else
                        {
                            searchSpace.Enqueue(pt);
                            alreadyIncluded.Add(pt);
                        }
                    }
                }
            }

            return null;
        }

        public static TreeNode<Point2d> MakeBfsTree(Point2d rootPt, Func<Point2d, bool> isInDomain)
        {
            TreeNode<Point2d> root = new TreeNode<Point2d>(rootPt);

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
                    if (isInDomain(pt) && !alreadyIncluded.Contains(pt))
                    {
                        searchSpace.Enqueue(new TreeNode<Point2d>(pt, node));
                        alreadyIncluded.Add(pt);
                    }
                }
            }

            return root;
        }

        public static void SetNeighbors(Point2d center, ref Point2d[] neighbors)
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
