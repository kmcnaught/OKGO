// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Threading.Tasks;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Models;
using log4net;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Prism.Mvvm;

namespace JuliusSweetland.OptiKey.Services
{
    public class XBoxControllerOutputService : BindableBase, IControllerOutputService
    {
        #region Private Member Vars

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ViGEmClient client;
        private IXbox360Controller controller;

        private readonly IKeyStateService keyStateService;

        #endregion

        #region Ctor

        public XBoxControllerOutputService(
            IKeyStateService keyStateService)
        {
            this.keyStateService = keyStateService;

            ReactToShiftStateChanges();
            ReactToPublishableKeyDownStateChanges();

            client = new ViGEmClient();
            controller = client.CreateXbox360Controller();
            controller.Connect();
            Log.Info("Virtual Xbox360 gamepad connected.");
        }

        #endregion

        #region Properties
        

        #endregion

        #region Methods - IKeyboardOutputService

        public async Task ProcessKeyPress(string inKey, KeyPressKeyValue.KeyPressType type)
        {
            Log.InfoFormat("ProcessSingleKeyPress called for key [{0}] press type [{1}]", inKey, type);

            XboxButtons button;
            if (Enum.TryParse(inKey, true, out button))
            {
                Xbox360Button xboxButton = button.ToViGemButton();
                if (xboxButton != null)
                {
                    if (type == KeyPressKeyValue.KeyPressType.Press)
                        controller.SetButtonState(xboxButton, true);
                    else if (type == KeyPressKeyValue.KeyPressType.Release)
                        controller.SetButtonState(xboxButton, false);
                    else
                    {
                        controller.SetButtonState(xboxButton, true);
                        await Task.Delay(50);
                        controller.SetButtonState(xboxButton, false);
                    }

                    return;
                }

                Xbox360Axis axis = button.ToViGemAxis();
                float amount = button.ToAxisAmount();
                if (axis != null)
                {
                    if (type == KeyPressKeyValue.KeyPressType.Press)
                        controller.SetAxisValue(axis, (short)(Int16.MaxValue * amount));
                    else if (type == KeyPressKeyValue.KeyPressType.Release)
                        controller.SetAxisValue(axis, 0);
                    else
                    {
                        controller.SetAxisValue(axis, (short)(Int16.MaxValue * amount));
                        await Task.Delay(50);
                        controller.SetAxisValue(axis, 0);
                    }
                }
            }
            else
            {
                Log.ErrorFormat("Could not parse xbox button: {0}", inKey);
            }
        }


        public async Task ProcessJoystick(string key, float amount)
        {
            XboxAxes axisEnum;
            if (Enum.TryParse(key, true, out axisEnum))
            {
                // amount is in range [-1.0, +1.0] and needs scaling to 
                // (signed) short range
                Xbox360Axis axis = axisEnum.ToViGemAxis();
                controller.SetAxisValue(axis, (short)(Int16.MaxValue*amount));
            }
        }


        #endregion

        #region Methods - private

        private void ReactToShiftStateChanges()
        {
            // FIXME: reinstate??
        }

        private void ReactToPublishableKeyDownStateChanges()
        {
            // FIXME: reinstate some key state handling here
        }

        #endregion
    }
}