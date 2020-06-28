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
    partial class MainViewModel : ILookToScrollOverlayViewModel
    {
        private bool choosingJoystickBoundsTarget = false;
        
        private Point pointJoystickBoundsTarget = new Point();
        private IntPtr windowJoystickBoundsTarget = IntPtr.Zero;

        private DateTime? joystickLastUpdate = null;

        #region ILookToScrollOverlayViewModel Members

        private bool isLookToScrollActive = false;
        public bool IsLookToScrollActive
        {
            get { return isLookToScrollActive; }
            private set { SetProperty(ref isLookToScrollActive, value); }
        }

        private Rect activeLookToScrollBounds = Rect.Empty;
        public Rect ActiveLookToScrollBounds
        {
            get { return activeLookToScrollBounds; }
            private set { SetProperty(ref activeLookToScrollBounds, value); }
        }

        private Rect activeLookToScrollDeadzone = Rect.Empty;
        public Rect ActiveLookToScrollDeadzone
        {
            get { return activeLookToScrollDeadzone; }
            private set { SetProperty(ref activeLookToScrollDeadzone, value); }
        }

        private Thickness activeLookToScrollMargins = new Thickness();
        public Thickness ActiveLookToScrollMargins
        {
            get { return activeLookToScrollMargins; }
            private set { SetProperty(ref activeLookToScrollMargins, value); }
        }

        #endregion

        private void ToggleLookToScroll()
        {
            Log.Info("Look to scroll active key was selected.");

            if (keyStateService.KeyDownStates[KeyValues.LookToScrollActiveKey].Value.IsDownOrLockedDown())
            {
                Log.Info("Look to scroll is now active.");

                if (keyStateService.KeyDownStates[KeyValues.LookToScrollBoundsKey].Value.IsDownOrLockedDown())
                {
                    Log.Info("Re-using previous bounds target.");
                }
                else
                {
                    ChooseLookToScrollBoundsTarget();
                }
            }
            else
            {
                Log.Info("Look to scroll is no longer active.");
                controllerOutputService.ProcessJoystick("RightJoystickAxisX", 0);
                controllerOutputService.ProcessJoystick("RightJoystickAxisY", 0);
            }
        }

        private void ChooseLookToScrollBoundsTarget()
        {
            Log.Info("Choosing look to scroll bounds target.");

            choosingJoystickBoundsTarget = true;
            pointJoystickBoundsTarget = new Point();
            windowJoystickBoundsTarget = IntPtr.Zero;

            Action<bool> callback = success => 
            {
                if (success && Settings.Default.LookToScrollLockDownBoundsKey)
                {
                    // Lock the bounds key down. This signals that the chosen target should be re-used during
                    // subsequent scrolling sessions.
                    keyStateService.KeyDownStates[KeyValues.LookToScrollBoundsKey].Value = KeyDownStates.LockedDown;
                }
                else if (!success)
                {
                    // If a target wasn't successfully chosen, de-activate scrolling and release the bounds key.
                    keyStateService.KeyDownStates[KeyValues.LookToScrollActiveKey].Value = KeyDownStates.Up;
                    keyStateService.KeyDownStates[KeyValues.LookToScrollBoundsKey].Value = KeyDownStates.Up;
                }

                choosingJoystickBoundsTarget = false;
            };

            ChoosePointLookToScrollBoundsTarget(callback);
        }

        private void ChoosePointLookToScrollBoundsTarget(Action<bool> callback)
        {
            Log.Info("Choosing point on screen to use as the centre point for scrolling.");

            SetupFinalClickAction(point =>
            {
                if (point.HasValue)
                {
                    Log.InfoFormat("User chose point: {0}.", point.Value);
                    pointJoystickBoundsTarget = point.Value;

                    if (Settings.Default.LookToScrollBringWindowToFrontAfterChoosingScreenPoint)
                    {
                        IntPtr hWnd = HideCursorAndGetHwndForFrontmostWindowAtPoint(point.Value);

                        if (hWnd == IntPtr.Zero)
                        {
                            Log.Info("No valid window at the point to bring to the front.");
                        }
                        else if (!PInvoke.SetForegroundWindow(hWnd))
                        {
                            Log.WarnFormat("Could not bring window at the point, {0}, to the front.", hWnd);
                        }
                        else
                        {
                            windowJoystickBoundsTarget = hWnd;
                            Log.InfoFormat("Brought window at the point, {0}, to the front.", hWnd);
                        }
                    }
                }

                ResetAndCleanupAfterMouseAction();
                callback(point.HasValue);
            }, suppressMagnification: true);
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

        private void HandleLookToScrollBoundsKeySelected()
        {
            Log.Info("Look to scroll bounds key was selected.");

            if (!Settings.Default.LookToScrollLockDownBoundsKey)
            {
                // The key acts as a simple cycle like the mode, speed, and increment keys. Don't let it be locked down.
                keyStateService.KeyDownStates[KeyValues.LookToScrollBoundsKey].Value = KeyDownStates.Up;
                SelectNextLookToScrollBounds();
            }
            else if (keyStateService.KeyDownStates[KeyValues.LookToScrollActiveKey].Value.IsDownOrLockedDown())
            {
                // If scrolling is active, force the bounds target to be rechosen. This will also cause the
                // bounds key to become locked down again.
                ChooseLookToScrollBoundsTarget();
            }
            else if (keyStateService.KeyDownStates[KeyValues.LookToScrollBoundsKey].Value.IsDownOrLockedDown())
            {
                // We're not scrolling, and the bounds key wasn't locked down previously. In this case, just
                // release the key and cycle to the next bounds value. It'll eventually get locked down for
                // real when scrolling is toggled on and the bounds target is chosen.
                keyStateService.KeyDownStates[KeyValues.LookToScrollBoundsKey].Value = KeyDownStates.Up;
                SelectNextLookToScrollBounds();
            }
            else
            {
                // We're not scrolling, but the bounds key was locked down previously. By releasing the
                // key, we'll force the bounds target to be rechosen. Since we might want to do so for
                // the current bounds value without having to cycle back around, we'll just leave it
                // alone. The next key press will start cycling.
                Log.Info("Unlocked look to scroll bounds key. The bounds target will need to be rechosen.");
            }
        }

        private void SelectNextLookToScrollBounds()
        {
            LookToScrollBounds before = Settings.Default.LookToScrollBounds;
            LookToScrollBounds after = GetNextEnumValue(before);

            Settings.Default.LookToScrollBounds = after;

            Log.InfoFormat("Changed look to scroll bounds from {0} to {1}.", before, after);
        }

        private void SelectNextLookToScrollIncrement()
        {
            Log.Info("Look to scroll increment key was selected.");

            IEnumerable<int> incrementChoices = Regex
                // Replace sequences of non-digit characters with a single space.
                .Replace(Settings.Default.LookToScrollIncrementChoices, @"\D+", " ")
                .Split(' ')
                .Where(str => !string.IsNullOrEmpty(str))
                .Select(str => int.Parse(str))
                .Where(increment => increment > 0)
                .DefaultIfEmpty(1);

            int before = Settings.Default.LookToScrollIncrement;
            int after = GetNextValueInSequence(before, incrementChoices);

            Settings.Default.LookToScrollIncrement = after;

            Log.InfoFormat("Changed look to scroll increment from {0} to {1}.", before, after);
        }

        private void SelectNextLookToScrollMode()
        {
            Log.Info("Look to scroll mode key was selected.");

            LookToScrollModes before = Settings.Default.LookToScrollMode;
            LookToScrollModes after = GetNextEnumValue(before);

            Settings.Default.LookToScrollMode = after;

            Log.InfoFormat("Changed look to scroll mode from {0} to {1}.", before, after);
        }

        private void SelectNextLookToScrollSpeed()
        {
            Log.Info("Look to scroll speed key was selected.");

            LookToScrollSpeeds before = Settings.Default.LookToScrollSpeed;
            LookToScrollSpeeds after = GetNextEnumValue(before);

            Settings.Default.LookToScrollSpeed = after;

            Log.InfoFormat("Changed look to scroll speed from {0} to {1}.", before, after);
        }

        private T GetNextValueInSequence<T>(T current, IEnumerable<T> values)
        {
            return values.SkipWhile(value => !Equals(value, current)) // Skip ahead to the current item.
                .Skip(1) // Move on to the item immediately following it.
                .Concat(values.Take(1)) // Make sure we wrap around if we've reached the end.
                .FirstOrDefault();
        }

        private T GetNextEnumValue<T>(T current)
        {
            return GetNextValueInSequence(current, Enum.GetValues(typeof(T)).Cast<T>());
        }

        private void UpdateLookToScroll(Point position)
        {
            var thisUpdate = DateTime.Now;

            Rect bounds;
            Point centre;
            bool active = ShouldUpdateLookToScroll(position, out bounds, out centre);

            if (active)
            {
                Log.DebugFormat("Updating look to scroll using position: {0}.", position);
                Log.DebugFormat("Current look to scroll bounds rect is: {0}.", bounds);
                Log.DebugFormat("Current look to scroll centre point is: {0}.", centre);

                Vector scrollAmount = CalculateLookToScrollVelocity(position, centre);
                PerformLookToScroll(scrollAmount);
            }
            else
            {
                controllerOutputService.ProcessJoystick("RightJoystickAxisX", 0.0f);
                controllerOutputService.ProcessJoystick("RightJoystickAxisY", 0.0f);
            }

            UpdateLookToScrollOverlayProperties(active, bounds, centre);

            joystickLastUpdate = thisUpdate;
        }

        private bool ShouldUpdateLookToScroll(Point position, out Rect bounds, out Point centre)
        {
            bounds = Rect.Empty;
            centre = new Point();

            if (!keyStateService.KeyDownStates[KeyValues.LookToScrollActiveKey].Value.IsDownOrLockedDown() ||
                keyStateService.KeyDownStates[KeyValues.SleepKey].Value.IsDownOrLockedDown() ||
                IsPointInsideMainWindow(position) ||
                choosingJoystickBoundsTarget ||
                !joystickLastUpdate.HasValue)
            {
                return false;
            }

            Rect? boundsContainer = GetCurrentLookToScrollBoundsRect();

            if (!boundsContainer.HasValue)
            {
                Log.Info("Look to scroll bounds is no longer valid. Deactivating look to scroll.");

                keyStateService.KeyDownStates[KeyValues.LookToScrollActiveKey].Value = KeyDownStates.Up;
                keyStateService.KeyDownStates[KeyValues.LookToScrollBoundsKey].Value = KeyDownStates.Up;

                return false;
            }

            bounds = boundsContainer.Value;
            centre = GetCurrentLookToScrollCentrePoint(bounds);

            // If using a window or portion of it as the bounds target, only scroll while pointing _at_ that window, 
            // not while pointing at another window on top of it.
            if (GetHwndForFrontmostWindowAtPoint(position) != windowJoystickBoundsTarget)
            {
                // this keeps flicking on/off with stadia, not sure why :(
                return false;
            }

            return bounds.Contains(position);
        }

        private Rect? GetCurrentLookToScrollBoundsRect()
        {
            Rect? bounds = IsMainWindowDocked() 
                ? FindLargestGapBetweenScreenAndMainWindow() 
                : GetVirtualScreenBoundsInPixels();

            return bounds;
        }

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

        private Point GetCurrentLookToScrollCentrePoint(Rect bounds)
        {
            return pointJoystickBoundsTarget;
        }

        private Vector CalculateLookToScrollVelocity(Point current, Point centre)
        {
            double baseSpeed = 0;
            double acceleration = 0.02;

            var velocity = new Vector { X = 0, Y = 0 };
            velocity.X = CalculateLookToScrollVelocity(
                current.X, 
                centre.X,
                Settings.Default.LookToScrollHorizontalDeadzone, 
                baseSpeed, 
                acceleration
            );
    
            velocity.Y = CalculateLookToScrollVelocity(
                current.Y,
                centre.Y,
                Settings.Default.LookToScrollVerticalDeadzone,
                baseSpeed,
                acceleration
            );
            
            Log.DebugFormat("Current scrolling velocity is: {0}.", velocity);

            return velocity;
        }

        private double CalculateLookToScrollVelocity(
            double current, 
            double centre, 
            double deadzone, 
            double baseSpeed, 
            double acceleration)
        {
            // Calculate the direction and distance from the centre to the current value. 
            double signedDistance = current - centre;
            double sign = Math.Sign(signedDistance);
            double distance = Math.Abs(signedDistance);

            // Remove the deadzone.
            distance -= deadzone;
            if (distance < 0)
            {
                return 0;
            }

            // Calculate total speed using base speed and distance-based acceleration.
            double speed = baseSpeed + Math.Sqrt(distance) * acceleration;

            Log.InfoFormat("current: {0}, centre: {1}, accel: {2}, velocity: {3}", current, centre, acceleration, sign*speed);

            // Give the speed the correct direction.
            return sign * speed;
        }

        private void PerformLookToScroll(Vector scrollAmount)
        {
            Action reinstateModifiers = () => { };
            if (keyStateService.SimulateKeyStrokes && Settings.Default.SuppressModifierKeysForAllMouseActions)
            {
                reinstateModifiers = keyStateService.ReleaseModifiers(Log);
            }

            controllerOutputService.ProcessJoystick("RightJoystickAxisX", (float)scrollAmount.X);
            controllerOutputService.ProcessJoystick("RightJoystickAxisY", -(float)scrollAmount.Y);
         
            reinstateModifiers();
        }

        private void UpdateLookToScrollOverlayProperties(bool active, Rect bounds, Point centre)
        {
            int hDeadzone = Settings.Default.LookToScrollHorizontalDeadzone;
            int vDeadzone = Settings.Default.LookToScrollVerticalDeadzone;

            var deadzone = new Rect
            {
                X = centre.X - hDeadzone,
                Y = centre.Y - vDeadzone,
                Width = hDeadzone * 2,
                Height = vDeadzone * 2,
            };

            IsLookToScrollActive = active;
            ActiveLookToScrollBounds = Graphics.PixelsToDips(bounds);
            ActiveLookToScrollDeadzone = Graphics.PixelsToDips(deadzone);
            ActiveLookToScrollMargins = Graphics.PixelsToDips(bounds.CalculateMarginsAround(deadzone));
        }

        private Action SuspendLookToScrollWhileChoosingPointForMouse()
        {
            Action resumeAction = () => { };

            if (Settings.Default.LookToScrollSuspendBeforeChoosingPointForMouse)
            {
                NotifyingProxy<KeyDownStates> activeKey = keyStateService.KeyDownStates[KeyValues.LookToScrollActiveKey];
                KeyDownStates originalState = activeKey.Value;

                // Make sure look to scroll is currently active. Otherwise, there's nothing to suspend or resume.
                if (originalState.IsDownOrLockedDown())
                {
                    // Force scrolling to stop by releasing the LookToScrollActiveKey.
                    activeKey.Value = KeyDownStates.Up;

                    // If configured to resume afterwards, just reapply the original state of the key so the user doesn't have 
                    // to rechoose the bounds. Otherwise, the user will have to press the key themselves and potentially rechoose 
                    // the bounds (depending on the state of the bounds key). 
                    if (Settings.Default.LookToScrollResumeAfterChoosingPointForMouse)
                    {
                        Log.Info("Look to scroll has suspended.");

                        resumeAction = () =>
                        {
                            activeKey.Value = originalState;
                            Log.Info("Look to scroll has resumed.");
                        };
                    }
                    else
                    {
                        Log.Info("Look to scroll has been suspended and will not automatically resume.");
                    }
                }
            }

            return resumeAction;
        }

        private void DeactivateLookToScrollUponSwitchingKeyboards()
        {
            if (Settings.Default.LookToScrollDeactivateUponSwitchingKeyboards)
            {
                keyStateService.KeyDownStates[KeyValues.LookToScrollActiveKey].Value = KeyDownStates.Up;
                Log.Info("Look to scroll is no longer active.");
            }
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
    }
}
