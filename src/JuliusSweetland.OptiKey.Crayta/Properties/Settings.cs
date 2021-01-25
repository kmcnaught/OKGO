using System;
using JuliusSweetland.OptiKey.Enums;

namespace JuliusSweetland.OptiKey.Crayta.Properties {

    class Settings : JuliusSweetland.OptiKey.Properties.Settings
    {
        
        public static void Initialise()
        {
            Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
            InitialiseWithDerivedSettings(defaultInstance);            
        }

        public override AppType GetApp()
        {
            // FIXME: HACK
            return AppType.Pro;
        }

        // If Settings.Default is requested from an instance of this object, return a cast version
        public new static Settings Default => (Settings)OptiKey.Properties.Settings.Default;

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Floating")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public virtual global::JuliusSweetland.OptiKey.Enums.WindowStates MainWindowState
        {
            get
            {
                return ((global::JuliusSweetland.OptiKey.Enums.WindowStates)(this["MainWindowState"]));
            }
            set
            {
                this["MainWindowState"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.6")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public new double MainWindowOpacity
        {
            get
            {
                return ((double)(this["MainWindowOpacity"]));
            }
            set
            {
                this["MainWindowOpacity"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public new bool LookToScrollBringWindowToFrontWhenActivated
        {
            get
            {
                return ((bool)(this["LookToScrollBringWindowToFrontWhenActivated"]));
            }
            set
            {
                this["LookToScrollBringWindowToFrontWhenActivated"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public new bool LookToScrollBringWindowToFrontAfterChoosingScreenPoint
        {
            get
            {
                return ((bool)(this["LookToScrollBringWindowToFrontAfterChoosingScreenPoint"]));
            }
            set
            {
                this["LookToScrollBringWindowToFrontAfterChoosingScreenPoint"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public new bool EnablePlugins
        {
            get
            {
                return true;
            }
            set
            {
                //no-op, forced on
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("EyeMineAllKeyboards")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public global::JuliusSweetland.OptiKey.Crayta.Enums.StartupKeyboardOptions EyeMineStartupKeyboard
        {
            get
            {
                return ((global::JuliusSweetland.OptiKey.Crayta.Enums.StartupKeyboardOptions)(this["EyeMineStartupKeyboard"]));
            }
            set
            {
                this["EyeMineStartupKeyboard"] = value;
            }
        }

        // We'll internally use the existing DynamicKeyboardsLocation setting to pick the installed keyboards,
        // so we want an additional setting to remember user's own custom location
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public string OwnDynamicKeyboardsLocation
        {
            get
            {
                return ((string)(this["OwnDynamicKeyboardsLocation"]));
            }
            set
            {
                this["OwnDynamicKeyboardsLocation"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("EnglishMinecraft")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public new global::JuliusSweetland.OptiKey.Enums.Languages KeyboardAndDictionaryLanguage
        {
            get
            {
                return ((global::JuliusSweetland.OptiKey.Enums.Languages)(this["KeyboardAndDictionaryLanguage"]));
            }
            set
            {
                this["KeyboardAndDictionaryLanguage"] = value;
            }
        }


        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("TobiiEyeX")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public new global::JuliusSweetland.OptiKey.Enums.PointsSources PointsSource
        {
            get
            {
                return ((global::JuliusSweetland.OptiKey.Enums.PointsSources)(this["PointsSource"]));
            }
            set
            {
                this["PointsSource"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("32")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public new double MainWindowFullDockThicknessAsPercentageOfScreen
        {
            get
            {
                return ((double)(this["MainWindowFullDockThicknessAsPercentageOfScreen"]));
            }
            set
            {
                this["MainWindowFullDockThicknessAsPercentageOfScreen"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Bottom")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public new global::JuliusSweetland.OptiKey.Enums.DockEdges MainWindowDockPosition
        {
            get
            {
                return ((global::JuliusSweetland.OptiKey.Enums.DockEdges)(this["MainWindowDockPosition"]));
            }
            set
            {
                this["MainWindowDockPosition"] = value;
            }
        }

        // TODO : own logic on top of the core startup keyboards
        // setting for 'selection' vs 'specific keyboard'

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("DynamicKeyboard")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public new global::JuliusSweetland.OptiKey.Enums.Keyboards StartupKeyboard
        {
            get
            {
                return ((global::JuliusSweetland.OptiKey.Enums.Keyboards)(this["StartupKeyboard"]));
            }
            set
            {
                this["StartupKeyboard"] = value;
            }
        }

        //TODO: reconsider? not sure if even relevant
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public new bool KeySelectionTriggerFixationCompleteTimesByIndividualKey
        {
            get
            {
                return ((bool)(this["KeySelectionTriggerFixationCompleteTimesByIndividualKey"]));
            }
            set
            {
                this["KeySelectionTriggerFixationCompleteTimesByIndividualKey"] = value;
            }
        }


        //TODO: there was some legacy backwards-compatibility handling in here, we don't need now
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public string CustomDynamicKeyboardsLocation
        {
            get
            {
                return ((string)(this["CustomDynamicKeyboardsLocation"]));
            }
            set
            {
                this["CustomDynamicKeyboardsLocation"] = value;
            }
        }


        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public new string DynamicKeyboardsLocation
        {
            get
            {
                return ((string)(this["DynamicKeyboardsLocation"]));
            }
            set
            {
                this["DynamicKeyboardsLocation"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1,30,1,1")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public global::System.Windows.Thickness BorderThickness
        {
            get
            {
                return ((global::System.Windows.Thickness)(this["BorderThickness"]));
            }
            set
            {
                this["BorderThickness"] = value;
            }
        }

    }
}
