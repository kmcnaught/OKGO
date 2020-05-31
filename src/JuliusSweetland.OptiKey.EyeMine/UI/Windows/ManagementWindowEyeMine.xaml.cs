// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System.Windows;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.UI.ViewModels;
using MahApps.Metro.Controls;

namespace JuliusSweetland.OptiKey.EyeMine.UI.Windows
{
    /// <summary>
    /// Interaction logic for ManagementWindowEyeMine.xaml
    /// </summary>
    public partial class ManagementWindowEyeMine : MetroWindow
    {
        public ManagementWindowEyeMine(
            IAudioService audioService,
            IDictionaryService dictionaryService,
            IWindowManipulationService windowManipulationService)
        {
            InitializeComponent();

            //Instantiate ManagementViewModel and set as DataContext of ManagementView
            var managementViewModel = new ManagementViewModelEyeMine(audioService, dictionaryService, windowManipulationService);
            this.ManagementView.DataContext = managementViewModel;
        }
    }
}
