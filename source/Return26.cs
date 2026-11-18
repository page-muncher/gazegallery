using System;using System.Threading.Tasks;
namespace gazegallery;
public partial class MainWindow {
 async Task ReturnFromCollage(){string target=collageReturnMode;string? origin=collageOrigin;if(origin!=null&&Files.Contains(origin)){current=viewerReturn=origin;Index=Files.IndexOf(origin);}await SetMode(target);}
 void QueueFolderArrival(string prior){string? previousChild=Config.ShowSubfolders&&string.Equals(System.IO.Directory.GetParent(prior)?.FullName,Folder,StringComparison.OrdinalIgnoreCase)?prior:null;QueueThumbnailArrival(previousChild,fallBack:true);}
}
