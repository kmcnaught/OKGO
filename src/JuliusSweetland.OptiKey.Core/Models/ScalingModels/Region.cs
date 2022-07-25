using System;
using System.Windows;

namespace JuliusSweetland.OptiKey.Models.ScalingModels
{
    public struct Region
    {
        public Region(double left, double top, double width, double height,
            double radius = 0, double amount = 0, double gradient = .5, bool visible = false)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            Radius = radius;
            Amount = amount;
            Gradient = gradient;
            Visible = visible;
        }
        public double Left;
        public double Top;
        public double Width;
        public double Height;
        public double Radius;
        public double Amount;
        public double Gradient;
        public bool Visible;
        public Point Center { get { return new Point(Left + Width / 2, Top + Height / 2); } }
        public double Right { get { return Left + Width; } }
        public double Bottom { get { return Top + Height; } }
        public bool Contains(double x, double y)
        {
            if (Radius > 0) //use ellipse logic
                return Math.Pow(x, 2) / Math.Pow(Width / 2, 2) + Math.Pow(y, 2) / Math.Pow(Height / 2, 2) <= 1;
            else //use rectangle logic
                return x >= Left && x <= Right && y >= Top && y <= Bottom;
        }
    }
}
