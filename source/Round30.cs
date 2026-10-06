using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
namespace gazegallery;

// A single adjustable queue for image decoding. In-flight jobs retain their permits
// when the limit changes; lowering it never disposes a semaphore under a worker.
public static class LoaderPool {
 static readonly object sync=new();static readonly Queue<TaskCompletionSource<IDisposable>> waiting=new();static int active,limit=3;
 public static int Limit{get{lock(sync)return limit;}set{lock(sync){limit=Math.Clamp(value,1,8);Drain();}}}
 public static int Active{get{lock(sync)return active;}}
 sealed class Permit:IDisposable {int released;public void Dispose(){if(Interlocked.Exchange(ref released,1)!=0)return;lock(sync){active--;Drain();}}}
 static void Drain(){while(active<limit&&waiting.Count>0){active++;waiting.Dequeue().SetResult(new Permit());}}
 public static async Task<T> Run<T>(Func<T> work){using var permit=await Acquire();return await Task.Run(work);}
 public static Task<IDisposable> Acquire(){lock(sync){if(active<limit&&waiting.Count==0){active++;return Task.FromResult<IDisposable>(new Permit());}var t=new TaskCompletionSource<IDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);waiting.Enqueue(t);return t.Task;}}
}
public static class AssociationRegistration {
 public static readonly string[] Extensions={".jpg",".jpeg",".png",".gif",".webm",".mp4",".bmp",".webp",".svg",".heic",".heif",".avif",".jp2",".tif",".tiff",".ico"};
 public static string[] AvailableExtensions()=>Extensions.Concat(PluginCatalog.Decoders.Where(d=>PluginCatalog.Enabled(d.Id)).SelectMany(d=>d.Decoder.Extensions)).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray();
 public static string Command(string exe)=>"\""+Path.GetFullPath(exe)+"\" \"%1\"";
 public static void Register(string exe,IEnumerable<string> extensions){
  string[] selected=extensions.Where(AvailableExtensions().Contains).Distinct().ToArray();
  using var app=Registry.CurrentUser.CreateSubKey(@"Software\gazegallery\Capabilities");app.SetValue("ApplicationName","gazegallery");app.SetValue("ApplicationDescription","Image and video viewing, sorting and collages");
  using(var associations=app.CreateSubKey("FileAssociations")){foreach(var name in associations.GetValueNames())associations.DeleteValue(name,false);foreach(var ext in selected){string id="gazegallery"+ext;associations.SetValue(ext,id);using var type=Registry.CurrentUser.CreateSubKey(@"Software\Classes\"+id);type.SetValue("","gazegallery "+ext.TrimStart('.')+" file");using(var icon=type.CreateSubKey("DefaultIcon"))icon.SetValue("","\""+exe+"\",0");using(var command=type.CreateSubKey(@"shell\open\command"))command.SetValue("",Command(exe));using var openWith=Registry.CurrentUser.CreateSubKey(@"Software\Classes\"+ext+@"\OpenWithProgids");openWith.SetValue(id,Array.Empty<byte>(),RegistryValueKind.None);}}
  using var registered=Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications");registered.SetValue("gazegallery",@"Software\gazegallery\Capabilities");
 }
}
public partial class MainWindow {
 int boardLimit=50;public int BoardLimit=>boardLimit;bool imageWindow;Rect imageWindowRestore;WindowState imageWindowState;double oldMinWidth,oldMinHeight;Filmstrip strip=null!;
 static Brush? checker;
 public Brush CanvasBrush{get{if(Mode=="viewer"&&Config.ViewerBackground=="Auto"&&!currentMotion&&background32Color!=null)return background32Color;bool check=Mode=="viewer"?Config.ViewerBackground=="Checkerboard":Config.Checkerboard;if(!check)return Config.Oled?Brushes.Black:Ui.Canvas;if(checker==null){var group=new DrawingGroup();using(var dc=group.Open()){dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(38,38,38)),null,new Rect(0,0,16,16));var light=new SolidColorBrush(Color.FromRgb(61,61,61));dc.DrawRectangle(light,null,new Rect(0,0,8,8));dc.DrawRectangle(light,null,new Rect(8,8,8,8));}group.Freeze();var brush=new DrawingBrush(group){TileMode=TileMode.Tile,ViewportUnits=BrushMappingMode.Absolute,Viewport=new Rect(0,0,16,16),Stretch=Stretch.None};brush.Freeze();checker=brush;}return checker;}}
 bool EnsureBoardCapacity(){if(View.Board.Count<boardLimit)return true;if(boardLimit>=200){Toast("Collage limit: 200 images");return false;}if(!Ui.Confirm(this,"Collage capacity","Increase limit by 50 more? The more you add, the laggier it might get.","OK",true))return false;boardLimit+=50;Toast($"Collage capacity: {View.Board.Count}/{boardLimit}",1);return true;}
 void SetupRound30(){strip=new Filmstrip(this){VerticalAlignment=VerticalAlignment.Bottom,Visibility=Visibility.Collapsed};root.Children.Insert(1,strip);SetupMetadata();SetupRound31();if(!Config.Background32){if(Config.Checkerboard)Config.ViewerBackground="Checkerboard";Config.Background32=true;}UpdateTitle();}
 void ResetFilmstrip(){strip?.Reset();View.InvalidateVisual();}
 void RefreshRound30(){if(strip==null)return;strip.Visibility=Mode=="viewer"&&Config.Filmstrip&&!Cropping?Visibility.Visible:Visibility.Collapsed;strip.Height=Math.Clamp(Config.FilmstripSize,24,240)*1.25+16;UpdateFilmstripLayout33();strip.Margin=new Thickness(View.Margin.Left,0,0,0);RefreshMetadata();RefreshHistogram();RefreshBackground32();}
 void AnimateRound30(){if(strip?.Visibility==Visibility.Visible)strip.Animate();if(slideshow&&!currentMotion&&SlideVisualEnabled&&Mode=="viewer"&&!slidePaused)View.InvalidateVisual();}
 bool PluginShortcut(string key)=>pluginCommands.Any(c=>!Config.DisabledPlugins.Contains(c.Id)&&Config.Keys.TryGetValue("Plugin: "+c.Id+" / "+c.Title,out var keys)&&keys.Contains(key,StringComparer.OrdinalIgnoreCase));
 void AddRound30Settings(Panel panel){Ui.Check(panel,"Pause motion when window loses focus",Config.PauseMotionOnBlur,v=>Config.PauseMotionOnBlur=v);Ui.Check(panel,"Checkerboard transparency background",Config.Checkerboard,v=>{Config.Checkerboard=v;Config.ViewerBackground=v?"Checkerboard":"Solid Color";View.GridVersion++;View.InvalidateVisual();});panel.Children.Add(Ui.Label("Loader threads (1–8; image and thumbnail decoding)",12));var threads=Ui.Input(Config.LoaderThreads.ToString());threads.Width=80;threads.HorizontalAlignment=HorizontalAlignment.Left;threads.TextChanged+=(_,_)=>{if(int.TryParse(threads.Text,out int n)){Config.LoaderThreads=Math.Clamp(n,1,8);LoaderPool.Limit=Config.LoaderThreads;}};panel.Children.Add(threads);}
 UIElement AssociationSettings(){var panel=new StackPanel{Margin=new Thickness(16)};panel.Children.Add(Ui.Label("File Associations",18));panel.Children.Add(Ui.Label("Register the selected formats for this copy of gazegallery, then confirm your defaults in Windows Settings. Moving the portable executable requires registering its new location.",12));foreach(var ext in AssociationRegistration.AvailableExtensions()){Ui.Check(panel,ext,Config.AssociationTypes.Contains(ext),v=>{Config.AssociationTypes.Remove(ext);if(v)Config.AssociationTypes.Add(ext);});}var button=Ui.Button("Set",()=>{try{string exe=Path.Combine(Paths.Root,"gazegallery.exe");if(!File.Exists(exe))exe=Environment.ProcessPath!;AssociationRegistration.Register(exe,Config.AssociationTypes);Config.Save();System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:defaultapps?registeredAppUser=gazegallery"){UseShellExecute=true});}catch(Exception e){Error(e);}});button.HorizontalAlignment=HorizontalAlignment.Center;panel.Children.Add(button);return new ScrollViewer{Content=panel};}
 Task ToggleImageWindow()=>ToggleImageWindowCore();
 async Task ToggleImageWindowCore(){if(imageWindow){imageWindow=false;MinWidth=oldMinWidth;MinHeight=oldMinHeight;WindowStyle=WindowStyle.SingleBorderWindow;ApplyBorderlessChrome36(false);WindowState=WindowState.Normal;Left=imageWindowRestore.X;Top=imageWindowRestore.Y;Width=imageWindowRestore.Width;Height=imageWindowRestore.Height;WindowState=imageWindowState;View.Fit();return;}
  string? file=ExternalActionImage;if(file==null)return;if(Mode=="grid"){await Open(file);if(current!=file)return;}if(View.Image==null)return;if(fullscreen)ToggleFullscreen();imageWindowRestore=WindowState==WindowState.Normal?new Rect(Left,Top,Width,Height):RestoreBounds;imageWindowState=WindowState;oldMinWidth=MinWidth;oldMinHeight=MinHeight;
  if(Config.BorderlessFitsWindow){imageWindow=true;WindowState=WindowState.Normal;WindowStyle=WindowStyle.None;ApplyBorderlessChrome36(true);View.Fit();return;}
  var monitor=System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle).WorkingArea;var dpi=VisualTreeHelper.GetDpi(this);double mw=monitor.Width/dpi.DpiScaleX,mh=monitor.Height/dpi.DpiScaleY,w=View.ImageRect.Width,h=View.ImageRect.Height;double f=Math.Min(1,Math.Min(mw*.95/w,mh*.95/h));w=Math.Max(32,w*f);h=Math.Max(32,h*f);imageWindowArea=w*h;imageWindow=true;MinWidth=MinHeight=32;WindowState=WindowState.Normal;WindowStyle=WindowStyle.None;ApplyBorderlessChrome36(true);Width=w;Height=h;Left=monitor.Left/dpi.DpiScaleX+(mw-w)/2;Top=monitor.Top/dpi.DpiScaleY+(mh-h)/2;View.Scale=Math.Min(w/View.Image.PixelWidth,h/View.Image.PixelHeight);View.Pan=new(0,0);View.ZoomUsed=false;View.InvalidateVisual();
 }
 void OpenWithMenu(){var menu=new ContextMenu();PopulateExternalMenu(menu);menu.IsOpen=true;}
 void PopulateExternalMenu(ItemsControl menu){string? file=ExternalActionImage;if(file==null)return;string ext=Path.GetExtension(file).ToLowerInvariant();if(!Config.ExternalPrograms.TryGetValue(ext,out var entries))Config.ExternalPrograms[ext]=entries=Enumerable.Range(0,3).Select(_=>new ExternalProgram()).ToList();foreach(var entry in entries){var item=new MenuItem{Header=string.IsNullOrEmpty(entry.Executable)?"Set program…":Path.GetFileNameWithoutExtension(entry.Executable)};item.Click+=(_,_)=>{if(entry.Executable.Length==0)ConfigureProgram(entry,ext);else try{LaunchExternal(entry,file);}catch(Exception e){Error(e);}};item.PreviewMouseRightButtonUp+=(_,e)=>{e.Handled=true;Ui.DismissMenus();ConfigureProgram(entry,ext);};menu.Items.Add(item);}menu.Items.Add(new Separator());var sets=new MenuItem{Header="Sets / other extensions…"};sets.Click+=(_,_)=>ExternalSetsMenu(ext);menu.Items.Add(sets);menu.Items.Add(new MenuItem{Header="Right-click to configure · "+ext,IsEnabled=false});NormalizeMenuSeparators(menu);}
}
