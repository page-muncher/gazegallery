using System;
using System.Linq;
using System.Threading.Tasks;

namespace gazegallery;

public partial class MainWindow
{
    private sealed record ThumbnailArrival(string? Path, bool Flash, bool FirstVisible, bool FallBack);

    private ThumbnailArrival? thumbnailArrival;
    private int thumbnailFlashRevision;

    private void QueueThumbnailArrival(string? path, bool flash = false, bool firstVisible = false, bool fallBack = false)
    {
        CancelThumbnailArrival();
        thumbnailArrival = new ThumbnailArrival(path, flash, firstVisible, fallBack);
    }

    private void CancelThumbnailArrival()
    {
        thumbnailArrival = null;
        thumbnailFlashRevision++;
        View.FlashIndex = -1;
        View.FlashAlpha = 0;
        View.InvalidateVisual();
    }

    private void TryRevealThumbnailArrival()
    {
        if (Mode != "grid" || thumbnailArrival is not { } arrival)
            return;

        int index = arrival.Path == null || GridHoles.Contains(arrival.Path) ? -1 : GridPaths.IndexOf(arrival.Path);
        if (index < 0 && (arrival.FirstVisible || arrival.FallBack))
        {
            int start = arrival.FirstVisible || FolderTileCount >= GridPaths.Count ? 0 : FolderTileCount;
            index = Enumerable.Range(start, GridPaths.Count - start).FirstOrDefault(
                i => !GridHoles.Contains(GridPaths[i]), -1);
        }
        if (index < 0 || index >= View.Tiles.Count)
            return;

        thumbnailArrival = null;
        KeyboardTile39 = View.Anchor = index;
        if (!arrival.FirstVisible)
            View.Scroll = Math.Max(0, View.GridStart35(View.Tiles[index]) - (Config.River ? 14 : ToolbarHeight));

        if (arrival.Flash)
            _ = FlashThumbnailArrival(index, GridPaths[index]);
        View.InvalidateVisual();
    }

    private async Task FlashThumbnailArrival(int index, string path)
    {
        int revision = thumbnailFlashRevision;
        int transition = modeTransition;
        bool StillFocused() => Mode == "grid" && transition == modeTransition &&
            revision == thumbnailFlashRevision && KeyboardTile39 == index &&
            index < GridPaths.Count && GridPaths[index] == path;

        // Start after the viewer-to-thumbnail transition, if present.
        await Task.Delay(150);
        if (!StillFocused())
            return;

        var clock = System.Diagnostics.Stopwatch.StartNew();
        View.FlashIndex = index;
        while (StillFocused() && clock.ElapsedMilliseconds < 450)
        {
            View.FlashAlpha = .2 * (1 - clock.Elapsed.TotalMilliseconds / 450);
            View.InvalidateVisual();
            await Task.Delay(16);
        }
        if (revision == thumbnailFlashRevision)
        {
            View.FlashIndex = -1;
            View.FlashAlpha = 0;
            View.InvalidateVisual();
        }
    }

    private void GoToThumbnailTop()
    {
        PauseScroll();
        ClosePeek34();
        ClearTransition36();
        QueueThumbnailArrival(null, firstVisible: true);
        View.Scroll = 0;
        KeyboardTile39 = -1;

        // Let queued decodes for the previous viewport retire without blocking top-of-folder requests.
        thumbGeneration++;
        thumbPending.Clear();
        lastPreload = DateTime.MinValue;
        priorScroll = 0;
        TryRevealThumbnailArrival();
        PreloadThumbs();
        View.Focus();
        View.InvalidateVisual();
    }
}
