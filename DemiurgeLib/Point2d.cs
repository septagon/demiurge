﻿using System;
using System.Drawing;

namespace DemiurgeLib.Common
{
    public struct Point2d
    {
        public static readonly Point2d zero = new Point2d(0, 0);

        public int x, y;

        public Point2d(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator Point(Point2d pt)
        {
            return new Point(pt.x, pt.y);
        }

        public static implicit operator Point2d(Point pt)
        {
            return new Point2d(pt.X, pt.Y);
        }

        public static bool operator ==(Point2d a, Point2d b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(Point2d a, Point2d b)
        {
            return !(a == b);
        }

        public static Point2d operator +(Point2d a, Point2d b)
        {
            return new Point2d(a.x + b.x, a.y + b.y);
        }

        public static Point2d operator -(Point2d p)
        {
            return new Point2d(-p.x, -p.y);
        }

        public static Point2d operator -(Point2d a, Point2d b)
        {
            return a + -b;
        }

        public static Point2d operator *(int s, Point2d p)
        {
            return new Point2d(s * p.x, s * p.y);
        }

        public static int SqDist(Point2d a, Point2d b)
        {
            return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y);
        }

        public static float Distance(Point2d a, Point2d b)
        {
            return (float)Math.Sqrt(SqDist(a, b));
        }
    }
}
