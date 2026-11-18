using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace gazegallery;

public partial class MainWindow
{
    private readonly HashSet<Key> panKeys = new();
    private TimeSpan? previousPanFrame;
    private const double KeyboardPanSpeed = 160;

    internal static Key InputKey(KeyEventArgs e) => e.Key == Key.System ? e.SystemKey : e.Key;

    internal static string[] MigrateFullscreenBindings(IEnumerable<string> bindings)
    {
        var normalized = bindings.Select(NormalizeKey).ToArray();
        if (!normalized.Contains("Enter", StringComparer.OrdinalIgnoreCase))
            return normalized;
        return normalized.Where(key => !key.Equals("Enter", StringComparison.OrdinalIgnoreCase))
            .Concat(new[] { "Shift+Enter", "Alt+Enter" })
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private bool HandleFullscreenKey(string chord, bool repeat)
    {
        string normalized = NormalizeKey(chord);
        if (normalized.Equals("Enter", StringComparison.OrdinalIgnoreCase) ||
            !Config.Keys["Fullscreen"].Contains(normalized, StringComparer.OrdinalIgnoreCase))
            return false;

        if (!repeat)
        {
            StopKeyboardPan();
            ToggleFullscreen();
        }
        return true;
    }

    private bool HandleViewerTransformKey(Key key, ModifierKeys modifiers)
    {
        if (Mode != "viewer" || View.Image == null || Cropping || Quick)
            return false;

        if (modifiers == ModifierKeys.Shift && key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            panKeys.Add(key);
            View.InvalidateVisual();
            return true;
        }

        double factor = (modifiers, key) switch
        {
            (ModifierKeys.Control, Key.Up) or (ModifierKeys.None, Key.OemPlus or Key.Add) => 1.05,
            (ModifierKeys.Control, Key.Down) or (ModifierKeys.None, Key.OemMinus or Key.Subtract) => 0.95,
            _ => 0,
        };
        if (factor == 0)
            return false;

        if (RenderWindowFit34)
            SyncWindowFit34(View.RenderSize);
        if (View.Scale <= 0 || View.ActualWidth <= 0 || View.ActualHeight <= 0)
            return true;
        var (min, max) = GeometryRules.Zoom(View.DisplayWidth38, View.DisplayHeight38, View.ActualWidth, View.ActualHeight);
        if (largeRaster != null)
            max *= largeRaster.Width / (double)View.DisplayWidth38;
        double newScale = Math.Clamp(View.Scale * factor, min, max);
        var center = new Vector(View.ActualWidth / 2, View.ActualHeight / 2);
        View.Pan = center + (View.Pan - center) * (newScale / View.Scale);
        View.Scale = newScale;
        View.ZoomUsed = true;
        View.InvalidateVisual();
        return true;
    }

    private void ReleaseKeyboardPan(Key key)
    {
        panKeys.Remove(key);
        if (key is Key.LeftShift or Key.RightShift || panKeys.Count == 0)
            StopKeyboardPan();
    }

    private void StopKeyboardPan()
    {
        panKeys.Clear();
        previousPanFrame = null;
    }

    private void AdvanceKeyboardPan(TimeSpan frameTime)
    {
        if (panKeys.Count == 0)
            return;
        if (!IsActive || Mode != "viewer" || Cropping || Quick || pluginOverlay != null ||
            View.Image == null || Keyboard.Modifiers != ModifierKeys.Shift)
        {
            StopKeyboardPan();
            return;
        }

        double elapsed = Math.Clamp((frameTime - (previousPanFrame ?? frameTime)).TotalSeconds, 0, 0.05);
        previousPanFrame = frameTime;
        var direction = new Vector(
            (panKeys.Contains(Key.Right) ? 1 : 0) - (panKeys.Contains(Key.Left) ? 1 : 0),
            (panKeys.Contains(Key.Down) ? 1 : 0) - (panKeys.Contains(Key.Up) ? 1 : 0));
        if (direction.LengthSquared > 0 && elapsed > 0)
        {
            direction.Normalize();
            if (RenderWindowFit34)
                SyncWindowFit34(View.RenderSize);
            View.Pan += direction * (KeyboardPanSpeed * elapsed);
            View.ZoomUsed = true;
        }
        View.InvalidateVisual();
    }
}
