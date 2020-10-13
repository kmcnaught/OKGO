// Copyright (c) K McNaught Consulting Ltd (UK company number 11297717) - All Rights Reserved

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
        XboxLeftThumb, // press down thumb
        XboxRightThumb, // press down thumb
        XboxLeftShoulder,
        XboxRightShoulder,
        XboxGuide,
        XboxA,
        XboxB,
        XboxX,
        XboxY,
        XBoxLeftThumbNeutral,  // FIXME: how to refer to specific axis? should this do both?
        XBoxRightThumbNeutral, // FIXME: how to refer to specific axis? should this do both?
        XBoxLeftTrigger, 
        XBoxRightTrigger,
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

        public static Xbox360Slider ToViGemSlider(this XboxButtons button)
        {
            switch (button)
            {
                case XboxButtons.XBoxLeftTrigger:
                    return Xbox360Slider.LeftTrigger;
                case XboxButtons.XBoxRightTrigger:
                    return Xbox360Slider.RightTrigger;
                default: return null;
            }
        }
    }
}
