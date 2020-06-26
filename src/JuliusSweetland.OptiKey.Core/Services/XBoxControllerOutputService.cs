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

                if (type == KeyPressKeyValue.KeyPressType.Press)
                    controller.SetButtonState(xboxButton, false);
                else if (type == KeyPressKeyValue.KeyPressType.Release)
                    controller.SetButtonState(xboxButton, true);
                else
                {
                    controller.SetButtonState(xboxButton, true);
                    // FIXME: async!
                    System.Threading.Thread.Sleep(50);
                    controller.SetButtonState(xboxButton, false);
                }

                return;
            }
            else
            {
                Log.ErrorFormat("Could not parse xbox button: {0}", inKey);
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