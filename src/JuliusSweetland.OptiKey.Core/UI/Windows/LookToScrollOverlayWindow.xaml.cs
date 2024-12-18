﻿// Copyright (c) 2022 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using JuliusSweetland.OptiKey.Static;
using System.Windows.Interop;
using JuliusSweetland.OptiKey.Native.Common.Enums;
using JuliusSweetland.OptiKey.UI.ViewModels;

namespace JuliusSweetland.OptiKey.UI.Windows
{
    /// <summary>
    /// Interaction logic for LookToScrollOverlayWindow.xaml
    /// </summary>
    public partial class LookToScrollOverlayWindow : Window
    {
        private readonly ILookToScrollOverlayViewModel viewModel;

        public LookToScrollOverlayWindow(ILookToScrollOverlayViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            DataContext = viewModel;

            this.UpdateContours(viewModel.ZeroContours);
        }

        private void AddEllipse(Point centre, Point radii)
        {
            double w = Graphics.PrimaryScreenWidthInPixels;
            double h = Graphics.PrimaryScreenHeightInPixels;

            Rectangle rect = new Rectangle();
            rect.Width = radii.X * 2;
            rect.Height = radii.Y * 2;
            rect.RadiusX = radii.X;
            rect.RadiusY = radii.Y;
            rect.Stroke = Brushes.CadetBlue;
            rect.Opacity = 0.25;
            rect.StrokeThickness = 4;

            var top = centre - radii;
            Canvas.SetTop(rect, top.Y);
            Canvas.SetLeft(rect, top.X);

            canvas.Children.Add(rect);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var windowHandle = new WindowInteropHelper(this).Handle;
            Static.Windows.SetExtendedWindowStyle(windowHandle,
                Static.Windows.GetExtendedWindowStyle(windowHandle) | ExtendedWindowStyles.WS_EX_TRANSPARENT | ExtendedWindowStyles.WS_EX_TOOLWINDOW);
        }

        private void UpdateContours(List<Point> zeroContours)
        {
            canvas.Children.Clear();
            
            Point centre = new Point(SystemParameters.PrimaryScreenWidth / 2, SystemParameters.PrimaryScreenHeight / 2);
            centre = new Point(viewModel.JoystickCentre.X / Graphics.DipScalingFactorX, 
                               viewModel.JoystickCentre.Y / Graphics.DipScalingFactorY);
            foreach (Point radii in zeroContours)
            {
                // we convert from px to dip for canvas
                Point radiiScaled = radii;
                radiiScaled.X /= Graphics.DipScalingFactorX;
                radiiScaled.Y /= Graphics.DipScalingFactorY;

                this.AddEllipse(centre, radiiScaled);
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Equals("ZeroContours", e.PropertyName) ||
                string.Equals("JoystickCentre", e.PropertyName))
            {                
                this.UpdateContours(viewModel.ZeroContours);
            }
        }
    }
}
