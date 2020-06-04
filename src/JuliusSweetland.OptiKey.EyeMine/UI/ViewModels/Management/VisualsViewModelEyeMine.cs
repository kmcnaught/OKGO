// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Collections.Generic;
using System.Windows;
using JuliusSweetland.OptiKey.EyeMine.Properties;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.UI.ViewModels.Management;
using log4net;
using MahApps.Metro.Controls;
using Prism.Mvvm;
using FontStretches = JuliusSweetland.OptiKey.Enums.FontStretches;
using FontWeights = JuliusSweetland.OptiKey.Enums.FontWeights;
using ResourcesCore = JuliusSweetland.OptiKey.Properties.Resources;
using ResourcesEyeMine = JuliusSweetland.OptiKey.EyeMine.Properties.Resources;
using EnumsCore = JuliusSweetland.OptiKey.Enums;

using Settings = JuliusSweetland.OptiKey.EyeMine.Properties.Settings;

namespace JuliusSweetland.OptiKey.EyeMine.UI.ViewModels.Management
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


        public List<KeyValuePair<string, EnumsCore.DockEdges>> DockPositions
        {
            get
            {
                return new List<KeyValuePair<string, EnumsCore.DockEdges>>
                {
                    new KeyValuePair<string, EnumsCore.DockEdges>(ResourcesCore.TOP, EnumsCore.DockEdges.Top),
                    new KeyValuePair<string, EnumsCore.DockEdges>(ResourcesCore.BOTTOM, EnumsCore.DockEdges.Bottom),
                    new KeyValuePair<string, EnumsCore.DockEdges>(ResourcesCore.LEFT, EnumsCore.DockEdges.Left),
                    new KeyValuePair<string, EnumsCore.DockEdges>(ResourcesCore.RIGHT, EnumsCore.DockEdges.Right),
                };
            }
        }

        public List<KeyValuePair<string, EnumsCore.WindowStates>> MainWindowStates
        {
            get
            {
                return new List<KeyValuePair<string, EnumsCore.WindowStates>>
                {
                    new KeyValuePair<string, EnumsCore.WindowStates>("Floating", EnumsCore.WindowStates.Floating),
                    new KeyValuePair<string, EnumsCore.WindowStates>("Docked", EnumsCore.WindowStates.Docked),

                };
            }
        }

        private EnumsCore.DockEdges dockPosition;
        public EnumsCore.DockEdges DockPosition
        {
            get { return dockPosition; }
            set { SetProperty(ref dockPosition, value); }
        }

        private EnumsCore.WindowStates mainWindowState;
        public EnumsCore.WindowStates MainWindowState
        {
            get { return mainWindowState; }
            set { SetProperty(ref mainWindowState, value); }
        }

        private double mainWindowOpacity;
        public double MainWindowOpacity
        {
            get { return mainWindowOpacity; }
            set { SetProperty(ref mainWindowOpacity, value); }
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
                       || Settings.Default.CustomDynamicKeyboardsLocation != CustomDynamicKeyboardsLocation;

            }
        }

        private void Load()
        {
            CustomDynamicKeyboardsLocation = Settings.Default.CustomDynamicKeyboardsLocation;
            
            DockPosition = Settings.Default.MainWindowDockPosition;
            MainWindowState = Settings.Default.MainWindowState;
            MainWindowOpacity = Settings.Default.MainWindowOpacity;

            var border = Settings.Default.BorderThickness;
            LeftBorder = border.Left;
            RightBorder = border.Right;
            TopBorder = border.Top;
            BottomBorder = border.Bottom;
        }

        public new void ApplyChanges()
        {
            base.ApplyChanges();

            // Plus own stuff as appropriate

            Settings.Default.CustomDynamicKeyboardsLocation = CustomDynamicKeyboardsLocation;
            Settings.Default.BorderThickness = new Thickness(LeftBorder, TopBorder, RightBorder, BottomBorder);

            // Changes to window state, these methods will save the new values also
            if (Settings.Default.MainWindowState != MainWindowState ||
                Settings.Default.MainWindowDockPosition != DockPosition ||
                Settings.Default.MainWindowFullDockThicknessAsPercentageOfScreen.IsCloseTo(MainWindowFullDockThicknessAsPercentageOfScreen))
            {
                // this also saves the changes
                // AGH FIXME: needs PR to OptiKey..
                //windowManipulationService.ChangeState(MainWindowState, DockPosition);
            }
            windowManipulationService.SetOpacity(MainWindowOpacity);

        }

        #endregion
    }
}
