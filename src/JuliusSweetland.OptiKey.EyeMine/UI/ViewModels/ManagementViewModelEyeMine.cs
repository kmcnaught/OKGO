// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System.Linq;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.UI.ViewModels.Management;
using log4net;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System.Windows;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.EyeMine.UI.ViewModels.Management;

namespace JuliusSweetland.OptiKey.UI.ViewModels
{
    public class ManagementViewModelEyeMine : ManagementViewModel
    {
        #region Private Member Vars

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion
        
        #region Ctor

        public ManagementViewModelEyeMine(
            IAudioService audioService,
            IDictionaryService dictionaryService,
            IWindowManipulationService windowManipulationService) :
            base(audioService, dictionaryService, windowManipulationService)
        {
            // Replace some VMs with our derived ones
            VisualsViewModel = new VisualsViewModelEyeMine(windowManipulationService);
        }
        
        #endregion
        
        #region Properties

        #endregion
        
        #region Methods

        #endregion
    }
}
