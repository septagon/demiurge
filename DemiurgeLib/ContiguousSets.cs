using System;
using System.Linq;
using System.Collections.Generic;

namespace DemiurgeLib.Common
{
    public static class ContiguousSets
    {
        public static Dictionary<T, HashSet<PointSet2d>> FindContiguousSets<T>(this IField2d<T> field)
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

        public static TreeNode<Point2d> MakeTreeFromContiguousSet(this PointSet2d pointSet, Func<Point2d, bool> isRoot)
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

        public static TreeNode<TreeNode<T>> GetMajorSubtrees<T>(this TreeNode<T> root, Func<TreeNode<T>, bool> isMajorSubtree)
        {
            var subtrees = root.children
                .Where(isMajorSubtree)
                .Select(child => GetMajorSubtrees(child, isMajorSubtree))
                .OrderByDescending(tree => tree.value.Depth())
                .ToList();

            if (subtrees.Count == 0)
            {
                return new TreeNode<TreeNode<T>>(root);
            }
            else
            {
                TreeNode<TreeNode<T>> ret = new TreeNode<TreeNode<T>>(root);
                ret.children = subtrees[0].children;

                for (int idx = 1; idx < subtrees.Count; idx++)
                {
                    subtrees[idx].SetParent(ret);
                }

                return ret;
            }
        }

        public static IEnumerator<TreeNode<T>> IteratePrimarySubtree<T>(this TreeNode<TreeNode<T>> root)
        {
            var prohibited = root.children.Select(child => child.value).ToList();

            Queue<TreeNode<T>> nodes = new Queue<TreeNode<T>>();
            nodes.Enqueue(root.value);

            while (nodes.Count > 0)
            {
                TreeNode<T> node = nodes.Dequeue();

                yield return node;

                if (!prohibited.Contains(node))
                {
                    foreach (var child in node.children)
                    {
                        nodes.Enqueue(child);
                    }
                }
            }
        }

        // This, I think, is a great evil.  Be very mindful of the performance of this, as I strongly
        // suspect it will go allocation-crazy if given too much leeway to operate.
        public static IEnumerator<IEnumerator<TreeNode<T>>> IterateAllSubtrees<T>(this TreeNode<TreeNode<T>> root)
        {
            yield return root.IteratePrimarySubtree();

            foreach (var child in root.children)
            {
                for (var iterator = child.IterateAllSubtrees(); iterator.MoveNext(); )
                {
                    yield return iterator.Current;
                }
            }
        }

        public static void Iterate<T>(this IEnumerator<T> enumerator, Action<T> action)
        {
            for (; enumerator.MoveNext();)
            {
                action(enumerator.Current);
            }
        }

        public static List<TreeNode<TreeNode<Point2d>>> GetRivers(this List<TreeNode<Point2d>> riverSystems, int subtreeDepth = 15)
        {
            List<TreeNode<TreeNode<Point2d>>> rivers = new List<TreeNode<TreeNode<Point2d>>>();
            foreach (var river in riverSystems)
            {
                rivers.Add(river.GetMajorSubtrees(node => node.Depth() >= subtreeDepth));
            }
            return rivers;
        }

        public static List<TreeNode<Point2d>> GetRiverSystems(
            this Dictionary<HydrologicalField.LandType, HashSet<PointSet2d>> geographicFeatures,
            IField2d<HydrologicalField.LandType> hydroField)
        {
            List<TreeNode<Point2d>> riverForest = new List<TreeNode<Point2d>>();
            foreach (var river in geographicFeatures[HydrologicalField.LandType.Shore])
            {
                riverForest.Add(river.MakeTreeFromContiguousSet(pt =>
                {
                    // Warning: naive non-boundary-checking test-only implementation.  This will probably CRASH THE PROGRAM
                    // if a river happens to border the edge of the map.
                    return
                        hydroField[pt.y + 1, pt.x + 1] == HydrologicalField.LandType.Ocean ||
                        hydroField[pt.y + 1, pt.x + 0] == HydrologicalField.LandType.Ocean ||
                        hydroField[pt.y + 1, pt.x - 1] == HydrologicalField.LandType.Ocean ||
                        hydroField[pt.y + 0, pt.x - 1] == HydrologicalField.LandType.Ocean ||
                        hydroField[pt.y - 1, pt.x - 1] == HydrologicalField.LandType.Ocean ||
                        hydroField[pt.y - 1, pt.x + 0] == HydrologicalField.LandType.Ocean ||
                        hydroField[pt.y - 1, pt.x + 1] == HydrologicalField.LandType.Ocean ||
                        hydroField[pt.y + 0, pt.x + 1] == HydrologicalField.LandType.Ocean;
                }));
            }
            return riverForest;
        }

        public static bool AreWaterwaysLegalForField(this List<TreeNode<Point2d>> waterways, IField2d<float> field)
        {
            foreach (var waterway in waterways)
            { 
                foreach (var node in waterway)
                {
                    if (node.value.x < 0 || node.value.x >= field.Width)
                    {
                        return false;
                    }

                    if (node.value.y < 0 || node.value.y >= field.Height)
                    {
                        return false;
                    }

                    if (node.parent != null && 
                        field[node.value.y, node.value.x] < field[node.parent.value.y, node.parent.value.x])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        
        public static void EnforceWaterwaysLegalForField(this List<TreeNode<Point2d>> waterways, Field2d<float> field, float minIncrement = 0f)
        {
            foreach (var waterway in waterways)
            {
                foreach (var node in waterway)
                {
                    if (node.parent != null &&
                        field[node.value.y, node.value.x] < field[node.parent.value.y, node.parent.value.x])
                    {
                        field[node.value.y, node.value.x] = field[node.parent.value.y, node.parent.value.x] + minIncrement;
                    }
                }
            }
        }
    }
}
