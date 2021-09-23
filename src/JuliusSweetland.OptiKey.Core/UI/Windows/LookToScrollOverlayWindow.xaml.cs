// Copyright (c) 2020 OPTIKEY LTD (UK company number 11854839) - All Rights Reserved
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
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
        Rectangle rect;

        public LookToScrollOverlayWindow(ILookToScrollOverlayViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            DataContext = viewModel;
            
            this.AddEllipse(new Point(Graphics.PrimaryScreenWidthInPixels/2, Graphics.PrimaryScreenHeightInPixels/2), new Point(150, 150));

        }

        private void AddEllipse(Point centre, Point radii)
        {
            rect = new Rectangle();
            rect.Width = radii.X*2;
            rect.Height = radii.Y*2;
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

        // Based on: https://stackoverflow.com/a/3367137/9091159
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Apply the WS_EX_TRANSPARENT flag to the overlay window so that mouse events will
            // pass through to any window underneath.
            var hWnd = new WindowInteropHelper(this).Handle;
            Static.Windows.SetWindowExTransparent(hWnd);
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Equals("ActiveDeadzone", e.PropertyName))
            {
                Rect bounds = viewModel.ActiveDeadzone;
                if (!bounds.IsEmpty)
                {
                    Canvas.SetTop(rect, bounds.Top);
                    Canvas.SetLeft(rect, bounds.Left);
                    
                    rect.Width = bounds.Width;
                    rect.Height = bounds.Height;
                    rect.RadiusX = bounds.Width / 2;
                    rect.RadiusY = bounds.Height / 2;
                }                
            }
        }
    }
}
