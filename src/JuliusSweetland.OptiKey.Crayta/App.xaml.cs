// Copyright (c) K McNaught Consulting Ltd (UK company number 11297717) - All Rights Reserved

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Observables.PointSources;
using JuliusSweetland.OptiKey.Observables.TriggerSources;
using JuliusSweetland.OptiKey.Crayta.Properties;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.Services.PluginEngine;
using JuliusSweetland.OptiKey.Static;
using JuliusSweetland.OptiKey.UI.ViewModels;
using JuliusSweetland.OptiKey.UI.Windows;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.Win32;
using Octokit;
using presage;
using log4net.Appender; //Do not remove even if marked as unused by Resharper - it is used by the Release build configuration
using NBug.Core.UI; //Do not remove even if marked as unused by Resharper - it is used by the Release build configuration
using WindowsRecipes.TaskbarSingleInstance;
using JuliusSweetland.OptiKey;
using JuliusSweetland.OptiKey.Crayta.Enums;
using JuliusSweetland.OptiKey.Crayta.UI.Windows;
using Prism.Commands;
using Application = System.Windows.Application;

namespace JuliusSweetland.OptiKey.Crayta
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : OptiKeyApp
    {
        private static SplashScreen splashScreen;
        private static App application;        

        private ICommand managementWindowRequestCommand;

        private IKeyStateService keyStateService;
        private IAudioService audioService;
        private IDictionaryService dictionaryService;
        private IWindowManipulationService mainWindowManipulationService;

        // Handle to management window whilst open
        private ManagementWindowEyeMine managementWindow;
        private MainViewModel mainViewModel;

        private string startKeyboardOverride = null;        

        #region Main
        [STAThread]
        public static void Main(string[] args)
        {
            // Setup derived settings class
            Settings.Initialise();
            String appName = "OK Game On";

            Action runApp = () =>
            {

                splashScreen = new SplashScreen("/Resources/Icons/OkGameOnSplash.png");
                splashScreen.Show(false);

                application = new App(args);
                application.InitializeComponent();
                application.Run();
            };

            if (Settings.Default.AllowMultipleInstances)
            {
                runApp();
            }
            else
            {
                var setup = new SingleInstanceManagerSetup(appName)
                {
                    InstanceNotificationOption = InstanceNotificationOption.NotifyAnyway,
                    ArgumentsHandler = (new_args) =>
                    {
                        // if user requests another keyboard opened, pass the request to this
                        // existing app
                        if (application != null)
                        {
                            application.ParseArgs(new_args);
                            application.ResetKeyboard();
                        }
                    }
                };                

                using (_manager = SingleInstanceManager.Initialize(setup))
                {
                    runApp();
                }
            }
        }
        #endregion

        #region Ctor

        public void ParseArgs(string[] args)
        {
            // depending on where we entered from, the first arg may be the exe name - if so, remove it
            if (args.Length > 0)
            {
                string exeName = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (args[0] == exeName)
                    args = args.Skip(1).ToArray(); // Remove first
            }

            if (args.Length > 0)
            {
                // Allow entry straight into specified dynamic keyboard(s)
                // keyboardArg may be:
                // - single XML file to load
                // - folder containing XML files
                // - single .zip file containing XML files

                startKeyboardOverride = args[0];
            }
        }

        public void ResetKeyboard()
        {
            Console.WriteLine(startKeyboardOverride);
            mainViewModel.ResetKeyboard(startKeyboardOverride);
        }

        public App(string[] args)
        {
            // Parse command-line args 
            ParseArgs(args);

            // Core setup for all OptiKey apps
            Initialise();

            // Setup specific to this app happens in App_OnStartup

        }

        #endregion

        #region On Startup

   
        private string GetStringFromResource(string resourceName)
        {
            string s = String.Empty;
            using (Stream stream = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                s = reader.ReadToEnd().Trim();
            }
            return s;
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            try
            {
                // FIXME: move declaration of all app-specific branding/directories into one place
                DiagnosticInfo.AppDataDirectoryName = @"OptiKey\Gaming";                

                // Grab the autogen commit SHA for About view model
                UI.ViewModels.Management.AboutViewModel.ReleaseSHA = GetStringFromResource("JuliusSweetland.OptiKey.Crayta." + "version.txt");
                
                Log.Info("Boot strapping the services and UI.");

                // We manually close this because automatic closure steals focus from the 
                // dynamic splash screen. 
                splashScreen.Close(TimeSpan.FromSeconds(0.5f));

                //Apply theme
                applyTheme();

                //Define MainViewModel before services so I can setup a delegate to call into the MainViewModel
                //This is to work around the fact that the MainViewModel is created after the services.
                mainViewModel = null;
                Action<KeyValue> fireKeySelectionEvent = kv =>
                {
                    if (mainViewModel != null) //Access to modified closure is a good thing here, for once!
                    {
                        mainViewModel.FireKeySelectionEvent(kv);
                    }
                };

                CleanupAndPrepareCommuniKateInitialState();

                // Handle plugins. Validate if directory exists and is accessible and pre-load all plugins, building a in-memory list of available ones.
                ValidatePluginsLocation();
                if (Settings.Default.EnablePlugins)
                {
                    PluginEngine.LoadAvailablePlugins();
                }

                ValidateDynamicKeyboardLocationEyeMine();

                //Create services
                var errorNotifyingServices = new List<INotifyErrors>();
                audioService = new AudioService();

                dictionaryService = new DictionaryService(Settings.Default.SuggestionMethod);
                IPublishService publishService = new PublishService();
                ISuggestionStateService suggestionService = new SuggestionStateService();
                ICalibrationService calibrationService = CreateCalibrationService();
                ICapturingStateManager capturingStateManager = new CapturingStateManager(audioService);
                ILastMouseActionStateManager lastMouseActionStateManager = new LastMouseActionStateManager();
                keyStateService = new KeyStateService(suggestionService, capturingStateManager,
                    lastMouseActionStateManager, calibrationService, fireKeySelectionEvent);
                IInputService inputService = CreateInputService(keyStateService, dictionaryService, audioService,
                    calibrationService, capturingStateManager, errorNotifyingServices);
                IKeyboardOutputService keyboardOutputService = new KeyboardOutputService(keyStateService,
                    suggestionService, publishService, dictionaryService, fireKeySelectionEvent);
                IMouseOutputService mouseOutputService = new MouseOutputService(publishService);
                errorNotifyingServices.Add(audioService);
                errorNotifyingServices.Add(dictionaryService);
                errorNotifyingServices.Add(publishService);
                errorNotifyingServices.Add(inputService);

                ReleaseKeysOnApplicationExit(keyStateService, publishService);

                //Compose UI
                var mainWindow = new MainWindow(audioService, dictionaryService, inputService, keyStateService);
                mainWindowManipulationService =
                    CreateMainWindowManipulationService(mainWindow);
                errorNotifyingServices.Add(mainWindowManipulationService);
                mainWindow.WindowManipulationService = mainWindowManipulationService;

                //Subscribing to the on closing events.
                mainWindow.Closing += dictionaryService.OnAppClosing;

                // Before the main view model loads the default keyboards, reconcile our own keyboard settings with Optikey's 
                // knowledge of settings
                this.SetupKeyboardSettings();

                mainViewModel = new MainViewModel(
                    audioService, calibrationService, dictionaryService, keyStateService,
                    suggestionService, capturingStateManager, lastMouseActionStateManager,
                    inputService, keyboardOutputService, mouseOutputService, mainWindowManipulationService,
                    errorNotifyingServices, startKeyboardOverride);

                mainWindow.SetMainViewModel(mainViewModel);

                //Setup actions to take once main view is loaded (i.e. the view is ready, so hook up the services which kicks everything off)
                Action postMainViewLoaded = () =>
                {
                    mainViewModel.AttachErrorNotifyingServiceHandlers();
                    mainViewModel.AttachInputServiceEventHandlers();

                    // Special warning if controller install hasn't been completed
                    if (!publishService.SupportsController())
                    {                                              
                        mainViewModel.RaiseToastNotification("Error setting up joysticks - please install ViGem",
                            "Cannot find virtual controller driver.\n\nPlease install ViGemBus\n\nGamepad control keys will not work until ViGemBus is installed", NotificationTypes.Error, () => { });

                        System.Diagnostics.Process.Start("https://github.com/ViGEm/ViGEmBus/releases");
                    }
                };

                mainWindow.AddOnMainViewLoadedAction(postMainViewLoaded);

                //Show the main window
                mainWindow.Show();

                if (Settings.Default.LookToScrollShowOverlayWindow)
                {
                    // Create the overlay window, but don't show it yet. It'll make itself visible when the conditions are right.
                    foreach (var joystick in mainViewModel.JoystickHandlers.Values)
                    {
                        //TODO: consider different colour overlays for diff handlers?
                        LookToScrollOverlayWindow w = new LookToScrollOverlayWindow(joystick);
                        w.Owner = mainWindow;
                    }
                }

                //Display splash screen and check for updates (and display message) after the window has been sized and positioned for the 1st time
                EventHandler sizeAndPositionInitialised = null;
                sizeAndPositionInitialised = async (_, __) =>
                {
                    mainWindowManipulationService.SizeAndPositionInitialised -=
                        sizeAndPositionInitialised; //Ensure this handler only triggers once
                    
                    await ShowSplashScreenEyeMine(inputService, audioService, mainViewModel, OptiKey.Crayta.Properties.Resources.EYEMINE_DESCRIPTION);
                    await mainViewModel.RaiseAnyPendingErrorToastNotifications();
                    await AttemptToStartMaryTTSService(inputService, audioService, mainViewModel);

                    inputService.RequestResume(); //Start the input service

                    await CheckForUpdatesOKGO(inputService, audioService, mainViewModel);
                };

                if (mainWindowManipulationService.SizeAndPositionIsInitialised)
                {
                    sizeAndPositionInitialised(null, null);
                }
                else
                {
                    mainWindowManipulationService.SizeAndPositionInitialised += sizeAndPositionInitialised;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error starting up application", ex);
                throw;
            }

            // Set up any mocks to replace core UIs etc
            // use reflection to inject a mock instance
            managementWindowRequestCommand = new DelegateCommand(RequestManagementWindow);

            typeof(MainWindow)
                .GetField("managementWindowRequestCommand", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(MainWindow, managementWindowRequestCommand);


        }

        protected static async Task<bool> CheckForUpdatesOKGO(IInputService inputService, IAudioService audioService, MainViewModel mainViewModel)
        {
            //FIXME: extract common code here - base class currently has hardcoded repo info, and versioning conventions are subtly different

            var taskCompletionSource = new TaskCompletionSource<bool>(); //Used to make this method awaitable on the InteractionRequest callback

            try
            {
                if (Settings.Default.CheckForUpdates)
                {
                    const string gitHubRepoOwnerLocal = "kmcnaught";
                    const string gitHubRepoNameLocal = "okgo";

                    Log.InfoFormat("Checking GitHub for updates (repo owner:'{0}', repo name:'{1}').", gitHubRepoOwnerLocal, gitHubRepoNameLocal);

                    var github = new GitHubClient(new ProductHeaderValue(gitHubRepoOwnerLocal));
                    var releases = await github.Repository.Release.GetAll(gitHubRepoOwnerLocal, gitHubRepoNameLocal);
                    var latestRelease = releases.FirstOrDefault(release => !release.Prerelease);
                    if (latestRelease != null)
                    {
                        var currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                        //Discard revision (4th number) as our GitHub releases are tagged with "vMAJOR.MINOR.PATCH"
                        currentVersion = new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build);

                        if (!string.IsNullOrEmpty(latestRelease.TagName))
                        {
                            var tagNameWithoutLetters =
                                new string(latestRelease.TagName.ToCharArray().Where(c => char.IsDigit(c) || c.Equals('.')).ToArray());
                            var latestAvailableVersion = new Version(tagNameWithoutLetters);

                            if (latestAvailableVersion > currentVersion)
                            {
                                Log.InfoFormat(
                                    "An update is available. Current version is {0}. Latest version on GitHub repo is {1}",
                                    currentVersion, latestAvailableVersion);

                                inputService.RequestSuspend();
                                audioService.PlaySound(Settings.Default.InfoSoundFile, Settings.Default.InfoSoundVolume);
                                mainViewModel.RaiseToastNotification(OptiKey.Properties.Resources.UPDATE_AVAILABLE,
                                    string.Format(OptiKey.Properties.Resources.URL_DOWNLOAD_PROMPT, latestAvailableVersion),
                                    NotificationTypes.Normal,
                                     () =>
                                     {
                                         inputService.RequestResume();
                                         taskCompletionSource.SetResult(true);
                                     });
                            }
                            else
                            {
                                Log.Info("No update found.");
                                taskCompletionSource.SetResult(false);
                            }
                        }
                        else
                        {
                            Log.Info("Unable to determine if an update is available as the latest release lacks a tag.");
                            taskCompletionSource.SetResult(false);
                        }
                    }
                    else
                    {
                        Log.Info("No releases found.");
                        taskCompletionSource.SetResult(false);
                    }
                }
                else
                {
                    Log.Info("Check for update is disabled - skipping check.");
                    taskCompletionSource.SetResult(false);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error when checking for updates. Exception message:{0}\nStackTrace:{1}", ex.Message, ex.StackTrace);
                taskCompletionSource.SetResult(false);
            }

            return await taskCompletionSource.Task;
        }

        public static string GetBuiltInKeyboardsFolder()
        {
            return OptiKeyApp.GetDefaultUserKeyboardFolder();
        }

        public static string GetKeyboardsFolderForInputSource()
        {
            string baseFolder = GetDefaultUserKeyboardFolder();
            // Currently we only support eye tracker, i.e. one set of keyboards
            return baseFolder;
        }

        private void SetupKeyboardSettings()
        {
            // Set all of Optikey's internal settings for default keyboard based on our own logic

            switch (Settings.Default.EyeMineStartupKeyboard)
            {
                case StartupKeyboardOptions.EyeMineAllKeyboards:
                    Settings.Default.StartupKeyboard = Keyboards.DynamicKeyboard;
                    
                    // Pick the appropriate path for source-specific keyboards
                    Settings.Default.DynamicKeyboardsLocation = App.GetKeyboardsFolderForInputSource();

                    break;

                case StartupKeyboardOptions.CustomKeyboardsFolder:
                    Settings.Default.StartupKeyboard = Keyboards.DynamicKeyboard;
                    Settings.Default.DynamicKeyboardsLocation = Settings.Default.OwnDynamicKeyboardsLocation;
                    break;

                case StartupKeyboardOptions.CustomKeyboardFile:
                    Settings.Default.StartupKeyboard = Keyboards.CustomKeyboardFile;
                    // Settings.Default.StartupKeyboardFile already stores the file
                    break;
            }
        }
    

        private static string GetRelativePath(string filename, string folder)
        {
            Uri pathUri = new Uri(filename);

            char separator = Path.DirectorySeparatorChar;
            if (!folder.EndsWith(separator.ToString()))
            {
                folder += separator;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', separator));
        }

        protected new static string GetDefaultUserKeyboardFolder()
        {
            var applicationDataPath = DiagnosticInfo.GetAppDataPath(@"Keyboards");

            // If directory doesn't exist, assume that this is the first run. So, move dynamic keyboards from installation package to target path
            if (!Directory.Exists(applicationDataPath))
            {
                Directory.CreateDirectory(applicationDataPath);
                String baseSourceDir = AppDomain.CurrentDomain.BaseDirectory + @"\Resources\GamingKeyboards";
                foreach (string dynamicKeyboard in Directory.GetFiles(baseSourceDir,"*.xml", SearchOption.AllDirectories))
                {
                    // Copy folder hierarchy
                    string relativePath = Path.GetDirectoryName(GetRelativePath(dynamicKeyboard, baseSourceDir));
                    Directory.CreateDirectory(Path.Combine(applicationDataPath, relativePath));

                    File.Copy(dynamicKeyboard, Path.Combine(applicationDataPath, relativePath, Path.GetFileName(dynamicKeyboard)), true);
                }
            }

            return applicationDataPath;
        }

        private void ValidateDynamicKeyboardLocationEyeMine()
        {
            string defaultDir = GetDefaultUserKeyboardFolder();
            
            if (string.IsNullOrEmpty(Settings.Default.DynamicKeyboardsLocation))
            {
                // First time we set to APPDATA location, user may move through settings later
                Settings.Default.DynamicKeyboardsLocation = GetDefaultUserKeyboardFolder();
            }
        }

        private void RequestManagementWindow()
        {
            Log.Info("RequestManagementWindow called.");
            
            var restoreModifierStates = keyStateService.ReleaseModifiers(Log);

            if (managementWindow == null)
            {
                managementWindow = new ManagementWindowEyeMine(audioService,
                    dictionaryService, mainWindowManipulationService);

                EventHandler closeHandler = null;
                closeHandler = (sender, e) =>
                {
                    managementWindow.Closed -= closeHandler;
                    managementWindow = null;
                };
                managementWindow.Closed += closeHandler;

                Log.Info("Showing Management Window (non-modal)");
                managementWindow.Show();
            }
            else
            {
                managementWindow.Focus();
            }

            Log.Info("RequestManagementWindow complete.");
        }
        #endregion


        #region Show Splash Screen 
        // We have a slightly more stripped down splash compared to the core optikey apps

        private static async Task<bool> ShowSplashScreenEyeMine(IInputService inputService, IAudioService audioService, MainViewModel mainViewModel, String title)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>(); //Used to make this method awaitable on the InteractionRequest callback

            if (Settings.Default.ShowSplashScreen)
            {
                Log.Info("Showing splash screen.");

                var message = new StringBuilder();

                string eyeMineVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                message.AppendLine(string.Format(OptiKey.Properties.Resources.VERSION_DESCRIPTION, eyeMineVersion));
                message.AppendLine("");
                message.AppendLine(string.Format(OptiKey.Properties.Resources.POINTING_SOURCE_DESCRIPTION, Settings.Default.PointsSource.ToDescription()));
                message.AppendLine("");

                var keySelectionSb = new StringBuilder();
                keySelectionSb.Append(Settings.Default.KeySelectionTriggerSource.ToDescription());
                switch (Settings.Default.KeySelectionTriggerSource)
                {
                    case TriggerSources.Fixations:
                        keySelectionSb.Append(string.Format(OptiKey.Properties.Resources.DURATION_FORMAT, Settings.Default.KeySelectionTriggerFixationDefaultCompleteTime.TotalMilliseconds));
                        break;

                    case TriggerSources.KeyboardKeyDownsUps:
                        keySelectionSb.Append(string.Format(" ({0})", Settings.Default.KeySelectionTriggerKeyboardKeyDownUpKey));
                        break;

                    case TriggerSources.MouseButtonDownUps:
                        keySelectionSb.Append(string.Format(" ({0})", Settings.Default.KeySelectionTriggerMouseDownUpButton));
                        break;
                }

                message.AppendLine(string.Format(OptiKey.Properties.Resources.KEY_SELECTION_TRIGGER_DESCRIPTION, keySelectionSb));

                var pointSelectionSb = new StringBuilder();
                pointSelectionSb.Append(Settings.Default.PointSelectionTriggerSource.ToDescription());
                switch (Settings.Default.PointSelectionTriggerSource)
                {
                    case TriggerSources.Fixations:
                        pointSelectionSb.Append(string.Format(OptiKey.Properties.Resources.DURATION_FORMAT, Settings.Default.PointSelectionTriggerFixationCompleteTime.TotalMilliseconds));
                        break;

                    case TriggerSources.KeyboardKeyDownsUps:
                        pointSelectionSb.Append(string.Format(" ({0})", Settings.Default.PointSelectionTriggerKeyboardKeyDownUpKey));
                        break;

                    case TriggerSources.MouseButtonDownUps:
                        pointSelectionSb.Append(string.Format(" ({0})", Settings.Default.PointSelectionTriggerMouseDownUpButton));
                        break;
                }

                message.AppendLine(string.Format(OptiKey.Properties.Resources.POINT_SELECTION_DESCRIPTION, pointSelectionSb));
                
                inputService.RequestSuspend();
                //audioService.PlaySound(Settings.Default.InfoSoundFile, Settings.Default.InfoSoundVolume);
                mainViewModel.RaiseToastNotification(
                          title,
                    message.ToString(),
                    NotificationTypes.Normal,
                    () =>
                    {
                        inputService.RequestResume();
                        taskCompletionSource.SetResult(true);
                    });
            }
            else
            {
                taskCompletionSource.SetResult(false);
            }

            return await taskCompletionSource.Task;
        }

        #endregion


    }

}
