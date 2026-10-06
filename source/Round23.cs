using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading.Tasks;using System.Windows;using System.Windows.Controls;using System.Windows.Media.Imaging;
namespace gazegallery;
public partial class MainWindow {
 public bool GridMetadataLoading;int gridPreparation;int countdownSecond=-1;
 public int GridLayoutCount {get{if((!Config.Masonry&&!Config.River)||!GridMetadataLoading)return GridPaths.Count;int n=FolderTileCount;while(n<GridPaths.Count&&GridRatios.ContainsKey(GridPaths[n]))n++;return n;}}
 static Dictionary<string,double> ReadGridRatios(string[] paths){var result=new System.Collections.Concurrent.ConcurrentDictionary<string,double>(StringComparer.OrdinalIgnoreCase);Task.WhenAll(paths.Select(async path=>{using var permit=await LoaderPool.Acquire();result[path]=await Task.Run(()=>{try{if(VideoThumbnails.IsVideo(path))return 1d;if(ExtendedImages.Supports(path))return ExtendedImages.Ratio(path);using var codec=SkiaSharp.SKCodec.Create(path);return codec==null?1:codec.Info.Height/(double)Math.Max(1,codec.Info.Width);}catch{return 1d;}});})).GetAwaiter().GetResult();return new Dictionary<string,double>(result,StringComparer.OrdinalIgnoreCase);}
 async Task PrepareProgressiveGrid(){
  int preparation=++gridPreparation,token=modeTransition;string folder=Folder;GridMetadataLoading=true;RebuildGridPaths();var paths=GridPaths.Skip(FolderTileCount).ToArray();View.LayoutTiles();
  // Publish small batches, adapting to slow storage. Append geometry without relaying out existing rows.
  int first=Math.Min(paths.Length,Math.Max(16,Array.IndexOf(paths,current??"")+1));
  var initial=await Task.Run(()=>ReadGridRatios(paths.Take(first).ToArray()));
  if(preparation!=gridPreparation||token!=modeTransition||folder!=Folder)return;
  foreach(var pair in initial)GridRatios[pair.Key]=pair.Value;GridLoading=false;View.AppendTiles();PreloadThumbs();
  if(first>=paths.Length){GridMetadataLoading=false;return;}
  Run(async()=>{try{int batchSize=32;for(int i=first;i<paths.Length;){
   if(preparation!=gridPreparation||token!=modeTransition||folder!=Folder||Mode!="grid")return;
   int take=Math.Min(batchSize,paths.Length-i);var clock=System.Diagnostics.Stopwatch.StartNew();
   var batch=await Task.Run(()=>ReadGridRatios(paths.Skip(i).Take(take).ToArray()));
   if(preparation!=gridPreparation||token!=modeTransition||folder!=Folder||Mode!="grid")return;
   foreach(var pair in batch)GridRatios[pair.Key]=pair.Value;View.AppendTiles();PreloadThumbs();i+=take;
   batchSize=clock.ElapsedMilliseconds>200?4:clock.ElapsedMilliseconds<30?64:16;
   await Task.Delay(8);
  }}finally{if(preparation==gridPreparation&&token==modeTransition){GridMetadataLoading=false;View.AppendTiles();}}});
 }
 public static Rect ClampCropArea(Rect area,BitmapSource source){if(area.IsEmpty)return Rect.Empty;return Rect.Intersect(area,new Rect(0,0,source.PixelWidth,source.PixelHeight));}
 static BitmapSource CropWithinImage(BitmapSource source,Rect area){var clipped=ClampCropArea(area,source);if(clipped.IsEmpty)throw new IOException("Crop must overlap the image");int x=(int)Math.Floor(clipped.Left),y=(int)Math.Floor(clipped.Top),right=Math.Min(source.PixelWidth,(int)Math.Ceiling(clipped.Right)),bottom=Math.Min(source.PixelHeight,(int)Math.Ceiling(clipped.Bottom));var result=new CroppedBitmap(source,new Int32Rect(x,y,Math.Max(1,right-x),Math.Max(1,bottom-y)));result.Freeze();return result;}
 Rect ViewerCropArea()=>sourceImage==null?Rect.Empty:Config.CropFillOutside?View.CropRect:ClampCropArea(View.CropRect,sourceImage);
 void ConstrainViewerCrop(){if(Cropping&&!Config.CropFillOutside)View.CropRect=ViewerCropArea();View.InvalidateVisual();}
 void AddStartupSettings(Panel panel){panel.Children.Add(new Separator());panel.Children.Add(Ui.Label("Startup",15));var label=Ui.Label(Config.StartupFolder.Length==0?"Startup Page":Config.StartupFolder,12);label.TextWrapping=TextWrapping.Wrap;panel.Children.Add(label);var row=new WrapPanel();var empty=Ui.Button("Startup Page",()=>{Config.StartupFolder="";Config.Save();label.Text="Startup Page";});row.Children.Add(empty);row.Children.Add(Ui.Button("Choose default folder…",()=>{var dialog=new Microsoft.Win32.OpenFolderDialog{Title="Default startup folder"};if(dialog.ShowDialog(this)==true){Config.StartupFolder=dialog.FolderName;Config.Save();label.Text=Config.StartupFolder;}}));var mode=new ComboBox{ItemsSource=new[]{"View mode","Thumbnail mode"},SelectedIndex=Config.StartupThumbnails?1:0,Margin=new Thickness(5)};mode.SelectionChanged+=(_,_)=>{Config.StartupThumbnails=mode.SelectedIndex==1;Config.Save();};row.Children.Add(Ui.Label("Start in",12));row.Children.Add(mode);panel.Children.Add(row);}
}
