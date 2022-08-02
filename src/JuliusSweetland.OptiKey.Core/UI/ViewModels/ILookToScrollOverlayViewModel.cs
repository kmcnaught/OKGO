// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace JuliusSweetland.OptiKey.UI.ViewModels
{
    public interface ILookToScrollOverlayViewModel : INotifyPropertyChanged
    {
        bool IsActive { get; }
        List<Point> ZeroContours { get;  }
        Point JoystickCentre { get; }
    }
}
