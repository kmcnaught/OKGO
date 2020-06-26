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
                default: return Xbox360Button.Start;
            }
        }
    }
}
