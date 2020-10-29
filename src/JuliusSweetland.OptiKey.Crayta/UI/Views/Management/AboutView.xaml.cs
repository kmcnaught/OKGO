
// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using JuliusSweetland.OptiKey.Crayta.UI.ViewModels.Management;
using System.Windows.Controls;
using System.Windows;

namespace JuliusSweetland.OptiKey.Crayta.UI.Views.Management
{
    /// <summary>
    /// Interaction logic for AboutView.xaml
    /// </summary>
    public partial class AboutView : UserControl
    {
        public AboutView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender,
            System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            AboutViewModel viewModel = this.DataContext as AboutViewModel;
            if (viewModel != null)
            {
                string content = "Optikey Gaming " + viewModel.AppVersion;
                Clipboard.SetText(content);
            }
        }
    }
}
