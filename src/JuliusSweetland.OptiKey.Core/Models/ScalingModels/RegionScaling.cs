using JuliusSweetland.OptiKey.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace JuliusSweetland.OptiKey.Models.ScalingModels
{
    class RegionScaling : ISensitivityFunction
    {
        private List<Region> deadzones;
        private List<Region> regions;

        public RegionScaling(List<Region> deadzones, List<Region> regions)
        {
            this.deadzones = deadzones;
            this.regions = regions;
        }

        public List<Region> GetContours()
        {
            var regions = this.regions;
            if (deadzones!= null && deadzones.Any())
            {
                regions.AddRange(deadzones);
            }

            var ctrs = new List<Region>();
            ctrs.AddRange(regions.Where(r => r.Visible));

            return ctrs;
        }

        public Vector CalculateScaling(Point point, Point centre)
        {
            point.X /= Graphics.PrimaryScreenWidthInPixels;
            point.Y /= Graphics.PrimaryScreenHeightInPixels;

            if (regions.First().Radius > 0)
                return RingScaling(point);
            else
                return RectangleScaling(point);
        }

        public Vector RingScaling(Point point)
        {
            var amount = 0d;
            var vector = point - regions[0].Center;

            for (int i = 0; i < regions.Count() - 1; i++)
            {
                var min = regions[i];
                var max = regions[i + 1];

                if (!(min.Contains(point.X, point.Y) && max.Contains(point.X, point.Y)))
                {
                    var delta = vector.Length - min.Radius;
                    var maxDelta = max.Radius - min.Radius;
                    amount = max.Gradient == 0 ? min.Amount : max.Gradient == 1 ? max.Amount
                        : max.Gradient == .5 ? min.Amount + (max.Amount - min.Amount) * delta / maxDelta
                        : min.Amount + (max.Amount - min.Amount) * Math.Pow(delta / maxDelta, 2 * max.Gradient);
                }
            }

            var theta = Math.Atan2(vector.Y, vector.X);
            return new Vector(amount * Math.Cos(theta), amount * Math.Sin(theta));
        }

        public Vector RectangleScaling(Point point)
        {
            var amountX = 0d;
            var amountY = 0d;

            for (int i = 0; i < regions.Count() - 1; i++)
            {
                var min = regions[i];
                var max = regions[i + 1];

                if (!(min.Left <= point.X && min.Right >= point.X) && max.Left <= point.X && max.Right >= point.X)
                {
                    var sX = point.X < min.Left ? -1 : 1;
                    var deltaX = sX < 0 ? min.Left - point.X : point.X - min.Right;
                    var maxX = sX < 0 ? min.Left - max.Left : max.Right - min.Right;
                    amountX = max.Gradient == 0 ? min.Amount : max.Gradient == 1 ? max.Amount
                        : max.Gradient == .5 ? min.Amount + (max.Amount - min.Amount) * deltaX / maxX
                        : min.Amount + (max.Amount - min.Amount) * Math.Pow(deltaX / maxX, 2 * max.Gradient);
                    amountX *= sX;
                }

                if (!(min.Top <= point.Y && min.Bottom >= point.Y) && max.Top <= point.Y && max.Bottom >= point.Y)
                {
                    var sY = point.Y < min.Top ? -1 : point.Y > min.Bottom ? 1 : 0;
                    var deltaY = sY < 0 ? min.Top - point.Y : point.Y - min.Bottom;
                    var maxY = sY < 0 ? min.Top - max.Top : max.Bottom - min.Bottom;
                    amountY = max.Gradient == 0 ? min.Amount : max.Gradient == 1 ? max.Amount
                        : max.Gradient == .5 ? min.Amount + (max.Amount - min.Amount) * deltaY / maxY
                        : min.Amount + (max.Amount - min.Amount) * Math.Pow(deltaY / maxY, 2 * max.Gradient);
                    amountY *= sY;
                }
            }

            return new Vector(amountX, amountY);
        }

        public bool Contains(Point point)
        {
            point.X /= Graphics.PrimaryScreenWidthInPixels;
            point.Y /= Graphics.PrimaryScreenHeightInPixels;
            //point within regions.Last(); and not within a deadzone
            return (regions.Last().Contains(point.X, point.Y))
                && !(deadzones != null && deadzones.Any() && deadzones.Exists(x => x.Contains(point.X, point.Y)));
        }
    }
}
