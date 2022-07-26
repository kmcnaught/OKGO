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


namespace JuliusSweetland.OptiKey.UI.ViewModels
{
    // This class should eventually be a generic handler for 2D interactions. It currently has some joystick-specific 
    // logic leaking in. 
    public class Look2DKeyFeather: Look2DInteractionHandler // TODO: use base class instead
    {
        #region Fields

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        IKeyboardOutputService keyboardOutputService;

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
        }

        #endregion

        private void updateActionFeather(float x, float y)
        {
            Log.DebugFormat("wasdJoystickAction, ({0}, {1})", x, y);
            float eps = 1e-6f;
            if (x > eps)
            {
                keyboardOutputService.PressKey(WindowsInput.Native.VirtualKeyCode.VK_D, KeyPressKeyValue.KeyPressType.Press);
                keyboardOutputService.PressKey(WindowsInput.Native.VirtualKeyCode.VK_A, KeyPressKeyValue.KeyPressType.Release);
            }
            else if (x < -eps)
            {
                keyboardOutputService.PressKey(WindowsInput.Native.VirtualKeyCode.VK_D, KeyPressKeyValue.KeyPressType.Release);
                keyboardOutputService.PressKey(WindowsInput.Native.VirtualKeyCode.VK_A, KeyPressKeyValue.KeyPressType.Press);
            }
            else
            {
                keyboardOutputService.PressKey(WindowsInput.Native.VirtualKeyCode.VK_D, KeyPressKeyValue.KeyPressType.Release);
                keyboardOutputService.PressKey(WindowsInput.Native.VirtualKeyCode.VK_A, KeyPressKeyValue.KeyPressType.Release);
            }

            if (y > eps)
            {
                keyboardOutputService.PressKey(WindowsInput.Native.VirtualKeyCode.VK_S, KeyPressKeyValue.KeyPressType.Press);
                keyboardOutputService.PressKey(WindowsInput.Native.VirtualKeyCode.VK_W, KeyPressKeyValue.KeyPressType.Release);
            }
            else if (y < -eps)
            {
                keyboardOutputService.PressKey(WindowsInput.Native.VirtualKeyCode.VK_S, KeyPressKeyValue.KeyPressType.Release);
                keyboardOutputService.PressKey(WindowsInput.Native.VirtualKeyCode.VK_W, KeyPressKeyValue.KeyPressType.Press);
            }
            else
            {
                keyboardOutputService.PressKey(WindowsInput.Native.VirtualKeyCode.VK_S, KeyPressKeyValue.KeyPressType.Release);
                keyboardOutputService.PressKey(WindowsInput.Native.VirtualKeyCode.VK_W, KeyPressKeyValue.KeyPressType.Release);
            }
        }

    }
}