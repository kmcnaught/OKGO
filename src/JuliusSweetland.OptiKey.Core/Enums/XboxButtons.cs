// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Runtime.InteropServices;
using JuliusSweetland.OptiKey.Properties;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace JuliusSweetland.OptiKey.Enums
{
    public enum XboxButtons
    {
        XboxUp,
        XboxDown,
        XboxLeft,
        XboxRight,
        XboxStart,
        XboxBack,
        XboxLeftThumb,
        XboxRightThumb,
        XboxLeftShoulder,
        XboxRightShoulder,
        XboxGuide,
        XboxA,
        XboxB,
        XboxX,
        XboxY,
        XBoxLeftThumbForward,
        XBoxRightThumbForward,
        XBoxLeftThumbHalfForward,
        XBoxRightThumbHalfForward,
        XBoxLeftThumbNeutral,
        XBoxRightThumbNeutral,
    }

    public static partial class EnumExtensions
    {
        public static Xbox360Button ToViGemButton(this XboxButtons button)
        {
            switch (button)
            {
                case XboxButtons.XboxUp: return Xbox360Button.Up;
                case XboxButtons.XboxDown: return Xbox360Button.Down;
                case XboxButtons.XboxLeft: return Xbox360Button.Left;
                case XboxButtons.XboxRight: return Xbox360Button.Right;
                case XboxButtons.XboxStart: return Xbox360Button.Start;
                case XboxButtons.XboxBack: return Xbox360Button.Back;
                case XboxButtons.XboxLeftThumb: return Xbox360Button.LeftThumb;
                case XboxButtons.XboxRightThumb: return Xbox360Button.RightThumb;
                case XboxButtons.XboxLeftShoulder: return Xbox360Button.LeftShoulder;
                case XboxButtons.XboxRightShoulder: return Xbox360Button.RightShoulder;
                case XboxButtons.XboxGuide: return Xbox360Button.Guide;
                case XboxButtons.XboxA: return Xbox360Button.A;
                case XboxButtons.XboxB: return Xbox360Button.B;
                case XboxButtons.XboxX: return Xbox360Button.X;
                case XboxButtons.XboxY: return Xbox360Button.Y;
                default: return null;
            }
        }

        public static Xbox360Axis ToViGemAxis(this XboxButtons button)
        {
            switch (button)
            {
                case XboxButtons.XBoxLeftThumbForward:
                case XboxButtons.XBoxLeftThumbHalfForward:
                case XboxButtons.XBoxLeftThumbNeutral:
                    return Xbox360Axis.LeftThumbY;
                case XboxButtons.XBoxRightThumbForward:
                case XboxButtons.XBoxRightThumbHalfForward:
                case XboxButtons.XBoxRightThumbNeutral:
                    return Xbox360Axis.RightThumbY;
                default: return null;
            }
        }

        public static float ToAxisAmount(this XboxButtons button)
        {
            switch (button)
            {
                case XboxButtons.XBoxLeftThumbForward:
                case XboxButtons.XBoxRightThumbForward:
                    return 1.0f;
                case XboxButtons.XBoxLeftThumbHalfForward:
                case XboxButtons.XBoxRightThumbHalfForward:
                    return 0.5f;
                case XboxButtons.XBoxLeftThumbNeutral:
                case XboxButtons.XBoxRightThumbNeutral:
                    return 0.0f;
                default: return 0.0f;
            }
        }
    }
}
