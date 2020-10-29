using System.IO;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Static;
using log4net;
using Prism.Mvvm;

namespace JuliusSweetland.OptiKey.Crayta.UI.ViewModels.Management
{
    public class AboutViewModel : BindableBase
    {
        #region Private Member Vars

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        #endregion

        #region Ctor

        public AboutViewModel()
        {
            Load();
        }

        #endregion

        #region Properties

        public string AppVersion
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "-" + 
                    ReleaseSHA; }
        }

        // Updated by release process, loaded from a resource in App.xaml.cs
        public static string ReleaseSHA
        { set; get; }

        public string ThirdPartyDetailsFile
        {
            get
            {
                string basePath = System.AppDomain.CurrentDomain.BaseDirectory;
                string fileName = "ThirdPartyLicenses.md";
                return Path.Combine(basePath, fileName);
            }
        }

        public string AboutInfo
        {
            get
            {
                string aboutInfo = "";
                aboutInfo += "Optikey Gaming was developed by Kirsty McNaught\n";
                aboutInfo += "The app is based on both the excellent Optikey project, ";
                aboutInfo += "and provides eye gaze access to a variety of PC games.\n";
                aboutInfo += "Optikey Gaming uses the third party libraries detailed below";
                aboutInfo += "";

                return aboutInfo;
            }
        }

        #endregion

        #region Methods

        private void Load()
        {

        }

        public bool ChangesRequireRestart
        {
            get
            {
                return false;
            }
        }

        public void ApplyChanges()
        {

        }

        #endregion
    }
}