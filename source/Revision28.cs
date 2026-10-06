using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
namespace gazegallery;
public partial class MainWindow {
 long lastRightClick; System.Drawing.Point lastRightPoint;
 bool TryDoubleRight(){
  if(Mode!="viewer"||Cropping||pluginOverlay!=null)return false;
  var point=System.Windows.Forms.Cursor.Position;long now=Environment.TickCount64;
  var size=System.Windows.Forms.SystemInformation.DoubleClickSize;
  bool twice=lastRightClick!=0&&now-lastRightClick<=System.Windows.Forms.SystemInformation.DoubleClickTime&&Math.Abs(point.X-lastRightPoint.X)<=size.Width&&Math.Abs(point.Y-lastRightPoint.Y)<=size.Height;
  lastRightClick=twice?0:now;lastRightPoint=point;
  if(twice)Run(()=>SetMode("grid"));return twice;
 }
 static void PreferRightSubmenu(MenuItem item){item.Loaded+=(_,_)=>{
  item.ApplyTemplate();if(item.Template.FindName("PART_Popup",item) is not Popup popup)return;
  popup.Placement=PlacementMode.Custom;
  popup.CustomPopupPlacementCallback=(size,target,offset)=>new[]{new CustomPopupPlacement(new Point(target.Width,0),PopupPrimaryAxis.Vertical),new CustomPopupPlacement(new Point(-size.Width,0),PopupPrimaryAxis.Vertical)};
 };}
 void CenteredFolderToast(string path){Toast(path,2);toast.VerticalAlignment=VerticalAlignment.Center;toast.Margin=new Thickness(20);toast.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(.25,1,TimeSpan.FromMilliseconds(180)){AutoReverse=false});}
 readonly StackPanel tempGridMessage=new(){HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,MaxWidth=620,Visibility=Visibility.Collapsed};
 void SetupTempGridMessage(){foreach(var text in new[]{"This is a temp folder for drag and dropping compatability on a fresh start","It's recommended to change the startup page to start in a real folder"}){var label=Ui.Label(text,12);label.TextAlignment=TextAlignment.Center;tempGridMessage.Children.Add(label);}tempGridMessage.Children.Add(Ui.Button("Pick a folder to start in on launch",()=>{var picker=new OpenFolderDialog{Title="Pick a startup folder"};if(picker.ShowDialog(this)!=true)return;Config.StartupFolder=picker.FolderName;Config.StartupThumbnails=true;Config.Save();Run(()=>NavigateFolder(picker.FolderName));}));root.Children.Add(tempGridMessage);}
 async Task PasteBoard(){if(clipBusy||importsRunning||BoardCropItem!=null)return;clipBusy=true;try{
  if(System.Windows.Clipboard.ContainsFileDropList()){await ImportBoard(System.Windows.Clipboard.GetFileDropList().Cast<string>().Where(File.Exists));return;}
  if(!System.Windows.Clipboard.ContainsImage()){Toast("Clipboard contains no supported image");return;}
  if(!EnsureBoardCapacity())return;
  var image=System.Windows.Clipboard.GetImage();if(image==null)return;image=image.Clone();image.Freeze();
  double nw=image.PixelWidth,nh=image.PixelHeight,factor=Math.Min(1,Math.Min(View.ActualWidth*.6/nw,View.ActualHeight*.6/nh)/View.BoardScale);
  var center=View.BoardPoint(new Point(View.ActualWidth/2,View.ActualHeight/2));double width=nw*factor,height=nh*factor;
  View.Snapshot();View.BoardSelection.Clear();View.BoardSelected=-1;
  View.Board.Add(new BoardItem{Image=image,Edited=true,NaturalWidth=nw,NaturalHeight=nh,ImportOrder=++importSequence,Rect=new Rect(center.X-width/2,center.Y-height/2,width,height)});
  View.InvalidateVisual();Toast($"Added clipboard image — {View.Board.Count}/{boardLimit} images");
 }catch(Exception ex){Error(ex);}finally{clipBusy=false;}}
 public void ToggleQuick(){
  if(Mode=="board"||Cropping)return;Quick=!Quick;quickPanel.Visibility=Quick?Visibility.Visible:Visibility.Collapsed;if(!Quick)return;
  quickPanel.Background=new SolidColorBrush(Color.FromArgb(230,34,38,42));
  var destinations=Config.Destinations(Folder);var outer=new StackPanel{Margin=new Thickness(4)};
  Button Small(string label,Action click){var button=Ui.FlatButton(label,click);if(label is "ONE-TIME" or "◂" or "▸" or "×")button.Tag=null;button.FontSize=10;button.Padding=new Thickness(4,2,4,2);button.Margin=new Thickness(1);return button;}
  var header=new DockPanel();var close=Small("×",ToggleQuick);DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);
  if(Config.QuickExpanded){var sets=Small("Sets",DestinationSetMenu);DockPanel.SetDock(sets,Dock.Right);header.Children.Add(sets);}
  var titles=new StackPanel{Orientation=Orientation.Horizontal};titles.Children.Add(Ui.Label("Universal",10));
  titles.Children.Add(Small(Config.QuickExpanded?"◂":"▸",()=>{Config.QuickExpanded=!Config.QuickExpanded;Config.Save();RefreshQuickPanel();}));
  if(Config.QuickExpanded){var bank=Small(Config.QuickSecondUniversal?"Universal 2":"Location specific",()=>{Config.QuickSecondUniversal=!Config.QuickSecondUniversal;Config.Save();RefreshQuickPanel();});bank.ToolTip="Switch between a second universal set and this folder’s set";titles.Children.Add(bank);var swapHint=Ui.Label("Click to swap",10);swapHint.Opacity=.4;swapHint.VerticalAlignment=VerticalAlignment.Center;titles.Children.Add(swapHint);}
  header.Children.Add(titles);outer.Children.Add(header);
  var row=new Grid();
  for(int group=0;group<(Config.QuickExpanded?2:1);group++){
   row.ColumnDefinitions.Add(new ColumnDefinition());var column=new StackPanel{MinWidth=Config.QuickExpanded?176:242};Grid.SetColumn(column,group);
   for(int i=group*10;i<group*10+10;i++){int slot=i;var destination=destinations[i];var button=Small("",()=>{if(string.IsNullOrEmpty(destination.Folder))EditDestination(slot);else Run(()=>QuickTransfer(destination.Folder));});
    var content=new Grid();content.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(30)});content.ColumnDefinitions.Add(new ColumnDefinition());
    content.Children.Add(new TextBlock{Text=FriendlyKey(destination.Key),Foreground=Ui.Fg});var label=new TextBlock{Text=string.IsNullOrEmpty(destination.Folder)?"Set destination…":Path.GetFileName(destination.Folder.TrimEnd('\\')),Foreground=Ui.Fg,TextTrimming=TextTrimming.CharacterEllipsis};Grid.SetColumn(label,1);content.Children.Add(label);
    button.Content=content;button.HorizontalAlignment=HorizontalAlignment.Stretch;button.FontSize=11;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.ToolTip=destination.Folder;button.PreviewMouseRightButtonUp+=(_,e)=>{e.Handled=true;EditDestination(slot);};column.Children.Add(button);
   }row.Children.Add(column);
  }outer.Children.Add(row);
  var actions=new StackPanel{Orientation=Orientation.Horizontal};quickModeButton=Small(quickCopy?"COPY":"MOVE",ToggleQuickCopy);actions.Children.Add(quickModeButton);var tabHint=Ui.Label("Tab to swap",10);tabHint.Opacity=.4;actions.Children.Add(tabHint);var hints=Ui.Label($"Trash: {string.Join(" / ",Config.Keys["Trash"].Select(FriendlyKey))}    Undo: {string.Join(" / ",Config.Keys["Undo"].Select(FriendlyKey))}",10);hints.Opacity=.6;actions.Children.Add(hints);outer.Children.Add(actions);
  var footer=new DockPanel();var once=Small("ONE-TIME",()=>{var picker=new OpenFolderDialog{InitialDirectory=DestinationStart()};if(picker.ShowDialog(this)==true)Run(()=>MoveSelection(picker.FolderName));});DockPanel.SetDock(once,Dock.Right);footer.Children.Add(once);var hint=Ui.Label("right click to set",10);hint.Opacity=.6;footer.Children.Add(hint);outer.Children.Add(footer);
  quickPanel.Child=outer;View.Cursor=Cursors.Arrow;
 }
}
