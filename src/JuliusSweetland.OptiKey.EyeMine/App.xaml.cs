// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved

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
using JuliusSweetland.OptiKey.EyeMine.Properties;
using JuliusSweetland.OptiKey.Services;
using JuliusSweetland.OptiKey.Services.PluginEngine;
using JuliusSweetland.OptiKey.Static;
using JuliusSweetland.OptiKey.UI.ViewModels;
using JuliusSweetland.OptiKey.UI.Windows;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.Win32;
using NBug.Core.UI;
using Octokit;
using presage;
using log4net.Appender; //Do not remove even if marked as unused by Resharper - it is used by the Release build configuration
using NBug.Core.UI; //Do not remove even if marked as unused by Resharper - it is used by the Release build configuration
using WindowsRecipes.TaskbarSingleInstance;
using JuliusSweetland.OptiKey.EyeMine.UI.Windows;
using Prism.Commands;
using Application = System.Windows.Application;

namespace JuliusSweetland.OptiKey.EyeMine
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : OptiKeyApp
    {
        private static SplashScreen splashScreen;

        private ICommand managementWindowRequestCommand;

        private IKeyStateService keyStateService;
        private IAudioService audioService;
        private IDictionaryService dictionaryService;
        private IWindowManipulationService mainWindowManipulationService;

        // Handle to management window whilst open
        private ManagementWindowEyeMine managementWindow;

        #region Main
        [STAThread]
        public static void Main()
        {
            // Setup derived settings class
            Settings.Initialise();
            String appName = "EyeMineV2";

            Action runApp = () =>
            {

                splashScreen = new SplashScreen("/Resources/Icons/EyeMineSplash.png");
                splashScreen.Show(false);

                var application = new App();
                application.InitializeComponent();
                application.Run();
            };

            if (Settings.Default.AllowMultipleInstances)
            {
                runApp();
            }
            else
            {
                using (_manager = SingleInstanceManager.Initialize(
                    new SingleInstanceManagerSetup(appName)))
                {
                    runApp();
                }
            }
        }
        #endregion

        #region Ctor

        public App()
        {
            // Core setup for all OptiKey apps
            Initialise();
        }

        #endregion

        #region On Startup

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            try
            {
                DiagnosticInfo.AppDataDirectoryName = @"SpecialEffect\EyeMineV2";

                // Swap out the AppData directory being used
                /*typeof(DiagnosticInfo)
                    .GetProperty("AppDataDirectoryName")
                    .SetValue(null, @"SpecialEffect\EyeMine");
                    */
                Log.Info("Boot strapping the services and UI.");

                // We manually close this because automatic closure steals focus from the 
                // dynamic splash screen. 
                splashScreen.Close(TimeSpan.FromSeconds(0.5f));

                //Apply theme
                applyTheme();

                //Define MainViewModel before services so I can setup a delegate to call into the MainViewModel
                //This is to work around the fact that the MainViewModel is created after the services.
                MainViewModel mainViewModel = null;
                Action<KeyValue> fireKeySelectionEvent = kv =>
                {
                    if (mainViewModel != null) //Access to modified closure is a good thing here, for once!
                    {
                        mainViewModel.FireKeySelectionEvent(kv);
                    }
                };

                CleanupAndPrepareCommuniKateInitialState();

                ValidateDynamicKeyboardLocation();

                // Handle plugins. Validate if directory exists and is accessible and pre-load all plugins, building a in-memory list of available ones.
                ValidatePluginsLocation();
                if (Settings.Default.EnablePlugins)
                {
                    PluginEngine.LoadAvailablePlugins();
                }

                var presageInstallationProblem = PresageInstallationProblemsDetected();

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

                mainViewModel = new MainViewModel(
                    audioService, calibrationService, dictionaryService, keyStateService,
                    suggestionService, capturingStateManager, lastMouseActionStateManager,
                    inputService, keyboardOutputService, mouseOutputService, mainWindowManipulationService,
                    errorNotifyingServices);

                mainWindow.SetMainViewModel(mainViewModel);

                //Setup actions to take once main view is loaded (i.e. the view is ready, so hook up the services which kicks everything off)
                Action postMainViewLoaded = () =>
                {
                    mainViewModel.AttachErrorNotifyingServiceHandlers();
                    mainViewModel.AttachInputServiceEventHandlers();
                };

                mainWindow.AddOnMainViewLoadedAction(postMainViewLoaded);

                //Show the main window
                mainWindow.Show();

                if (Settings.Default.LookToScrollEnabled && Settings.Default.LookToScrollShowOverlayWindow)
                {
                    // Create the overlay window, but don't show it yet. It'll make itself visible when the conditions are right.
                    new LookToScrollOverlayWindow(mainViewModel);
                }

                //Display splash screen and check for updates (and display message) after the window has been sized and positioned for the 1st time
                EventHandler sizeAndPositionInitialised = null;
                sizeAndPositionInitialised = async (_, __) =>
                {
                    mainWindowManipulationService.SizeAndPositionInitialised -=
                        sizeAndPositionInitialised; //Ensure this handler only triggers once
                    //FIXME: add as resource OptiKey.Properties.Resources.EYEMINE_DESCRIPTION
                    await ShowSplashScreenEyeMine(inputService, audioService, mainViewModel, "EyeMine V2");
                    await mainViewModel.RaiseAnyPendingErrorToastNotifications();
                    await AttemptToStartMaryTTSService(inputService, audioService, mainViewModel);
                    await AlertIfPresageBitnessOrBootstrapOrVersionFailure(presageInstallationProblem, inputService,
                        audioService, mainViewModel);

                    inputService.RequestResume(); //Start the input service

                    await CheckForUpdates(inputService, audioService, mainViewModel);
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

                message.AppendLine(string.Format(OptiKey.Properties.Resources.VERSION_DESCRIPTION, DiagnosticInfo.AssemblyVersion));
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
                audioService.PlaySound(Settings.Default.InfoSoundFile, Settings.Default.InfoSoundVolume);
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
