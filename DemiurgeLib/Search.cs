using DemiurgeLib.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemiurgeLib
{
    public class Search
    {
        public static List<Point2d> FindPath(Point2d from, Point2d to, Func<Point2d, Point2d, float> getCostOfStep = null, Func<Point2d, Point2d, float> estimateCostOfStep = null)
        {
            getCostOfStep = getCostOfStep ?? Point2d.Distance;
            estimateCostOfStep = estimateCostOfStep ?? Point2d.Distance;

            Dictionary<Point2d, float> visited = new Dictionary<Point2d, float>();
            visited.Add(from, 0f);

            SortedList<float, Point2d> toVisit = new SortedList<float, Point2d>();
            toVisit.Add(visited[from] + estimateCostOfStep(from, to), from);

            Dictionary<Point2d, Point2d> cameFrom = new Dictionary<Point2d, Point2d>();
            cameFrom.Add(from, from);

            while (toVisit.Count > 0)
            {
                Point2d curr = toVisit.Values[0];
                toVisit.RemoveAt(0);
                // next has already been added to the visited list, no need to add it again.

                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        if (i == 0 && j == 0)
                            continue;

                        Point2d next = new Point2d(curr.x + i, curr.y + j);

                        if (next == to)
                        {
                            List<Point2d> path = new List<Point2d>();
                            path.Add(next);
                            path.Add(curr);

                            while (cameFrom[curr] != curr)
                            {
                                curr = cameFrom[curr];
                                path.Add(curr);
                            }

                            path.Reverse();
                            return path;
                        }

                        float cost = getCostOfStep(curr, next);

                        if (!visited.ContainsKey(next) || visited[next] > cost)
                        {
                            visited[next] = cost;
                            cameFrom[next] = curr;
                            //toVisit.Add(cost + estimateCostOfStep(next, to), next); // TODO: Guard against double-adding here?
                            toVisit[cost + estimateCostOfStep(next, to)] = next;
                        }
                    }
                }
            }

            return null;
        }
    }
}
