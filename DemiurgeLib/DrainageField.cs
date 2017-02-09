using System;
using System.Collections.Generic;

namespace DemiurgeLib.Common
{
    public class DrainageField : Field2d<Point2d>
    {
        public DrainageField(IField2d<HydrologicalField.LandType> hydroField, List<TreeNode<Point2d>> rivers)
            : base(new Transformation2d<HydrologicalField.LandType, Point2d>(hydroField, (x, y, landType) =>
            {
                switch (landType)
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
