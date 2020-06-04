// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System.Windows.Controls;
using System.Windows;
using Microsoft.Win32;
using System;
using System.Windows.Forms;
using JuliusSweetland.OptiKey.Properties;
using System.IO;

namespace JuliusSweetland.OptiKey.EyeMine.UI.Views.Management
{
    /// <summary>
    /// Interaction logic for VisualsView.xaml
    /// </summary>
    public partial class VisualsView : System.Windows.Controls.UserControl
    {

        public VisualsView()
        {
            InitializeComponent();
        }

        private void FindKeyboardsFolder(object sender, System.Windows.RoutedEventArgs e)
        {
            string origPath = txtKeyboardsLocation.Text;
            if (String.IsNullOrEmpty(origPath) ||
                !Directory.Exists(origPath))
            {
                origPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            folderBrowser.Description = Properties.Resources.SELECT_DYNAMIC_FOLDER_INSTRUCTIONS;
            folderBrowser.SelectedPath = origPath;

            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                // This is hooked up to the DynamicKeyboardsLocation property
                txtKeyboardsLocation.Text = folderBrowser.SelectedPath;
            }
        }
        
        private void ResetKeyboardsFolder(object sender, System.Windows.RoutedEventArgs e)
        {
            // FIXME: Need to test this, was DefaultDynamicKeyboardsLocation in EyeMine
            txtKeyboardsLocation.Text = Settings.Default.DynamicKeyboardsLocation;
        }
 
        private void FindStartupKeyboardFile(object sender, System.Windows.RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "XML keyboard definition (*.xml)|*.xml"
            };

            // Start location, in priority order: 
            // - Location of existing custom file
            // - Location of existing custom directory
            // - Location of built-in files

            if (File.Exists(txtStartupKeyboardLocation.Text))
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(txtStartupKeyboardLocation.Text);
            }
            else if (Directory.Exists(txtKeyboardsLocation.Text))
            {
                openFileDialog.InitialDirectory = txtKeyboardsLocation.Text;
            }
            else
            {
                openFileDialog.InitialDirectory = App.GetBuiltInKeyboardsFolder();
            }

            if (openFileDialog.ShowDialog() == true)
            {
                if (openFileDialog.FileName.EndsWith(@".xml"))
                {
                    // This is hooked up to the CustomDynamicKeyboardsLocation property
                    txtStartupKeyboardLocation.Text = openFileDialog.FileName;
                }                
            }
        }
    }
}
