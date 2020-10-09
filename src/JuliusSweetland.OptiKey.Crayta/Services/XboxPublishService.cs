// Copyright (c) K McNaught Consulting Ltd (UK company number 11297717) - All Rights Reserved

using System;
using JuliusSweetland.OptiKey.Enums;
using log4net;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;


namespace JuliusSweetland.OptiKey.Crayta.Services
{
    // A controller which knows how to press / release
    // different keys / sliders / sticks.
    class XboxPublishService
    {

        private ViGEmClient client;
        private IXbox360Controller controller;
        private bool supportsController = false;

        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public XboxPublishService()
        {
            try
            {
                // TODO: extract this into a "EnsureConnected" method to be called whenever any methods 
                // try to use the client? 
                client = new ViGEmClient();
                controller = client.CreateXbox360Controller();
                controller.Connect();
                supportsController = true;
                Log.Info("Virtual Xbox360 gamepad connected.");
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Exception connecting to ViGem USB controller driver:\n{0}", e);
                supportsController = false;
            }
        }

        public static bool IsXboxKey(string keyString)
        {
            XboxButtons button;
            return Enum.TryParse(keyString, true, out button);
        }

        public void KeyDown(string keyString)
        {
            XboxButtons button;
            if (Enum.TryParse(keyString, true, out button))
            {
                Log.DebugFormat("Simulating button down {0}", button);
                Xbox360Button xboxButton = button.ToViGemButton();
                if (xboxButton != null)
                {
                    controller.SetButtonState(xboxButton, true);
                    return;
                }

                Xbox360Axis axis = button.ToViGemAxis();
                float amount = button.ToAxisAmount();
                if (axis != null)
                {
                    controller.SetAxisValue(axis, (short) (Int16.MaxValue * amount));
                    return;
                }

                // Triggers are analogue 'sliders', but we'll treat them as buttons
                Xbox360Slider slider = button.ToViGemSlider();
                if (slider != null)
                {
                    controller.SetSliderValue(slider, (byte) (255 * amount));
                }
            }
        }

        public void KeyUp(string keyString)
        {
            XboxButtons button;
            if (Enum.TryParse(keyString, true, out button))
            {
                Log.DebugFormat("Simulating button up: {0}", button);
                Xbox360Button xboxButton = button.ToViGemButton();
                if (xboxButton != null)
                {
                    controller.SetButtonState(xboxButton, false);
                    return;
                }

                Xbox360Axis axis = button.ToViGemAxis();
                float amount = button.ToAxisAmount();
                if (axis != null)
                {
                    controller.SetAxisValue(axis, (short) (0));
                    return;
                }

                // Triggers are analogue 'sliders', but we'll treat them as buttons
                Xbox360Slider slider = button.ToViGemSlider();
                if (slider != null)
                {
                    controller.SetSliderValue(slider, (byte) (0));
                }
            }
        }
    }
}
