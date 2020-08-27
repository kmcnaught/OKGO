// Copyright (c) K McNaught Consulting (UK company number 11297717) - All Rights Reserved

using System;
using System.Windows;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Native;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.Static;
using log4net;
using Prism.Mvvm;

namespace JuliusSweetland.OptiKey.UI.ViewModels
{
    public class Look2DInteractionHandler : BindableBase, ILookToScrollOverlayViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private bool choosingBoundsTarget = false;
        private Point pointBoundsTarget = new Point();
        private DateTime? lastUpdate = null;
        private bool hasTarget = false;

        private readonly Action<float, float> updateAction;
        private readonly FunctionKeys triggerKey;  // This function key controls the handler

        private readonly IKeyStateService keyStateService;
        private readonly MainViewModel mainViewModel;

        private float scaleX = 1.0f;
        private float scaleY = 1.0f;

        public bool active = false;
        // FIXME: need state renaming for different active state: enabled vs active?

        public Look2DInteractionHandler(FunctionKeys triggerKey, Action<float, float> updateAction, 
            IKeyStateService keyStateService, MainViewModel mainViewModel)
        {
            this.triggerKey = triggerKey;
            this.updateAction = updateAction;
            this.keyStateService = keyStateService;
            this.mainViewModel = mainViewModel;
        }

        public void SetScaleFactor(float[] scaleXY)
        {
            scaleX = scaleXY[0];
            scaleY = scaleXY[1];
        }

        private float[] ParseScaleFromString(string s)
        {
            float xScale = 1.0f;
            float yScale = 1.0f;

            if (!String.IsNullOrEmpty(s))
            {
                try
                {
                    char[] delimChars = { ',' };
                    float[] parts = s.ToFloatArray(delimChars);
                    if (parts.Length == 1)
                    {
                        xScale = yScale = parts[0];
                    }
                    else if (parts.Length > 1)
                    {
                        xScale = parts[0];
                        yScale = parts[1];
                    }
                }
                catch (Exception e)
                {
                    Log.ErrorFormat("Couldn't parse scale {0}", s);
                }
            }

            float[] scale = { xScale, yScale }; ;
            return scale;
        }

        public bool Enable(KeyValue keyValue)
        {
            Log.InfoFormat("Activating 2D control: {0}", this.triggerKey);

            this.SetScaleFactor(ParseScaleFromString(keyValue.String));

            if (!hasTarget || keyStateService.KeyDownStates[KeyValues.ResetJoystickKey].Value == KeyDownStates.LockedDown)
            {
                // will set 'active' once complete
                ChooseLookToScrollBoundsTarget();
            }
            else
            {
                active = true;
            }

            return active;
        }

        public void Disable()
        {
            if (active)
            {
                active = false;
                IsLookToScrollActive = false;

                // Turn off any keys associated with this interaction handler
                foreach (var keyValue in keyStateService.KeyDownStates.Keys)
                {
                    if (keyValue.FunctionKey != null)
                    {
                        if (keyValue.FunctionKey == triggerKey)
                        {
                            keyStateService.KeyDownStates[keyValue].Value = KeyDownStates.Up;
                        }
                    }
                }

                Log.Info("Look to scroll is no longer active.");
                updateAction(0.0f, 0.0f);
            }
        }

        private void ChooseLookToScrollBoundsTarget()
        {
            Log.Info("Choosing look to scroll bounds target.");

            choosingBoundsTarget = true;
            pointBoundsTarget = new Point();

            Action<bool> callback = success =>
            {
                if (success)
                {
                    active = true;
                    hasTarget = true;

                    // Release the ResetJoystickKey if it was used
                    keyStateService.KeyDownStates[KeyValues.ResetJoystickKey].Value = KeyDownStates.Up;
                }
                else 
                {
                    // If a target wasn't successfully chosen, de-activate scrolling and release the bounds key.
                    this.Disable();
                }

                choosingBoundsTarget = false;
            };

            ChoosePointLookToScrollBoundsTarget(callback);
        }

        private void ChoosePointLookToScrollBoundsTarget(Action<bool> callback)
        {
            Log.Info("Choosing point on screen to use as the centre point for scrolling.");

            mainViewModel.SetupFinalClickAction(point =>
            {
                if (point.HasValue)
                {
                    Log.InfoFormat("User chose point: {0}.", point.Value);
                    pointBoundsTarget = point.Value;

                    if (Settings.Default.LookToScrollBringWindowToFrontAfterChoosingScreenPoint)
                    {
                        IntPtr hWnd = mainViewModel.HideCursorAndGetHwndForFrontmostWindowAtPoint(point.Value);

                        if (hWnd == IntPtr.Zero)
                        {
                            Log.Info("No valid window at the point to bring to the front.");
                        }
                        else if (!PInvoke.SetForegroundWindow(hWnd))
                        {
                            Log.WarnFormat("Could not bring window at the point, {0}, to the front.", hWnd);
                        }
                    }
                }

                mainViewModel.ResetAndCleanupAfterMouseAction();
                callback(point.HasValue);
            }, suppressMagnification: true);
        }


        public void UpdateLookToScroll(Point position)
        {
            if (!active)
                return;

            var thisUpdate = DateTime.Now;

            bool shouldUpdate = ShouldUpdateLookToScroll(position, out Rect bounds, out Point centre);

            if (shouldUpdate)
            {
                Log.DebugFormat("Updating look to scroll using position: {0}.", position);
                Log.DebugFormat("Current look to scroll bounds rect is: {0}.", bounds);
                Log.DebugFormat("Current look to scroll centre point is: {0}.", centre);

                Vector scrollAmount = CalculateLookToScrollVelocity(position, centre);
                PerformLookToScroll(scrollAmount);
            }
            else
            {
                updateAction(0.0f, 0.0f);
            }

            UpdateLookToScrollOverlayProperties(active, bounds, centre);

            lastUpdate = thisUpdate;
        }

        private bool ShouldUpdateLookToScroll(Point position, out Rect bounds, out Point centre)
        {
            bounds = Rect.Empty;
            centre = new Point();

            if (keyStateService.KeyDownStates[KeyValues.SleepKey].Value.IsDownOrLockedDown() ||
                mainViewModel.IsPointInsideMainWindow(position) ||
                choosingBoundsTarget ||
                !lastUpdate.HasValue)
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

            return bounds.Contains(position);
        }

        private Rect? GetCurrentLookToScrollBoundsRect()
        {
            Rect? bounds = mainViewModel.IsMainWindowDocked()
                ? mainViewModel.FindLargestGapBetweenScreenAndMainWindow()
                : mainViewModel.GetVirtualScreenBoundsInPixels();

            return bounds;
        }

        private Point GetCurrentLookToScrollCentrePoint(Rect bounds)
        {
            return pointBoundsTarget;
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

            Log.InfoFormat("current: {0}, centre: {1}, accel: {2}, velocity: {3}", current, centre, acceleration, sign * speed);

            // Give the speed the correct direction.
            return sign * speed;
        }

        private void PerformLookToScroll(Vector scrollAmount)
        {
            updateAction(scaleX*(float)scrollAmount.X, scaleY*(float)scrollAmount.Y);
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

        public Action SuspendLookToScrollWhileChoosingPointForMouse()
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

    }
}