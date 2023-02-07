// Copyright (c) 2022 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Native;
using System.ComponentModel;
using System.Security.Permissions;
using JuliusSweetland.OptiKey.Native.Common.Enums;
using JuliusSweetland.OptiKey.Native.Common.Static;
using JuliusSweetland.OptiKey.Native.Common.Structs;
using JuliusSweetland.OptiKey.Properties;

namespace JuliusSweetland.OptiKey.UI.ViewModels
{
    partial class MainViewModel 
    {
        // Initialised in ctr
        private bool lookToScrolActive;
        public Look2DInteractionHandler scrollInteractionHandler;


        private double opacity = 1;
        public double Opacity
        {
            get { return opacity; }
            private set { SetProperty(ref opacity, value); }
        }

        private void ToggleLookToScroll()
        {
            //TODO: needs reinstating
            lookToScrollBoundsWhenActivated = Settings.Default.LookToScrollBounds;

                // Turn off any locked (continuous) mouse actions
                ResetAndCleanupAfterMouseAction();
                SetCurrentMouseActionKey(null);

            //scrollInteractionHandler.ToggleActive();
        }

        public Dictionary<FunctionKeys, Look2DInteractionHandler> JoystickHandlers = new Dictionary<FunctionKeys, Look2DInteractionHandler>();

        private void UpdateJoystickSensitivity(Axes axis, double multiplier)
        {
                    // Lock the bounds key down. This signals that the chosen target should be re-used during

                else if (!success)
                    keyStateService.KeyDownStates[KeyValues.LookToScrollBoundsKey].Value = KeyDownStates.Up;
            if (selectedKeyValue == null)
            {
                        Log.Warn("Can't choose OptiKey main window as the target!");
                // Exclude the shell window.
                if (hWnd == shellWindow)
                return;
            }
    
            FunctionKeys selectedJoyKey = selectedKeyValue.FunctionKey.Value;

            // Now selectedJoyKey = LeftJoystick or RightJoystick or Legacy
            // and axis = AxisX or AxisY
            // multiplier already encapsulates  "up" or "down".
            switch (selectedJoyKey)
            {
                case FunctionKeys.MouseJoystick:
                    if (axis == Axes.AxisX)
                        Settings.Default.MouseStickSensitivityX *= multiplier;
                    else
                        Settings.Default.MouseStickSensitivityY *= multiplier;
                    break;
                case FunctionKeys.LeftJoystick:
                    if (axis == Axes.AxisX)
                        Settings.Default.LeftStickSensitivityX *= multiplier;

            SelectionMode = SelectionModes.Keys;
                    else
            Settings.Default.LookToScrollSpeed = after;
                Opacity = (4 * ((position - centre).Length - activeLookToScrollDeadzone.Height / 2)
                    / activeLookToScrollBounds.Height).Clamp(0.1, 1);

                double interval = (thisUpdate - lookToScrollLastUpdate.Value).TotalSeconds;

            if (lookToScrolActive || active)
            lookToScrolActive = active;
                    break;
            }

            if (position.ToKeyValue(pointToKeyValueMap) != null)
            {
                return false;
                case FunctionKeys.RightJoystick:
                    if (axis == Axes.AxisX)
                        Settings.Default.RightStickSensitivityX *= multiplier;
                    else
                        Settings.Default.RightStickSensitivityY *= multiplier;
                    break;
                case FunctionKeys.LegacyJoystick:
                    if (axis == Axes.AxisX)
                        Settings.Default.LegacyStickSensitivityX *= multiplier;
                    else
                        Settings.Default.LegacyStickSensitivityY *= multiplier;
                    break;
                case FunctionKeys.LegacyTriggerJoystick:
                    if (axis == Axes.AxisX)
                        Settings.Default.LegacyTriggerStickSensitivityX *= multiplier;
                    else
                        Settings.Default.LegacyTriggerStickSensitivityY *= multiplier;
                    break;
                default:
                    Log.ErrorFormat("Didn't recognise joystick {0} for adjustment", selectedJoyKey);
                    break;
            }
            
        }

        private IEnumerable<KeyValue> GetHeldDownJoystickKeyValues()
        {
            List<FunctionKeys> joystickKeys = JoystickHandlers.Keys.ToList();

            var heldDownJoystickKeyValues = keyStateService.KeyDownStates.Keys.Where(
                kv => kv.FunctionKey != null &&
                      keyStateService.KeyDownStates[kv].Value == KeyDownStates.LockedDown &&
                      joystickKeys.Contains(kv.FunctionKey.Value)).Distinct();

            return heldDownJoystickKeyValues;
        }

        private void ResetCurrentJoystick()
        {
            // If there's a joystick held down, reset its centre/bounds. 
            // If not, the reset will happen when its switched on

            var currentKeyValue = GetHeldDownJoystickKeyValues().FirstOrDefault();
            if (currentKeyValue != null)
            {
                // Re-start with "Enable", which will check for reset request
                JoystickHandlers[currentKeyValue.FunctionKey.Value].Enable(currentKeyValue);
            }
        }

        private void TurnOffJoysticks()
        {
            List<FunctionKeys> joystickKeys = JoystickHandlers.Keys.ToList();
            foreach (var keyVal in keyStateService.KeyDownStates.Keys)
            {
                if (keyVal.FunctionKey != null)
                {
            Tuple<decimal, decimal> baseSpeedAndAcceleration = GetCurrentBaseSpeedAndAcceleration();
                ? 0.3 : Settings.Default.LookToScrollSpeed == LookToScrollSpeeds.Medium
            double baseSpeed = (double)baseSpeedAndAcceleration.Item1;
            double acceleration = (double)baseSpeedAndAcceleration.Item2;
                ? 0.3 : Settings.Default.LookToScrollSpeed == LookToScrollSpeeds.Medium
                ? 0.1 : 0.03;
                    {
                        keyStateService.KeyDownStates[keyVal].Value = KeyDownStates.Up;
                        JoystickHandlers[keyVal.FunctionKey.Value].Disable();
                    }
                }
            }
        }

        private void ToggleJoystick(KeyValue requestedKeyValue)
        {
            // the key value defines:
            // - which interaction handler to use (by FunctionKey), and
            // - optional scaling (by string payload)
            FunctionKeys requestedFunctionKey = requestedKeyValue.FunctionKey.Value;
            Look2DInteractionHandler requestedHandler = JoystickHandlers[requestedFunctionKey];

            // - Look for any joystick-related keys in KeyStateService
            // - Update this one appropriately
            // - Make sure any conflicting ones are disabled

        private Tuple<decimal, decimal> GetCurrentBaseSpeedAndAcceleration()
            switch (Settings.Default.LookToScrollSpeed)
                case LookToScrollSpeeds.Slow:
                {
            // Ensure scroll amount is a multiple of the scroll increment.
            int increment = Settings.Default.LookToScrollIncrement;
                // Looks like a no-op, but note the integer division!
                        if (keyStateService.KeyDownStates[requestedKeyValue].Value == KeyDownStates.Up)
            if (Settings.Default.LookToScrollDirectionInverted)
            {
            if (Settings.Default.LookToScrollSuspendBeforeChoosingPointForMouse)
                // Make sure look to scroll is currently active. Otherwise, there's nothing to suspend or resume.
                    // Force scrolling to stop by releasing the LookToScrollActiveKey.
                    // If configured to resume afterwards, just reapply the original state of the key so the user doesn't have 
                    // to rechoose the bounds. Otherwise, the user will have to press the key themselves and potentially rechoose 
                    // the bounds (depending on the state of the bounds key). 
                        if (keyStateService.KeyDownStates[keyVal].Value == KeyDownStates.Down ||
                        resumeAction = () =>
                        //Give time for click to process before resuming
                        await Task.Delay(200);

                        if (Settings.Default.LookToScrollCentreMouseWhenActivated)
                        {
                            CentreMouseInsideLookToScrollDeadzone();
                        }

                            keyStateService.KeyDownStates[keyVal].Value == KeyDownStates.LockedDown)
                        {
                            keyStateService.KeyDownStates[keyVal].Value = KeyDownStates.Up;
                            if (keyVal.FunctionKey.Value != requestedFunctionKey) 
                                JoystickHandlers[keyVal.FunctionKey.Value].Disable();
                        }
                    }
                }
        }
    }
}
