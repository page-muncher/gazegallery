using System;using System.Linq;using System.Collections.Generic;using System.Threading.Tasks;using System.Windows;using System.Windows.Controls;using System.Windows.Controls.Primitives;using System.Windows.Input;
namespace gazegallery;
public partial class MainWindow {
 Popup? filterPopup41;string lastDurationTitle41="";
 void NavigateKeyboard41(Key key,bool repeat){
  int source=KeyboardTile39,target=ThumbnailNavigation39.Next(View.Tiles,source,key,i=>i<GridPaths.Count&&!GridHoles.Contains(GridPaths[i]));
  if(source>=0&&target>=0&&source<View.Tiles.Count&&target<View.Tiles.Count){bool horizontal=key is Key.Left or Key.Right;double Center(Rect r)=>horizontal?r.X+r.Width/2:r.Y+r.Height/2;int sign=key is Key.Left or Key.Up?-1:1;if(repeat&&(Center(View.Tiles[target])-Center(View.Tiles[source]))*sign< -1)return;}
  KeyboardTile39=target;if(target>=0){View.Anchor=target;View.Reveal(target);}View.Focus();
 }
 bool FilterKey41(Key key,ModifierKeys mods)=>Mode=="grid"&&Config.Keys.TryGetValue("Filter thumbnails",out var keys)&&keys.Any(k=>NormalizeKey(k).Equals(NormalizeKey((mods.HasFlag(ModifierKeys.Control)?"Ctrl+":"")+(mods.HasFlag(ModifierKeys.Shift)?"Shift+":"")+(mods.HasFlag(ModifierKeys.Alt)?"Alt+":"")+key),StringComparison.OrdinalIgnoreCase));
 void OpenKeyboardFilter41(){var owner=toolbar.Children.OfType<Button>().FirstOrDefault();if(owner!=null)OpenFilter36(owner);}
 Task CollageThumbnails41()=>SetMode("grid");
 void RestoreCollageOrigin41(){if(collageOrigin!=null&&Files.Contains(collageOrigin)){current=viewerReturn=collageOrigin;Index=Files.IndexOf(collageOrigin);}}

 bool CollageThumbnailKey41(string key)=>Mode=="board"&&Config.Keys["Thumbnails"].Any(k=>NormalizeKey(k).Equals(NormalizeKey(key),StringComparison.OrdinalIgnoreCase));
 double VideoDuration41=>Mode=="viewer"&&videoActive&&!bufferingVideo&&videoPath35==current?Math.Max(0,(video?.Length??0)/1000d):0;
 void RefreshDurationTitle41(){string value=TitleBar32.Duration(VideoDuration41);if(value==lastDurationTitle41)return;lastDurationTitle41=value;UpdateTitle();}
 void SetupRound41(){if(!Config.Keys.ContainsKey("Filter thumbnails"))Config.Keys["Filter thumbnails"]=new[]{"Ctrl+F"};if(!Config.TitleDuration41){if(Config.ViewerTitle.Count>0&&!Config.ViewerTitle.Contains("Video duration")){int at=Config.ViewerTitle.IndexOf("File size");Config.ViewerTitle.Insert(at<0?Config.ViewerTitle.Count:at+1,"Video duration");}Config.TitleDuration41=true;}}
}
