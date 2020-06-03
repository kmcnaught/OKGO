using System;
using JuliusSweetland.OptiKey.Enums;

namespace JuliusSweetland.OptiKey.EyeMine.Properties {

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
        [global::System.Configuration.DefaultSettingValueAttribute("EyeMineAllKeyboards")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public global::JuliusSweetland.OptiKey.EyeMine.Enums.StartupKeyboardOptions EyeMineStartupKeyboard
        {
            get
            {
                return ((global::JuliusSweetland.OptiKey.EyeMine.Enums.StartupKeyboardOptions)(this["EyeMineStartupKeyboard"]));
            }
            set
            {
                this["EyeMineStartupKeyboard"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("EnglishMinecraft")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public global::JuliusSweetland.OptiKey.Enums.Languages KeyboardAndDictionaryLanguage
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
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public bool PointsMousePositionHideCursor
        {
            get
            {
                return ((bool)(this["PointsMousePositionHideCursor"]));
            }
            set
            {
                this["PointsMousePositionHideCursor"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("TobiiEyeX")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public global::JuliusSweetland.OptiKey.Enums.PointsSources PointsSource
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



        //TODO: volume! (previously I just made the sounds null..)


        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("32")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public double MainWindowFullDockThicknessAsPercentageOfScreen
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
        public global::JuliusSweetland.OptiKey.Enums.DockEdges MainWindowDockPosition
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
        public global::JuliusSweetland.OptiKey.Enums.Keyboards StartupKeyboard
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


        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Title")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public global::JuliusSweetland.OptiKey.Enums.Case KeyCase
        {
            get
            {
                return ((global::JuliusSweetland.OptiKey.Enums.Case)(this["KeyCase"]));
            }
            set
            {
                this["KeyCase"] = value;
            }
        }

        //TODO: reconsider? not sure if even relevant
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public bool KeySelectionTriggerFixationCompleteTimesByIndividualKey
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
        public string DefaultDynamicKeyboardsLocation
        {
            get
            {
                return ((string)(this["DefaultDynamicKeyboardsLocation"]));
            }
            set
            {
                this["DefaultDynamicKeyboardsLocation"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public string DynamicKeyboardsLocation
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

        // Turn off all sound effects


        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public string MouseDownSoundFile
        {
            get
            {
                return ((string)(this["MouseDownSoundFile"]));
            }
            set
            {
                this["MouseDownSoundFile"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public string MouseUpSoundFile
        {
            get
            {
                return ((string)(this["MouseUpSoundFile"]));
            }
            set
            {
                this["MouseUpSoundFile"] = value;
            }
        }


        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public string MouseClickSoundFile
        {
            get
            {
                return ((string)(this["MouseClickSoundFile"]));
            }
            set
            {
                this["MouseClickSoundFile"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public string MouseDoubleClickSoundFile
        {
            get
            {
                return ((string)(this["MouseDoubleClickSoundFile"]));
            }
            set
            {
                this["MouseDoubleClickSoundFile"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        [global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
        public string MouseScrollSoundFile
        {
            get
            {
                return ((string)(this["MouseScrollSoundFile"]));
            }
            set
            {
                this["MouseScrollSoundFile"] = value;
            }
        }

    }
}
