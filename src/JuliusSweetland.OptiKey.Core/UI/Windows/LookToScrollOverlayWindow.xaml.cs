// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using JuliusSweetland.OptiKey.Models.ScalingModels;
using JuliusSweetland.OptiKey.Static;
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

        private void AddEllipse(Region region)
        {
            Rectangle rect = new Rectangle();
            rect.Width = region.Width * Graphics.PrimaryScreenWidthInPixels / Graphics.DipScalingFactorX;
            rect.Height = region.Height * Graphics.PrimaryScreenHeightInPixels / Graphics.DipScalingFactorY;
            rect.RadiusX = region.Radius * Graphics.PrimaryScreenWidthInPixels / Graphics.DipScalingFactorX; ;
            rect.RadiusY = rect.RadiusX;
            rect.Stroke = Brushes.CadetBlue;
            rect.Opacity = 0.25;
            rect.StrokeThickness = 4;

            Canvas.SetLeft(rect, region.Left * Graphics.PrimaryScreenWidthInPixels / Graphics.DipScalingFactorX);
            Canvas.SetTop(rect, region.Top * Graphics.PrimaryScreenHeightInPixels / Graphics.DipScalingFactorY);

            canvas.Children.Add(rect);
        }

        // Based on: https://stackoverflow.com/a/3367137/9091159
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Apply the WS_EX_TRANSPARENT flag to the overlay window so that mouse events will
            // pass through to any window underneath.
            var hWnd = new WindowInteropHelper(this).Handle;
            Static.Windows.SetWindowExTransparent(hWnd);
        }

        private void UpdateContours(List<Region> zeroContours)
        {
            canvas.Children.Clear();
            
            foreach (var region in zeroContours)
            {
                this.AddEllipse(region);
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Equals("ZeroContours", e.PropertyName))
            {                
                this.UpdateContours(viewModel.ZeroContours);
            }
        }
    }
}
