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
    // This class should eventually be a generic handler for 2D interactions. It currently has some joystick-specific 
    // logic leaking in. 
    public class Look2DInteractionHandler : BindableBase, ILookToScrollOverlayViewModel
    {
        #region Fields

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IKeyStateService keyStateService;
        private readonly MainViewModel mainViewModel;

        // How this handler is configured (once)
        private readonly Action<float, float> updateAction;
        private readonly FunctionKeys triggerKey;  // This function key controls the handler
        private int keyPadding = 10; // controls when handling is paused over/near keys

        // How this handler is configured (may change)
        private Point? pointBoundsTarget;
        private float scaleX = 1.0f;
        private float scaleY = 1.0f;

        // Temporary state 
        private bool choosingBoundsTarget = false;
        private DateTime? lastUpdate = null;

        #endregion

        #region Constructor
        
        public Look2DInteractionHandler(FunctionKeys triggerKey, Action<float, float> updateAction, 
            IKeyStateService keyStateService, MainViewModel mainViewModel)
        {
            this.triggerKey = triggerKey;
            this.updateAction = updateAction;
            this.keyStateService = keyStateService;
            this.mainViewModel = mainViewModel;
        }

        #endregion

        #region Properties

        private bool isActive = false;
        public bool IsActive
        {
            get { return isActive; }
            private set { SetProperty(ref isActive, value); }
        }

        private Rect activeBounds = Rect.Empty;
        public Rect ActiveBounds
        {
            get { return activeBounds; }
            private set { SetProperty(ref activeBounds, value); }
        }

        private Rect activeDeadzone = Rect.Empty;
        public Rect ActiveDeadzone
        {
            get { return activeDeadzone; }
            private set { SetProperty(ref activeDeadzone, value); }
        }

        private Thickness activeMargins = new Thickness();
        public Thickness ActiveMargins
        {
            get { return activeMargins; }
            private set { SetProperty(ref activeMargins, value); }
        }

        #endregion

        #region Public methods

        public void Enable(KeyValue keyValue)
        {
            Log.InfoFormat("Activating 2D control: {0}", this.triggerKey);

            this.SetScaleFactor(ParseScaleFromString(keyValue.String));

            // Choose joystick centre via "Reset" key
            if (keyStateService.KeyDownStates[KeyValues.ResetJoystickKey].Value == KeyDownStates.LockedDown)
            {                
                ChooseLookToScrollBoundsTarget(false);
            }
            // Default to centre of screen
            else if (!pointBoundsTarget.HasValue) 
            {
                ChooseLookToScrollBoundsTarget(true);
            }
            else
            {
                IsActive = true;
            }
        }

        public void Disable()
        {
            if (IsActive)
            {
                IsActive = false;

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

        public void SetScaleFactor(float[] scaleXY)
        {
            scaleX = scaleXY[0];
            scaleY = scaleXY[1];
        }

        // Call this method with new gaze points
        public void UpdateLookToScroll(Point position)
        {
            if (!IsActive)
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

                UpdateLookToScrollOverlayProperties(bounds, centre);
            }
            else
            {
                updateAction(0.0f, 0.0f);
            }

            lastUpdate = thisUpdate;
        }

        #endregion


        #region Private methods

        private void ChooseLookToScrollBoundsTarget(bool useCentre = false)
        {
            Log.Info("Choosing look to scroll bounds target.");

            choosingBoundsTarget = true;
            pointBoundsTarget = null;

            Action<bool> callback = success =>
            {
                if (success)
                {
                    IsActive = true;

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

            if (useCentre)
            {
                ChooseScreenLookToScrollBoundsTarget(callback);
            }
            else
            {
                ChoosePointLookToScrollBoundsTarget(callback);
            }
        }
        
        private void ChooseScreenLookToScrollBoundsTarget(Action<bool> callback)
        {
            Log.Info("Will use entire usable portion of the screen as the scroll bounds.");
            pointBoundsTarget = GetCurrentLookToScrollBoundsRect().Value.CalculateCentre();
            callback(true); // Always successful.
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
                        mainViewModel.TryGrabFocusAtPoint(point.Value);
                    }
                }

                mainViewModel.ResetAndCleanupAfterMouseAction();
                callback(point.HasValue);
            }, suppressMagnification: true);
        }



        private bool ShouldUpdateLookToScroll(Point position, out Rect bounds, out Point centre)
        {
            bounds = Rect.Empty;
            centre = new Point();

            KeyValue keyVal = mainViewModel.PointToKeyValue(position);
            keyPadding = 25;

            if (keyStateService.KeyDownStates[KeyValues.SleepKey].Value.IsDownOrLockedDown() ||
                mainViewModel.IsPointInsideValidKey(position, keyPadding) ||
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

            if (!pointBoundsTarget.HasValue)
            {
                Log.Info("Look to scroll doesn't have target. Deactivating look to scroll.");
                return false;
            }

            bounds = boundsContainer.Value;
            centre = pointBoundsTarget.Value;

            return bounds.Contains(position);
        }

        private Rect? GetCurrentLookToScrollBoundsRect()
        {
            Rect? bounds = mainViewModel.IsMainWindowDocked()
                ? mainViewModel.FindLargestGapBetweenScreenAndMainWindow()
                : mainViewModel.GetPrimaryScreenBoundsInPixels();

            return bounds;
        }

        private Vector CalculateLookToScrollVelocity(Point current, Point centre)
        {
            double baseSpeed = 0;
            double acceleration = 0.02;
            double hDeadzone = (double)Settings.Default.JoystickHorizontalDeadzonePercentScreen * Graphics.PrimaryScreenWidthInPixels / 100.0d;
            double vDeadzone = hDeadzone / Settings.Default.JoystickDeadzoneAspectRatio;

            var velocity = new Vector { X = 0, Y = 0 };
            velocity.X = CalculateLookToScrollVelocity(
                current.X,
                centre.X,
                0.5f * hDeadzone,
                baseSpeed,
                acceleration
            );

            velocity.Y = CalculateLookToScrollVelocity(
                current.Y,
                centre.Y,
                0.5f * vDeadzone,
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

            //Log.InfoFormat("current: {0}, centre: {1}, accel: {2}, velocity: {3}", current, centre, acceleration, sign * speed);

            // Give the speed the correct direction.
            return sign * speed;
        }

        private void PerformLookToScroll(Vector scrollAmount)
        {
            updateAction(scaleX * (float)scrollAmount.X, scaleY * (float)scrollAmount.Y);
        }

        private void UpdateLookToScrollOverlayProperties(Rect bounds, Point centre)
        {
            double hDeadzone = (double)Settings.Default.JoystickHorizontalDeadzonePercentScreen * Graphics.PrimaryScreenWidthInPixels / 100.0d;
            double vDeadzone = hDeadzone / Settings.Default.JoystickDeadzoneAspectRatio;

            bool b = IsActive;

            var deadzone = new Rect
            {
                X = centre.X - (int)(hDeadzone/2.0),
                Y = centre.Y - (int)(vDeadzone/2.0),
                Width = hDeadzone,
                Height = vDeadzone,
            };

            IsActive = isActive;
            ActiveBounds = Graphics.PixelsToDips(bounds);
            ActiveDeadzone = Graphics.PixelsToDips(deadzone);
            ActiveMargins = Graphics.PixelsToDips(bounds.CalculateMarginsAround(deadzone));
        }

        private static float[] ParseScaleFromString(string s)
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

        #endregion
    }
}