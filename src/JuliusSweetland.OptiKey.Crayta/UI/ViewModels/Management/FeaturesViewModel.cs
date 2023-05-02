// Copyright (c) 2022 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System.Collections.Generic;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Services.PluginEngine;
using log4net;
using Prism.Mvvm;
using JuliusSweetland.OptiKey.Services.Translation.Languages;
using JuliusSweetland.OptiKey.Services.Translation;
using System.Windows.Media;
using System.Linq;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Management
{
    public class FeaturesViewModel : BindableBase
    {
        #region Private Member Vars

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Ctor

        public FeaturesViewModel()
        {
            Load();
        }

        #endregion

        #region Properties

        private string pluginsLocation;
        public string PluginsLocation
        {
            get { return pluginsLocation; }
            set { SetProperty(ref pluginsLocation, value); }
        }

        public List<Plugin> AvailablePlugins
        {
            get
            {
                return PluginEngine.GetAllAvailablePlugins();
            }
        }

        public bool ChangesRequireRestart
        {
            get
            {

                return false;
            }
        }

        private bool enablePlugins;
        public bool EnablePlugins
        {
            get { return enablePlugins; }
            set { SetProperty(ref enablePlugins, value); }
        }

        #endregion
        
        #region Methods

        private void Load()
        {
            PluginsLocation = Settings.Default.PluginsLocation;
            EnablePlugins = Settings.Default.EnablePlugins;
        }

        public void ApplyChanges()
        {
            Settings.Default.PluginsLocation = PluginsLocation;
            Settings.Default.EnablePlugins = EnablePlugins;
        }

        #endregion
    }
}
