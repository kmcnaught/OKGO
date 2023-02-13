﻿// Copyright (c) K McNaught Consulting Ltd (UK company number 11297717) - All Rights Reserved
// based on GPL3 code Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Native.Common.Enums;
using JuliusSweetland.OptiKey.Native.Common.Static;
using JuliusSweetland.OptiKey.Native.Common.Structs;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Static;

namespace JuliusSweetland.OptiKey.UI.ViewModels
{
    partial class MainViewModel
    {
        // Initialised in ctr
        public Look2DInteractionHandler scrollInteractionHandler;


        private void ToggleLookToScroll()
        {
            //TODO: needs reinstating
            //scrollInteractionHandler.ToggleActive();
        }

        public Dictionary<FunctionKeys, Look2DInteractionHandler> JoystickHandlers = new Dictionary<FunctionKeys, Look2DInteractionHandler>();

        private void UpdateJoystickSensitivity(Axes axis, double multiplier)
        {
            // Apply changes to settings for currently-selected joystick, e.g. Left / Right / Legacy

            var selectedKeyValue = GetHeldDownJoystickKeyValues().FirstOrDefault();
            if (selectedKeyValue == null)
            {
                Log.Error("Attempting sensitivity adjustment without any joysticks enabled");
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
                    else
                        Settings.Default.LeftStickSensitivityY *= multiplier;
                    break;
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
                SelectionMode = SelectionModes.SinglePoint;
                // Re-start with "Enable", which will check for reset request
                JoystickHandlers[currentKeyValue.FunctionKey.Value].Enable(currentKeyValue);
            }
        }

        private void TurnOffJoysticks()
        {
            List<FunctionKeys> joystickKeys = JoystickHandlers.Keys.ToList();
            foreach (var keyValTop in keyStateService.KeyDownStates.Keys)
            {
                foreach (var keyValNested in keyValTop.AllKeyValues) {
                    if (keyValNested?.FunctionKey != null)
                    {
                        if (joystickKeys.Contains(keyValNested.FunctionKey.Value))
                        {
                            keyStateService.KeyDownStates[keyValTop].Value = KeyDownStates.Up;
                            JoystickHandlers[keyValNested.FunctionKey.Value].Disable();
                        }
                    }
                }
            }
        }        

        private void SetJoystickCentre(KeyValue requestedKeyValue)
        {
            //FunctionKeys? joystick = requestedKeyValue.FunctionKey;
            string payload = requestedKeyValue.String.RemoveWhitespace();

            if (!String.IsNullOrEmpty(payload))
            {
                try
                {
                    char[] delimColon = { ':' };
                    char[] delimComma = { ',' };

                    string[] parts = payload.Split(delimColon, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        string joystickString = parts[0], pointString = parts[1];

                        if (Enum.TryParse(joystickString, out FunctionKeys joystickKey))
                        {
                            //bool relative = pointString.EndsWith("%");

                            string[] pointParts = pointString.Split(delimComma, StringSplitOptions.RemoveEmptyEntries);

                            if (pointParts.Length == 2)
                            {
                                Func<string, double, int> parseNumberAsPosition = (string inputString, double scale) =>
                                {
                                // If ends in "%" parse as percentage
                                if (inputString.EndsWith("%"))
                                    {
                                        inputString = inputString.Remove(inputString.Length - 1);
                                        float amount = Convert.ToSingle(inputString);
                                        return (int)(amount * scale / 100.0f);
                                    }
                                    else
                                    {
                                        float amount = Convert.ToSingle(inputString);
                                    // If less than 1.0, parse as fraction
                                    if (amount <= 1.0)
                                        {
                                            return (int)(amount * scale);
                                        }
                                        else
                                        {
                                        // Else parse as absoluate
                                        return (int)amount;
                                        }
                                    }
                                };

                                int x = parseNumberAsPosition(pointParts[0], Graphics.PrimaryScreenWidthInPixels);
                                int y = parseNumberAsPosition(pointParts[1], Graphics.PrimaryScreenHeightInPixels);

                                Look2DInteractionHandler requestedHandler = JoystickHandlers[joystickKey];
                                requestedHandler.SetJoystickCentre((int)x, (int)y);
                                return;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // Logged below
                }
            }
            Log.Error($"Cannot parse string: \"{ payload }\" as screen point for joystick centre");                
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

            var requestedKeyState = keyStateService.KeyDownStates[requestedKeyValue].Value;

            // turn off all keys
            List<FunctionKeys> joystickKeys = JoystickHandlers.Keys.ToList();

            foreach (var keyValTop in keyStateService.KeyDownStates.Keys)
            {
                foreach (var keyValNested in keyValTop.AllKeyValues)
                {
                    if (keyValNested?.FunctionKey != null)
                    {
                        // Turn off all relevant keys - we'll re-enable the requested one if appropriate
                        if (joystickKeys.Contains(keyValNested.FunctionKey.Value))
                        {
                            // Any other key which should be mutually-exclusive. 
                            // Disable button and joystick 
                            if (keyStateService.KeyDownStates[keyValTop].Value == KeyDownStates.Down ||
                                keyStateService.KeyDownStates[keyValTop].Value == KeyDownStates.LockedDown)
                            {
                                keyStateService.KeyDownStates[keyValTop].Value = KeyDownStates.Up;
                                JoystickHandlers[keyValNested.FunctionKey.Value].Disable();
                            }
                        }
                    }
                }
            }

            // Now set joystick state on requested key according to button state
            keyStateService.KeyDownStates[requestedKeyValue].Value = requestedKeyState; // turn back on if disabled by previous step
            if (requestedKeyState == KeyDownStates.Up)
                JoystickHandlers[requestedFunctionKey].Disable();
            else
                JoystickHandlers[requestedFunctionKey].Enable(requestedKeyValue);
        }
    }
}
