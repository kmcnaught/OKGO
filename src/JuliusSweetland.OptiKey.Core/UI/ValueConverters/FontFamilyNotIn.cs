﻿// Copyright (c) 2022 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JuliusSweetland.OptiKey.UI.ValueConverters
{
    public class FontFamilyNotIn : IValueConverter
    {
        public List<string> Fonts { get; set; } = new List<string>();
         
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Fonts != null && value is string && !Fonts.Contains(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
