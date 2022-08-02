// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Properties;
using JuliusSweetland.OptiKey.Services.PluginEngine;
using JuliusSweetland.OptiKey.Services.Translation;
using JuliusSweetland.OptiKey.UI.ViewModels.Keyboards;
using JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.Base;
using JuliusSweetland.OptiKey.Static;
using JuliusSweetland.OptiKey.Native;

namespace JuliusSweetland.OptiKey.UI.ViewModels
{
    public partial class MainViewModel
    {
        public void AttachErrorNotifyingServiceHandlers()
        {
            Log.Info("AttachErrorNotifyingServiceHandlers called.");

            if (errorNotifyingServices != null)
            {
                errorNotifyingServices.ForEach(s => s.Error += HandleServiceError);
            }

            Log.Info("AttachErrorNotifyingServiceHandlers complete.");
        }
        
        private void SetupInputServiceEventHandlers()
        {
            Log.Info("SetupInputServiceEventHandlers called.");

            inputServicePointsPerSecondHandler = (o, value) => { PointsPerSecond = value; };

            // Things that need to happen with every new (x,y) position
            inputServiceCurrentPositionHandler = (o, tuple) =>
            {
                CurrentPositionPoint = tuple.Item1;
                CurrentPositionKey = tuple.Item2;

                if (keyStateService.KeyDownStates[KeyValues.MouseMagneticCursorKey].Value.IsDownOrLockedDown()
                    && !keyStateService.KeyDownStates[KeyValues.SleepKey].Value.IsDownOrLockedDown())
                {
                    mouseOutputService.MoveTo(CurrentPositionPoint);
                }

                foreach (var joystick in JoystickHandlers.Values)
                {
                    joystick.UpdateLookToScroll(CurrentPositionPoint);
                }
            };

            inputServiceSelectionProgressHandler = (o, progress) =>
            {
                if (progress.Item1 == null
                    && progress.Item2 == 0)
                {
                    ResetSelectionProgress(); //Reset all keys
                }
                else if (progress.Item1 != null)
                {
                    if (SelectionMode == SelectionModes.Key
                        && progress.Item1.KeyValue != null)
                    {
                        keyStateService.KeySelectionProgress[progress.Item1.KeyValue] =
                            new NotifyingProxy<double>(progress.Item2);
                    }
                    else if (SelectionMode == SelectionModes.Point)
                    {
                        PointSelectionProgress = new Tuple<Point, double>(progress.Item1.Point, progress.Item2);
                    }
                }
            };

            inputServiceSelectionHandler = (o, value) =>
            {
                Log.Info("Selection event received from InputService.");

                SelectionResultPoints = null; //Clear captured points from previous SelectionResult event

                if (SelectionMode == SelectionModes.Key
                    && value.KeyValue != null)
                {
                    if (!capturingStateManager.CapturingMultiKeySelection)
                    {
                        audioService.PlaySound(Settings.Default.KeySelectionSoundFile, Settings.Default.KeySelectionSoundVolume);
                    }

                    if (KeySelection != null)
                    {
                        Log.InfoFormat("Firing KeySelection event with KeyValue '{0}'", value.KeyValue);
                        KeySelection(this, value.KeyValue);
                    }
                }
                else if (SelectionMode == SelectionModes.Point)
                {
                    if (PointSelection != null)
                    {
                        PointSelection(this, value.Point);

                        if (nextPointSelectionAction != null)
                        {
                            Log.InfoFormat("Executing nextPointSelectionAction delegate with point '{0}'", value.Point);
                            nextPointSelectionAction(value.Point);
                        }
                    }
                }
            };

            inputServiceSelectionResultHandler = async (o, tuple) =>
            {
                Log.Info("SelectionResult event received from InputService.");

                try
                {
                    var points = tuple.Item1;
                    var singleKeyValue = tuple.Item2;
                    var multiKeySelection = tuple.Item3;

                    SelectionResultPoints = points; //Store captured points from SelectionResult event (displayed for debugging)

                    if (SelectionMode == SelectionModes.Key && (singleKeyValue != null || (multiKeySelection != null && multiKeySelection.Any())))
                    {
                        //DynamicKeys can have a list of Commands and perform multiple actions
                        if (singleKeyValue != null && singleKeyValue.Commands != null && singleKeyValue.Commands.Any())
                        {                            
                            //if the key is in a running state and gets pressed, then stop it
                            if (keyStateService.KeyRunningStates[singleKeyValue].Value)
                            {
                                Log.InfoFormat("CommandKey key triggered while in a running state. Ending key: {0}", singleKeyValue.String);
                                keyStateService.KeyRunningStates[singleKeyValue].Value = false;
                            }
                            else
                            {
                                Log.InfoFormat("Starting CommandKey key: {0}", singleKeyValue.String);
                                await CommandKey(singleKeyValue, multiKeySelection);
                            }
                        }
                        else
                        {
                            KeySelectionResult(singleKeyValue, multiKeySelection);
                        }
                    }
                    else if (SelectionMode == SelectionModes.Point)
                    {
                        //SelectionResult event has no real meaning when dealing with point selection
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Exception caught by inputServiceSelectionResultHandler", ex);

                    RaiseToastNotification(OptiKey.Properties.Resources.ERROR_TITLE,
                        OptiKey.Properties.Resources.ERROR_HANDLING_INPUT_SERVICE_SELECTION_RESULT,
                        NotificationTypes.Error, () => { });
                }
            };

            perKeyPauseHandlers = new Dictionary<KeyValue, KeyPauseHandler>();

            Log.Info("SetupInputServiceEventHandlers complete.");
        }

        public void AttachInputServiceEventHandlers()
        {
            Log.Info("AttachInputServiceEventHandlers called.");

            inputService.PointsPerSecond += inputServicePointsPerSecondHandler;
            inputService.CurrentPosition += inputServiceCurrentPositionHandler;
            inputService.SelectionProgress += inputServiceSelectionProgressHandler;
            inputService.Selection += inputServiceSelectionHandler;
            inputService.SelectionResult += inputServiceSelectionResultHandler;

            inputService.PointToKeyValueMap = pointToKeyValueMap;
            inputService.SelectionMode = SelectionMode;
            
            Log.Info("AttachInputServiceEventHandlers complete.");
        }


        public void DetachInputServiceEventHandlers()
        {
            Log.Info("DetachInputServiceEventHandlers called.");

            inputService.PointsPerSecond -= inputServicePointsPerSecondHandler;
            inputService.CurrentPosition -= inputServiceCurrentPositionHandler;
            inputService.SelectionProgress -= inputServiceSelectionProgressHandler;
            inputService.Selection -= inputServiceSelectionHandler;
            inputService.SelectionResult -= inputServiceSelectionResultHandler;
            
            Log.Info("DetachInputServiceEventHandlers complete.");
        }

        private void ProcessChangeKeyboardKeyValue(ChangeKeyboardKeyValue keyValue)
        {
            var currentKeyboard = Keyboard;

            Action backAction = () => { };
            Action exitAction = () => { };
            Action enterAction = () => { };

            // Set up back action
            if (keyValue.Replace)
            {
                var navigableKeyboard = Keyboard as IBackAction;
                if (navigableKeyboard != null && navigableKeyboard.BackAction != null)
                {
                    backAction = navigableKeyboard.BackAction;
                }
            }
            else
            {
                backAction = () =>
                {
                    mainWindowManipulationService.ResizeDockToFull();
                    Keyboard = currentKeyboard;
                };
            }

            if (keyValue.BuiltInKeyboard.HasValue)
            {
                SetKeyboardFromEnum(keyValue.BuiltInKeyboard.Value, mainWindowManipulationService, backAction);
            }
            else
            {
                // Set up new dynamic keyboard
                XmlKeyboard keyboard = new XmlKeyboard();
                // Extract any key states if present
                var initialKeyStates = new Dictionary<KeyValue, KeyDownStates>();
                try
                {
                    keyboard = XmlKeyboard.ReadFromFile(keyValue.KeyboardFilename);
                    XmlKeyStates states = keyboard.InitialKeyStates;

                    if (states != null)
                    {
                        foreach (var item in states.GetKeyOverrides())
                        {
                            // TODO: move this into XmlKeyStates.GetKeyOverrides ?
                            FunctionKeys? fKey = FunctionKeysExtensions.FromString(item.Item1);
                            if (fKey.HasValue)
                            {
                                KeyValue val = new KeyValue(fKey.Value);
                                initialKeyStates.Add(val, item.Item2);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // will get caught and handled when DynamicKeyboard is created so we are good to ignore here 
                }

                DynamicKeyboard newDynKeyboard = new DynamicKeyboard(backAction, keyStateService,
                    keyValue.KeyboardFilename, initialKeyStates);
                Keyboard = newDynKeyboard;

                // Clear the scratchpad when launching a dynamic keyboard.
                // (scratchpad only supported on single dynamic keyboard currently)
                keyboardOutputService.ProcessFunctionKey(FunctionKeys.ClearScratchpad);
            }
        }

        private void ProcessBasicKeyValue(KeyValue singleKeyValue)
        {
            Log.InfoFormat("KeySelectionResult received with string value '{0}' and function key values '{1}'",
                singleKeyValue.String.ToPrintableString(), singleKeyValue.FunctionKey);

            keyStateService.ProgressKeyDownState(singleKeyValue);

            if (!string.IsNullOrEmpty(singleKeyValue.String)
                && singleKeyValue.FunctionKey != null)
            {
                HandleStringAndFunctionKeySelectionResult(singleKeyValue);
            }
            else
            {
                if (!string.IsNullOrEmpty(singleKeyValue.String))
                {
                    //Single key string
                    keyboardOutputService.ProcessSingleKeyText(singleKeyValue.String);
                }

                if (singleKeyValue.FunctionKey != null)
                {
                    //Single key function key
                    HandleFunctionKeySelectionResult(singleKeyValue);
                }
            }
        }

        private void KeySelectionResult(KeyValue singleKeyValue, List<string> multiKeySelection)
        {
            // Pass single key to appropriate processing function
            if (singleKeyValue != null)
            {
                ChangeKeyboardKeyValue kv_link = singleKeyValue as ChangeKeyboardKeyValue;

                if (kv_link != null)
                {
                    ProcessChangeKeyboardKeyValue(kv_link);
                }
                else
                {
                    ProcessBasicKeyValue(singleKeyValue);
                }
            }

            //Multi key selection
            if (multiKeySelection != null
                && multiKeySelection.Any())
            {
                Log.InfoFormat("KeySelectionResult received with '{0}' multiKeySelection results", multiKeySelection.Count);
                keyboardOutputService.ProcessMultiKeyTextAndSuggestions(multiKeySelection);
            }
        }

        private void HandleStringAndFunctionKeySelectionResult(KeyValue singleKeyValue)
        {
            var currentKeyboard = Keyboard;

            switch (singleKeyValue.FunctionKey.Value)
            {
                case FunctionKeys.CommuniKate:
                    if (singleKeyValue.String.Contains(":action:"))
                    {
                        string[] stringSeparators = new string[] { ":action:" };
                        foreach (var action in singleKeyValue.String.Split(stringSeparators, StringSplitOptions.None).ToList())
                        {
                            Log.DebugFormat("Performing CommuniKate action: {0}.", action);
                            if (action.StartsWith("board:"))
                            {
                                string board = action.Substring(6);
                                switch (board)
                                {
                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.Alpha1":
                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.Alpha2":
                                        Settings.Default.UsingCommuniKateKeyboardLayout = false;
                                        Log.Info("Changing keyboard back to Alpha.");
                                        Keyboard = new Alpha1();
                                        break;

                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.ConversationAlpha1":
                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.ConversationAlpha2":
                                        Settings.Default.UsingCommuniKateKeyboardLayout = false;
                                        Log.Info("Changing keyboard back to Conversation Alpha.");
                                        Action conversationAlphaBackAction = () =>
                                        {
                                            Log.Info("Restoring window size.");
                                            mainWindowManipulationService.Restore();
                                            Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                                            Keyboard = new Menu(() => Keyboard = new Alpha1());
                                        };
                                        Keyboard = new ConversationAlpha1(conversationAlphaBackAction);
                                        break;

                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.ConversationConfirm":
                                        Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                                        Log.Info("Changing keyboard back to Conversation Confirm.");
                                        Action conversationConfirmBackAction = () =>
                                        {
                                            Log.Info("Restoring window size.");
                                            mainWindowManipulationService.Restore();
                                            Keyboard = new Menu(() => Keyboard = new Alpha1());
                                        };
                                        Keyboard = new ConversationConfirm(conversationConfirmBackAction);
                                        break;

                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.ConversationNumericAndSymbols":
                                        Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                                        Log.Info("Changing keyboard back to Conversation Numeric And Symbols.");
                                        Action conversationNumericAndSymbolsBackAction = () =>
                                        {
                                            Log.Info("Restoring window size.");
                                            mainWindowManipulationService.Restore();
                                            Keyboard = new Menu(() => Keyboard = new Alpha1());
                                        };
                                        Keyboard = new ConversationNumericAndSymbols(conversationNumericAndSymbolsBackAction);
                                        break;

                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.Currencies1":
                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.Currencies2":
                                        Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                                        Log.Info("Changing keyboard back to Currencies.");
                                        Keyboard = new Currencies1();
                                        break;

                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.Diacritics1":
                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.Diacritics2":
                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.Diacritics3":
                                        Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                                        Log.Info("Changing keyboard back to Diacritics.");
                                        Keyboard = new Diacritics1();
                                        break;

                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.Menu":
                                        Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                                        Log.Info("Changing keyboard back to Menu.");
                                        if (mainWindowManipulationService.WindowState == WindowStates.Maximised)
                                        {
                                            Log.Info("Restoring window size.");
                                            mainWindowManipulationService.Restore();
                                        }
                                        Keyboard = new Menu(() => Keyboard = new Alpha1());
                                        break;

                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.Mouse":
                                        Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                                        Log.Info("Changing keyboard back to Mouse.");
                                        if (mainWindowManipulationService.WindowState == WindowStates.Maximised)
                                        {
                                            Log.Info("Restoring window size.");
                                            mainWindowManipulationService.Restore();
                                        }
                                        Keyboard = new Mouse(() => Keyboard = new Menu(() => Keyboard = new Alpha1()));
                                        break;

                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.NumericAndSymbols1":
                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.NumericAndSymbols2":
                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.NumericAndSymbols3":
                                        Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                                        Log.Info("Changing keyboard back to Numeric And Symbols.");
                                        Keyboard = new NumericAndSymbols1();
                                        break;

                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.PhysicalKeys":
                                        Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                                        Log.Info("Changing keyboard back to Mouse.");
                                        Keyboard = new PhysicalKeys();
                                        break;

                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.SimplifiedAlpha":
                                        Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                                        Log.Info("Changing keyboard back to Simplified Alpha.");
                                        Keyboard = new SimplifiedAlpha(() => Keyboard = new Menu(() => Keyboard = new Alpha1()));
                                        break;

                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.SimplifiedConversationAlpha":
                                        Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                                        Log.Info("Changing keyboard back to Simplified Conversation Alpha.");
                                        Action simplifiedConversationAlphaBackAction = () =>
                                        {
                                            Log.Info("Restoring window size.");
                                            mainWindowManipulationService.Restore();
                                            Keyboard = new Menu(() => Keyboard = new Alpha1());
                                        };
                                        Keyboard = new SimplifiedConversationAlpha(simplifiedConversationAlphaBackAction);
                                        break;

                                    case "JuliusSweetland.OptiKey.UI.ViewModels.Keyboards.WebBrowsing":
                                        Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                                        Log.Info("Changing keyboard back to Web Browsing.");
                                        Keyboard = new WebBrowsing();
                                        break;

                                    default:
                                        if (string.IsNullOrEmpty(Settings.Default.CommuniKateKeyboardCurrentContext))
                                        {
                                            Settings.Default.CommuniKateKeyboardPrevious1Context = Settings.Default.CommuniKateDefaultBoard;
                                            Settings.Default.CommuniKateKeyboardPrevious2Context = Settings.Default.CommuniKateDefaultBoard;
                                            Settings.Default.CommuniKateKeyboardPrevious3Context = Settings.Default.CommuniKateDefaultBoard;
                                            Settings.Default.CommuniKateKeyboardPrevious4Context = Settings.Default.CommuniKateDefaultBoard;
                                        }
                                        else if (Settings.Default.CommuniKateKeyboardPrevious1Context == board)
                                        {
                                            Settings.Default.CommuniKateKeyboardPrevious1Context = Settings.Default.CommuniKateKeyboardPrevious2Context;
                                            Settings.Default.CommuniKateKeyboardPrevious2Context = Settings.Default.CommuniKateKeyboardPrevious3Context;
                                            Settings.Default.CommuniKateKeyboardPrevious3Context = Settings.Default.CommuniKateKeyboardPrevious4Context;
                                            Settings.Default.CommuniKateKeyboardPrevious4Context = Settings.Default.CommuniKateDefaultBoard;
                                        }
                                        else
                                        {
                                            Settings.Default.CommuniKateKeyboardPrevious4Context = Settings.Default.CommuniKateKeyboardPrevious3Context;
                                            Settings.Default.CommuniKateKeyboardPrevious3Context = Settings.Default.CommuniKateKeyboardPrevious2Context;
                                            Settings.Default.CommuniKateKeyboardPrevious2Context = Settings.Default.CommuniKateKeyboardPrevious1Context;
                                            Settings.Default.CommuniKateKeyboardPrevious1Context = Settings.Default.CommuniKateKeyboardCurrentContext;
                                        }

                                        Settings.Default.CommuniKateKeyboardCurrentContext = board;
                                        Log.InfoFormat("CommuniKate keyboard page changed to {0}.", board);
                                        break;
                                }
                            }
                            else if (action.StartsWith("text:"))
                            {
                                keyboardOutputService.ProcessSingleKeyText(action.Substring(5));
                            }
                            else if (action.StartsWith("speak:"))
                            {
                                if (Settings.Default.CommuniKateSpeakSelected)
                                {
                                    var speechCommuniKate = audioService.SpeakNewOrInterruptCurrentSpeech(
                                        action.Substring(6),
                                        () => { KeyStateService.KeyDownStates[KeyValues.SpeakKey].Value = KeyDownStates.Up; },
                                        Settings.Default.CommuniKateSpeakSelectedVolume,
                                        Settings.Default.CommuniKateSpeakSelectedRate,
                                        Settings.Default.SpeechVoice);
                                    KeyStateService.KeyDownStates[KeyValues.SpeakKey].Value = speechCommuniKate ? KeyDownStates.Down : KeyDownStates.Up;
                                }
                            }
                            else if (action.StartsWith("sound:"))
                                audioService.PlaySound(action.Substring(6), Settings.Default.CommuniKateSoundVolume);
                            else if (action.StartsWith("action:"))
                            {
                                string thisAction = action.Substring(7);
                                if (thisAction.StartsWith("+"))
                                {
                                    bool changedAutoSpace = false;
                                    if (Settings.Default.AutoAddSpace)
                                    {
                                        Settings.Default.AutoAddSpace = false;
                                        changedAutoSpace = true;
                                    }
                                    foreach (char letter in thisAction.Substring(1))
                                        keyboardOutputService.ProcessSingleKeyText(letter.ToString());

                                    if (changedAutoSpace)
                                        Settings.Default.AutoAddSpace = true;
                                }
                                else if (thisAction.StartsWith(":"))
                                    switch (thisAction)
                                    {
                                        case ":space":
                                            keyboardOutputService.ProcessSingleKeyText(" ");
                                            break;
                                        case ":home":
                                            Settings.Default.CommuniKateKeyboardCurrentContext = Settings.Default.CommuniKateDefaultBoard;
                                            Log.InfoFormat("CommuniKate keyboard page changed to home board.");
                                            break;
                                        case ":speak":
                                            keyboardOutputService.ProcessFunctionKey(FunctionKeys.Speak);
                                            break;
                                        case ":clear":
                                            keyboardOutputService.ProcessFunctionKey(FunctionKeys.ClearScratchpad);
                                            break;
                                        case ":deleteword":
                                            keyboardOutputService.ProcessFunctionKey(FunctionKeys.BackMany);
                                            break;
                                        case ":backspace":
                                            keyboardOutputService.ProcessFunctionKey(FunctionKeys.BackOne);
                                            break;
                                        case ":ext_volume_up":
                                            Native.PInvoke.keybd_event((byte)Keys.VolumeUp, 0, 0, 0);
                                            break;
                                        case ":ext_volume_down":
                                            Native.PInvoke.keybd_event((byte)Keys.VolumeDown, 0, 0, 0);
                                            break;
                                        case ":ext_volume_mute":
                                            Native.PInvoke.keybd_event((byte)Keys.VolumeMute, 0, 0, 0);
                                            break;
                                        case ":ext_media_next":
                                            Native.PInvoke.keybd_event((byte)Keys.MediaNextTrack, 0, 0, 0);
                                            break;
                                        case ":ext_media_previous":
                                            Native.PInvoke.keybd_event((byte)Keys.MediaPreviousTrack, 0, 0, 0);
                                            break;
                                        case ":ext_media_pause":
                                            Native.PInvoke.keybd_event((byte)Keys.MediaPlayPause, 0, 0, 0);
                                            break;
                                        case ":ext_letters":
                                            Settings.Default.UsingCommuniKateKeyboardLayout = false;
                                            if (mainWindowManipulationService.WindowState == WindowStates.Maximised)
                                            {
                                                Log.Info("Changing keyboard to ConversationAlpha.");
                                                Action conversationAlphaBackAction = () =>
                                                {
                                                    Settings.Default.UsingCommuniKateKeyboardLayout = true;
                                                    Keyboard = currentKeyboard;
                                                };
                                                Keyboard = new ConversationAlpha1(conversationAlphaBackAction);
                                            }
                                            else
                                            {
                                                Log.Info("Changing keyboard to Alpha.");
                                                Keyboard = new Alpha1();
                                            }
                                            break;

                                        case ":ext_numbers":
                                            if (mainWindowManipulationService.WindowState == WindowStates.Maximised)
                                            {
                                                Log.Info("Changing keyboard to ConversationNumericAndSymbols.");
                                                Action BackAction = () =>
                                                {
                                                    Keyboard = currentKeyboard;
                                                };
                                                Keyboard = new ConversationNumericAndSymbols(BackAction);
                                            }
                                            else
                                            {
                                                Log.Info("Changing keyboard to Numeric And Symbols.");
                                                Keyboard = new NumericAndSymbols1();
                                            }
                                            break;

                                        case ":ext_mouse":
                                            if (mainWindowManipulationService.WindowState != WindowStates.Maximised)
                                            {
                                                Log.Info("Changing keyboard to Mouse.");
                                                Action BackAction = () =>
                                                {
                                                    Keyboard = currentKeyboard;
                                                };
                                                Keyboard = new Mouse(BackAction);
                                            }
                                            else
                                            {
                                                Log.Info("Changing keyboard to Mouse.");
                                                Action BackAction = () =>
                                                {
                                                    Keyboard = currentKeyboard;
                                                    Log.Info("Maximising window.");
                                                    mainWindowManipulationService.Maximise();
                                                };
                                                Keyboard = new Mouse(BackAction);
                                                Log.Info("Restoring window size.");
                                                mainWindowManipulationService.Restore();
                                            }
                                            break;
                                        default:
                                            Log.InfoFormat("Unsupported CommuniKate action: {0}.", thisAction);
                                            break;
                                    }
                                else
                                    Log.InfoFormat("Unsupported CommuniKate action: {0}.", thisAction);
                            }

                        }
                    }

                    break;

                case FunctionKeys.SelectVoice:
                    SelectVoice(singleKeyValue.String);
                    break;

                case FunctionKeys.Plugin:
                    RunPlugin_Legacy(singleKeyValue.String);
                    break;

                case FunctionKeys.DynamicKeyboard: // Case where Dynamic keyboard selector requested for a specific location
                {
                    Log.Info("Changing keyboard to DynamicKeyboard.");
                    Log.InfoFormat("directory is {0}", singleKeyValue.String);
                    string directory = singleKeyValue.String;

                    Action reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                    Action backAction = () =>
                    {
                        Keyboard = currentKeyboard;

                        reinstateModifiers();

                        // Clear the scratchpad when leaving keyboard
                        // (proper scratchpad functionality not supported in dynamic keyboards presently)
                        keyboardOutputService.ProcessFunctionKey(FunctionKeys.ClearScratchpad);
                    };

                    int pageIndex = 0;
                    Keyboard = new DynamicKeyboardSelector(backAction, pageIndex, directory);
                }
                    break;

                case FunctionKeys.MouseJoystick:
                case FunctionKeys.LeftJoystick:
                case FunctionKeys.RightJoystick:
                case FunctionKeys.LegacyJoystick:
                case FunctionKeys.WasdJoystick:
                case FunctionKeys.LegacyTriggerJoystick:
                    ToggleJoystick(singleKeyValue);
                    
                    break;

		// FIXME: Came in from merging ee34fb
		// I think this conflicts with the function key payload, 
		// so might not ever be hit? 
                default:
                    //Process single key text, THEN function key. The use case might be to output text and then change keyboard, for example.
                    //N.B. Combining text and a function key changes the KeyValue, which will impact whether the KeyValue can be used to detect
                    //a key which can be locked down, or anything keyed on that KeyValue.
                    keyboardOutputService.ProcessSingleKeyText(singleKeyValue.String);
                    HandleFunctionKeySelectionResult(singleKeyValue);
                break;

                case FunctionKeys.NoJoystick:
                    TurnOffJoysticks();
                break;                    
            }
        }
        

        private async void HandleFunctionKeySelectionResult(KeyValue singleKeyValue)
        {
            var currentKeyboard = Keyboard;
            Action resumeLookToScroll;

            switch (singleKeyValue.FunctionKey.Value)
            {
                case FunctionKeys.AddToDictionary:
                    AddTextToDictionary();
                    break;

                case FunctionKeys.Alpha1Keyboard:
                    if (Settings.Default.EnableCommuniKateKeyboardLayout)
                    {
                        Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                        Settings.Default.CommuniKateKeyboardCurrentContext = Settings.Default.CommuniKateDefaultBoard;
                        Settings.Default.CommuniKateKeyboardPrevious1Context = currentKeyboard.ToString();
                    }
                    Log.Info("Changing keyboard to Alpha1.");
                    Keyboard = new Alpha1();
                    break;

                case FunctionKeys.Alpha2Keyboard:
                    Log.Info("Changing keyboard to Alpha2.");
                    Keyboard = new Alpha2();
                    break;

                case FunctionKeys.Alpha3Keyboard:
                    Log.Info("Changing keyboard to Alpha3.");
                    Keyboard = new Alpha3();
                    break;

                case FunctionKeys.Attention:
                    audioService.PlaySound(Settings.Default.AttentionSoundFile,
                        Settings.Default.AttentionSoundVolume);
                    break;

                case FunctionKeys.BackFromKeyboard:
                    Log.Info("Navigating back from keyboard.");
                    var navigableKeyboard = Keyboard as IBackAction;
                    if (navigableKeyboard != null && navigableKeyboard.BackAction != null)
                    {
                        navigableKeyboard.BackAction();
                    }
                    else
                    {
                        Log.Error("Keyboard doesn't have back action, going back to initial keyboard instead");
                        Keyboard = new Alpha1();
                        if (Settings.Default.EnableCommuniKateKeyboardLayout)
                        {
                            Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                            Settings.Default.CommuniKateKeyboardCurrentContext = Settings.Default.CommuniKateDefaultBoard;
                            Settings.Default.CommuniKateKeyboardPrevious1Context = currentKeyboard.ToString();
                        }

                        InitialiseKeyboard(this.mainWindowManipulationService);
                    }
                    break;

                case FunctionKeys.Calibrate:
                    if (CalibrationService != null)
                    {
                        Log.Info("Calibrate requested.");

                        var question = CalibrationService.CanBeCompletedWithoutManualIntervention
                            ? Resources.CALIBRATION_CONFIRMATION_MESSAGE
                            : Resources.CALIBRATION_REQUIRES_MANUAL_INTERACTION;

                        Keyboard = new YesNoQuestion(
                            question,
                            () =>
                            {
                                inputService.RequestSuspend();
                                CalibrateRequest.Raise(new NotificationWithCalibrationResult(), calibrationResult =>
                                {
                                    if (calibrationResult.Success)
                                    {
                                        audioService.PlaySound(Settings.Default.InfoSoundFile, Settings.Default.InfoSoundVolume);
                                        RaiseToastNotification(Resources.SUCCESS, calibrationResult.Message, NotificationTypes.Normal, () => inputService.RequestResume());
                                    }
                                    else
                                    {
                                        audioService.PlaySound(Settings.Default.ErrorSoundFile, Settings.Default.ErrorSoundVolume);
                                        RaiseToastNotification(Resources.CRASH_TITLE, calibrationResult.Exception != null
                                                ? calibrationResult.Exception.Message
                                                : calibrationResult.Message ?? Resources.UNKNOWN_CALIBRATION_ERROR,
                                            NotificationTypes.Error,
                                            () => inputService.RequestResume());
                                    }
                                });
                                Keyboard = currentKeyboard;
                            },
                            () =>
                            {
                                Keyboard = currentKeyboard;
                            });
                    }
                    break;

                case FunctionKeys.CatalanSpain:
                    SelectLanguage(Languages.CatalanSpain);
                    break;

                case FunctionKeys.CollapseDock:
                    Log.Info("Collapsing dock.");
                    mainWindowManipulationService.ResizeDockToCollapsed();
                    if (Keyboard is ViewModels.Keyboards.Mouse)
                    {
                        Settings.Default.MouseKeyboardDockSize = DockSizes.Collapsed;
                    }
                    break;

                case FunctionKeys.CommuniKateKeyboard:
                    Settings.Default.CommuniKateKeyboardCurrentContext = Settings.Default.CommuniKateDefaultBoard;
                    Settings.Default.UsingCommuniKateKeyboardLayout = true;
                    Settings.Default.CommuniKateKeyboardPrevious1Context = currentKeyboard.ToString();
                    Log.Info("Changing keyboard to CommuniKate.");
                    Keyboard = new Alpha1();
                    break;

                case FunctionKeys.ConversationAlpha1Keyboard:
                    if (Settings.Default.EnableCommuniKateKeyboardLayout)
                    {
                        Settings.Default.UsingCommuniKateKeyboardLayout = Settings.Default.UseCommuniKateKeyboardLayoutByDefault;
                        Settings.Default.CommuniKateKeyboardCurrentContext = Settings.Default.CommuniKateDefaultBoard;
                        Settings.Default.CommuniKateKeyboardPrevious1Context = currentKeyboard.ToString();
                    }
                    Log.Info("Changing keyboard to ConversationAlpha1.");
                    var opacityBeforeConversationAlpha1 = mainWindowManipulationService.GetOpacity();
                    Action conversationAlpha1BackAction = currentKeyboard is ConversationAlpha2
                        ? ((ConversationAlpha2)currentKeyboard).BackAction
                        : currentKeyboard is ConversationAlpha3
                            ? ((ConversationAlpha3)currentKeyboard).BackAction
                            : currentKeyboard is ConversationNumericAndSymbols
                                ? ((ConversationNumericAndSymbols)currentKeyboard).BackAction
                                : currentKeyboard is SimplifiedConversationAlpha
                                    ? ((SimplifiedConversationAlpha)currentKeyboard).BackAction
                                    : currentKeyboard is ConversationConfirm
                                        ? ((ConversationConfirm)currentKeyboard).BackAction
                                        : () =>
                                        {
                                            Log.Info("Restoring window size.");
                                            mainWindowManipulationService.Restore();
                                            Log.InfoFormat("Restoring window opacity to {0}", opacityBeforeConversationAlpha1);
                                            mainWindowManipulationService.SetOpacity(opacityBeforeConversationAlpha1);
                                            Keyboard = currentKeyboard;
                                        };
                    Keyboard = new ConversationAlpha1(conversationAlpha1BackAction);
                    Log.Info("Maximising window.");
                    mainWindowManipulationService.Maximise();
                    Log.InfoFormat("Setting opacity to 1 (fully opaque)");
                    mainWindowManipulationService.SetOpacity(1);
                    break;

                case FunctionKeys.ConversationAlpha2Keyboard:
                    Log.Info("Changing keyboard to ConversationAlpha2.");
                    var opacityBeforeConversationAlpha2 = mainWindowManipulationService.GetOpacity();
                    Action conversationAlpha2BackAction = currentKeyboard is ConversationAlpha1
                        ? ((ConversationAlpha1)currentKeyboard).BackAction
                        : currentKeyboard is ConversationAlpha3
                            ? ((ConversationAlpha3)currentKeyboard).BackAction
                            : currentKeyboard is ConversationNumericAndSymbols
                                ? ((ConversationNumericAndSymbols)currentKeyboard).BackAction
                                : currentKeyboard is SimplifiedConversationAlpha
                                    ? ((SimplifiedConversationAlpha)currentKeyboard).BackAction
                                    : currentKeyboard is ConversationConfirm
                                        ? ((ConversationConfirm)currentKeyboard).BackAction
                                        : () =>
                                        {
                                            Log.Info("Restoring window size.");
                                            mainWindowManipulationService.Restore();
                                            Log.InfoFormat("Restoring window opacity to {0}", opacityBeforeConversationAlpha2);
                                            mainWindowManipulationService.SetOpacity(opacityBeforeConversationAlpha2);
                                            Keyboard = currentKeyboard;
                                        };
                    Keyboard = new ConversationAlpha2(conversationAlpha2BackAction);
                    Log.Info("Maximising window.");
                    mainWindowManipulationService.Maximise();
                    Log.InfoFormat("Setting opacity to 1 (fully opaque)");
                    mainWindowManipulationService.SetOpacity(1);
                    break;

                case FunctionKeys.ConversationAlpha3Keyboard:
                    Log.Info("Changing keyboard to ConversationAlpha3.");
                    var opacityBeforeConversationAlpha3 = mainWindowManipulationService.GetOpacity();
                    Action conversationAlpha3BackAction = currentKeyboard is ConversationAlpha1
                        ? ((ConversationAlpha1)currentKeyboard).BackAction
                        : currentKeyboard is ConversationAlpha2
                            ? ((ConversationAlpha2)currentKeyboard).BackAction
                            : currentKeyboard is ConversationNumericAndSymbols
                            ? ((ConversationNumericAndSymbols)currentKeyboard).BackAction
                            : currentKeyboard is SimplifiedConversationAlpha
                                ? ((SimplifiedConversationAlpha)currentKeyboard).BackAction
                                : currentKeyboard is ConversationConfirm
                                    ? ((ConversationConfirm)currentKeyboard).BackAction
                                    : () =>
                                    {
                                        Log.Info("Restoring window size.");
                                        mainWindowManipulationService.Restore();
                                        Log.InfoFormat("Restoring window opacity to {0}", opacityBeforeConversationAlpha3);
                                        mainWindowManipulationService.SetOpacity(opacityBeforeConversationAlpha3);
                                        Keyboard = currentKeyboard;
                                    };
                    Keyboard = new ConversationAlpha3(conversationAlpha3BackAction);
                    Log.Info("Maximising window.");
                    mainWindowManipulationService.Maximise();
                    Log.InfoFormat("Setting opacity to 1 (fully opaque)");
                    mainWindowManipulationService.SetOpacity(1);
                    break;

                case FunctionKeys.ConversationCommuniKateKeyboard:
                    Settings.Default.CommuniKateKeyboardCurrentContext = Settings.Default.CommuniKateDefaultBoard;
                    Settings.Default.UsingCommuniKateKeyboardLayout = true;
                    Settings.Default.CommuniKateKeyboardPrevious1Context = currentKeyboard.ToString();
                    Log.Info("Changing keyboard to Conversation CommuniKate.");
                    Action conversationAlphaBackAction = () =>
                    {
                        Log.Info("Restoring window size.");
                        mainWindowManipulationService.Restore();
                        Keyboard = new Menu(() => Keyboard = new Alpha1());
                    };
                    Keyboard = new ConversationAlpha1(conversationAlphaBackAction);
                    break;

                case FunctionKeys.ConversationConfirmKeyboard:
                    Log.Info("Changing keyboard to ConversationConfirm.");
                    var opacityBeforeConversationConfirm = mainWindowManipulationService.GetOpacity();
                    Action conversationConfirmBackAction = currentKeyboard is ConversationAlpha1
                        ? ((ConversationAlpha1)currentKeyboard).BackAction
                        : currentKeyboard is ConversationAlpha2
                            ? ((ConversationAlpha2)currentKeyboard).BackAction
                            : currentKeyboard is SimplifiedConversationAlpha
                                ? ((SimplifiedConversationAlpha)currentKeyboard).BackAction
                                : currentKeyboard is ConversationNumericAndSymbols
                                    ? ((ConversationNumericAndSymbols)currentKeyboard).BackAction
                                    : () =>
                                    {
                                        Log.Info("Restoring window size.");
                                        mainWindowManipulationService.Restore();
                                        Log.InfoFormat("Restoring window opacity to {0}", opacityBeforeConversationConfirm);
                                        mainWindowManipulationService.SetOpacity(opacityBeforeConversationConfirm);
                                        Keyboard = currentKeyboard;
                                    };
                    Keyboard = new ConversationConfirm(conversationConfirmBackAction);
                    Log.Info("Maximising window.");
                    mainWindowManipulationService.Maximise();
                    Log.InfoFormat("Setting opacity to 1 (fully opaque)");
                    mainWindowManipulationService.SetOpacity(1);
                    break;

                case FunctionKeys.ConversationConfirmNo:
                    var speechStartedNo = audioService.SpeakNewOrInterruptCurrentSpeech(
                        Resources.NO,
                        () => { KeyStateService.KeyDownStates[KeyValues.SpeakKey].Value = KeyDownStates.Up; },
                        Settings.Default.SpeechVolume,
                        Settings.Default.SpeechRate,
                        Settings.Default.SpeechVoice);
                    KeyStateService.KeyDownStates[KeyValues.SpeakKey].Value = speechStartedNo ? KeyDownStates.Down : KeyDownStates.Up;
                    break;

                case FunctionKeys.ConversationConfirmYes:
                    var speechStartedYes = audioService.SpeakNewOrInterruptCurrentSpeech(
                        Resources.YES,
                        () => { KeyStateService.KeyDownStates[KeyValues.SpeakKey].Value = KeyDownStates.Up; },
                        Settings.Default.SpeechVolume,
                        Settings.Default.SpeechRate,
                        Settings.Default.SpeechVoice);
                    KeyStateService.KeyDownStates[KeyValues.SpeakKey].Value = speechStartedYes ? KeyDownStates.Down : KeyDownStates.Up;
                    break;

                case FunctionKeys.ConversationNumericAndSymbolsKeyboard:
                    Log.Info("Changing keyboard to ConversationNumericAndSymbols.");
                    var opacityBeforeConversationNumericAndSymbols = mainWindowManipulationService.GetOpacity();
                    Action conversationNumericAndSymbolsBackAction = currentKeyboard is ConversationConfirm
                        ? ((ConversationConfirm)currentKeyboard).BackAction
                        : currentKeyboard is ConversationAlpha1
                            ? ((ConversationAlpha1)currentKeyboard).BackAction
                            : currentKeyboard is ConversationAlpha2
                                ? ((ConversationAlpha2)currentKeyboard).BackAction
                                : currentKeyboard is SimplifiedConversationAlpha
                                    ? ((SimplifiedConversationAlpha)currentKeyboard).BackAction
                                    : () =>
                                    {
                                        Log.Info("Restoring window size.");
                                        mainWindowManipulationService.Restore();
                                        Log.InfoFormat("Restoring window opacity to {0}", opacityBeforeConversationNumericAndSymbols);
                                        mainWindowManipulationService.SetOpacity(opacityBeforeConversationNumericAndSymbols);
                                        Keyboard = currentKeyboard;
                                    };
                    Keyboard = new ConversationNumericAndSymbols(conversationNumericAndSymbolsBackAction);
                    Log.Info("Maximising window.");
                    mainWindowManipulationService.Maximise();
                    Log.InfoFormat("Setting opacity to 1 (fully opaque)");
                    mainWindowManipulationService.SetOpacity(1);
                    break;

                case FunctionKeys.CopyAllFromScratchpad:
                    {
                        Log.Info("Copying text from scratchpad to clipboard.");
                        string textFromScratchpad = KeyboardOutputService.Text;

                        if (!string.IsNullOrEmpty(textFromScratchpad))
                        {
                            Clipboard.SetText(textFromScratchpad);
                        }
                    }
                    break;

                case FunctionKeys.CroatianCroatia:
                    SelectLanguage(Languages.CroatianCroatia);
                    break;
                case FunctionKeys.SerbianSerbia:
                    SelectLanguage(Languages.SerbianSerbia);
                    break;

                case FunctionKeys.Currencies1Keyboard:
                    Log.Info("Changing keyboard to Currencies1.");
                    Keyboard = new Currencies1();
                    break;

                case FunctionKeys.Currencies2Keyboard:
                    Log.Info("Changing keyboard to Currencies2.");
                    Keyboard = new Currencies2();
                    break;

                case FunctionKeys.DynamicKeyboard:
                    {
                        Log.Info("Changing keyboard to DynamicKeyboard.");

                        var currentKeyboard2 = Keyboard;

                        Action reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        Action backAction = () =>
                        {
                            Keyboard = currentKeyboard2;

                            reinstateModifiers();

                            // Clear the scratchpad when leaving keyboard
                            // (proper scratchpad functionality not supported in dynamic keyboards presently
                            keyboardOutputService.ProcessFunctionKey(FunctionKeys.ClearScratchpad);
                        };

                        int pageIndex = 0;
                        if (Keyboard is DynamicKeyboardSelector)
                        {
                            var kb = Keyboard as DynamicKeyboardSelector;
                            backAction = kb.BackAction;
                            pageIndex = kb.PageIndex + 1;
                        }
                        Keyboard = new DynamicKeyboardSelector(backAction, pageIndex);
                    }
                    break;


                case FunctionKeys.DynamicKeyboardPrev:
                    {
                        Log.Info("Changing keyboard to prev DynamicKeyboard.");

                        Action backAction;
                        var currentKeyboard2 = Keyboard;
                        int pageIndex = 0;
                        string directory = Settings.Default.DynamicKeyboardsLocation;
                        if (Keyboard is DynamicKeyboardSelector)
                        {
                            var kb = Keyboard as DynamicKeyboardSelector;
                            directory = kb.Directory;
                            backAction = kb.BackAction;
                            pageIndex = kb.PageIndex - 1;
                        }
                        else
                        {
                            Log.Error("Unexpectedly entering DynamicKeyboardPrev from somewhere other than DynamicKeyboard");
                            backAction = () =>
                            {
                                Keyboard = currentKeyboard2;
                            };
                        }
                        Keyboard = new DynamicKeyboardSelector(backAction, pageIndex, directory);
                    }
                    break;

                case FunctionKeys.DynamicKeyboardNext:
                    {
                        Log.Info("Changing keyboard to next DynamicKeyboard.");

                        Action backAction;
                        var currentKeyboard2 = Keyboard;
                        int pageIndex = 0;
                        string directory = Settings.Default.DynamicKeyboardsLocation;
                        if (Keyboard is DynamicKeyboardSelector)
                        {
                            var kb = Keyboard as DynamicKeyboardSelector;
                            directory = kb.Directory;
                            backAction = kb.BackAction;
                            pageIndex = kb.PageIndex + 1;
                        }
                        else
                        {
                            Log.Error("Unexpectedly entering DynamicKeyboardNext from somewhere other than DynamicKeyboard");
                            backAction = () =>
                            {
                                Keyboard = currentKeyboard2;
                            };
                        }
                        Keyboard = new DynamicKeyboardSelector(backAction, pageIndex, directory);
                    }
                    break;

                case FunctionKeys.CzechCzechRepublic:
                    SelectLanguage(Languages.CzechCzechRepublic);
                    break;

                case FunctionKeys.DanishDenmark:
                    SelectLanguage(Languages.DanishDenmark);
                    break;

                case FunctionKeys.DecreaseOpacity:
                    Log.Info("Decreasing opacity.");
                    mainWindowManipulationService.IncrementOrDecrementOpacity(false);
                    break;

                case FunctionKeys.Diacritic1Keyboard:
                    Log.Info("Changing keyboard to Diacritic1.");
                    Keyboard = new Diacritics1();
                    break;

                case FunctionKeys.Diacritic2Keyboard:
                    Log.Info("Changing keyboard to Diacritic2.");
                    Keyboard = new Diacritics2();
                    break;

                case FunctionKeys.Diacritic3Keyboard:
                    Log.Info("Changing keyboard to Diacritic3.");
                    Keyboard = new Diacritics3();
                    break;

                case FunctionKeys.DutchBelgium:
                    SelectLanguage(Languages.DutchBelgium);
                    break;

                case FunctionKeys.DutchNetherlands:
                    SelectLanguage(Languages.DutchNetherlands);
                    break;

                case FunctionKeys.EnglishCanada:
                    SelectLanguage(Languages.EnglishCanada);
                    break;

                case FunctionKeys.EnglishUK:
                    SelectLanguage(Languages.EnglishUK);
                    break;

                case FunctionKeys.EnglishUS:
                    SelectLanguage(Languages.EnglishUS);
                    break;

                case FunctionKeys.ExpandDock:
                    Log.Info("Expanding dock.");
                    mainWindowManipulationService.ResizeDockToFull();
                    if (Keyboard is ViewModels.Keyboards.Mouse)
                    {
                        Settings.Default.MouseKeyboardDockSize = DockSizes.Full;
                    }
                    break;

                case FunctionKeys.ExpandToBottom:
                    Log.InfoFormat("Expanding to bottom by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Expand(ExpandToDirections.Bottom, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ExpandToBottomAndLeft:
                    Log.InfoFormat("Expanding to bottom and left by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Expand(ExpandToDirections.BottomLeft, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ExpandToBottomAndRight:
                    Log.InfoFormat("Expanding to bottom and right by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Expand(ExpandToDirections.BottomRight, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ExpandToLeft:
                    Log.InfoFormat("Expanding to left by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Expand(ExpandToDirections.Left, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ExpandToRight:
                    Log.InfoFormat("Expanding to right by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Expand(ExpandToDirections.Right, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ExpandToTop:
                    Log.InfoFormat("Expanding to top by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Expand(ExpandToDirections.Top, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ExpandToTopAndLeft:
                    Log.InfoFormat("Expanding to top and left by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Expand(ExpandToDirections.TopLeft, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ExpandToTopAndRight:
                    Log.InfoFormat("Expanding to top and right by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Expand(ExpandToDirections.TopRight, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.FinnishFinland:
                    SelectLanguage(Languages.FinnishFinland);
                    break;

                case FunctionKeys.FrenchCanada:
                    SelectLanguage(Languages.FrenchCanada);
                    break;

                case FunctionKeys.FrenchFrance:
                    SelectLanguage(Languages.FrenchFrance);
                    break;

                case FunctionKeys.GeorgianGeorgia:
                    SelectLanguage(Languages.GeorgianGeorgia);
                    break;

                case FunctionKeys.GermanGermany:
                    SelectLanguage(Languages.GermanGermany);
                    break;

                case FunctionKeys.GreekGreece:
                    SelectLanguage(Languages.GreekGreece);
                    break;

                case FunctionKeys.HebrewIsrael:
                    SelectLanguage(Languages.HebrewIsrael);
                    break;

                case FunctionKeys.HindiIndia:
                    SelectLanguage(Languages.HindiIndia);
                    break;

                case FunctionKeys.HungarianHungary:
                    SelectLanguage(Languages.HungarianHungary);
                    break;

                case FunctionKeys.IncreaseOpacity:
                    Log.Info("Increasing opacity.");
                    mainWindowManipulationService.IncrementOrDecrementOpacity(true);
                    break;

                case FunctionKeys.ItalianItaly:
                    SelectLanguage(Languages.ItalianItaly);
                    break;

                case FunctionKeys.JapaneseJapan:
                    SelectLanguage(Languages.JapaneseJapan);
                    break;

                case FunctionKeys.KoreanKorea:
                    SelectLanguage(Languages.KoreanKorea);
                    break;

                case FunctionKeys.LanguageKeyboard:
                    Log.Info("Restoring window size.");
                    mainWindowManipulationService.Restore();
                    Log.Info("Changing keyboard to Language.");
                    Keyboard = new Language(() => Keyboard = currentKeyboard);
                    break;

                case FunctionKeys.LookToScrollActive:
                    ToggleLookToScroll();
                    break;

                case FunctionKeys.MouseJoystick:
                case FunctionKeys.LeftJoystick:
                case FunctionKeys.RightJoystick:
                case FunctionKeys.LegacyJoystick:
                case FunctionKeys.LegacyTriggerJoystick:
                case FunctionKeys.WasdJoystick:
                    // these all have optional payloads in the FunctionKey's string, so the logic
                    // is kept in HandleStringAndFunctionKeySelectionResult
                    KeyValue newValue = new KeyValue(singleKeyValue.FunctionKey, null);
                    HandleStringAndFunctionKeySelectionResult(newValue);
                    break;

                case FunctionKeys.LookToScrollBounds:
                    //FIXME: reinstate for scrolling
                    ////HandleLookToScrollBoundsKeySelected();
                    break;

                case FunctionKeys.LookToScrollIncrement:
                    //FIXME: reinstate for scrolling
                    ////SelectNextLookToScrollIncrement();
                    break;

                case FunctionKeys.LookToScrollMode:
                    //FIXME: reinstate for scrolling
                    ////SelectNextLookToScrollMode();
                    break;

                case FunctionKeys.LookToScrollSpeed:
                    //FIXME: reinstate for scrolling
                    //SelectNextLookToScrollSpeed();
                    break;

                case FunctionKeys.MenuKeyboard:
                    Log.Info("Restoring window size.");
                    mainWindowManipulationService.Restore();
                    Log.Info("Changing keyboard to Menu.");
                    Keyboard = new Menu(() => Keyboard = currentKeyboard);
                    break;

                case FunctionKeys.Minimise:
                    Log.Info("Minimising window.");
                    mainWindowManipulationService.Minimise();
                    Log.Info("Changing keyboard to Minimised.");
                    Keyboard = new Minimised(() =>
                    {
                        Log.Info("Restoring window size.");
                        mainWindowManipulationService.Restore();
                        Keyboard = currentKeyboard;
                    });
                    break;

                case FunctionKeys.More:
                    ShowMore();
                    break;

                case FunctionKeys.MouseDrag:
                case FunctionKeys.MouseDragLive:
                    Log.Info("Mouse drag selected.");

                    // If we're doing the 'live' version, we'll temporarily hold down the magnetic cursor key
                    bool doActionLive = singleKeyValue.FunctionKey.Value == FunctionKeys.MouseDragLive;
                    bool forceMagneticCursor = doActionLive && 
                        !keyStateService.KeyDownStates[KeyValues.MouseMagneticCursorKey].Value.IsDownOrLockedDown();                    

                    // FIXME: suspend other 2d handlers too
                    // FIXME reinstate LookToScroll handler resumeLookToScroll = leftJoystickInteractionHandler.SuspendLookToScrollWhileChoosingPointForMouse();
                        SetupFinalClickAction(firstFinalPoint =>
                    {
                        if (firstFinalPoint != null)
                        {
                            audioService.PlaySound(Settings.Default.MouseDownSoundFile, Settings.Default.MouseDownSoundVolume);

                            if (doActionLive)
                            {
                                // start immediately
                                mouseOutputService.MoveTo(firstFinalPoint.Value);
                                audioService.PlaySound(Settings.Default.MouseDownSoundFile, Settings.Default.MouseDownSoundVolume);
                                mouseOutputService.LeftButtonDown();
                                keyStateService.KeyDownStates[KeyValues.MouseMagneticCursorKey].Value = KeyDownStates.Down;                                
                            }

                            //This class reacts to the point selection event AFTER the MagnifyPopup reacts to it.
                            //This means that if the MagnifyPopup sets the nextPointSelectionAction from the
                            //MagnifiedPointSelectionAction then it will be called immediately i.e. for the same point.
                            //The workaround is to set the nextPointSelectionAction to a lambda which sets the NEXT
                            //nextPointSelectionAction. This means the immediate call to the lambda just sets up the
                            //delegate for the subsequent call.
                            nextPointSelectionAction = repeatFirstClickOrSecondClickAction =>
                            {
                                Action<Point> deferIfMagnifyingElseDoNow = repeatFirstClickOrSecondClickPoint =>
                                {
                                    Action<Point?> secondFinalClickAction = secondFinalPoint =>
                                    {
                                        if (secondFinalPoint != null)
                                        {
                                            Action<Point, Point> simulateDrag = (fp1, fp2) =>
                                            {
                                                Log.InfoFormat("Performing mouse drag between points ({0},{1}) and {2},{3}).", fp1.X, fp1.Y, fp2.X, fp2.Y);
                                                Action reinstateModifiers = () => { };
                                                if (keyStateService.SimulateKeyStrokes
                                                    && Settings.Default.SuppressModifierKeysForAllMouseActions)
                                                {
                                                    reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                                                }

                                                // Finish drag
                                                if (doActionLive)
                                                {
                                                    if (forceMagneticCursor)
                                                    {
                                                        keyStateService.KeyDownStates[KeyValues.MouseMagneticCursorKey].Value = KeyDownStates.Up;
                                                    }
                                                    mouseOutputService.MoveTo(fp2);
                                                    audioService.PlaySound(Settings.Default.MouseUpSoundFile, Settings.Default.MouseUpSoundVolume);
                                                    mouseOutputService.LeftButtonUp();
                                                }
                                                else
                                                {
                                                    // Perform whole drag event now


                                                    mouseOutputService.MoveTo(fp1);
                                                    audioService.PlaySound(Settings.Default.MouseDownSoundFile, Settings.Default.MouseDownSoundVolume);
                                                    mouseOutputService.LeftButtonDown();
                                                    Thread.Sleep(Settings.Default.MouseDragDelayAfterLeftMouseButtonDownBeforeMove);

                                                    Vector stepVector = fp1 - fp2;
                                                    int steps = Settings.Default.MouseDragNumberOfSteps;
                                                    stepVector = stepVector / steps;

                                                    do
                                                    {
                                                        fp1.X = fp1.X - stepVector.X;
                                                        fp1.Y = fp1.Y - stepVector.Y;
                                                        mouseOutputService.MoveTo(fp1);
                                                        Thread.Sleep(Settings.Default.MouseDragDelayBetweenEachStep);
                                                        steps--;
                                                    } while (steps > 0);

                                                    mouseOutputService.MoveTo(fp2);
                                                    Thread.Sleep(Settings.Default.MouseDragDelayAfterMoveBeforeLeftMouseButtonUp);
                                                    audioService.PlaySound(Settings.Default.MouseUpSoundFile, Settings.Default.MouseUpSoundVolume);
                                                    mouseOutputService.LeftButtonUp();
                                                }

                                                reinstateModifiers();
                                            };

                                            lastMouseActionStateManager.LastMouseAction =
                                                () => simulateDrag(firstFinalPoint.Value, secondFinalPoint.Value);
                                            simulateDrag(firstFinalPoint.Value, secondFinalPoint.Value);
                                        }

                                        ResetAndCleanupAfterMouseAction();
                                        //FIXME reinstate resumeLookToScroll();
                                    };

                                    if (keyStateService.KeyDownStates[KeyValues.MouseMagnifierKey].Value.IsDownOrLockedDown())
                                    {
                                        ShowCursor = false; //See MouseMoveAndLeftClick case for explanation of this
                                        MagnifiedPointSelectionAction = secondFinalClickAction;
                                        MagnifyAtPoint = repeatFirstClickOrSecondClickPoint;
                                        ShowCursor = true;
                                    }
                                    else
                                    {
                                        secondFinalClickAction(repeatFirstClickOrSecondClickPoint);
                                    }

                                    nextPointSelectionAction = null;
                                };

                                if (keyStateService.KeyDownStates[KeyValues.MouseMagnifierKey].Value.IsDownOrLockedDown())
                                {
                                    nextPointSelectionAction = deferIfMagnifyingElseDoNow;
                                }
                                else
                                {
                                    deferIfMagnifyingElseDoNow(repeatFirstClickOrSecondClickAction);
                                }
                            };
                        }
                        else
                        {
                            //Reset and clean up if we are not continuing to 2nd point
                            SelectionMode = SelectionModes.Key;
                            nextPointSelectionAction = null;
                            ShowCursor = false;
                            if (keyStateService.KeyDownStates[KeyValues.MouseMagnifierKey].Value == KeyDownStates.Down)
                            {
                                keyStateService.KeyDownStates[KeyValues.MouseMagnifierKey].Value = KeyDownStates.Up; //Release magnifier if down but not locked down
                            }
                            //FIXME reinstate resumeLookToScroll();

                            // Reset overridden magnetic cursor
                            if (forceMagneticCursor)
                            {
                                keyStateService.KeyDownStates[KeyValues.MouseMagneticCursorKey].Value = KeyDownStates.Up;
                            }
                        }

                        //Reset and clean up
                        MagnifyAtPoint = null;
                        MagnifiedPointSelectionAction = null;
                    }, finalClickInSeries: false);
                    break;

                case FunctionKeys.MouseKeyboard:
                    {
                        Log.Info("Changing keyboard to Mouse.");
                        Action backAction;
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysWhenInMouseKeyboard)
                        {
                            var restoreModifierStates = keyStateService.ReleaseModifiers(Log);
                            backAction = () =>
                            {
                                restoreModifierStates();
                                Keyboard = currentKeyboard;
                            };
                        }
                        else
                        {
                            backAction = () => Keyboard = currentKeyboard;
                        }
                        Keyboard = new Mouse(backAction);
                        //Reinstate mouse keyboard docked state (if docked)
                        if (Settings.Default.MainWindowState == WindowStates.Docked)
                        {
                            if (Settings.Default.MouseKeyboardDockSize == DockSizes.Full
                                && Settings.Default.MainWindowDockSize != DockSizes.Full)
                            {
                                mainWindowManipulationService.ResizeDockToFull();
                            }
                            else if (Settings.Default.MouseKeyboardDockSize == DockSizes.Collapsed
                                && Settings.Default.MainWindowDockSize != DockSizes.Collapsed)
                            {
                                mainWindowManipulationService.ResizeDockToCollapsed();
                            }
                        }
                    }
                    break;

                case FunctionKeys.MouseLeftClick:                    
                    Log.InfoFormat("Mouse left click at current point");
                    Action performLeftClick = () =>
                    {
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }                        
                        audioService.PlaySound(Settings.Default.MouseClickSoundFile, Settings.Default.MouseClickSoundVolume);
                        mouseOutputService.LeftButtonClick();
                        reinstateModifiers();
                    };
                    lastMouseActionStateManager.LastMouseAction = () => performLeftClick();
                    performLeftClick();
                    break;

                case FunctionKeys.MouseLeftClickAtCentre:
                    // Get "centre of screen" point
                    // FIXME: do we need GetTransformFromDevice/GetTransformToDevice here? not sure coordinates

                    var leftClickPoint2 = new Point(Graphics.PrimaryScreenWidthInPixels/2, Graphics.PrimaryScreenHeightInPixels/2);
                    
                    Log.InfoFormat("Mouse left click selected at point ({0},{1}).", leftClickPoint2.X, leftClickPoint2.Y);
                    Action performLeftClick2 = () =>
                    {
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }
                        mouseOutputService.MoveTo(leftClickPoint2);
                        audioService.PlaySound(Settings.Default.MouseClickSoundFile, Settings.Default.MouseClickSoundVolume);
                        mouseOutputService.LeftButtonClick();
                        reinstateModifiers();
                    };
                    lastMouseActionStateManager.LastMouseAction = () => performLeftClick2();
                    performLeftClick2();
                    break;

                case FunctionKeys.MouseLeftDoubleClick:
                    var leftDoubleClickPoint = mouseOutputService.GetCursorPosition();
                    Log.InfoFormat("Mouse left double click selected at point ({0},{1}).", leftDoubleClickPoint.X, leftDoubleClickPoint.Y);
                    Action performLeftDoubleClick = () =>
                    {
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }
                        mouseOutputService.MoveTo(leftDoubleClickPoint);
                        audioService.PlaySound(Settings.Default.MouseDoubleClickSoundFile, Settings.Default.MouseDoubleClickSoundVolume);
                        mouseOutputService.LeftButtonDoubleClick();
                        reinstateModifiers();
                    };
                    lastMouseActionStateManager.LastMouseAction = () => performLeftDoubleClick();
                    performLeftDoubleClick();
                    break;

                case FunctionKeys.MouseLeftDownUp:
                    var leftDownUpPoint = mouseOutputService.GetCursorPosition();
                    if (keyStateService.KeyDownStates[KeyValues.MouseLeftDownUpKey].Value.IsDownOrLockedDown())
                    {
                        Log.InfoFormat("Pressing mouse left button down at point ({0},{1}).", leftDownUpPoint.X, leftDownUpPoint.Y);
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }
                        audioService.PlaySound(Settings.Default.MouseDownSoundFile, Settings.Default.MouseDownSoundVolume);
                        mouseOutputService.LeftButtonDown();
                        reinstateModifiers();
                        lastMouseActionStateManager.LastMouseAction = null;
                    }
                    else
                    {
                        Log.InfoFormat("Releasing mouse left button at point ({0},{1}).", leftDownUpPoint.X, leftDownUpPoint.Y);
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }
                        audioService.PlaySound(Settings.Default.MouseUpSoundFile, Settings.Default.MouseUpSoundVolume);
                        mouseOutputService.LeftButtonUp();
                        reinstateModifiers();
                        lastMouseActionStateManager.LastMouseAction = null;
                    }
                    break;

                case FunctionKeys.MouseMiddleClick:
                    var middleClickPoint = mouseOutputService.GetCursorPosition();
                    Log.InfoFormat("Mouse middle click selected at point ({0},{1}).", middleClickPoint.X, middleClickPoint.Y);
                    Action performMiddleClick = () =>
                    {
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }
                        mouseOutputService.MoveTo(middleClickPoint);
                        audioService.PlaySound(Settings.Default.MouseClickSoundFile, Settings.Default.MouseClickSoundVolume);
                        mouseOutputService.MiddleButtonClick();
                        reinstateModifiers();
                    };
                    lastMouseActionStateManager.LastMouseAction = () => performMiddleClick();
                    performMiddleClick();
                    break;

                case FunctionKeys.MouseMiddleDownUp:
                    var middleDownUpPoint = mouseOutputService.GetCursorPosition();
                    if (keyStateService.KeyDownStates[KeyValues.MouseMiddleDownUpKey].Value.IsDownOrLockedDown())
                    {
                        Log.InfoFormat("Pressing mouse middle button down at point ({0},{1}).", middleDownUpPoint.X, middleDownUpPoint.Y);
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }
                        audioService.PlaySound(Settings.Default.MouseDownSoundFile, Settings.Default.MouseDownSoundVolume);
                        mouseOutputService.MiddleButtonDown();
                        reinstateModifiers();
                        lastMouseActionStateManager.LastMouseAction = null;
                    }
                    else
                    {
                        Log.InfoFormat("Releasing mouse middle button at point ({0},{1}).", middleDownUpPoint.X, middleDownUpPoint.Y);
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }
                        audioService.PlaySound(Settings.Default.MouseUpSoundFile, Settings.Default.MouseUpSoundVolume);
                        mouseOutputService.MiddleButtonUp();
                        reinstateModifiers();
                        lastMouseActionStateManager.LastMouseAction = null;
                    }
                    break;

                case FunctionKeys.FocusAtPoint:
                    Log.Info("Mouse move and left click selected.");
                    // FIXME reinstate LookToScroll handler resumeLookToScroll = leftJoystickInteractionHandler.SuspendLookToScrollWhileChoosingPointForMouse();
                    SetupFinalClickAction(finalPoint =>
                    {
                        if (finalPoint != null)
                        {
                            Action<Point> setFocus = fp =>
                            {
                                
                                Log.InfoFormat("Performing mouse left click at point ({0},{1}).", fp.X, fp.Y);
                                Action reinstateModifiers = () => { };
                                if (keyStateService.SimulateKeyStrokes
                                    && Settings.Default.SuppressModifierKeysForAllMouseActions)
                                {
                                    reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                                }
                                audioService.PlaySound(Settings.Default.MouseClickSoundFile, Settings.Default.MouseClickSoundVolume);

                                TryGrabFocusAtPoint(fp);

                                reinstateModifiers();
                            };
                            lastMouseActionStateManager.LastMouseAction = () => setFocus(finalPoint.Value);
                            ShowCursor = false; //Hide cursor popup before performing action as it is possible for it to be performed on the popup
                            setFocus(finalPoint.Value);
                        }

                        ResetAndCleanupAfterMouseAction();
                        // FIXME reinstate LookToScroll handler resumeLookToScroll();
                    });
                    break;

                case FunctionKeys.MouseMoveAndLeftClick:
                    Log.Info("Mouse move and left click selected.");
                    // FIXME reinstate LookToScroll handler resumeLookToScroll = leftJoystickInteractionHandler.SuspendLookToScrollWhileChoosingPointForMouse();
                    SetupFinalClickAction(finalPoint =>
                    {
                        if (finalPoint != null)
                        {
                            Action<Point> simulateClick = fp =>
                            {
                                Log.InfoFormat("Performing mouse left click at point ({0},{1}).", fp.X, fp.Y);
                                Action reinstateModifiers = () => { };
                                if (keyStateService.SimulateKeyStrokes
                                    && Settings.Default.SuppressModifierKeysForAllMouseActions)
                                {
                                    reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                                }
                                audioService.PlaySound(Settings.Default.MouseClickSoundFile, Settings.Default.MouseClickSoundVolume);
                                mouseOutputService.MoveAndLeftClick(fp, true);
                                reinstateModifiers();
                            };
                            lastMouseActionStateManager.LastMouseAction = () => simulateClick(finalPoint.Value);
                            ShowCursor = false; //Hide cursor popup before performing action as it is possible for it to be performed on the popup
                            simulateClick(finalPoint.Value);
                        }

                        ResetAndCleanupAfterMouseAction();
                        // FIXME reinstate LookToScroll handler resumeLookToScroll();
                    });
                    break;

                case FunctionKeys.MouseMoveAndLeftDoubleClick:
                    Log.Info("Mouse move and left double click selected.");
                    // FIXME reinstate LookToScroll handler resumeLookToScroll = leftJoystickInteractionHandler.SuspendLookToScrollWhileChoosingPointForMouse();
                    SetupFinalClickAction(finalPoint =>
                    {
                        if (finalPoint != null)
                        {
                            Action<Point> simulateClick = fp =>
                            {
                                Log.InfoFormat("Performing mouse left double click at point ({0},{1}).", fp.X, fp.Y);
                                Action reinstateModifiers = () => { };
                                if (keyStateService.SimulateKeyStrokes
                                    && Settings.Default.SuppressModifierKeysForAllMouseActions)
                                {
                                    reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                                }
                                audioService.PlaySound(Settings.Default.MouseDoubleClickSoundFile, Settings.Default.MouseDoubleClickSoundVolume);
                                mouseOutputService.MoveAndLeftDoubleClick(fp, true);
                                reinstateModifiers();
                            };
                            lastMouseActionStateManager.LastMouseAction = () => simulateClick(finalPoint.Value);
                            ShowCursor = false; //Hide cursor popup before performing action as it is possible for it to be performed on the popup
                            simulateClick(finalPoint.Value);
                        }

                        ResetAndCleanupAfterMouseAction();
                        // FIXME reinstate LookToScroll handler resumeLookToScroll();
                    });
                    break;

                case FunctionKeys.MouseMoveAndMiddleClick:
                    Log.Info("Mouse move and middle click selected.");
                    // FIXME reinstate LookToScroll handler resumeLookToScroll = leftJoystickInteractionHandler.SuspendLookToScrollWhileChoosingPointForMouse();
                    SetupFinalClickAction(finalPoint =>
                    {
                        if (finalPoint != null)
                        {
                            Action<Point> simulateClick = fp =>
                            {
                                Log.InfoFormat("Performing mouse middle click at point ({0},{1}).", fp.X, fp.Y);
                                Action reinstateModifiers = () => { };
                                if (keyStateService.SimulateKeyStrokes
                                    && Settings.Default.SuppressModifierKeysForAllMouseActions)
                                {
                                    reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                                }
                                audioService.PlaySound(Settings.Default.MouseClickSoundFile, Settings.Default.MouseClickSoundVolume);
                                mouseOutputService.MoveAndMiddleClick(fp, true);
                                reinstateModifiers();
                            };
                            lastMouseActionStateManager.LastMouseAction = () => simulateClick(finalPoint.Value);
                            ShowCursor = false; //Hide cursor popup before performing action as it is possible for it to be performed on the popup
                            simulateClick(finalPoint.Value);
                        }

                        ResetAndCleanupAfterMouseAction();
                        // FIXME reinstate LookToScroll handler resumeLookToScroll();
                    });
                    break;

                case FunctionKeys.MouseMoveAndRightClick:
                    Log.Info("Mouse move and right click selected.");
                    // FIXME reinstate LookToScroll handler resumeLookToScroll = leftJoystickInteractionHandler.SuspendLookToScrollWhileChoosingPointForMouse();
                    SetupFinalClickAction(finalPoint =>
                    {
                        if (finalPoint != null)
                        {
                            Action<Point> simulateClick = fp =>
                            {
                                Log.InfoFormat("Performing mouse right click at point ({0},{1}).", fp.X, fp.Y);
                                Action reinstateModifiers = () => { };
                                if (keyStateService.SimulateKeyStrokes
                                    && Settings.Default.SuppressModifierKeysForAllMouseActions)
                                {
                                    reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                                }
                                audioService.PlaySound(Settings.Default.MouseClickSoundFile, Settings.Default.MouseClickSoundVolume);
                                mouseOutputService.MoveAndRightClick(fp, true);
                                reinstateModifiers();
                            };
                            lastMouseActionStateManager.LastMouseAction = () => simulateClick(finalPoint.Value);
                            ShowCursor = false; //Hide cursor popup before performing action as it is possible for it to be performed on the popup
                            simulateClick(finalPoint.Value);
                        }

                        ResetAndCleanupAfterMouseAction();
                        // FIXME reinstate LookToScroll handler resumeLookToScroll();
                    });
                    break;

                case FunctionKeys.MouseMoveAmountInPixels:
                    Log.Info("Progressing MouseMoveAmountInPixels.");
                    switch (Settings.Default.MouseMoveAmountInPixels)
                    {
                        case 1:
                            Settings.Default.MouseMoveAmountInPixels = 5;
                            break;

                        case 5:
                            Settings.Default.MouseMoveAmountInPixels = 10;
                            break;

                        case 10:
                            Settings.Default.MouseMoveAmountInPixels = 25;
                            break;

                        case 25:
                            Settings.Default.MouseMoveAmountInPixels = 50;
                            break;

                        case 50:
                            Settings.Default.MouseMoveAmountInPixels = 100;
                            break;

                        default:
                            Settings.Default.MouseMoveAmountInPixels = 1;
                            break;
                    }
                    break;

                case FunctionKeys.MouseMoveAndScrollToBottom:
                    Log.Info("Mouse move and scroll to bottom selected.");
                    //FIXME: more
                    // FIXME reinstate LookToScroll handler resumeLookToScroll = leftJoystickInteractionHandler.SuspendLookToScrollWhileChoosingPointForMouse();
                    SetupFinalClickAction(finalPoint =>
                    {
                        if (finalPoint != null)
                        {
                            Action<Point> simulateScrollToBottom = fp =>
                            {
                                Log.InfoFormat("Performing mouse scroll to bottom at point ({0},{1}).", fp.X, fp.Y);
                                Action reinstateModifiers = () => { };
                                if (keyStateService.SimulateKeyStrokes
                                    && Settings.Default.SuppressModifierKeysForAllMouseActions)
                                {
                                    reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                                }
                                audioService.PlaySound(Settings.Default.MouseScrollSoundFile, Settings.Default.MouseScrollSoundVolume);
                                mouseOutputService.MoveAndScrollWheelDown(fp, Settings.Default.MouseScrollAmountInClicks, true);
                                reinstateModifiers();
                            };
                            lastMouseActionStateManager.LastMouseAction = () => simulateScrollToBottom(finalPoint.Value);
                            ShowCursor = false; //Hide cursor popup before performing action as it is possible for it to be performed on the popup
                            simulateScrollToBottom(finalPoint.Value);
                        }

                        ResetAndCleanupAfterMouseAction();
                        //FIXME reinstate resumeLookToScroll();
                    }, suppressMagnification: true);
                    break;

                case FunctionKeys.MouseMoveAndScrollToLeft:
                    Log.Info("Mouse move and scroll to left selected.");
                    //fixme
                    // FIXME reinstate LookToScroll handler resumeLookToScroll = leftJoystickInteractionHandler.SuspendLookToScrollWhileChoosingPointForMouse();
                    SetupFinalClickAction(finalPoint =>
                    {
                        if (finalPoint != null)
                        {
                            Action<Point> simulateScrollToLeft = fp =>
                            {
                                Log.InfoFormat("Performing mouse scroll to left at point ({0},{1}).", fp.X, fp.Y);
                                Action reinstateModifiers = () => { };
                                if (keyStateService.SimulateKeyStrokes
                                    && Settings.Default.SuppressModifierKeysForAllMouseActions)
                                {
                                    reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                                }
                                audioService.PlaySound(Settings.Default.MouseScrollSoundFile, Settings.Default.MouseScrollSoundVolume);
                                mouseOutputService.MoveAndScrollWheelLeft(fp, Settings.Default.MouseScrollAmountInClicks, true);
                                reinstateModifiers();
                            };
                            lastMouseActionStateManager.LastMouseAction = () => simulateScrollToLeft(finalPoint.Value);
                            ShowCursor = false; //Hide cursor popup before performing action as it is possible for it to be performed on the popup
                            simulateScrollToLeft(finalPoint.Value);
                        }

                        ResetAndCleanupAfterMouseAction();
                        // FIXME reinstate LookToScroll handler resumeLookToScroll();
                    }, suppressMagnification: true);
                    break;

                case FunctionKeys.MouseMoveAndScrollToRight:
                    Log.Info("Mouse move and scroll to right selected.");
                    // FIXME reinstate LookToScroll handler resumeLookToScroll = leftJoystickInteractionHandler.SuspendLookToScrollWhileChoosingPointForMouse();
                    SetupFinalClickAction(finalPoint =>
                    {
                        if (finalPoint != null)
                        {
                            Action<Point> simulateScrollToRight = fp =>
                            {
                                Log.InfoFormat("Performing mouse scroll to right at point ({0},{1}).", fp.X, fp.Y);
                                Action reinstateModifiers = () => { };
                                if (keyStateService.SimulateKeyStrokes
                                    && Settings.Default.SuppressModifierKeysForAllMouseActions)
                                {
                                    reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                                }
                                audioService.PlaySound(Settings.Default.MouseScrollSoundFile, Settings.Default.MouseScrollSoundVolume);
                                mouseOutputService.MoveAndScrollWheelRight(fp, Settings.Default.MouseScrollAmountInClicks, true);
                                reinstateModifiers();
                            };
                            lastMouseActionStateManager.LastMouseAction = () => simulateScrollToRight(finalPoint.Value);
                            ShowCursor = false; //Hide cursor popup before performing action as it is possible for it to be performed on the popup
                            simulateScrollToRight(finalPoint.Value);
                        }

                        ResetAndCleanupAfterMouseAction();
                        // FIXME reinstate LookToScroll handler resumeLookToScroll();
                    }, suppressMagnification: true);
                    break;

                case FunctionKeys.MouseMoveAndScrollToTop:
                    Log.Info("Mouse move and scroll to top selected.");
                    // FIXME reinstate LookToScroll handler resumeLookToScroll = leftJoystickInteractionHandler.SuspendLookToScrollWhileChoosingPointForMouse();
                    SetupFinalClickAction(finalPoint =>
                    {
                        if (finalPoint != null)
                        {
                            Action<Point> simulateScrollToTop = fp =>
                            {
                                Log.InfoFormat("Performing mouse scroll to top at point ({0},{1}).", fp.X, fp.Y);
                                Action reinstateModifiers = () => { };
                                if (keyStateService.SimulateKeyStrokes
                                    && Settings.Default.SuppressModifierKeysForAllMouseActions)
                                {
                                    reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                                }
                                audioService.PlaySound(Settings.Default.MouseScrollSoundFile, Settings.Default.MouseScrollSoundVolume);
                                mouseOutputService.MoveAndScrollWheelUp(fp, Settings.Default.MouseScrollAmountInClicks, true);
                                reinstateModifiers();
                            };
                            lastMouseActionStateManager.LastMouseAction = () => simulateScrollToTop(finalPoint.Value);
                            ShowCursor = false; //Hide cursor popup before performing action as it is possible for it to be performed on the popup
                            simulateScrollToTop(finalPoint.Value);
                        }

                        ResetAndCleanupAfterMouseAction();
                        //FIXME reinstate resumeLookToScroll();
                    }, suppressMagnification: true);
                    break;

                case FunctionKeys.MouseScrollToTop:

                    var currentPoint = mouseOutputService.GetCursorPosition();
                    Log.InfoFormat("Mouse scroll to top selected at point ({0},{1}).", currentPoint.X, currentPoint.Y);
                    Action<Point?> performScroll = point =>
                    {
                        if (point != null)
                        {
                            Action<Point> simulateScrollToTop = fp =>
                            {
                                Log.InfoFormat("Performing mouse scroll to top at point ({0},{1}).", fp.X, fp.Y);
                                audioService.PlaySound(Settings.Default.MouseScrollSoundFile, Settings.Default.MouseScrollSoundVolume);
                                mouseOutputService.MoveAndScrollWheelUp(fp, Settings.Default.MouseScrollAmountInClicks, true);
                            };
                            lastMouseActionStateManager.LastMouseAction = () => simulateScrollToTop(point.Value);
                            simulateScrollToTop(point.Value);
                        }
                    };
                    performScroll(currentPoint);
                    ResetAndCleanupAfterMouseAction();

                    break;

                case FunctionKeys.MouseScrollToBottom:

                    var currentPointScroll = mouseOutputService.GetCursorPosition();
                    Log.InfoFormat("Mouse scroll to top selected at point ({0},{1}).", currentPointScroll.X, currentPointScroll.Y);
                    Action<Point?> performScrollDown = point =>
                    {
                        if (point != null)
                        {
                            Action<Point> simulateScrollToBottom = fp =>
                            {
                                Log.InfoFormat("Performing mouse scroll to top at point ({0},{1}).", fp.X, fp.Y);
                                audioService.PlaySound(Settings.Default.MouseScrollSoundFile, Settings.Default.MouseScrollSoundVolume);
                                mouseOutputService.MoveAndScrollWheelDown(fp, Settings.Default.MouseScrollAmountInClicks, true);
                            };
                            lastMouseActionStateManager.LastMouseAction = () => simulateScrollToBottom(point.Value);
                            simulateScrollToBottom(point.Value);
                        }
                    };
                    performScrollDown(currentPointScroll);
                    ResetAndCleanupAfterMouseAction();

                    break;

                case FunctionKeys.MouseMoveTo:
                    Log.Info("Mouse move to selected.");
                    // FIXME reinstate LookToScroll handler resumeLookToScroll = leftJoystickInteractionHandler.SuspendLookToScrollWhileChoosingPointForMouse();
                    SetupFinalClickAction(finalPoint =>
                    {
                        if (finalPoint != null)
                        {
                            Action<Point> simulateMoveTo = fp =>
                            {
                                Log.InfoFormat("Performing mouse move to point ({0},{1}).", fp.X, fp.Y);
                                Action reinstateModifiers = () => { };
                                if (keyStateService.SimulateKeyStrokes
                                    && Settings.Default.SuppressModifierKeysForAllMouseActions)
                                {
                                    reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                                }
                                mouseOutputService.MoveTo(fp);
                                reinstateModifiers();
                            };
                            lastMouseActionStateManager.LastMouseAction = () => simulateMoveTo(finalPoint.Value);
                            simulateMoveTo(finalPoint.Value);
                        }
                        ResetAndCleanupAfterMouseAction();
                        // FIXME reinstate LookToScroll handler resumeLookToScroll();
                    });
                    break;

                case FunctionKeys.MouseMoveToBottom:
                    Log.Info("Mouse move to bottom selected.");
                    Action simulateMoveToBottom = () =>
                    {
                        var cursorPosition = mouseOutputService.GetCursorPosition();
                        var moveToPoint = new Point(cursorPosition.X, cursorPosition.Y + Settings.Default.MouseMoveAmountInPixels);
                        Log.InfoFormat("Performing mouse move to point ({0},{1}).", moveToPoint.X, moveToPoint.Y);
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }
                        mouseOutputService.MoveTo(moveToPoint);
                        reinstateModifiers();
                    };
                    lastMouseActionStateManager.LastMouseAction = simulateMoveToBottom;
                    simulateMoveToBottom();
                    break;

                case FunctionKeys.MouseMoveToLeft:
                    Log.Info("Mouse move to left selected.");
                    Action simulateMoveToLeft = () =>
                    {
                        var cursorPosition = mouseOutputService.GetCursorPosition();
                        var moveToPoint = new Point(cursorPosition.X - Settings.Default.MouseMoveAmountInPixels, cursorPosition.Y);
                        Log.InfoFormat("Performing mouse move to point ({0},{1}).", moveToPoint.X, moveToPoint.Y);
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }
                        mouseOutputService.MoveTo(moveToPoint);
                        reinstateModifiers();
                    };
                    lastMouseActionStateManager.LastMouseAction = simulateMoveToLeft;
                    simulateMoveToLeft();
                    break;

                case FunctionKeys.MouseMoveToRight:
                    Log.Info("Mouse move to right selected.");
                    Action simulateMoveToRight = () =>
                    {
                        var cursorPosition = mouseOutputService.GetCursorPosition();
                        var moveToPoint = new Point(cursorPosition.X + Settings.Default.MouseMoveAmountInPixels, cursorPosition.Y);
                        Log.InfoFormat("Performing mouse move to point ({0},{1}).", moveToPoint.X, moveToPoint.Y);
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }
                        mouseOutputService.MoveTo(moveToPoint);
                        reinstateModifiers();
                    };
                    lastMouseActionStateManager.LastMouseAction = simulateMoveToRight;
                    simulateMoveToRight();
                    break;

                case FunctionKeys.MouseMoveToTop:
                    Log.Info("Mouse move to top selected.");
                    Action simulateMoveToTop = () =>
                    {
                        var cursorPosition = mouseOutputService.GetCursorPosition();
                        var moveToPoint = new Point(cursorPosition.X, cursorPosition.Y - Settings.Default.MouseMoveAmountInPixels);
                        Log.InfoFormat("Performing mouse move to point ({0},{1}).", moveToPoint.X, moveToPoint.Y);
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }
                        mouseOutputService.MoveTo(moveToPoint);
                        reinstateModifiers();
                    };
                    lastMouseActionStateManager.LastMouseAction = simulateMoveToTop;
                    simulateMoveToTop();
                    break;

                case FunctionKeys.MouseRightClick:
                    Log.InfoFormat("Mouse right click selected at current point");
                    Action performRightClick = () =>
                    {
                        Log.InfoFormat("Performing mouse right click at current point");
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }

                        audioService.PlaySound(Settings.Default.MouseClickSoundFile, Settings.Default.MouseClickSoundVolume);
                        mouseOutputService.RightButtonClick();
                        reinstateModifiers();
                    };
                    lastMouseActionStateManager.LastMouseAction = () => performRightClick();
                    performRightClick();
                    break;

                case FunctionKeys.MouseRightDownUp:
                    var rightDownUpPoint = mouseOutputService.GetCursorPosition();
                    if (keyStateService.KeyDownStates[KeyValues.MouseRightDownUpKey].Value.IsDownOrLockedDown())
                    {
                        Log.InfoFormat("Pressing mouse right button down at point ({0},{1}).", rightDownUpPoint.X, rightDownUpPoint.Y);
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }
                        audioService.PlaySound(Settings.Default.MouseDownSoundFile, Settings.Default.MouseDownSoundVolume);
                        mouseOutputService.RightButtonDown();
                        reinstateModifiers();
                        lastMouseActionStateManager.LastMouseAction = null;
                    }
                    else
                    {
                        Log.InfoFormat("Releasing mouse right button at point ({0},{1}).", rightDownUpPoint.X, rightDownUpPoint.Y);
                        Action reinstateModifiers = () => { };
                        if (keyStateService.SimulateKeyStrokes
                            && Settings.Default.SuppressModifierKeysForAllMouseActions)
                        {
                            reinstateModifiers = keyStateService.ReleaseModifiers(Log);
                        }
                        audioService.PlaySound(Settings.Default.MouseUpSoundFile, Settings.Default.MouseUpSoundVolume);
                        mouseOutputService.RightButtonUp();
                        reinstateModifiers();
                        lastMouseActionStateManager.LastMouseAction = null;
                    }
                    break;

                case FunctionKeys.MoveAndResizeAdjustmentAmount:
                    Log.Info("Progressing MoveAndResizeAdjustmentAmount.");
                    switch (Settings.Default.MoveAndResizeAdjustmentAmountInPixels)
                    {
                        case 1:
                            Settings.Default.MoveAndResizeAdjustmentAmountInPixels = 5;
                            break;

                        case 5:
                            Settings.Default.MoveAndResizeAdjustmentAmountInPixels = 10;
                            break;

                        case 10:
                            Settings.Default.MoveAndResizeAdjustmentAmountInPixels = 25;
                            break;

                        case 25:
                            Settings.Default.MoveAndResizeAdjustmentAmountInPixels = 50;
                            break;

                        case 50:
                            Settings.Default.MoveAndResizeAdjustmentAmountInPixels = 100;
                            break;

                        default:
                            Settings.Default.MoveAndResizeAdjustmentAmountInPixels = 1;
                            break;
                    }
                    break;

                case FunctionKeys.MouseScrollAmountInClicks:
                    Log.Info("Progressing MouseScrollAmountInClicks.");
                    switch (Settings.Default.MouseScrollAmountInClicks)
                    {
                        case 1:
                            Settings.Default.MouseScrollAmountInClicks = 3;
                            break;

                        case 3:
                            Settings.Default.MouseScrollAmountInClicks = 5;
                            break;

                        case 5:
                            Settings.Default.MouseScrollAmountInClicks = 10;
                            break;

                        case 10:
                            Settings.Default.MouseScrollAmountInClicks = 25;
                            break;

                        default:
                            Settings.Default.MouseScrollAmountInClicks = 1;
                            break;
                    }
                    break;

                case FunctionKeys.MoveToBottom:
                    Log.InfoFormat("Moving to bottom by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Move(MoveToDirections.Bottom, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.MoveToBottomAndLeft:
                    Log.InfoFormat("Moving to bottom and left by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Move(MoveToDirections.BottomLeft, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.MoveToBottomAndLeftBoundaries:
                    Log.Info("Moving to bottom and left boundaries.");
                    mainWindowManipulationService.Move(MoveToDirections.BottomLeft, null);
                    break;

                case FunctionKeys.MoveToBottomAndRight:
                    Log.InfoFormat("Moving to bottom and right by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Move(MoveToDirections.BottomRight, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.MoveToBottomAndRightBoundaries:
                    Log.Info("Moving to bottom and right boundaries.");
                    mainWindowManipulationService.Move(MoveToDirections.BottomRight, null);
                    break;

                case FunctionKeys.MoveToBottomBoundary:
                    Log.Info("Moving to bottom boundary.");
                    mainWindowManipulationService.Move(MoveToDirections.Bottom, null);
                    break;

                case FunctionKeys.MoveToLeft:
                    Log.InfoFormat("Moving to left by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Move(MoveToDirections.Left, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.MoveToLeftBoundary:
                    Log.Info("Moving to left boundary.");
                    mainWindowManipulationService.Move(MoveToDirections.Left, null);
                    break;

                case FunctionKeys.MoveToRight:
                    Log.InfoFormat("Moving to right by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Move(MoveToDirections.Right, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.MoveToRightBoundary:
                    Log.Info("Moving to right boundary.");
                    mainWindowManipulationService.Move(MoveToDirections.Right, null);
                    break;

                case FunctionKeys.MoveToTop:
                    Log.InfoFormat("Moving to top by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Move(MoveToDirections.Top, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.MoveToTopAndLeft:
                    Log.InfoFormat("Moving to top and left by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Move(MoveToDirections.TopLeft, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.MoveToTopAndLeftBoundaries:
                    Log.Info("Moving to top and left boundaries.");
                    mainWindowManipulationService.Move(MoveToDirections.TopLeft, null);
                    break;

                case FunctionKeys.MoveToTopAndRight:
                    Log.InfoFormat("Moving to top and right by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Move(MoveToDirections.TopRight, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.MoveToTopAndRightBoundaries:
                    Log.Info("Moving to top and right boundaries.");
                    mainWindowManipulationService.Move(MoveToDirections.TopRight, null);
                    break;

                case FunctionKeys.MoveToTopBoundary:
                    Log.Info("Moving to top boundary.");
                    mainWindowManipulationService.Move(MoveToDirections.Top, null);
                    break;

                case FunctionKeys.NextSuggestions:
                    Log.Info("Incrementing suggestions page.");

                    if (suggestionService.Suggestions != null
                        && (suggestionService.Suggestions.Count > (suggestionService.SuggestionsPage + 1) * SuggestionService.SuggestionsPerPage))
                    {
                        suggestionService.SuggestionsPage++;
                    }
                    break;

                case FunctionKeys.NoQuestionResult:
                    HandleYesNoQuestionResult(false);
                    break;

                case FunctionKeys.NumericAndSymbols1Keyboard:
                    Log.Info("Changing keyboard to NumericAndSymbols1.");

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    Keyboard = new NumericAndSymbols1();

                    mainWindowManipulationService.LogTimeAfterLoading(sw);
                    break;

                case FunctionKeys.NumericAndSymbols2Keyboard:
                    Log.Info("Changing keyboard to NumericAndSymbols2.");
                    Keyboard = new NumericAndSymbols2();
                    break;

                case FunctionKeys.NumericAndSymbols3Keyboard:
                    Log.Info("Changing keyboard to Symbols3.");
                    Keyboard = new NumericAndSymbols3();
                    break;

                case FunctionKeys.PersianIran:
                    SelectLanguage(Languages.PersianIran);
                    break;

                case FunctionKeys.PhysicalKeysKeyboard:
                    Log.Info("Changing keyboard to PhysicalKeys.");
                    Keyboard = new PhysicalKeys();
                    break;

                case FunctionKeys.PolishPoland:
                    SelectLanguage(Languages.PolishPoland);
                    break;

                case FunctionKeys.PortuguesePortugal:
                    SelectLanguage(Languages.PortuguesePortugal);
                    break;

                case FunctionKeys.PreviousSuggestions:
                    Log.Info("Decrementing suggestions page.");

                    if (suggestionService.SuggestionsPage > 0)
                    {
                        suggestionService.SuggestionsPage--;
                    }
                    break;

                case FunctionKeys.Quit:
                    Log.Info("Quit key selected.");
                    var keyboardBeforeQuit = Keyboard;
                    Keyboard = new YesNoQuestion(Resources.QUIT_MESSAGE,
                        () =>
                        {
                            Keyboard = new YesNoQuestion(Resources.QUIT_CONFIRMATION_MESSAGE,
                                () => Application.Current.Shutdown(),
                                () => { Keyboard = keyboardBeforeQuit; });
                        },
                        () => { Keyboard = keyboardBeforeQuit; });
                    break;

                case FunctionKeys.RepeatLastMouseAction:
                    if (lastMouseActionStateManager.LastMouseAction != null)
                    {
                        lastMouseActionStateManager.LastMouseAction();
                    }
                    break;

                case FunctionKeys.Restart:
                    keyboardBeforeQuit = Keyboard;
                    Keyboard = new YesNoQuestion(Resources.REFRESH_MESSAGE,
                        () =>
                        {
                            Settings.Default.CleanShutdown = true;
                            OptiKeyApp.RestartApp();
                            Application.Current.Shutdown();
                        },
                        () => { Keyboard = keyboardBeforeQuit; });
                    break;

                case FunctionKeys.RussianRussia:
                    SelectLanguage(Languages.RussianRussia);
                    break;

                case FunctionKeys.ShrinkFromBottom:
                    Log.InfoFormat("Shrinking from bottom by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Shrink(ShrinkFromDirections.Bottom, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ShrinkFromBottomAndLeft:
                    Log.InfoFormat("Shrinking from bottom and left by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Shrink(ShrinkFromDirections.BottomLeft, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ShrinkFromBottomAndRight:
                    Log.InfoFormat("Shrinking from bottom and right by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Shrink(ShrinkFromDirections.BottomRight, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ShrinkFromLeft:
                    Log.InfoFormat("Shrinking from left by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Shrink(ShrinkFromDirections.Left, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ShrinkFromRight:
                    Log.InfoFormat("Shrinking from right by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Shrink(ShrinkFromDirections.Right, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ShrinkFromTop:
                    Log.InfoFormat("Shrinking from top by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Shrink(ShrinkFromDirections.Top, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ShrinkFromTopAndLeft:
                    Log.InfoFormat("Shrinking from top and left by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Shrink(ShrinkFromDirections.TopLeft, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.ShrinkFromTopAndRight:
                    Log.InfoFormat("Shrinking from top and right by {0}px.", Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    mainWindowManipulationService.Shrink(ShrinkFromDirections.TopRight, Settings.Default.MoveAndResizeAdjustmentAmountInPixels);
                    break;

                case FunctionKeys.SizeAndPositionKeyboard:
                    Log.Info("Changing keyboard to Size & Position.");
                    Keyboard = new SizeAndPosition(() => Keyboard = currentKeyboard);
                    break;

                case FunctionKeys.SlovakSlovakia:
                    SelectLanguage(Languages.SlovakSlovakia);
                    break;

                case FunctionKeys.SlovenianSlovenia:
                    SelectLanguage(Languages.SlovenianSlovenia);
                    break;

                case FunctionKeys.SpanishSpain:
                    SelectLanguage(Languages.SpanishSpain);
                    break;

                case FunctionKeys.Speak:
                    var speechStarted = audioService.SpeakNewOrInterruptCurrentSpeech(
                        keyboardOutputService.Text,
                        () => { KeyStateService.KeyDownStates[KeyValues.SpeakKey].Value = KeyDownStates.Up; },
                        Settings.Default.SpeechVolume,
                        Settings.Default.SpeechRate,
                        Settings.Default.SpeechVoice);
                    KeyStateService.KeyDownStates[KeyValues.SpeakKey].Value = speechStarted ? KeyDownStates.Down : KeyDownStates.Up;
                    break;

                case FunctionKeys.Translation:
                    {
                        Log.Info("Translating text from scratchpad.");
                        string textFromScratchpad = KeyboardOutputService.Text;

                        if (!string.IsNullOrEmpty(textFromScratchpad))
                        { 
                            TranslationService.Response response = await translationService.Translate(textFromScratchpad);
                            if (response.Status == "Error")
                            {
                                Log.Error($"Error/exception during translation: {response.ExceptionMessage}");
                                audioService.PlaySound(Settings.Default.ErrorSoundFile, Settings.Default.ErrorSoundVolume);
                                RaiseToastNotification(Resources.ERROR_DURING_TRANSLATION, response.ExceptionMessage, NotificationTypes.Error, () =>
                                {
                                     inputService.RequestResume();
                                });
                            }
                            else
                            {
                                keyboardOutputService.ProcessFunctionKey(FunctionKeys.ClearScratchpad);
                                keyboardOutputService.Text = response.TranslatedText;
                                Clipboard.SetText(response.TranslatedText);
                                audioService.PlaySound(Settings.Default.InfoSoundFile, Settings.Default.InfoSoundVolume);
                            }
                        }
                    }
                    break;

                case FunctionKeys.TurkishTurkey:
                    SelectLanguage(Languages.TurkishTurkey);
                    break;

                case FunctionKeys.UkrainianUkraine:
                    SelectLanguage(Languages.UkrainianUkraine);
                    break;

                case FunctionKeys.UrduPakistan:
                    SelectLanguage(Languages.UrduPakistan);
                    break;

                case FunctionKeys.WebBrowsingKeyboard:
                    Log.Info("Changing keyboard to WebBrowsing.");
                    Keyboard = new WebBrowsing();
                    break;

                case FunctionKeys.YesQuestionResult:
                    HandleYesNoQuestionResult(true);
                    break;

                case FunctionKeys.ResetJoystick:
                    // If there's already a joystick key down, this will re-trigger it, 
                    // otherwise the ResetJoystickKey state will just be queried next time.
                    this.ResetCurrentJoystick();
                    break;

                case FunctionKeys.JoystickXSensitivityDown:
                {
                    this.UpdateJoystickSensitivity(Axes.AxisX, Settings.Default.StickSensitivityAdjustmentAmount);
                    break;
                }

                case FunctionKeys.JoystickXSensitivityUp:
                {
                    this.UpdateJoystickSensitivity(Axes.AxisX, 1.0/Settings.Default.StickSensitivityAdjustmentAmount);
                    break;
                }

                case FunctionKeys.JoystickYSensitivityDown:
                {
                    this.UpdateJoystickSensitivity(Axes.AxisY, Settings.Default.StickSensitivityAdjustmentAmount);
                    break;
                }
                case FunctionKeys.JoystickYSensitivityUp:
                {
                    this.UpdateJoystickSensitivity(Axes.AxisY, 1.0/Settings.Default.StickSensitivityAdjustmentAmount);
                    break;
                }
                case FunctionKeys.CopyJoystickSettings:
                {
                    // Copy for use in keyboards
                    {
                        string msg0 = "Use *one* of these in a dynamic key to replicate the settings you've chosen";
                        string msg1 = $"<Action Payload=\"{Settings.Default.LeftStickSensitivityX}, {Settings.Default.LeftStickSensitivityY}\">LeftJoystick</Action>";
                        string msg2 = $"<Action Payload=\"{Settings.Default.RightStickSensitivityX}, {Settings.Default.RightStickSensitivityY}\">RightJoystick</Action>";
                        string msg3 = $"<Action Payload=\"{Settings.Default.LegacyStickSensitivityX}, {Settings.Default.LegacyStickSensitivityY}\">LegacyJoystick</Action>";
                        string msg4 = $"<Action Payload=\"{Settings.Default.MouseStickSensitivityX}, {Settings.Default.MouseStickSensitivityY}\">MouseJoystick</Action>";
                        string msg5 = $"<Action Payload=\"{Settings.Default.LegacyTriggerStickSensitivityX}, {Settings.Default.LegacyTriggerStickSensitivityY}\">LegacyTriggerJoystick</Action>";
                        string combinedMsg = $"{msg0}\n{msg1}\n{msg2}\n{msg3}\n{msg4}\n{msg5}";

                        Clipboard.SetText(combinedMsg);
                    }
                    // User facing message
                    {
                        string msg1 = $"Left Stick: ({Settings.Default.LeftStickSensitivityX}, {Settings.Default.LeftStickSensitivityY})";
                        string msg2 = $"Right Stick: ({Settings.Default.RightStickSensitivityX}, {Settings.Default.RightStickSensitivityY})";
                        string msg3 = $"Legacy Stick: ({Settings.Default.LegacyStickSensitivityX}, {Settings.Default.LegacyStickSensitivityY})";
                        string msg4 = $"Mouse Stick: ({Settings.Default.MouseStickSensitivityX}, {Settings.Default.MouseStickSensitivityY})";
                        string msg5 = $"Legacy trigger Stick: ({Settings.Default.LegacyTriggerStickSensitivityX}, {Settings.Default.LegacyTriggerStickSensitivityY})";
                        string combinedMsg = $"{msg1}\n{msg2}\n{msg3}\n{msg4}\n{msg5}";

                        RaiseToastNotification("Copied!", "Sensitivities copied to clipboard\n" + combinedMsg, NotificationTypes.Normal, () => { });
                    }
                        
                    // FIXME: Resource
                    
                    break;
                }
                case FunctionKeys.ResetAllJoystickSettings:
                {
                    Log.Info("Resetting all joystick sensitivities to 1.0");

                    Settings.Default.LeftStickSensitivityX = 1.0;
                    Settings.Default.LeftStickSensitivityY = 1.0;

                    Settings.Default.RightStickSensitivityX = 1.0;
                    Settings.Default.RightStickSensitivityY = 1.0;

                    Settings.Default.LegacyStickSensitivityX = 1.0;
                    Settings.Default.LegacyStickSensitivityY = 1.0;

                    Settings.Default.MouseStickSensitivityX = 1.0;
                    Settings.Default.MouseStickSensitivityY = 1.0;
                        
                    break;
                }
                case FunctionKeys.NoJoystick:
                    TurnOffJoysticks();
                    break;
            }

            keyboardOutputService.ProcessFunctionKey(singleKeyValue.FunctionKey.Value);
        }

        public void SetupFinalClickAction(Action<Point?> finalClickAction, bool finalClickInSeries = true, bool suppressMagnification = false)
        {
            nextPointSelectionAction = nextPoint =>
            {
                if (!suppressMagnification
                    && keyStateService.KeyDownStates[KeyValues.MouseMagnifierKey].Value.IsDownOrLockedDown())
                {
                    ShowCursor = false; //Ensure cursor is not showing when MagnifyAtPoint is set because...
                    //1.This triggers a screen capture, which shouldn't have the cursor in it.
                    //2.Last popup open stays on top (I know the VM in MVVM shouldn't care about this, so pretend it's all reason 1).
                    MagnifiedPointSelectionAction = finalClickAction;
                    MagnifyAtPoint = nextPoint;
                    if (MagnifyAtPoint != null) //If the magnification fails then MagnifyAtPoint will be null
                    {
                        ShowCursor = true;
                    }
                }
                else
                {
                    finalClickAction(nextPoint);
                }

                if (finalClickInSeries)
                {
                    nextPointSelectionAction = null;
                }
            };

            SelectionMode = SelectionModes.Point;
            ShowCursor = true;
        }

        public void ResetAndCleanupAfterMouseAction()
        {
            SelectionMode = SelectionModes.Key;
            nextPointSelectionAction = null;
            ShowCursor = false;
            MagnifyAtPoint = null;
            MagnifiedPointSelectionAction = null;

            if (keyStateService.KeyDownStates[KeyValues.MouseMagnifierKey].Value == KeyDownStates.Down)
            {
                keyStateService.KeyDownStates[KeyValues.MouseMagnifierKey].Value = KeyDownStates.Up; //Release magnifier if down but not locked down
            }
        }

        private void HandleServiceError(object sender, Exception exception)
        {
            Log.Error("Error event received from service. Raising ErrorNotificationRequest and playing ErrorSoundFile (from settings)", exception);

            inputService.RequestSuspend();

            if (RaiseToastNotification(Resources.CRASH_TITLE, exception.Message, NotificationTypes.Error, () => inputService.RequestResume()))
            {
                audioService.PlaySound(Settings.Default.ErrorSoundFile, Settings.Default.ErrorSoundVolume);
            }
        }

        private void NavigateToMenu()
        {
            Log.Info("Changing keyboard to Menu.");
            Keyboard = new Menu(CreateBackAction());
        }

        private void NavigateToVoiceKeyboard()
        {
            List<string> voices = GetAvailableVoices();

            if (voices != null && voices.Any())
            {
                Log.Info("Changing keyboard to Voice.");
                Keyboard = new Voice(CreateBackAction(), voices);
            }
            else
            {
                Log.Warn("No voices available. Returning to menu.");
                NavigateToMenu();
            }
        }

        private void SelectLanguage(Languages language)
        {
            Log.Info("Changing keyboard language to " + language);
            InputService.RequestSuspend(); //Reloading the dictionary locks the UI thread, so suspend input service to prevent accidental selections until complete
            Settings.Default.KeyboardAndDictionaryLanguage = language;
            InputService.RequestResume();

            if (Settings.Default.DisplayVoicesWhenChangingKeyboardLanguage)
            {
                NavigateToVoiceKeyboard();
            }
            else
            {
                NavigateToMenu();
            }
        }

        private void SelectVoice(string voice)
        {
            if (Settings.Default.MaryTTSEnabled)
            {
                Log.Info("Changing Mary TTS voice to " + voice);
                Settings.Default.MaryTTSVoice = voice;
            }
            else
            {
                Log.Info("Changing speech voice to " + voice);
                Settings.Default.SpeechVoice = voice;
            }

            NavigateToMenu();
        }

        private async Task CommandKey(KeyValue singleKeyValue, List<string> multiKeySelection)
        {
            if (singleKeyValue.Commands != null && singleKeyValue.Commands.Any())
            {
                Log.InfoFormat("CommandKey called with singleKeyValue: {0}", singleKeyValue.String);

                var commandList = new List<KeyCommand>();
                commandList.AddRange(singleKeyValue.Commands);
                keyStateService.KeyRunningStates[singleKeyValue].Value = true;

                TimeSpanOverrides timeSpanOverrides = null;
                inputService.OverrideTimesByKey?.TryGetValue(singleKeyValue, out timeSpanOverrides);

                //Action the key's command list before setting the key to Down/LockedDown to avoid a scenario where the command changes the keyboard
                //to one which toggles SimulateKeyStrokes, which results in all key states being stored and restored when leaving the keyboard. This
                //incorrectly stores the key Down/LockedDown state, which is restored when we leave the keyboard. This *should* be safe as the command
                //key's value is different from the key values of each individual command.
                
                // FIXME: I've reordered this to fix the locking of loop commands, but the above issue is presumably present

                //if there is an override lock down time then do not set the key to LockedDown
                keyStateService.KeyDownStates[singleKeyValue].Value = timeSpanOverrides != null && timeSpanOverrides.TimeRequiredToLockDown > TimeSpan.Zero
                    ? KeyDownStates.Down : KeyDownStates.LockedDown;

                await CommandList(singleKeyValue, multiKeySelection, commandList, 0);

                //if there is an override lock down time then run this key until the gaze stops or another trigger stops it
                if (timeSpanOverrides != null && timeSpanOverrides.TimeRequiredToLockDown > TimeSpan.Zero)
                {
                    //keep the key running until triggered to stop (key lock down), or focus has been lost (key up)
                    while (keyStateService.KeyRunningStates[singleKeyValue].Value)
                    {
                        await Task.Delay(10);
                        //if the timeout is equal to the min it means the key no longer has focus and has timed out
                        keyStateService.KeyRunningStates[singleKeyValue].Value = (timeSpanOverrides.LockDownCancelTime > DateTimeOffset.MinValue) 
                            ? keyStateService.KeyRunningStates[singleKeyValue].Value : false;
                    }
                    //if the timeout has not been set to the min then we lock down the key and return
                    if (timeSpanOverrides.LockDownCancelTime > DateTimeOffset.MinValue)
                    {
                        keyStateService.KeyDownStates[singleKeyValue].Value = KeyDownStates.LockedDown;
                        return;
                    }
                }

                //if the Task was stopped by an external process and there are any keys
                //that were pressed and not released then execute KeyUp processing
                if (!keyStateService.KeyRunningStates[singleKeyValue].Value)
                {
                    foreach (var keyUpCandidate in keyStateService.KeyFamily.Where(x => x.Item1 == singleKeyValue
                        && keyStateService.KeyDownStates[x.Item2].Value != KeyDownStates.Up))
                    {
                        Log.InfoFormat("CommandKey canceled. Sending key up on [{0}] key", keyUpCandidate.Item2.String);
                        await keyboardOutputService.ProcessSingleKeyPress(keyUpCandidate.Item2.String, KeyPressKeyValue.KeyPressType.Release);
                        keyStateService.KeyDownStates[keyUpCandidate.Item2].Value = KeyDownStates.Up;
                    }
                }
                else
                {
                    keyStateService.KeyRunningStates[singleKeyValue].Value = false;

                    //if the Task left any keys down then return without changing the singleKeyValue to Up
                    var keyUpCandidate = keyStateService.KeyFamily.Find(x => x.Item1 == singleKeyValue
                        && keyStateService.KeyDownStates[x.Item2].Value != KeyDownStates.Up);
                    if (keyUpCandidate != null)
                    {
                        keyStateService.KeyDownStates[singleKeyValue].Value = KeyDownStates.LockedDown;
                        Log.InfoFormat("CommandKey {0} finished without changing state to Up because key [{1}] is down", singleKeyValue.String, keyUpCandidate.Item2.String);
                        return;
                    }
                }
                //if no keys are down then change the CommandKey singleKeyValue to Up
                keyStateService.KeyDownStates[singleKeyValue].Value = KeyDownStates.Up;
            }
        }

        private async Task CommandList(KeyValue singleKeyValue, List<string> multiKeySelection, List<KeyCommand> commandList, int nestLevel)
        {
            Log.InfoFormat("CommandList called with command count: {0}, nest level: {1}", commandList.Count, nestLevel);
            
            foreach(KeyCommand keyCommand in commandList)
            {
                //if an external process has ordered this key to stop then return
                if (!keyStateService.KeyRunningStates[singleKeyValue].Value) 
                    return;

                if (keyCommand.Name == KeyCommands.Loop)
                {
                    var loopCount = Int32.Parse(keyCommand.Value);
                    var logMessage = loopCount > 0 ? loopCount + " times" : "indefinitely until stopped";
                    Log.InfoFormat("CommandList: Looping {0}", logMessage);

                    while (keyStateService.KeyRunningStates[singleKeyValue].Value)
                    {
                        var loopCommandList = new List<KeyCommand>();
                        loopCommandList.AddRange(keyCommand.LoopCommands);

                        //when calling another instance do so with a larger nestLevel
                        await CommandList(singleKeyValue, multiKeySelection, loopCommandList, nestLevel + 1);

                        //we need to throttle if in a perpetual loop with no nested loop and no pre-defined wait 
                        if (loopCount < 1 && !keyCommand.LoopCommands.Exists(x => x.Name == KeyCommands.Loop)
                            && !keyCommand.LoopCommands.Exists(x => x.Name == KeyCommands.Wait))
                        {
                            int waitMs = 500;
                            Log.InfoFormat("Throttling perpetual loop for {0}ms", waitMs);
                            await Task.Delay(waitMs);
                        }

                        //if we completed the final iteration then break out of the loop
                        if (loopCount == 1)
                            break;
                        //only decrement the counter if we are not in perpetual loop
                        if (loopCount > 1)
                            loopCount--;
                    }
                }
                else
                {
                    if (keyCommand.Name == KeyCommands.Action)
                    {
                        Log.InfoFormat("CommandList: Press function key: {0}", keyCommand.KeyValue.FunctionKey.ToString());
                        Log.InfoFormat("CommandList: function key payload: {0}", keyCommand.KeyValue.String);
                        KeySelectionResult(keyCommand.KeyValue, multiKeySelection);
                    }
                    else if (keyCommand.Name == KeyCommands.ChangeKeyboard)
                    {
                        Log.InfoFormat("CommandList: Change keyboard");
                        KeySelectionResult(keyCommand.KeyValue, multiKeySelection);
                    }
                    else if (keyCommand.Name == KeyCommands.KeyDown)
                    {
                        Log.InfoFormat("CommandList: Key down on [{0}] key", keyCommand.KeyValue.String);
                        await keyboardOutputService.ProcessSingleKeyPress(keyCommand.KeyValue.String, KeyPressKeyValue.KeyPressType.Press);
                        keyStateService.KeyDownStates[keyCommand.KeyValue].Value = KeyDownStates.LockedDown;
                    }
                    else if (keyCommand.Name == KeyCommands.KeyToggle || 
                             keyCommand.Name == KeyCommands.KeyTogglePauseOnThisKey ||
                             keyCommand.Name == KeyCommands.KeyTogglePauseOnAnyKey)
                    {

                         // What kind of pausing are we after?
                         bool doPause = false;
                         Func<Point, bool> whenRequiresPausing = null;
                         if (keyCommand.Name == KeyCommands.KeyTogglePauseOnThisKey) {
                             doPause = true;
                             whenRequiresPausing = (point) => this.IsPointInsideKey(point, singleKeyValue);
                         }
                         else if (keyCommand.Name == KeyCommands.KeyTogglePauseOnAnyKey)
                         {
                             doPause = true;
                             whenRequiresPausing = (point) => this.IsPointInsideValidKey(point);
                         }

                        // Key is released
                        if (keyStateService.KeyDownStates[keyCommand.KeyValue].Value != KeyDownStates.Up)
                        {
                            Log.InfoFormat("CommandList: Toggle key up on [{0}] key", keyCommand.KeyValue.String);

                            await KeyUpProcessing(singleKeyValue, keyCommand.KeyValue);
                        }
                        // Key is pressed
                        else
                        {
                            Log.InfoFormat("CommandList: Toggle key down on [{0}] key", keyCommand.KeyValue.String);
                            await keyboardOutputService.ProcessSingleKeyPress(keyCommand.KeyValue.String, KeyPressKeyValue.KeyPressType.Press);
                            keyStateService.KeyDownStates[keyCommand.KeyValue].Value = KeyDownStates.LockedDown;

                            // Subscribe to position stream to allow pausing
                            if (doPause) {
                                if (!perKeyPauseHandlers.ContainsKey(singleKeyValue))
                                {
                                    perKeyPauseHandlers.Add(singleKeyValue, new KeyPauseHandler(
                                        whenRequiresPausing,
                                        new Action(() => { keyboardOutputService.ProcessSingleKeyPress(keyCommand.KeyValue.String, KeyPressKeyValue.KeyPressType.Release); }),
                                        new Action(() => { keyboardOutputService.ProcessSingleKeyPress(keyCommand.KeyValue.String, KeyPressKeyValue.KeyPressType.Press); })
                                        ));
                                }                                
                                perKeyPauseHandlers.GetValueOrDefault(singleKeyValue)?.AttachListener(inputService);                                
                            }
                        }
                    }
                    else if (keyCommand.Name == KeyCommands.KeyUp)
                    {
                        Log.InfoFormat("CommandList: Key up on [{0}]", keyCommand.KeyValue.String);
                        await KeyUpProcessing(singleKeyValue, keyCommand.KeyValue);

                        //the KeyUp value could be a KeyGroup so add any matches from KeyValueByGroup
                        if (keyStateService.KeyValueByGroup.ContainsKey(keyCommand.KeyValue.String.ToUpper()))
                        {
                            var keyValueList = new List<KeyValue>();
                            keyValueList.Add(keyCommand.KeyValue);
                            keyValueList.AddRange(KeyStateService.KeyValueByGroup[keyCommand.KeyValue.String.ToUpper()]);
                            foreach (var keyValue in keyValueList.Where(x => x != null && keyStateService.KeyDownStates[x].Value != KeyDownStates.Up))
                            {
                                await KeyUpProcessing(singleKeyValue, keyValue);
                            }
                        }
                    }                                        
                    else if (keyCommand.Name == KeyCommands.Text)
                    {
                        Log.InfoFormat("CommandList: Text of [{0}]", keyCommand.KeyValue.String);
                        KeySelectionResult(keyCommand.KeyValue, multiKeySelection);
                    }
                    else if (keyCommand.Name == KeyCommands.Wait)
                    {
                        var waitMs = int.Parse(keyCommand.Value);
                        Log.InfoFormat("CommandList: Wait of {0}ms", waitMs);
                        await Task.Delay(waitMs);
                    }
                    else if (keyCommand.Name == KeyCommands.Plugin)
                    {
                        Log.InfoFormat("CommandList: Plugin [{0}]", keyCommand.Value);
                        RunDynamicPlugin(keyCommand.Plugin);
                    }
                }
            }
        }

        private async Task KeyUpProcessing(KeyValue singleKeyValue, KeyValue commandKey)
        {
            // Either singleKeyValue = xml keyvalue, like "R2-C3", commandKey = actual keyValue, like String:"a" or FunctionKey:"Sleep"
            // OR singleKeyValue = xml keyvalue (as above) and commandKey = a child's XML key value

            var inKey = commandKey.FunctionKey.HasValue
                ? commandKey.FunctionKey.Value.ToString() : commandKey.String;
            await keyboardOutputService.ProcessSingleKeyPress(inKey, KeyPressKeyValue.KeyPressType.Release);
            keyStateService.KeyDownStates[commandKey].Value = KeyDownStates.Up;

            // Unsubscribe from position stream (if attached)               
            perKeyPauseHandlers.GetValueOrDefault(singleKeyValue)?.DetachListener(inputService);

            // If the released key has any child commmands then release them as well             
            // For instance, if key is holding down multiple keyboard keys, release them all
            foreach (var keyPair in keyStateService.KeyFamily.Where(x => x.Item1 == commandKey
                && KeyStateService.KeyDownStates[x.Item2].Value != KeyDownStates.Up))
            {
                KeyValue keyValParent = keyPair.Item1; 
                KeyValue keyValChildCommand = keyPair.Item2;  

                inKey = keyValChildCommand.FunctionKey.HasValue
                    ? keyValChildCommand.FunctionKey.Value.ToString() : keyValChildCommand.String;

                await keyboardOutputService.ProcessSingleKeyPress(inKey, KeyPressKeyValue.KeyPressType.Release);
                keyStateService.KeyDownStates[keyValChildCommand].Value = KeyDownStates.Up;
            }
            
            //if the released key has a parent 
            //and the parent is not up
            //and the parent is not running
            //and the parent has no child that is not released
            //then release the parent
            foreach (var keyPair in keyStateService.KeyFamily.Where(x => x.Item2 == commandKey
                && KeyStateService.KeyDownStates[x.Item1].Value != KeyDownStates.Up
                && !KeyStateService.KeyRunningStates[x.Item1].Value
                && !keyStateService.KeyFamily.Exists(y => y.Item1 == x.Item1 && KeyStateService.KeyDownStates[y.Item2].Value != KeyDownStates.Up)))
            {
                KeyValue keyValParent = keyPair.Item1;
                KeyValue keyValChildCommand = keyPair.Item2;  
                
                await keyboardOutputService.ProcessSingleKeyPress(keyValParent.String, KeyPressKeyValue.KeyPressType.Release);

                keyStateService.KeyDownStates[keyValParent].Value = KeyDownStates.Up;
                perKeyPauseHandlers.GetValueOrDefault(keyValParent)?.DetachListener(inputService);
            }

            if (commandKey != singleKeyValue && keyStateService.KeyRunningStates[commandKey].Value != false)
                keyStateService.KeyRunningStates[commandKey].Value = false;
        }

        private void RunDynamicPlugin(DynamicPlugin pluginKey)
        {
            Log.InfoFormat("Running plugin [{0}]", pluginKey.Name);

            // Build plugin context
            Dictionary<string, string> context = BuildPluginContext();
            try
            {
                PluginEngine.RunDynamicPlugin(context, pluginKey);
            }
            catch (Exception exception)
            {
                Log.Error("Error running plugin.", exception);
                while (exception.InnerException != null) exception = exception.InnerException;
                //FIXME: would be nice to have title "Error running plugin" or message like "No plugins loaded"
                if (RaiseToastNotification(Resources.CRASH_TITLE, exception.Message, NotificationTypes.Error, () => inputService.RequestResume()))
                {
                    audioService.PlaySound(Settings.Default.ErrorSoundFile, Settings.Default.ErrorSoundVolume);
                }
            }
        }

        private void RunPlugin_Legacy(string command)
        {
            //FIXME: Log Message is logging entire XML
            Log.InfoFormat("Running plugin [{0}]", command);

            // Build plugin context
            Dictionary<string, string> context = BuildPluginContext();

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(XmlPluginKey));
                StringReader rdr = new StringReader(command);
                PluginEngine.RunPlugin_Legacy(context, (XmlPluginKey)serializer.Deserialize(rdr));
            }
            catch (Exception exception)
            {
                Log.Error("Error running plugin.", exception);
                while (exception.InnerException != null) exception = exception.InnerException;
                if (RaiseToastNotification(Resources.CRASH_TITLE, exception.Message, NotificationTypes.Error, () => inputService.RequestResume()))
                {
                    audioService.PlaySound(Settings.Default.ErrorSoundFile, Settings.Default.ErrorSoundVolume);
                }
            }
        }

        private Dictionary<string, string> BuildPluginContext()
        {
            Dictionary<string, string> context = new Dictionary<string, string>
            {
                { "scratchpadText", keyboardOutputService.Text }
            };
            return context;
        }

        private void ShowMore()
        {
            if (Keyboard is Voice)
            {
                var voiceKeyboard = Keyboard as Voice;

                Log.Info("Moving to next page of voices.");
                Keyboard = new Voice(CreateBackAction(), voiceKeyboard.RemainingVoices);
            }
        }

        private Action CreateBackAction()
        {
            IKeyboard previousKeyboard = Keyboard;
            return () => Keyboard = previousKeyboard;
        }

        private List<string> GetAvailableVoices()
        {
            try
            {
                return Settings.Default.MaryTTSEnabled
                    ? audioService.GetAvailableMaryTTSVoices()
                    : audioService.GetAvailableVoices();
            }
            catch (Exception e)
            {
                Log.Error("Failed to fetch available voices.", e);
                return null;
            }
        }
    }

}
