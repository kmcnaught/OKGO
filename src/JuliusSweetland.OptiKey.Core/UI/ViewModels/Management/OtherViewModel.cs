// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using JuliusSweetland.OptiKey.Properties;
using log4net;
using Prism.Mvvm;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Management
{
    public class OtherViewModel : BindableBase
    {
        #region Private Member Vars

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion
        
        #region Ctor

        public OtherViewModel()
        {
            Load();
        }
        
        #endregion
        
        #region Properties

        public List<string> ColourNames
        {
            get
            {
                // Based on: https://stackoverflow.com/a/26287682/9091159
                return typeof(Brushes)
                    .GetProperties()
                    .Where(pi => pi.PropertyType == typeof(SolidColorBrush))
                    .Select(pi => pi.Name)
                    .ToList();
            }
        }

        private bool showSplashScreen;
        public bool ShowSplashScreen
        {
            get { return showSplashScreen; }
            set { SetProperty(ref showSplashScreen, value); }
        }

        private bool checkForUpdates;
        public bool CheckForUpdates
        {
            get { return checkForUpdates; }
            set { SetProperty(ref checkForUpdates, value); }
        }

        private bool publishVirtualKeyCodesForCharacters;
        public bool PublishVirtualKeyCodesForCharacters
        {
            get { return publishVirtualKeyCodesForCharacters; }
            set { SetProperty(ref publishVirtualKeyCodesForCharacters, value); }
        }

        private bool suppressModifierKeysForAllMouseActions;
        public bool SuppressModifierKeysForAllMouseActions
        {
            get { return suppressModifierKeysForAllMouseActions; }
            set { SetProperty(ref suppressModifierKeysForAllMouseActions, value); }
        }

        private bool suppressModifierKeysWhenInMouseKeyboard;
        public bool SuppressModifierKeysWhenInMouseKeyboard
        {
            get { return suppressModifierKeysWhenInMouseKeyboard; }
            set { SetProperty(ref suppressModifierKeysWhenInMouseKeyboard, value); }
        }

        private bool magnifySuppressedForScrollingActions;
        public bool MagnifySuppressedForScrollingActions
        {
            get { return magnifySuppressedForScrollingActions; }
            set { SetProperty(ref magnifySuppressedForScrollingActions, value); }
        }

        private bool debug;
        public bool Debug
        {
            get { return debug; }
            set { SetProperty(ref debug, value); }
        }        

        private bool lookToScrollCentreMouseWhenActivated;
        public bool LookToScrollCentreMouseWhenActivated
        {
            get { return lookToScrollCentreMouseWhenActivated; }
            set { SetProperty(ref lookToScrollCentreMouseWhenActivated, value); }
        }

        private bool lookToScrollBringWindowToFrontWhenActivated;
        public bool LookToScrollBringWindowToFrontWhenActivated
        {
            get { return lookToScrollBringWindowToFrontWhenActivated; }
            set { SetProperty(ref lookToScrollBringWindowToFrontWhenActivated, value); }
        }

        private bool lookToScrollBringWindowToFrontAfterChoosingScreenPoint;
        public bool LookToScrollBringWindowToFrontAfterChoosingScreenPoint
        {
            get { return lookToScrollBringWindowToFrontAfterChoosingScreenPoint; }
            set { SetProperty(ref lookToScrollBringWindowToFrontAfterChoosingScreenPoint, value); }
        }

        private bool lookToScrollResumeAfterChoosingPointForMouse;
        public bool LookToScrollResumeAfterChoosingPointForMouse
        {
            get { return lookToScrollResumeAfterChoosingPointForMouse; }
            set { SetProperty(ref lookToScrollResumeAfterChoosingPointForMouse, value); }
        }

        private bool lookToScrollDeactivateUponSwitchingKeyboards;
        public bool LookToScrollDeactivateUponSwitchingKeyboards
        {
            get { return lookToScrollDeactivateUponSwitchingKeyboards; }
            set { SetProperty(ref lookToScrollDeactivateUponSwitchingKeyboards, value); }
        }

        private bool lookToScrollShowOverlayWindow;
        public bool LookToScrollShowOverlayWindow
        {
            get { return lookToScrollShowOverlayWindow; }
            set { SetProperty(ref lookToScrollShowOverlayWindow, value); }
        }

        private string lookToScrollOverlayBoundsColour;
        public string LookToScrollOverlayBoundsColour
        {
            get { return lookToScrollOverlayBoundsColour; }
            set { SetProperty(ref lookToScrollOverlayBoundsColour, value); }
        }

        private string lookToScrollOverlayDeadzoneColour;
        public string LookToScrollOverlayDeadzoneColour
        {
            get { return lookToScrollOverlayDeadzoneColour; }
            set { SetProperty(ref lookToScrollOverlayDeadzoneColour, value); }
        }

        private int lookToScrollOverlayBoundsThickness;
        public int LookToScrollOverlayBoundsThickness
        {
            get { return lookToScrollOverlayBoundsThickness; }
            set { SetProperty(ref lookToScrollOverlayBoundsThickness, value); }
        }

        private int lookToScrollOverlayDeadzoneThickness;
        public int LookToScrollOverlayDeadzoneThickness
        {
            get { return lookToScrollOverlayDeadzoneThickness; }
            set { SetProperty(ref lookToScrollOverlayDeadzoneThickness, value); }
        }

        private int lookToScrollHorizontalDeadzone;
        public int LookToScrollHorizontalDeadzone
        {
            get { return lookToScrollHorizontalDeadzone; }
            set { SetProperty(ref lookToScrollHorizontalDeadzone, value); }
        }

        private int lookToScrollVerticalDeadzone;
        public int LookToScrollVerticalDeadzone
        {
            get { return lookToScrollVerticalDeadzone; }
            set { SetProperty(ref lookToScrollVerticalDeadzone, value); }
        }

        public bool ChangesRequireRestart
        {
            get
            {
                return (
                    Settings.Default.Debug != Debug
                );
            }
        }
        
        #endregion
        
        #region Methods

        private void Load()
        {
            ShowSplashScreen = Settings.Default.ShowSplashScreen;
            CheckForUpdates = Settings.Default.CheckForUpdates;
            PublishVirtualKeyCodesForCharacters = Settings.Default.PublishVirtualKeyCodesForCharacters;
            SuppressModifierKeysForAllMouseActions = Settings.Default.SuppressModifierKeysForAllMouseActions;
            SuppressModifierKeysWhenInMouseKeyboard = Settings.Default.SuppressModifierKeysWhenInMouseKeyboard;
            MagnifySuppressedForScrollingActions = Settings.Default.MagnifySuppressedForScrollingActions;
            Debug = Settings.Default.Debug;
            LookToScrollCentreMouseWhenActivated = Settings.Default.LookToScrollCentreMouseWhenActivated;
            LookToScrollBringWindowToFrontWhenActivated = Settings.Default.LookToScrollBringWindowToFrontWhenActivated;
            LookToScrollBringWindowToFrontAfterChoosingScreenPoint = Settings.Default.LookToScrollBringWindowToFrontAfterChoosingScreenPoint;
            LookToScrollResumeAfterChoosingPointForMouse = Settings.Default.LookToScrollResumeAfterChoosingPointForMouse;
            LookToScrollDeactivateUponSwitchingKeyboards = Settings.Default.LookToScrollDeactivateUponSwitchingKeyboards;
            LookToScrollShowOverlayWindow = Settings.Default.LookToScrollShowOverlayWindow;
            LookToScrollOverlayBoundsColour = Settings.Default.LookToScrollOverlayBoundsColour;
            LookToScrollOverlayDeadzoneColour = Settings.Default.LookToScrollOverlayDeadzoneColour;
            LookToScrollOverlayBoundsThickness = Settings.Default.LookToScrollOverlayBoundsThickness;
            LookToScrollOverlayDeadzoneThickness = Settings.Default.LookToScrollOverlayDeadzoneThickness;
            LookToScrollHorizontalDeadzone = Settings.Default.LookToScrollHorizontalDeadzone;
            LookToScrollVerticalDeadzone = Settings.Default.LookToScrollVerticalDeadzone;
            
        }

        public void ApplyChanges()
        {
            Settings.Default.ShowSplashScreen = ShowSplashScreen;
            Settings.Default.CheckForUpdates = CheckForUpdates;
            Settings.Default.PublishVirtualKeyCodesForCharacters = PublishVirtualKeyCodesForCharacters;
            Settings.Default.SuppressModifierKeysForAllMouseActions = SuppressModifierKeysForAllMouseActions;
            Settings.Default.SuppressModifierKeysWhenInMouseKeyboard = SuppressModifierKeysWhenInMouseKeyboard;
            Settings.Default.MagnifySuppressedForScrollingActions = MagnifySuppressedForScrollingActions;
            Settings.Default.Debug = Debug;
            Settings.Default.LookToScrollCentreMouseWhenActivated = LookToScrollCentreMouseWhenActivated;
            Settings.Default.LookToScrollBringWindowToFrontWhenActivated = LookToScrollBringWindowToFrontWhenActivated;
            Settings.Default.LookToScrollBringWindowToFrontAfterChoosingScreenPoint = LookToScrollBringWindowToFrontAfterChoosingScreenPoint;
            Settings.Default.LookToScrollResumeAfterChoosingPointForMouse = LookToScrollResumeAfterChoosingPointForMouse;
            Settings.Default.LookToScrollDeactivateUponSwitchingKeyboards = LookToScrollDeactivateUponSwitchingKeyboards;
            Settings.Default.LookToScrollShowOverlayWindow = LookToScrollShowOverlayWindow;
            Settings.Default.LookToScrollOverlayBoundsColour = LookToScrollOverlayBoundsColour;
            Settings.Default.LookToScrollOverlayDeadzoneColour = LookToScrollOverlayDeadzoneColour;
            Settings.Default.LookToScrollOverlayBoundsThickness = LookToScrollOverlayBoundsThickness;
            Settings.Default.LookToScrollOverlayDeadzoneThickness = LookToScrollOverlayDeadzoneThickness;
            Settings.Default.LookToScrollHorizontalDeadzone = LookToScrollHorizontalDeadzone;
            Settings.Default.LookToScrollVerticalDeadzone = LookToScrollVerticalDeadzone;
        }

        #endregion
    }
}
