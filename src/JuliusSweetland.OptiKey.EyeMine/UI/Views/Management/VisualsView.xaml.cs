// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System.Windows.Controls;
using System.Windows;
using Microsoft.Win32;
using System;
using System.Windows.Forms;
using JuliusSweetland.OptiKey.Properties;
using System.IO;
using JuliusSweetland.OptiKey.Static;

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
        
        private void CopyKeyboards(object sender, System.Windows.RoutedEventArgs e)
        {
            // Copy the builtin keyboards to the user's choice of directory

            // Ask user to select a directory
            string origPath = txtKeyboardsLocation.Text;
            if (String.IsNullOrEmpty(origPath) ||
                !Directory.Exists(origPath))
            {
                origPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                
            }

            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            folderBrowser.Description = Properties.Resources.SELECT_FOLDER_FOR_COPY;
            folderBrowser.SelectedPath = origPath;

            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                string inputFolder = App.GetKeyboardsFolderForInputSource(); // %APPDATA% path
                string outputFolder = folderBrowser.SelectedPath;

                // make a subdir there
                outputFolder = Path.Combine(outputFolder, "EyeMineKeyboards");
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                // copy files across
                foreach (string dynamicKeyboard in Directory.GetFiles(inputFolder, "*.xml"))
                {
                    File.Copy(dynamicKeyboard, Path.Combine(outputFolder, Path.GetFileName(dynamicKeyboard)), true);
                }

                // update settings
                txtKeyboardsLocation.Text = outputFolder;
            }
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
