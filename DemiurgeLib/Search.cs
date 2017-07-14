using DemiurgeLib.Common;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DemiurgeLib
{
    public class Search
    {
        private struct CostedPoint : IComparable<CostedPoint>
        {
            public Point2d point;
            public float cost;

            public int CompareTo(CostedPoint other)
            {
                return cost.CompareTo(other.cost);
            }
        }

        public static List<Point2d> FindPath(Rectangle area, Point2d from, Point2d to, Func<Point2d, Point2d, float> getCostOfStep = null, Func<Point2d, Point2d, float> estimateCostOfStep = null)
        {
            getCostOfStep = getCostOfStep ?? Point2d.Distance;
            estimateCostOfStep = estimateCostOfStep ?? Point2d.Distance;
            
            Field2d<float> visited = new Field2d<float>(new ConstantField<float>(area.Width, area.Height, float.NaN));
            visited[from.y, from.x] = 0f;

            BinaryHeap<CostedPoint> toVisit = new BinaryHeap<CostedPoint>();
            toVisit.Push(new CostedPoint() { point = from, cost = visited[from.y, from.x] + estimateCostOfStep(from, to) });

            Field2d<Point2d> cameFrom = new Field2d<Point2d>(new ConstantField<Point2d>(area.Width, area.Height, new Point2d(-1, -1)));
            cameFrom[from.y, from.x] = from;

            while (toVisit.Count > 0)
            {
                var curr = toVisit.Pop().point;
                
                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        if (i == 0 && j == 0)
                            continue;
                        
                        Point2d next = new Point2d(curr.x + i, curr.y + j);

                        if (next.x <= area.Left || next.x >= area.Right || next.y <= area.Top || next.y >= area.Bottom)
                            continue;

                        if (next == to)
                        {
                            List<Point2d> path = new List<Point2d>();
                            path.Add(next);
                            path.Add(curr);

                            Point2d it = curr;
                            while (cameFrom[it.y, it.x] != it)
                            {
                                it = cameFrom[it.y, it.x];
                                path.Add(it);
                            }

                            path.Reverse();
                            return path;
                        }

                        float cost = visited[curr.y, curr.x] + getCostOfStep(curr, next);

                        if (float.IsNaN(visited[next.y, next.x]) || visited[next.y, next.x] > cost)
                        {
                            visited[next.y, next.x] = cost;
                            cameFrom[next.y, next.x] = curr;
                            
                            toVisit.Push(new CostedPoint() { point = next, cost = cost + estimateCostOfStep(next, to) });
                        }
                    }
                }
            }

            return null;
        }
    }
}
