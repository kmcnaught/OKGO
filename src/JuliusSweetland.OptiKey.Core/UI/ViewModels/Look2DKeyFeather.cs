// Copyright (c) K McNaught Consulting (UK company number 11297717) - All Rights Reserved

using System;
using System.Collections.Generic;
using System.Windows;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Models.ScalingModels;
using JuliusSweetland.OptiKey.Native;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.Static;
using log4net;
using WindowsInput.Native;

namespace JuliusSweetland.OptiKey.UI.ViewModels
{
    // This class should eventually be a generic handler for 2D interactions. It currently has some joystick-specific 
    // logic leaking in. 
    public class Look2DKeyFeather: Look2DInteractionHandler // TODO: use base class instead
    {
        #region Fields

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        IKeyboardOutputService keyboardOutputService;

        

        private enum DirectionKeys
        {
            Up, 
            Down,
            Left,
            Right
        }

        Dictionary<DirectionKeys, VirtualKeyCode> keyMappings;
        Dictionary<DirectionKeys, bool> keyDownStates; // probably superfluous?
        Dictionary<DirectionKeys, DateTime> keyDownUpTimes; // keep track of when last changed (for 'active' keys)

        #endregion

        #region Constructor

        public Look2DKeyFeather(FunctionKeys triggerKey, 
            IKeyStateService keyStateService, 
            MainViewModel mainViewModel)            
            : base(triggerKey, (x, y) => { }, keyStateService, mainViewModel )
        {
            // Replace base update action method with our own
            this.updateAction = this.updateActionFeather;
            
            keyboardOutputService = mainViewModel.KeyboardOutputService;

            // Default WASD
            ConfigureKeys(WindowsInput.Native.VirtualKeyCode.VK_W,
                          WindowsInput.Native.VirtualKeyCode.VK_A,
                          WindowsInput.Native.VirtualKeyCode.VK_S,
                          WindowsInput.Native.VirtualKeyCode.VK_D);
        }

        void ConfigureKeys(VirtualKeyCode upKey,
                           VirtualKeyCode leftKey,
                           VirtualKeyCode downKey,
                           VirtualKeyCode rightKey)
        {

            keyMappings = new Dictionary<DirectionKeys, VirtualKeyCode>();
            keyMappings.Add(DirectionKeys.Up, upKey);
            keyMappings.Add(DirectionKeys.Left, leftKey);
            keyMappings.Add(DirectionKeys.Down, downKey);
            keyMappings.Add(DirectionKeys.Right, rightKey);

            keyDownStates = new Dictionary<DirectionKeys, bool>();
            keyDownStates.Add(DirectionKeys.Up, false);
            keyDownStates.Add(DirectionKeys.Left, false);
            keyDownStates.Add(DirectionKeys.Down, false);
            keyDownStates.Add(DirectionKeys.Right, false);

            keyDownUpTimes = new Dictionary<DirectionKeys, DateTime>();
            keyDownUpTimes.Add(DirectionKeys.Up, DateTime.MaxValue);
            keyDownUpTimes.Add(DirectionKeys.Left, DateTime.MaxValue);
            keyDownUpTimes.Add(DirectionKeys.Down, DateTime.MaxValue);
            keyDownUpTimes.Add(DirectionKeys.Right, DateTime.MaxValue);

        }

        #endregion
        private void UpdateKey(DirectionKeys key, bool active)
        {
            DateTime now = Time.HighResolutionUtcNow;
            if (active)
            {
                if (keyDownUpTimes[key] < now)
                {
                    // already pressed, no-op
                }
                else
                {
                    keyboardOutputService.PressKey(keyMappings[key], KeyPressKeyValue.KeyPressType.Press);
                    keyDownUpTimes[key] = now;
                }
            }
            else
            {
                if (keyDownUpTimes[key] < now) // was pressed
                {
                    keyboardOutputService.PressKey(keyMappings[key], KeyPressKeyValue.KeyPressType.Release);
                    keyDownUpTimes[key] = DateTime.MaxValue;
                }
            }
        }

        private void updateActionFeather(float x, float y)
        {
            Log.DebugFormat("wasdJoystickAction, ({0}, {1})", x, y);
            float eps = 1e-6f;
            DateTime now = Time.HighResolutionUtcNow;

            bool keyRightActive = x > eps;
            bool keyLeftActive = x < -eps;
            bool keyUpActive = y < -eps;
            bool keyDownActive = y > eps;

            UpdateKey(DirectionKeys.Left, keyLeftActive);
            UpdateKey(DirectionKeys.Right, keyRightActive);
            UpdateKey(DirectionKeys.Up, keyUpActive);
            UpdateKey(DirectionKeys.Down, keyDownActive);            
        }

    }
}