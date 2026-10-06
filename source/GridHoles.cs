using System;using System.Linq;using System.Collections.Generic;using System.IO;
namespace gazegallery;
public partial class MainWindow {
 public readonly HashSet<string> GridHoles=new(StringComparer.OrdinalIgnoreCase);string gridLayoutStamp="";
 public void ClearGridHoles(){if(GridHoles.Count==0)return;GridHoles.Clear();RebuildGridPaths();View.GridVersion++;}
 public void PrepareGridLayout(){string stamp=$"{Folder}|{Config.Sort}|{Config.Masonry}|{Config.River}|{Config.Columns}|{Config.Names}|{View.ActualWidth:0}|{Config.ShowSubfolders}|{gridFilter34}";if(stamp!=gridLayoutStamp){gridLayoutStamp=stamp;ClearGridHoles();}}
 void RememberGridHole(string path){if(Mode=="grid"&&GridPaths.Contains(path))GridHoles.Add(path);}
 void MergeGridHoles(List<string> list){bool hadHoles=GridHoles.Count>0;GridHoles.RemoveWhere(File.Exists);if(!hadHoles){list.AddRange(Files);return;}var old=GridPaths.Skip(FolderTileCount).Where(p=>GridHoles.Contains(p)||Files.Contains(p)).ToArray();list.AddRange(old);list.AddRange(Files.Where(p=>!old.Contains(p)));}
 int lastSelectedCount=-1;void SelectionToast(){if(Mode!="grid"){lastSelectedCount=-1;return;}int count=View.Selected.Count(i=>i>=FolderTileCount&&i<GridPaths.Count&&!GridHoles.Contains(GridPaths[i]));if(count==lastSelectedCount)return;lastSelectedCount=count;if(count>1)Toast($"{count} of {Files.Count} images selected");}
}
