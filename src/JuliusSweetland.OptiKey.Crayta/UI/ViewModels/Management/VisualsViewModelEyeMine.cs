// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Collections.Generic;
using System.Windows;
using JuliusSweetland.OptiKey.Crayta.Properties;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.UI.ViewModels.Management;
using log4net;
using MahApps.Metro.Controls;
using Prism.Mvvm;
using FontStretches = JuliusSweetland.OptiKey.Enums.FontStretches;
using FontWeights = JuliusSweetland.OptiKey.Enums.FontWeights;
using ResourcesCore = JuliusSweetland.OptiKey.Properties.Resources;
using ResourcesEyeMine = JuliusSweetland.OptiKey.Crayta.Properties.Resources;
using EnumsCore = JuliusSweetland.OptiKey.Enums;

using Settings = JuliusSweetland.OptiKey.Crayta.Properties.Settings;

namespace JuliusSweetland.OptiKey.Crayta.UI.ViewModels.Management
{
    // Extend the visuals view model with EyeMine-specific stuff

    public class VisualsViewModelEyeMine : VisualsViewModel
    {
        #region Private Member Var

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IWindowManipulationService windowManipulationService;

        #endregion

        #region Ctor

        public VisualsViewModelEyeMine(IWindowManipulationService windowManipulationService)
            : base(windowManipulationService)
        {
            this.windowManipulationService = windowManipulationService;
            this.Load();
        }


        #endregion

        #region Properties

	    private Enums.StartupKeyboardOptions startupKeyboardOption;
        public Enums.StartupKeyboardOptions StartupKeyboardOption
        {
            get { return startupKeyboardOption; }
            set { SetProperty(ref startupKeyboardOption, value); }
        }

        private double leftBorder;
        public double LeftBorder
        {
            get { return leftBorder; }
            set { SetProperty(ref leftBorder, value); }
        }

        private double rightBorder;
        public double RightBorder
        {
            get { return rightBorder; }
            set { SetProperty(ref rightBorder, value); }
        }

        private double topBorder;
        public double TopBorder
        {
            get { return topBorder; }
            set { SetProperty(ref topBorder, value); }
        }

        private double bottomBorder;
        public double BottomBorder
        {
            get { return bottomBorder; }
            set { SetProperty(ref bottomBorder, value); }
        }

        public List<KeyValuePair<string, Enums.StartupKeyboardOptions>> StartupOptionsList
        {
            get
            {
                return new List<KeyValuePair<string, Enums.StartupKeyboardOptions>>
                {
                    new KeyValuePair<string, Enums.StartupKeyboardOptions>(ResourcesEyeMine.EYEMINE_ALL_KEYBOARDS, Enums.StartupKeyboardOptions.EyeMineAllKeyboards),
                    new KeyValuePair<string, Enums.StartupKeyboardOptions>(ResourcesEyeMine.CUSTOM_KEYBOARDS_FOLDER, Enums.StartupKeyboardOptions.CustomKeyboardsFolder),
                    new KeyValuePair<string, Enums.StartupKeyboardOptions>(ResourcesEyeMine.CUSTOM_KEYBOARD_FILE, Enums.StartupKeyboardOptions.CustomKeyboardFile),
                };
            }
        }

        private string customDynamicKeyboardsLocation;
        public string CustomDynamicKeyboardsLocation
        {
            get { return customDynamicKeyboardsLocation; }
            set { SetProperty(ref customDynamicKeyboardsLocation, value); }
        }

        #endregion

        #region Methods

        public new bool ChangesRequireRestart
        {
            get
            {
                return base.ChangesRequireRestart ||
                       Settings.Default.StartupKeyboard != StartupKeyboard
                       || Settings.Default.StartupKeyboardFile != StartupKeyboardFile
                       || StartupKeyboardOption != Settings.Default.EyeMineStartupKeyboard
                       || Settings.Default.OwnDynamicKeyboardsLocation != CustomDynamicKeyboardsLocation;
            }
        }

        private void Load()
        {
            CustomDynamicKeyboardsLocation = Settings.Default.OwnDynamicKeyboardsLocation;
            
            DockPosition = Settings.Default.MainWindowDockPosition;
            MainWindowState = Settings.Default.MainWindowState;

            var border = Settings.Default.BorderThickness;
            LeftBorder = border.Left;
            RightBorder = border.Right;
            TopBorder = border.Top;
            BottomBorder = border.Bottom;

            StartupKeyboardOption = Settings.Default.EyeMineStartupKeyboard;
        }

        public new void ApplyChanges()
        {
            base.ApplyChanges();

            // Plus own stuff as appropriate
            Settings.Default.OwnDynamicKeyboardsLocation = CustomDynamicKeyboardsLocation;
            // the actual CustomDynamicKeyboardsLocation will get appropriately propagated at next startup

            Settings.Default.BorderThickness = new Thickness(LeftBorder, TopBorder, RightBorder, BottomBorder);
            Settings.Default.EyeMineStartupKeyboard = StartupKeyboardOption;
        }

        #endregion
    }
}
