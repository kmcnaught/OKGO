// Copyright (c) 2022 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using JuliusSweetland.OptiKey.Models;

namespace JuliusSweetland.OptiKey.Extensions
{
    public static class PointExtensions
    {
        public static Point CalculateCentrePoint(this List<Point> points)
        {
            return new Point(
                Math.Round(points.Average(p => p.X), MidpointRounding.AwayFromZero), 
                Math.Round(points.Average(p => p.Y), MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Convert a point to a PointAndKeyValue (if one can be mapped from the supplied pointToKeyValueMap).
        /// N.B. Null will be returned if the point supplied is null.
        /// </summary>
        public static PointAndKeyValue ToPointAndKeyValue(this Point? point, Dictionary<Rect, KeyValue> pointToKeyValueMap)
        {
            if (point == null)
            {
                return null;
            }

            return point.Value.ToPointAndKeyValue(pointToKeyValueMap);
        }

        /// <summary>
        /// Convert a point to a PointAndKeyValue (if one can be mapped from the supplied pointToKeyValueMap).
        /// </summary>
        public static PointAndKeyValue ToPointAndKeyValue(this Point point, Dictionary<Rect, KeyValue> pointToKeyValueMap)
        {
            return new PointAndKeyValue(point, point.ToKeyValue(pointToKeyValueMap));
        }

        public static KeyValue ToKeyValue(this Point point, Dictionary<Rect, KeyValue> pointToKeyValueMap, int padding = 0)
        {
            if (pointToKeyValueMap == null)
            {
                return null;
            }

            Rect keyRect = pointToKeyValueMap.Keys.FirstOrDefault(r => r.Contains(point));
            
            // If not *inside* any key, try again with padding
            // Will return first one that matches
            if (!pointToKeyValueMap.ContainsKey(keyRect) && padding > 0)
            {
                keyRect = pointToKeyValueMap.Keys.FirstOrDefault(r =>
                {
                    r.Inflate(padding, padding);
                    return r.Contains(point);
                });
            }

            return pointToKeyValueMap.ContainsKey(keyRect)
                ? pointToKeyValueMap[keyRect]
                : (KeyValue)null;
        }
    }
}
