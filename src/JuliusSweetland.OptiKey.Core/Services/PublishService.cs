// Copyright (c) K McNaught Consulting Ltd (UK company number 11297717) - All Rights Reserved
// based on GPL3 code Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using JuliusSweetland.OptiKey.Enums;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Static;
using JuliusSweetland.OptiKey.Properties;
using log4net;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System.Threading;

namespace JuliusSweetland.OptiKey.Services
{
    public class PublishService : IPublishService
    {
        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly InputSimulator inputSimulator;
        private readonly WindowsInputDeviceStateAdaptor inputDeviceStateAdaptor;

        private ViGEmClient client;
        private IXbox360Controller controller;
        private bool supportsController = false;

        public event EventHandler<Exception> Error;

        public PublishService()
        {
            inputSimulator = new WindowsInput.InputSimulator();
            inputDeviceStateAdaptor = new WindowsInput.WindowsInputDeviceStateAdaptor();

            try
            {
                client = new ViGEmClient();
                controller = client.CreateXbox360Controller();
                controller.Connect();
                supportsController = true;
                Log.Info("Virtual Xbox360 gamepad connected.");
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Exception connecting to ViGem USB controller driver:\n{0}", e);
                supportsController = false;
            }
        }

        public bool SupportsController()
        {
            return supportsController;
        }


        public void ReleaseAllDownKeys()
        {
            try
            {
                Log.InfoFormat("Releasing all keys (with virtual key codes) which are down.");
                foreach (var virtualKeyCode in Enum.GetValues(typeof(VirtualKeyCode)).Cast<VirtualKeyCode>())
                {
                    if (inputDeviceStateAdaptor.IsHardwareKeyDown(virtualKeyCode))
                    {
                        Log.DebugFormat("{0} is down - calling KeyUp", virtualKeyCode);
                        KeyUp(virtualKeyCode);
                    }
                }
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void XBoxProcessJoystick(XboxAxes axisEnum, float amount)
        {
            // amount is in range [-1.0, +1.0] and needs scaling to 
            // (signed) short range
            Xbox360Axis axis = axisEnum.ToViGemAxis();
            amount = Math.Min(1.0f, amount);
            amount = Math.Max(-1.0f, amount);
            controller.SetAxisValue(axis, (short)(Int16.MaxValue * amount));
        }

        public void XBoxButtonDown(XboxButtons button)
        {
            if (!supportsController)
            {
                throw new Exception("No controller set up. \nHave you installed ViGemBus?");
            }
            try
            {
                Log.DebugFormat("Simulating button down {0}", button);
                Xbox360Button xboxButton = button.ToViGemButton();
                if (xboxButton != null)
                {
                    controller.SetButtonState(xboxButton, true);
                    return;
                }

                // Triggers are analogue 'sliders', but we'll treat them as buttons
                Xbox360Slider slider = button.ToViGemSlider();
                double amount = 1.0f;
                if (slider != null)
                {
                    controller.SetSliderValue(slider, (byte)(255 * amount));
                }
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public bool TryXBoxThumbPress(string buttonString, KeyPressKeyValue.KeyPressType pressType)
        {
            if (!supportsController)
            {
                throw new Exception("No controller set up. \nHave you installed ViGemBus?");
            }

            // Examples of valid buttonString :
            // XBoxLeftThumbLeft
            // XBoxRightThumbUp
            // Any combination: XBox{Left/Right}Thumb{Left/Right/Up/Down/Forward/Back/Backward}
            // Half versions: XBox{Left/Right}ThumbHalf{Left/Right/Up/Down/Forward/Back/Backward}
            // 
            // With stick, direction & amplitude separated with |:
            // XBoxLeftThumb | NorthEast | 0.25
            // XBoxRightThumb | South | 1.0
            // XBoxRightThumb | Up | 1.0
            // XBoxRightThumb | 10 | 1.0
            // XBoxRightThumb | Down 
            // XBoxRightThumb|Left
            //  
            // If no amplitude given, will default to 1.0
            // Direction is word, compass direction or degrees clockwise from north (integer)
            // Whitespace and capitalisation is ignored

            buttonString = buttonString.ToLowerInvariant();
            if (!buttonString.Contains("xbox") || !(buttonString.Contains("thumb")))
            {
                return false;
            }

            // Split string into parts
            const char sep = '|';
            string[] parts = buttonString.Split(sep);
            string mainString = parts[0].Trim();
            string directionString = parts.Length > 1 ? parts[1].Trim() : mainString;
            string amountString = parts.Length > 2 ? parts[2].Trim() : null;

            // Extract amplitude
            float amount = 1.0f;
            if (mainString.Contains("half"))
                amount = 0.5f;
            else if (mainString.Contains("neutral"))
                amount = 0.0f;
            else if (amountString != null) 
                float.TryParse(amountString, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);

            // Extract direction from either directionString, (which might be copy of whole of mainString)
            int direction = 0; // Degrees clockwise from north

            if (directionString.EndsWith("northeast"))
                direction = 45;
            else if (directionString.EndsWith("southeast"))
                direction = 135;
            else if (directionString.EndsWith("southwest"))
                direction = 225;
            else if (directionString.EndsWith("northwest"))
                direction = 315;
            else if (directionString.EndsWith("up") ||
                      directionString.EndsWith("forward") ||
                      directionString.EndsWith("north"))
                direction = 0;
            else if (directionString.EndsWith("right") ||
                     directionString.EndsWith("east"))
                direction = 90;
            else if (directionString.EndsWith("down") ||
                     directionString.EndsWith("backward") ||
                     directionString.EndsWith("back") ||
                     directionString.EndsWith("south"))
                direction = 180;
            else if (directionString.EndsWith("left") ||
                     directionString.EndsWith("west"))
                direction = 270;                      
            else {
                bool success = int.TryParse(directionString, NumberStyles.Any, CultureInfo.InvariantCulture, out direction);
                if (!success) { return false; }
            }
        
            
            // Split amount into x, y components
            double dAmountX = (double)amount*Math.Sin((double)direction / 180 * Math.PI);
            double dAmountY = (double)amount *Math.Cos((double)direction / 180 * Math.PI);
            short amountX = (short)(Int16.MaxValue * dAmountX);
            short amountY = (short)(Int16.MaxValue * dAmountY);

            // Press a thumbstick!
            if (mainString.Contains("leftthumb"))
            {
                // TODO: do we *always* want to set an axis, even to value of 0? Would we sometimes want to control
                // axes independently? 
                if (pressType == KeyPressKeyValue.KeyPressType.Press || pressType == KeyPressKeyValue.KeyPressType.PressAndRelease)
                {
                    controller.SetAxisValue(Xbox360Axis.LeftThumbX, amountX);
                    controller.SetAxisValue(Xbox360Axis.LeftThumbY, amountY);
                    return true;
                }
                if (pressType == KeyPressKeyValue.KeyPressType.Release)
                {

                    controller.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
                    controller.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
                    return true;
                }
            }
            else if (mainString.Contains("rightthumb"))
            {
                if (pressType == KeyPressKeyValue.KeyPressType.Press)
                {
                    controller.SetAxisValue(Xbox360Axis.RightThumbX, amountX);
                    controller.SetAxisValue(Xbox360Axis.RightThumbY, amountY);
                    return true;
                }
                if (pressType == KeyPressKeyValue.KeyPressType.Release)
                {
                    controller.SetAxisValue(Xbox360Axis.RightThumbX, 0);
                    controller.SetAxisValue(Xbox360Axis.RightThumbY, 0);
                    return true;
                }
            }
            return false;
        }

        public void XBoxButtonUp(XboxButtons button)
        {
            if (!supportsController)
            {
                throw new Exception("No controller set up. \nHave you installed ViGemBus?");
            }
            try
            {
                Log.DebugFormat("Simulating button up: {0}", button);
                Xbox360Button xboxButton = button.ToViGemButton();
                if (xboxButton != null)
                {
                    controller.SetButtonState(xboxButton, false);
                    return;
                }

                // Triggers are analogue 'sliders', but we'll treat them as buttons
                Xbox360Slider slider = button.ToViGemSlider();
                if (slider != null)
                {
                    controller.SetSliderValue(slider, (byte) (0));
                    return;
                }
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void KeyDown(VirtualKeyCode virtualKeyCode)
        {
            try
            {
                Log.DebugFormat("Simulating key down {0}", virtualKeyCode);
                inputSimulator.Keyboard.KeyDown(virtualKeyCode);
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void KeyUp(VirtualKeyCode virtualKeyCode)
        {
            try
            {
                Log.DebugFormat("Simulating key up: {0}", virtualKeyCode);
                inputSimulator.Keyboard.KeyUp(virtualKeyCode);
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void KeyDownUp(VirtualKeyCode virtualKeyCode)
        {
            try
            {
                Log.DebugFormat("Simulating key press (down & up): {0}", virtualKeyCode);
                inputSimulator.Keyboard.KeyPress(virtualKeyCode);
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void TypeText(string text)
        {
            try
            {
                Log.DebugFormat("Simulating typing text '{0}'", text);
                inputSimulator.Keyboard.TextEntry(text);
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void MouseMoveBy(Point point)
        {
            try
            {
                Log.DebugFormat("Simulating moving mouse by '{0}' pixels", point);
                inputSimulator.Mouse.MoveMouseBy((int)point.X, (int)point.Y);
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void MouseMouseToPoint(Point point)
        {
            try
            {
                Log.DebugFormat("Simulating moving mouse to point '{0}'", point);
                
                //N.B. InputSimulator does not deal in pixels. The position should be a scaled point between 0 and 65535. 
                //https://inputsimulator.codeplex.com/discussions/86530

                inputSimulator.Mouse.MoveMouseTo(
                    Math.Ceiling(65535 * (point.X / Graphics.PrimaryScreenWidthInPixels)),
                    Math.Ceiling(65535 * (point.Y / Graphics.PrimaryScreenHeightInPixels)));
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void LeftMouseButtonClick()
        {
            try
            {
                Log.Info("Simulating clicking the left mouse button click");
                inputSimulator.Mouse.LeftButtonClick();
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void LeftMouseButtonDoubleClick()
        {
            try
            {
                Log.Info("Simulating pressing the left mouse button down twice");
                if (Settings.Default.DoubleClickDelay > TimeSpan.Zero)
                {
                    inputSimulator.Mouse.LeftButtonClick();
                    Thread.Sleep(Settings.Default.DoubleClickDelay);
                    inputSimulator.Mouse.LeftButtonClick();
                }
                else
                {
                    inputSimulator.Mouse.LeftButtonDoubleClick();
                }
                
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void LeftMouseButtonDown()
        {
            try
            {
                Log.Info("Simulating pressing the left mouse button down");
                inputSimulator.Mouse.LeftButtonDown();
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void LeftMouseButtonUp()
        {
            try
            {
                Log.Info("Simulating releasing the left mouse button down");
                inputSimulator.Mouse.LeftButtonUp();
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void MiddleMouseButtonClick()
        {
            try
            {
                Log.Info("Simulating clicking the middle mouse button click");
                inputSimulator.Mouse.MiddleButtonClick();
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void MiddleMouseButtonDown()
        {
            try
            {
                Log.Info("Simulating pressing the middle mouse button down");
                inputSimulator.Mouse.MiddleButtonDown();
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void MiddleMouseButtonUp()
        {
            try
            {
                Log.Info("Simulating releasing the middle mouse button down");
                inputSimulator.Mouse.MiddleButtonUp();
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void RightMouseButtonClick()
        {
            try
            {
                Log.Info("Simulating pressing the right mouse button down");
                inputSimulator.Mouse.RightButtonClick();
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void RightMouseButtonDown()
        {
            try
            {
                Log.Info("Simulating pressing the right mouse button down");
                inputSimulator.Mouse.RightButtonDown();
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void RightMouseButtonUp()
        {
            try
            {
                Log.Info("Simulating releasing the right mouse button down");
                inputSimulator.Mouse.RightButtonUp();
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void ScrollMouseWheelUp(int clicks)
        {
            try
            {
                Log.DebugFormat("Simulating scrolling the vertical mouse wheel up by {0} clicks", clicks);
                inputSimulator.Mouse.VerticalScroll(clicks);
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void ScrollMouseWheelDown(int clicks)
        {
            try
            {
                Log.DebugFormat("Simulating scrolling the vertical mouse wheel down by {0} clicks", clicks);
                inputSimulator.Mouse.VerticalScroll(0 - clicks);
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void ScrollMouseWheelLeft(int clicks)
        {
            try
            {
                Log.DebugFormat("Simulating scrolling the horizontal mouse wheel left by {0} clicks", clicks);
                inputSimulator.Mouse.HorizontalScroll(0 - clicks);
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void ScrollMouseWheelRight(int clicks)
        {
            try
            {
                Log.DebugFormat("Simulating scrolling the horizontal mouse wheel right by {0} clicks", clicks);
                inputSimulator.Mouse.HorizontalScroll(clicks);
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void ScrollMouseWheelAbsoluteHorizontal(int amount)
        {
            try
            {
                Log.DebugFormat("Simulating scrolling the horizontal mouse wheel by {0} units", amount);
                var tmpMouseWheelClickSize = inputSimulator.Mouse.MouseWheelClickSize;
                inputSimulator.Mouse.MouseWheelClickSize = amount;
                inputSimulator.Mouse.HorizontalScroll(1); //Scroll by one click, which is the absolute amount temporarily set in MouseWheelClickSize
                inputSimulator.Mouse.MouseWheelClickSize = tmpMouseWheelClickSize;
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        public void ScrollMouseWheelAbsoluteVertical(int amount)
        {
            try
            {
                Log.DebugFormat("Simulating scrolling the vertical mouse wheel by {0} units", amount);
                var tmpMouseWheelClickSize = inputSimulator.Mouse.MouseWheelClickSize;
                inputSimulator.Mouse.MouseWheelClickSize = amount;
                inputSimulator.Mouse.VerticalScroll(1); //Scroll by one click, which is the absolute amount temporarily set in MouseWheelClickSize
                inputSimulator.Mouse.MouseWheelClickSize = tmpMouseWheelClickSize;
            }
            catch (Exception exception)
            {
                PublishError(this, exception);
            }
        }

        private void PublishError(object sender, Exception ex)
        {
            Log.Error("Publishing Error event (if there are any listeners)", ex);
            if (Error != null)
            {
                Error(sender, ex);
            }
        }
    }
}
