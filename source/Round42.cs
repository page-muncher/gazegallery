using System;using System.Linq;using System.IO;using System.Collections.Generic;using System.Windows;using System.Windows.Controls;using System.Windows.Input;using System.Windows.Media;using System.Windows.Threading;
namespace gazegallery;
public partial class MainWindow {
 readonly List<(Destination Destination,Button Button)> quickRows42=new();int quickFocus42=-1;Point quickPointer42;
 readonly DispatcherTimer spaceTimer42=new(){Interval=TimeSpan.FromMilliseconds(500)};bool spaceDown42,spaceEnded42;long spaceStart42;
 void SetupRound42(){spaceTimer42.Tick+=(_,_)=>{spaceTimer42.Stop();if(spaceDown42){spaceEnded42=true;EndSlideshow();}};Deactivated+=(_,_)=>CancelSpace42();Closed+=(_,_)=>spaceTimer42.Stop();quickPanel.MouseMove+=(_,e)=>{if((e.GetPosition(root)-quickPointer42).Length>1){quickPanel.ForceCursor=false;quickPanel.Cursor=null;}};}
 void CancelSpace42(){spaceTimer42.Stop();spaceDown42=false;spaceEnded42=false;}
 void BeginSpace42(){if(spaceDown42)return;spaceDown42=true;spaceEnded42=false;spaceStart42=Environment.TickCount64;spaceTimer42.Start();}
 void ReleaseSpace42(){if(!spaceDown42)return;spaceTimer42.Stop();bool ended=spaceEnded42||Environment.TickCount64-spaceStart42>=500;spaceDown42=false;if(ended){if(!spaceEnded42)EndSlideshow();}else if(Mode=="viewer"&&!Cropping&&!Quick)ViewerSpace39();spaceEnded42=false;}
 bool QuickKeys42(Key key){if(!Quick||key is not (Key.Up or Key.Down or Key.Enter))return false;if(quickRows42.Count==0)return true;if(key==Key.Enter){if(quickFocus42>=0)Run(()=>QuickTransfer(quickRows42[quickFocus42].Destination.Folder));return true;}quickFocus42=quickFocus42<0?0:(quickFocus42+(key==Key.Down?1:-1)+quickRows42.Count)%quickRows42.Count;for(int i=0;i<quickRows42.Count;i++)quickRows42[i].Button.Background=i==quickFocus42?Ui.SelectionTint:Brushes.Transparent;return true;}
 string[] GridActionImages42(){if(View.Selected.Count>0)return SelectedGridImages();int i=KeyboardTile39;return i>=FolderTileCount&&i>=0&&i<GridPaths.Count&&!GridHoles.Contains(GridPaths[i])&&File.Exists(GridPaths[i])?new[]{GridPaths[i]}:Array.Empty<string>();}
 bool ThumbnailExtraKeys42(Key key,ModifierKeys mods,bool repeat){if(Mode!="grid"||Quick||mods.HasFlag(ModifierKeys.Control)||mods.HasFlag(ModifierKeys.Alt))return false;if(key is Key.OemMinus or Key.Subtract or Key.OemPlus or Key.Add){ColumnStep(key is Key.OemMinus or Key.Subtract?1:-1);return true;}if(key==Key.C&&mods==ModifierKeys.None){if(!repeat)Run(StartCollage);return true;}return false;}
}
