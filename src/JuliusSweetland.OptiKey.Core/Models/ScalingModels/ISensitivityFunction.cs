using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JuliusSweetland.OptiKey.Models.ScalingModels
{
    interface ISensitivityFunction
    {
        Vector CalculateScaling(Point current, Point centre);
        List<Point> GetContours();
    }
}
