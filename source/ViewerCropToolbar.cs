using System;
using System.Windows;
using System.Windows.Controls;

namespace gazegallery;

public partial class MainWindow
{
    private void BuildViewerCropToolbar()
    {
        var adjust = Ui.Button("Adjust image…", EditAdjustments);
        var rotationControls = CreateRotationControls32();
        var options = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        AddCropRatios(options);
        Ui.Check(options, "Allow overcropping", Config.CropFillOutside, value =>
        {
            Config.CropFillOutside = value;
            Config.Save();
            ConstrainViewerCrop();
        });
        ((CheckBox)options.Children[^1]).ToolTip = "Fill the area outside the image with the canvas color";

        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        actions.Children.Add(Ui.Button("Apply and save", () => Run(ApplyCrop)));
        actions.Children.Add(Ui.Button("Save as copy", () => Run(SaveCropCopy)));
        actions.Children.Add(Ui.Button("Cancel", CancelCrop));

        var toolbar = new ViewerCropToolbar(adjust, rotationControls, options, actions);
        var show = Ui.Button("Show cropping UI", () =>
        {
            cropPanel.Child = toolbar;
            ResizeViewerCropToolbar();
            View.Focus();
        });
        var reveal = ViewerCropToolbar.Backdrop(show);
        options.Children.Add(Ui.Button("Hide cropping UI", () =>
        {
            adjustmentWindow?.Close();
            motionLineMode = motionLineDragging = false;
            cropPanel.Width = double.NaN;
            cropPanel.Child = reveal;
            View.Focus();
        }));

        cropPanel.Background = null;
        cropPanel.Child = toolbar;
        ResizeViewerCropToolbar();
    }

    private void ResizeViewerCropToolbar()
    {
        if (cropPanel.Child is not ViewerCropToolbar toolbar)
            return;

        double viewport = View.ActualWidth > 0 ? View.ActualWidth : Width;
        double width = Math.Max(1, viewport - cropPanel.Margin.Left - cropPanel.Margin.Right);
        if (rotationRuler != null)
            rotationRuler.MaxWidth = Math.Max(rotationRuler.MinWidth, width - 16);
        cropPanel.Width = width;
        toolbar.SetAvailableWidth(width);
    }

    private sealed class ViewerCropToolbar : Grid
    {
        private readonly Border adjust;
        private readonly Border rotation;
        private readonly Border options;
        private readonly double rotationWidth;
        private bool? narrow;

        public ViewerCropToolbar(Button adjust, FrameworkElement rotation, Panel options, Panel actions)
        {
            this.adjust = Backdrop(adjust, HorizontalAlignment.Left);
            this.rotation = Backdrop(rotation, HorizontalAlignment.Center);
            this.options = Backdrop(options);
            var save = Backdrop(actions);

            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ColumnDefinitions.Add(new ColumnDefinition());
            for (int i = 0; i < 3; i++)
                RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            SetColumn(this.options, 2);
            SetRow(save, 2);
            SetColumnSpan(save, 3);
            Children.Add(this.adjust);
            Children.Add(this.rotation);
            Children.Add(this.options);
            Children.Add(save);

            this.rotation.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            rotationWidth = this.rotation.DesiredSize.Width;
        }

        public void SetAvailableWidth(double width)
        {
            // Give the rotation ruler its own full-width row when the sides become too narrow.
            bool useNarrowLayout = width < rotationWidth + 360;
            if (narrow == useNarrowLayout)
                return;

            narrow = useNarrowLayout;
            ColumnDefinitions[1].Width = useNarrowLayout ? new GridLength(0) : GridLength.Auto;
            SetColumn(rotation, useNarrowLayout ? 0 : 1);
            SetColumnSpan(rotation, useNarrowLayout ? 3 : 1);
            SetRow(adjust, useNarrowLayout ? 1 : 0);
            SetRow(options, useNarrowLayout ? 1 : 0);
        }

        public static Border Backdrop(UIElement content, HorizontalAlignment alignment = HorizontalAlignment.Right)
        {
            return new Border
            {
                Background = Ui.Bg,
                Child = content,
                CornerRadius = new CornerRadius(6),
                HorizontalAlignment = alignment,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 2),
                Padding = new Thickness(4)
            };
        }
    }
}
