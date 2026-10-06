using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace gazegallery;
public partial class MainWindow {
 public static string FolderDisplayName(string path){var full=Path.GetFullPath(path);var name=Path.GetFileName(full.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar));return string.Equals(full.TrimEnd('\\','/'),Path.GetPathRoot(full)?.TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase)?Path.GetPathRoot(full)!:name;}
 string? ExternalActionImage {get{if(Mode=="viewer")return current; if(Mode!="grid")return null;var selected=SelectedGridImages();return selected.Length==1&&View.Selected.Count==1?selected[0]:null;}}
 bool CanContextAction(string name){
  if(name is "Open file with external program" or "Open with")return ExternalActionImage!=null;
  if(Mode=="board")return true;
  if(name is "Save as" or "Copy" or "Trash" or "Crop" or "Rename" or "Flip horizontal" or "Flip vertical" or "Rotate 90°" or "Collage"){
   if(Mode=="grid"&&name=="Trash"&&SelectedGridFolders().Length>0)return true;
   return current!=null||Mode=="grid"&&SelectedGridImages().Length>0;
  }return true;
 }
 void HandleViewerRightClick(Point point){
  bool overlay=Mode=="viewer"&&(Config.Arrows&&(View.Arrow(true).Contains(point)||View.Arrow(false).Contains(point))||Config.ZoomIndicator&&ZoomOverlay.Contains(point)||Config.SlideshowTimer&&SlideOverlay.Contains(point)||Config.PositionIndicator&&PositionOverlay.Contains(point));
  if(overlay){lastRightClick=0;
   if(Config.Arrows&&(View.Arrow(true).Contains(point)||View.Arrow(false).Contains(point))){var menu=new ContextMenu();CloseOverlayMenu(menu,()=>Config.Arrows=false);menu.IsOpen=true;}
   else if(Config.ZoomIndicator&&ZoomOverlay.Contains(point))FitMenu();
   else if(Config.SlideshowTimer&&SlideOverlay.Contains(point))SlideshowMenu();else PositionContext();return;
  }
  if(TryDoubleRight())return;ShowContext(point);
 }
 void AddEndOfFolderChoices(ItemsControl menu){
  menu.Items.Add(new Separator());menu.Items.Add(new MenuItem{Header="EOF behaviour",IsEnabled=false});
  foreach(var option in new[]{("Loop",false),("Adjacent folder siblings",true)}){
   var item=new MenuItem{Header=option.Item1,IsCheckable=true,IsChecked=Config.SiblingNavigation==option.Item2,InputGestureText=Config.SiblingNavigation==option.Item2?"✓":""};
   item.Click+=(_,_)=>{Config.SiblingNavigation=option.Item2;Config.Save();};menu.Items.Add(item);
  }
 }
 void NormalizeMenuSeparators(ItemsControl menu){for(int i=menu.Items.Count-1;i>=0;i--){if(menu.Items[i] is Separator separator){if(i==0||i==menu.Items.Count-1||menu.Items[i-1] is Separator){menu.Items.RemoveAt(i);continue;}separator.Style=(Style)FindResource(typeof(Separator));}else if(menu.Items[i] is MenuItem child&&child.HasItems)NormalizeMenuSeparators(child);}}
}
