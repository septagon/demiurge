using System;

namespace DemiurgeLib.Common
{
    /// <summary>
    /// Performs a absolute value reflectance transform (a.k.a. the "mountain transform") on the
    /// input field.  Expects input field to be normalized to range [0,maxValue]. 
    /// </summary>
    public class AbsTransform : Transformation2d
    {
        public AbsTransform(IField2d<float> field, float inflectionPoint = 0.5f, float maxValue = 1f) : base(field, (val) =>
        {
            float divisor = Math.Max(inflectionPoint, maxValue - inflectionPoint);
            return Math.Abs(val - inflectionPoint) / divisor;
        }) { }
    }

    public class InvertTransform : Transformation2d
    {
        public InvertTransform(IField2d<float> field) : base(field, (val) => -1f * val + 1f) { }
    }

    public class ScaleTransform : Transformation2d
    {
        public ScaleTransform(IField2d<float> field, float scalar) : base(field, (val) => scalar * val) { }
    }

    public class DrainageField : Field2d<Point2d>
    {
        public DrainageField(IField2d<HydrologicalField.LandType> hydroField, System.Collections.Generic.List<TreeNode<Point2d>> rivers)
            : base(new Transformation2d<HydrologicalField.LandType, Point2d>(hydroField, (x, y, landType) =>
            {
                switch(landType)
                {
                    case HydrologicalField.LandType.Ocean:
                        return new Point2d(x, y);
                    case HydrologicalField.LandType.Shore:
                        return default(Point2d); // Handled in local constructor.
                    case HydrologicalField.LandType.Land:
                        Point2d? drain = Utils.Bfs(
                            new Point2d(x, y),
                            pt => x >= 0 && x < hydroField.Width && y >= 0 && y < hydroField.Height,
                            pt => hydroField[pt.y, pt.x] != HydrologicalField.LandType.Land);
                        return drain.HasValue ? drain.Value : new Point2d(x, y);
                    default:
                        throw new Exception();
                }
            }))
        {
            foreach (var river in rivers)
            {
                SetRiverDrainage(river);
            }
        }

        private void SetRiverDrainage(TreeNode<Point2d> node)
        {
            if (node.parent != null)
            {
                this[node.value.y, node.value.x] = node.parent.value;
            }
            else
            {
                this[node.value.y, node.value.x] = node.value;
            }

            foreach (var child in node.children)
            {
                SetRiverDrainage(child);
            }
        }
    }
}
