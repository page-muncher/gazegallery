using System;using System.Linq;using System.Collections.Generic;using System.Threading.Tasks;using System.Windows;using System.Windows.Controls;using System.Windows.Controls.Primitives;using System.Windows.Input;
namespace gazegallery;
// Key repeats are delayed only when physically wrapping between the collection's two end files.
public sealed class ArrowWrapDelay41 {
 Key key;int from=-1,to=-1;double began;
 public void Reset(){from=to=-1;key=Key.None;}
 public bool Wait(Key direction,int source,int target,bool repeat,bool endWrap,double now){
  if(!repeat||!endWrap){Reset();return false;}
  if(key!=direction||from!=source||to!=target){key=direction;from=source;to=target;began=now;}
  if(now-began<2)return true;Reset();return false;
 }
 public static bool EndWrap(IReadOnlyList<Rect> tiles,int source,int target,Key direction,int first,int last){
  if(source<0||target<0||source>=tiles.Count||target>=tiles.Count||first==last)return false;
  if(!((source==first&&target==last)||(source==last&&target==first)))return false;
  bool horizontal=direction is Key.Left or Key.Right;int sign=direction is Key.Left or Key.Up?-1:1;
  double Center(Rect r)=>horizontal?r.X+r.Width/2:r.Y+r.Height/2;
  return (Center(tiles[target])-Center(tiles[source]))*sign < -1;
 }
}
public partial class MainWindow {
 readonly ArrowWrapDelay41 arrowWrap41=new();Popup? filterPopup41;string lastDurationTitle41="";
 void NavigateKeyboard41(Key key,bool repeat){
  int source=KeyboardTile39,target=ThumbnailNavigation39.Next(View.Tiles,source,key,i=>i<GridPaths.Count&&!GridHoles.Contains(GridPaths[i]));
  var files=Enumerable.Range(FolderTileCount,Math.Max(0,GridPaths.Count-FolderTileCount)).Where(i=>!GridHoles.Contains(GridPaths[i])).ToArray();
  bool loop=files.Length>1&&ArrowWrapDelay41.EndWrap(View.Tiles,source,target,key,files[0],files[^1]);
  if(arrowWrap41.Wait(key,source,target,repeat,loop,Environment.TickCount64/1000d))return;
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
