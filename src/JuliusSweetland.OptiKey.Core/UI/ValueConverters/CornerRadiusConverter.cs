// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace JuliusSweetland.OptiKey.UI.ValueConverters
{
    public class CornerRadiusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Takes single constant for all corners, 
            // or two values for left, right
            if (values.Length >= 4)
                return new CornerRadius((double) values[0], (double) values[1], (double) values[2], (double) values[3]);
            else if (values.Length >= 2)
                return new CornerRadius((double) values[0], (double) values[1], (double) values[1], (double) values[0]);
            else if (values.Length >= 1)
                return new CornerRadius((double) values[0]);
            return new CornerRadius(0.0);
        }
        
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
