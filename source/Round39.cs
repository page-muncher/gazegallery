using System;using System.Linq;using System.Collections.Generic;using System.Windows;using System.Windows.Controls;using System.Windows.Controls.Primitives;using System.Windows.Input;using System.Windows.Media;
namespace gazegallery;
public static class ThumbnailNavigation39 {
 public static int Next(IReadOnlyList<Rect> tiles,int from,Key key,Func<int,bool> valid){
  var indices=Enumerable.Range(0,tiles.Count).Where(valid).ToArray();if(indices.Length==0)return -1;if(!indices.Contains(from))return indices[0];
  bool horizontal=key is Key.Left or Key.Right;int sign=key is Key.Left or Key.Up?-1:1;var origin=tiles[from];double Axis(Rect r)=>horizontal?r.X+r.Width/2:r.Y+r.Height/2;double Cross(Rect r)=>horizontal?r.Y+r.Height/2:r.X+r.Width/2;
  double Gap(Rect r)=>horizontal?Math.Max(0,Math.Max(origin.Top-r.Bottom,r.Top-origin.Bottom)):Math.Max(0,Math.Max(origin.Left-r.Right,r.Left-origin.Right));
  if(horizontal){
   // Choose the adjacent physical column before considering vertical distance.
   var columns=indices.Select(i=>Axis(tiles[i])).OrderBy(x=>x).Aggregate(new List<double>(),(list,x)=>{if(list.Count==0||Math.Abs(x-list[^1])>1)list.Add(x);return list;});
   double target=sign>0?columns.FirstOrDefault(x=>x>Axis(origin)+1,columns[0]):columns.LastOrDefault(x=>x<Axis(origin)-1,columns[^1]);
   return indices.Where(i=>Math.Abs(Axis(tiles[i])-target)<=1).OrderBy(i=>Math.Abs(Cross(tiles[i])-Cross(origin))).ThenBy(i=>i).First();
  }
  var forward=indices.Where(i=>i!=from&&(Axis(tiles[i])-Axis(origin))*sign>1).ToArray();
  if(forward.Length>0)return forward.OrderBy(i=>Gap(tiles[i])>1?1:0).ThenBy(i=>Math.Abs(Cross(tiles[i])-Cross(origin))*2+Math.Abs(Axis(tiles[i])-Axis(origin))).First();
  var others=indices.Where(i=>i!=from).ToArray();if(others.Length==0)return from;
  return others.OrderBy(i=>Gap(tiles[i])>1?1:0).ThenBy(i=>Axis(tiles[i])*sign).ThenBy(i=>Math.Abs(Cross(tiles[i])-Cross(origin))).First();
 }
}
public partial class MainWindow {
 public int KeyboardTile39=-1;bool chromeSuppressed39;int scanEpoch39;
 public string? PresentedPath39=>presentedPath35;
 bool ThumbnailKeys39(Key key,ModifierKeys mods,bool repeat){
  if(Mode!="grid")return false;
  if(key is Key.Left or Key.Right or Key.Up or Key.Down){PauseScroll();if(KeyboardTile39<0&&View.Selected.Count==1)KeyboardTile39=View.Selected.Single();KeyboardTile39=ThumbnailNavigation39.Next(View.Tiles,KeyboardTile39,key,i=>i<GridPaths.Count&&!GridHoles.Contains(GridPaths[i]));if(KeyboardTile39>=0){View.Anchor=KeyboardTile39;View.Reveal(KeyboardTile39);}View.Focus();return true;}
  if(key==Key.Space&&mods.HasFlag(ModifierKeys.Control)){if(!repeat&&KeyboardTile39>=0&&KeyboardTile39<GridPaths.Count&&!(IsFolderTile(KeyboardTile39)&&KeyboardTile39==0)){int i=KeyboardTile39;bool folder=IsFolderTile(i);View.Selected.RemoveWhere(j=>IsFolderTile(j)!=folder);if(!View.Selected.Remove(i))View.Selected.Add(i);View.InvalidateVisual();}return true;}
  if(key==Key.Space&&mods==ModifierKeys.None){if(!repeat)ToggleAutoscroll();View.Focus();return true;}
  if(key==Key.Enter&&KeyboardTile39>=0&&KeyboardTile39<GridPaths.Count){int i=KeyboardTile39;if(IsFolderTile(i))Run(()=>NavigateFolder(GridPaths[i]));else Run(()=>EnterThumbnailView35(i));return true;}return false;
 }
 public void DrawKeyboardFocus39(DrawingContext dc){int i=KeyboardTile39;if(i<0||i>=View.Tiles.Count)return;var tile=View.ScreenTile35(View.Tiles[i]);if(View.Selected.Contains(i)){dc.PushOpacity(.17);dc.DrawRoundedRectangle(Ui.Selection,null,tile,6,6);dc.Pop();}bool selected=View.Selected.Contains(i);if(!selected)dc.PushOpacity(.5);dc.DrawRoundedRectangle(null,new Pen(Ui.Selection,4){DashStyle=selected?DashStyles.Solid:DashStyles.Dash},tile,6,6);if(!selected)dc.Pop();}
 void Category39(string name,Action<ContextMenu> populate){var button=Ui.Button(name+" ▾",()=>{});button.Click+=(_,_)=>{var menu=new ContextMenu{PlacementTarget=button,Placement=PlacementMode.Bottom};populate(menu);menu.IsOpen=true;};toolbar.Children.Add(button);}
 static void Choice39(ContextMenu menu,string title,Action action,bool? check=null){var item=new MenuItem{Header=title,IsCheckable=check.HasValue,IsChecked=check??false};item.Click+=(_,_)=>action();menu.Items.Add(item);}
 void BuildGridToolbar39(){
  Category39("Display",m=>{Choice39(m,"Layout: "+ThumbnailLayoutName35,CycleLayout35);Choice39(m,"Names",ToggleNames,Config.Names);Choice39(m,"Subfolders",()=>{Config.ShowSubfolders=!Config.ShowSubfolders;Config.Save();RebuildGridPaths();View.Scroll=0;View.LayoutTiles();},Config.ShowSubfolders);Choice39(m,"Sort…",SortMenu);Choice39(m,"Filter…",()=>OpenFilter36(toolbar.Children.OfType<Button>().First()));Choice39(m,"Zoom in",()=>ColumnStep(-1));Choice39(m,"Zoom out",()=>ColumnStep(1));});
  Category39("Navigation",m=>{Choice39(m,autoScroll?"Stop autoscroll":"Start autoscroll",ToggleAutoscroll);Choice39(m,"Scroll speed…",SetScrollSpeed);Choice39(m,"To bottom",ToBottom);});
  Category39("Actions",m=>{Choice39(m,"Select all / deselect all",ToggleSelectAll);Choice39(m,"Move selected",ToggleQuick);Choice39(m,"Create new folder",CreateSubfolder);});
 }
 static MenuItem? BorderlessEntry39(object source){var node=source as DependencyObject;while(node!=null){if(node is MenuItem item&&Equals(item.Tag,"borderless-options"))return item;node=node is Visual?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node);}return null;}
 void SetupKeys39(){if(!Config.Keys.ContainsKey("Folder timeline"))Config.Keys["Folder timeline"]=new[]{"Shift+G"};}
 bool TimelineKey39(string key){if(!Config.Keys.TryGetValue("Folder timeline",out var keys)||!keys.Contains(key))return false;Config.SlideTimeline=!Config.SlideTimeline;Config.Save();timeline38?.InvalidateVisual();return true;}
 void ViewerSpace39(){if(slideshow||!currentMotion&&current!=null){if(!slideshow){Config.SlideshowTimer=true;Config.Save();}ChangeSlideshow();}else if(videoActive)video?.Pause();else if(animation!=null)animation.Playing=!animation.Playing;}
}
