// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using JuliusSweetland.OptiKey.Crayta.Properties;

namespace JuliusSweetland.OptiKey.Crayta.UI.ValueConverters
{
    // Return a string property, with a fallback value if empty
    public class StringOrEmptyLabel : IValueConverter
    {
        public string EmptyLabel { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            String fallback = String.IsNullOrEmpty(EmptyLabel) ? Resources.STRING_NOT_SET : EmptyLabel;

            if (value == null || value == DependencyProperty.UnsetValue)
                return fallback;

            string stringVal = (string) value;
            if (String.IsNullOrEmpty(stringVal))
                return fallback;

            return stringVal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (String.Equals(value, EmptyLabel))
                return "";
            else
                return value;
        }
    }
}
