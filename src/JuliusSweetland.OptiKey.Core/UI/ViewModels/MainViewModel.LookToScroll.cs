// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
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
        private IntPtr windowBoundsTarget = IntPtr.Zero;
        private DateTime? lastUpdate = null;

        private Action<float, float> updateAction;
        private KeyValue triggerKeyValue;
        private IKeyStateService keyStateService;
        private MainViewModel mainViewModel;

        public Look2DInteractionHandler(KeyValue triggerKeyValue, Action<float, float> updateAction, 
                                        IKeyStateService keyStateService, MainViewModel mainViewModel)
        {
            this.triggerKeyValue = triggerKeyValue;
            this.updateAction = updateAction;
            this.keyStateService = keyStateService;
            this.mainViewModel = mainViewModel;
        }

        public void ToggleActive()
        {
            Log.InfoFormat("{0} key was selected.", this.triggerKeyValue);

            if (keyStateService.KeyDownStates[this.triggerKeyValue].Value.IsDownOrLockedDown())
            {
                Log.Info("Look to scroll is now active.");

                //FIXME: reinstate some way to re-use bounds
                ChooseLookToScrollBoundsTarget();
            }
            else
            {
                Log.Info("Look to scroll is no longer active.");
                updateAction(0.0f, 0.0f);
            }
        }

        private void ChooseLookToScrollBoundsTarget()
        {
            Log.Info("Choosing look to scroll bounds target.");

            choosingBoundsTarget = true;
            pointBoundsTarget = new Point();
            windowBoundsTarget = IntPtr.Zero;

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
                        else
                        {
                            windowBoundsTarget = hWnd;
                            Log.InfoFormat("Brought window at the point, {0}, to the front.", hWnd);
                        }
                    }
                }

                mainViewModel.ResetAndCleanupAfterMouseAction();
                callback(point.HasValue);
            }, suppressMagnification: true);
        }


        public void UpdateLookToScroll(Point position)
        {
            var thisUpdate = DateTime.Now;

            bool active = ShouldUpdateLookToScroll(position, out Rect bounds, out Point centre);

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
                updateAction(0.0f, 0.0f);
            }

            UpdateLookToScrollOverlayProperties(active, bounds, centre);

            lastUpdate = thisUpdate;
        }

        private bool ShouldUpdateLookToScroll(Point position, out Rect bounds, out Point centre)
        {
            bounds = Rect.Empty;
            centre = new Point();

            if (!keyStateService.KeyDownStates[KeyValues.LookToScrollActiveKey].Value.IsDownOrLockedDown() ||
                keyStateService.KeyDownStates[KeyValues.SleepKey].Value.IsDownOrLockedDown() ||
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

            // If using a window or portion of it as the bounds target, only scroll while pointing _at_ that window, 
            // not while pointing at another window on top of it.
            if (mainViewModel.GetHwndForFrontmostWindowAtPoint(position) != windowBoundsTarget)
            {
                // this keeps flicking on/off with stadia, not sure why :(
                return false;
            }

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
            Action reinstateModifiers = () => { };
            if (keyStateService.SimulateKeyStrokes && Settings.Default.SuppressModifierKeysForAllMouseActions)
            {
                reinstateModifiers = keyStateService.ReleaseModifiers(Log);
            }

            updateAction((float)scrollAmount.X, (float)scrollAmount.Y);
            
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

    partial class MainViewModel 
    {
        // Initialised in ctr
        public Look2DInteractionHandler joystickInteractionHandler;

        private void ToggleLookToScroll()
        {
            joystickInteractionHandler.ToggleActive();
        }

        
    }
}
