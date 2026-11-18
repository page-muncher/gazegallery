using System;
using System.Windows;
using System.Windows.Controls;

namespace gazegallery;

public partial class MainWindow
{
    private void BuildViewerCropToolbar()
    {
        var adjust = Ui.Button("Adjust image…", EditAdjustments);
        var rotation = CreateRotationControls32();
        var options = new StackPanel();
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

        var toolbar = new ViewerCropToolbar(adjust, rotation, options, actions, () =>
        {
            adjustmentWindow?.Close();
            motionLineMode = motionLineDragging = false;
            View.Focus();
        });
        cropPanel.Background = null;
        cropPanel.VerticalAlignment = VerticalAlignment.Stretch;
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
        toolbar.SetAvailableHeight(View.ActualHeight > 0 ? View.ActualHeight : Height);
    }

    private sealed class ViewerCropToolbar : Grid
    {
        private readonly Border cropOptions;
        private readonly StackPanel bottom;
        private readonly Border adjust;
        private readonly Button toggle;
        private bool hidden;

        public ViewerCropToolbar(Button adjustments, FrameworkElement rotation, Panel options, Panel actions, Action onHide)
        {
            adjust = Backdrop(adjustments, HorizontalAlignment.Left, VerticalAlignment.Top);
            adjust.Margin = new Thickness(8, 20, 0, 0);
            Children.Add(adjust);

            var right = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
            toggle = Ui.Button("Hide cropping UI", () =>
            {
                hidden = !hidden;
                toggle!.Content = hidden ? "Show cropping UI" : "Hide cropping UI";
                adjust.Visibility = cropOptions!.Visibility = bottom!.Visibility = hidden ? Visibility.Collapsed : Visibility.Visible;
                if (hidden)
                    onHide();
            });
            toggle.Width = 132;
            right.Children.Add(Backdrop(toggle, HorizontalAlignment.Right, VerticalAlignment.Top));
            cropOptions = Backdrop(options, HorizontalAlignment.Right, VerticalAlignment.Top);
            right.Children.Add(cropOptions);
            Children.Add(right);

            bottom = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };
            var rotationBackdrop = Backdrop(rotation, HorizontalAlignment.Center, VerticalAlignment.Bottom);
            rotationBackdrop.Margin = new Thickness(0, 0, 0, 8);
            bottom.Children.Add(rotationBackdrop);
            bottom.Children.Add(Backdrop(actions, HorizontalAlignment.Right, VerticalAlignment.Bottom));
            Children.Add(bottom);
        }

        public void SetAvailableHeight(double height)
        {
            cropOptions.Margin = new Thickness(0, Math.Clamp(height * 0.12, 10, 135), 0, 0);
        }

        private static Border Backdrop(UIElement content, HorizontalAlignment horizontal, VerticalAlignment vertical) => new()
        {
            Background = Ui.Bg,
            Child = content,
            CornerRadius = new CornerRadius(6),
            HorizontalAlignment = horizontal,
            VerticalAlignment = vertical,
            Padding = new Thickness(4)
        };
    }
}
