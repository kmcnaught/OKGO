using System.Collections.Generic;
using System.Windows;

namespace JuliusSweetland.OptiKey.Models.ScalingModels
{
    interface ISensitivityFunction
    {
        Vector CalculateScaling(Point current, Point centre);
        bool Contains(Point point);
        List<Region> GetContours();
    }
}
