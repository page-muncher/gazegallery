using System;using System.Threading.Tasks;
namespace gazegallery;
public partial class MainWindow {
 async Task ReturnFromCollage(){string target=collageReturnMode;string? origin=collageOrigin;if(origin!=null&&Files.Contains(origin)){current=viewerReturn=origin;Index=Files.IndexOf(origin);}await SetMode(target);}
 void RevealFolderArrival(string prior){int tile=Config.ShowSubfolders&&System.IO.Directory.GetParent(prior)?.FullName==Folder?GridPaths.IndexOf(prior):FolderTileCount;if(tile>=0&&tile<View.Tiles.Count&&tile<GridPaths.Count){View.Scroll=Math.Max(0,View.GridStart35(View.Tiles[tile])-(Config.River?14:ToolbarHeight));View.Anchor=tile;FlashThumbnail(tile);}}
}
