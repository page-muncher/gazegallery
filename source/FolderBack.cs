using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace gazegallery;

public partial class MainWindow
{
    private readonly Stack<string> folderHistory = new();

    private void PushFolderHistory(string from, string to)
    {
        if (!Path.TrimEndingDirectorySeparator(from).Equals(
                Path.TrimEndingDirectorySeparator(to), StringComparison.OrdinalIgnoreCase))
            folderHistory.Push(from);
    }

    private void ThumbnailFolderBack(object sender, MouseButtonEventArgs e)
    {
        if (Mode != "grid" || e.ChangedButton != MouseButton.XButton1)
            return;
        e.Handled = true;
        if (!Operations.Busy)
            Run(GoBackFolder);
    }

    private async Task GoBackFolder()
    {
        while (folderHistory.TryPop(out string? folder))
        {
            if (!Directory.Exists(folder))
                continue;
            await NavigateFolderCore(folder, false);
            return;
        }
    }

}
