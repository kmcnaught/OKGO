// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Native;
using JuliusSweetland.OptiKey.Native.Common.Enums;
using JuliusSweetland.OptiKey.Native.Common.Static;
using JuliusSweetland.OptiKey.Native.Common.Structs;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Static;

namespace JuliusSweetland.OptiKey.UI.ViewModels
{
    partial class MainViewModel
    {

        private Rect FindLargestGapBetweenScreenAndMainWindow()
        {
            Rect screen = GetVirtualScreenBoundsInPixels();
            Rect window = GetMainWindowBoundsInPixels();

            var above = new Rect { X = screen.Left, Y = screen.Top, Width = screen.Width, Height = window.Top >= screen.Top ? window.Top - screen.Top : 0 };
            var below = new Rect { X = screen.Left, Y = window.Bottom, Width = screen.Width, Height = screen.Bottom >= window.Bottom ? screen.Bottom - window.Bottom : 0 };
            var left = new Rect { X = screen.Left, Y = screen.Top, Width = window.Left >= screen.Left ? window.Left - screen.Left : 0, Height = screen.Height };
            var right = new Rect { X = window.Right, Y = screen.Top, Width = screen.Right >= window.Right ? screen.Right - window.Right : 0, Height = screen.Height };

            return new Rect[] { above, below, left, right }.OrderByDescending(rect => rect.CalculateArea()).First();
        }

        private Rect GetVirtualScreenBoundsInPixels()
        {
            return new Rect
            {
                X = 0,
                Y = 0,
                Width = Graphics.VirtualScreenWidthInPixels,
                Height = Graphics.VirtualScreenHeightInPixels,
            };
        }

        private Rect GetMainWindowBoundsInPixels()
        {
            return Graphics.DipsToPixels(mainWindowManipulationService.WindowBounds);
        }

        private bool IsPointInsideMainWindow(Point point)
        {
            return GetMainWindowBoundsInPixels().Contains(point);
        }

        private bool IsMainWindowDocked()
        {
            return mainWindowManipulationService.WindowState == WindowStates.Docked;
        }


        private IntPtr GetHwndForFrontmostWindowAtPoint(Point point)
        {
            IntPtr shellWindow = PInvoke.GetShellWindow();

            Func<IntPtr, bool> criteria = hWnd =>
            {
                // Exclude the shell window.
                if (hWnd == shellWindow)
                {
                    return false;
                }

                // Exclude windows that aren't visible or that have been minimized.
                if (!PInvoke.IsWindowVisible(hWnd) || PInvoke.IsIconic(hWnd))
                {
                    return false;
                }

                // Exclude popup windows that have neither a frame like those used for regular windows
                // nor a frame like those used for dialog windows. This is intended to filter out things
                // like the lock screen, Start screen, and desktop wallpaper manager without filtering out
                // legitimate popup windows like "Open" and "Save As" dialogs as well as UWP apps.
                var style = Static.Windows.GetWindowStyle(hWnd);
                if ((style & WindowStyles.WS_POPUP) != 0 &&
                    (style & WindowStyles.WS_THICKFRAME) == 0 &&
                    (style & WindowStyles.WS_DLGFRAME) == 0)
                {
                    return false;
                }

                // Exclude transparent windows.
                var exStyle = Static.Windows.GetExtendedWindowStyle(hWnd);
                if (exStyle.HasFlag(ExtendedWindowStyles.WS_EX_TRANSPARENT))
                {
                    return false;
                }

                // Only include windows that contain the point.
                Rect? bounds = GetWindowBounds(hWnd);
                return bounds.HasValue && bounds.Value.Contains(point);
            };

            // Find the front-most top-level window that matches our criteria (expanding UWP apps into their CoreWindows).
            List<IntPtr> windows = Static.Windows.GetHandlesOfTopLevelWindows();
            windows = Static.Windows.ReplaceUWPTopLevelWindowsWithCoreWindowChildren(windows);
            windows = windows.Where(criteria).ToList();
            return Static.Windows.GetFrontmostWindow(windows);
        }

        private IntPtr HideCursorAndGetHwndForFrontmostWindowAtPoint(Point point)
        {
            // Make sure the cursor is hidden or else it may be picked as the front-most "window"!
            ShowCursor = false;

            return GetHwndForFrontmostWindowAtPoint(point);
        }
    }
}
