// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System.Windows.Controls;
using System.Windows;
using Microsoft.Win32;
using System;
using System.Configuration;
using System.Windows.Forms;
using JuliusSweetland.OptiKey.Properties;
using System.IO;
using JuliusSweetland.OptiKey.Static;
using log4net;
using MessageBox = System.Windows.MessageBox;

namespace JuliusSweetland.OptiKey.EyeMine.UI.Views.Management
{
    /// <summary>
    /// Interaction logic for VisualsView.xaml
    /// </summary>
    public partial class VisualsView : System.Windows.Controls.UserControl
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

                try
                {
                    // make a subdir 
                    outputFolder = Path.Combine(outputFolder, "EyeMineKeyboards");
                    if (!Directory.Exists(outputFolder))
                        Directory.CreateDirectory(outputFolder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        OptiKey.EyeMine.Properties.Resources.ERROR_COPYING_DETAILS + "\n\n" + ex.ToString(),
                        OptiKey.EyeMine.Properties.Resources.ERROR_COPYING,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }


                int successes = 0;
                int failures = 0;

                // copy files across
                foreach (string dynamicKeyboard in Directory.GetFiles(inputFolder, "*.xml"))
                {
                    try
                    {
                        File.Copy(dynamicKeyboard, Path.Combine(outputFolder, Path.GetFileName(dynamicKeyboard)),
                            false);
                        ++successes;
                    }

                    catch (Exception ex)
                    {
                        Log.ErrorFormat("Error copying file {0}", dynamicKeyboard);
                        Log.Error(ex.ToString());
                        ++failures;
                    }
                }

                string msgText, msgCaption;
                MessageBoxImage msgImage;
                if (failures == 0)
                {
                    msgCaption = "Files copied successfully";
                    msgText = String.Format("{0} files copied to {1}", successes, outputFolder);
                    msgImage = MessageBoxImage.Information;
                }
                else
                {
                    msgCaption = "Error copying files";
                    msgText = String.Format("{0}/{1} files successfully copied {2}", successes, successes+failures, outputFolder);
                    msgText += "\n\nThis may be because of folder permissions, or files existing already";
                    msgText += "\n\nSee logs for more information";
                    msgImage = MessageBoxImage.Warning;
                } 

                MessageBox.Show(
                    msgText,
                    msgCaption,
                    MessageBoxButton.OK,
                    msgImage);

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
