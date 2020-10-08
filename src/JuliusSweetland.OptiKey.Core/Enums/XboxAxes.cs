// Copyright (c) K McNaught Consulting Ltd (UK company number 11297717) - All Rights Reserved
using System;
using System.Runtime.InteropServices;
using JuliusSweetland.OptiKey.Properties;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace JuliusSweetland.OptiKey.Enums
{

    public enum XboxAxes
    {
        LeftJoystickAxisX,
        LeftJoystickAxisY,
        RightJoystickAxisX,
        RightJoystickAxisY,
    }

    public enum Axes
    {
        AxisX,
        AxisY
    }

    public static partial class EnumExtensions
    {
        public static Xbox360Axis ToViGemAxis(this XboxAxes button)
        {
            switch (button)
            {
                case XboxAxes.LeftJoystickAxisX: return Xbox360Axis.LeftThumbX;
                case XboxAxes.LeftJoystickAxisY: return Xbox360Axis.LeftThumbY;
                case XboxAxes.RightJoystickAxisX: return Xbox360Axis.RightThumbX;
                case XboxAxes.RightJoystickAxisY: return Xbox360Axis.RightThumbY;
                default: return Xbox360Axis.LeftThumbX;
            }
        }
    }
}
