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

        public Rect FindLargestGapBetweenScreenAndMainWindow()
        {
            Rect screen = GetVirtualScreenBoundsInPixels();
            Rect window = GetMainWindowBoundsInPixels();

            var above = new Rect { X = screen.Left, Y = screen.Top, Width = screen.Width, Height = window.Top >= screen.Top ? window.Top - screen.Top : 0 };
            var below = new Rect { X = screen.Left, Y = window.Bottom, Width = screen.Width, Height = screen.Bottom >= window.Bottom ? screen.Bottom - window.Bottom : 0 };
            var left = new Rect { X = screen.Left, Y = screen.Top, Width = window.Left >= screen.Left ? window.Left - screen.Left : 0, Height = screen.Height };
            var right = new Rect { X = window.Right, Y = screen.Top, Width = screen.Right >= window.Right ? screen.Right - window.Right : 0, Height = screen.Height };

            return new Rect[] { above, below, left, right }.OrderByDescending(rect => rect.CalculateArea()).First();
        }

        public Rect GetVirtualScreenBoundsInPixels()
        {
            return new Rect
            {
                X = 0,
                Y = 0,
                Width = Graphics.VirtualScreenWidthInPixels,
                Height = Graphics.VirtualScreenHeightInPixels,
            };
        }

        public Rect GetMainWindowBoundsInPixels()
        {
            return Graphics.DipsToPixels(mainWindowManipulationService.WindowBounds);
        }

        public bool IsPointInsideMainWindow(Point point)
        {
            return GetMainWindowBoundsInPixels().Contains(point);
        }

        public bool IsPointInsideValidKey(Point point, int padding = 0)
        {
            return point.ToKeyValue(pointToKeyValueMap, padding) != null;
        }

        public bool IsPointInsideKey(Point point, KeyValue keyValue)
        {
            KeyValue pointKeyVal = point.ToKeyValue(pointToKeyValueMap);
            return (pointKeyVal != null &&
                    pointKeyVal == keyValue);

        }

        public bool IsMainWindowDocked()
        {
            return mainWindowManipulationService.WindowState == WindowStates.Docked;
        }


        public IntPtr GetHwndForFrontmostWindowAtPoint(Point point)
        {
            IntPtr shellWindow = PInvoke.GetShellWindow();

            Func<IntPtr, bool> criteria = hWnd =>
            {
                // Exclude the shell window.
                if (hWnd == shellWindow)
                {
                    return false;
                }

                // Exclude ourselves
                if (Static.Windows.GetWindowTitle(hWnd).Contains("CraytaAccess") ||
                    Static.Windows.GetWindowClassName(hWnd).Contains("CraytaAccess"))
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

            Log.Debug("WINDOWS");
            foreach (IntPtr hWnd in windows)
            {
                Log.DebugFormat("Window: {0}", hWnd);
                Log.DebugFormat("\t\t Class name: {0}", Static.Windows.GetWindowClassName(hWnd));
                Log.DebugFormat("\t\t Title: {0}", Static.Windows.GetWindowTitle(hWnd));
                Log.DebugFormat("\t\t Style: {0}", Static.Windows.GetWindowStyle(hWnd));
                Log.DebugFormat("\t\t Visible?: {0}", PInvoke.IsWindowVisible(hWnd));
                Log.DebugFormat("\t\t Iconic?: {0}", PInvoke.IsIconic(hWnd));
                var flags = Static.Windows.GetWindowStyles(hWnd);
                string flagsString = "\t\t Flags: ";
                foreach (var f in flags)
                    flagsString += f.Value + " ";
                Log.Info(flagsString);
            }

            return Static.Windows.GetFrontmostWindow(windows);
        }

        public bool TryGrabFocusAtPoint(Point point)
        {
            IntPtr hWnd = HideCursorAndGetHwndForFrontmostWindowAtPoint(point);
            bool bSuccess = false;

            if (hWnd == IntPtr.Zero)
            {
                Log.Info("No valid window at the point to bring to the front.");
            }
            else
            {
                Log.InfoFormat("Focusing frontmost window {0} ({1})",
                    Static.Windows.GetWindowClassName(hWnd),
                    Static.Windows.GetWindowTitle(hWnd));
                bSuccess = PInvoke.SetForegroundWindow(hWnd);
                if (!bSuccess)
                {
                    Log.WarnFormat("Could not bring window at the point, {0}, to the front.", hWnd);
                }
            }
            return bSuccess;
        }

        public IntPtr HideCursorAndGetHwndForFrontmostWindowAtPoint(Point point)
        {
            // Make sure the cursor is hidden or else it may be picked as the front-most "window"!
            ShowCursor = false;

            return GetHwndForFrontmostWindowAtPoint(point);
        }

        private Rect? GetWindowBounds(IntPtr hWnd)
        {
            if (!PInvoke.IsWindow(hWnd))
            {
                Log.WarnFormat("{0} does not or no longer points to a valid window.", hWnd);
                return null;
            }

            RECT rawRect;

            if (PInvoke.DwmGetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.ExtendedFrameBounds, out rawRect, Marshal.SizeOf<RECT>()) != 0)
            {
                Log.WarnFormat("Failed to get bounds of window {0} using DwmGetWindowAttribute. Falling back to GetWindowRect.", hWnd);

                if (!PInvoke.GetWindowRect(hWnd, out rawRect))
                {
                    Log.WarnFormat("Failed to get bounds of window {0} using GetWindowRect.", hWnd);
                    return null;
                }
            }

            return new Rect
            {
                X = rawRect.Left,
                Y = rawRect.Top,
                Width = rawRect.Right - rawRect.Left,
                Height = rawRect.Bottom - rawRect.Top
            };
        }

        private Rect? GetSubwindowBoundsOnScreen(IntPtr hWnd, Rect relativeBounds)
        {
            Rect? windowBounds = GetWindowBounds(hWnd);
            if (windowBounds.HasValue)
            {
                // Express the relative bounds in virtual screen-space again now that we know the location of
                // the window's top-left corner.
                Rect subwindowBounds = relativeBounds;
                subwindowBounds.Offset(windowBounds.Value.Left, windowBounds.Value.Top);

                // Make sure the subwindow bounds are fully contained within the window since the window may have 
                // shrunk since it was chosen.
                subwindowBounds.Intersect(windowBounds.Value);

                return subwindowBounds;
            }
            else
            {
                return null;
            }
        }
    }
}
