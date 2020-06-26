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
    public class NullControllerOutputService : BindableBase, IControllerOutputService
    {
        #region Private Member Vars

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ViGEmClient client;
        private IXbox360Controller controller;

        private readonly IKeyStateService keyStateService;

        #endregion

        #region Ctor

        public NullControllerOutputService(
            IKeyStateService keyStateService)
        {
            
        }

        #endregion

        #region Properties
        

        #endregion

        #region Methods - IKeyboardOutputService

        public async Task ProcessKeyPress(string inKey, KeyPressKeyValue.KeyPressType type)
        {
            Log.ErrorFormat("ProcessKeyPress ({0}, {1}) called with no controller output service set up", inKey, type);
        }

        
        #endregion

        #region Methods - private
        
        #endregion
    }
}