using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using gazegallery.PluginApi;
namespace gazegallery;
public partial class MainWindow {
 async Task VerifyExpansion(List<object> report,Action<bool,string> check,string work){
  var fresh=new Settings();check(fresh.Columns==5&&!fresh.Names,"Fresh profile defaults: five columns and no names");
  Config.Save();var first=Settings.Load();var second=Settings.Load();first.Global[0].Folder=Path.Combine(work,"destination-a");first.Save();second.Columns=7;second.Save();var merged=Settings.Load();check(merged.Global[0].Folder==first.Global[0].Folder&&merged.Columns==7,"Concurrent settings save preserves another instance's destinations");
  string seed=Path.Combine(Paths.Test,"small_mario.png");await Open(seed);check(plugins.Any(p=>p.Id=="gazegallery.simple-draw"),"Example drawing plugin loads");var command=pluginCommands.First(c=>c.Id=="gazegallery.simple-draw");command.Run();check(pluginOverlay!=null&&pluginTools!=null,"Plugin opens drawing directly over viewer");EndPluginOverlay();check(pluginOverlay==null,"Plugin cancellation removes overlay");
  string folder=Path.Combine(work,"crop-navigation");Directory.CreateDirectory(folder);for(int i=0;i<3;i++){string name=Path.Combine(folder,$"item-{i}.png");File.Copy(seed,name);File.SetLastWriteTimeUtc(name,DateTime.UtcNow.AddMinutes(-i-5));}await Open(Path.Combine(folder,"item-1.png"));string next=Files[(Index+1)%Files.Count];var original=File.ReadAllBytes(current!);StartCrop();int width=View.Image!.PixelWidth,height=View.Image.PixelHeight;TransformCrop("Rotate 90°");check(View.Image.PixelWidth==height&&View.Image.PixelHeight==width,"Crop rotation changes working pixels");check(View.CropRect.IsEmpty,"Crop transform clears selection");View.CropRect=new Rect(0,0,View.Image.PixelWidth,View.Image.PixelHeight);await ApplyCrop();check(ImageCache.Read(current!,0).PixelWidth==height,"Crop Apply saves rotated pixels");await NavigateViewer(1);check(current==next,"After crop Next uses original neighbor");await Undo();check(File.ReadAllBytes(Path.Combine(folder,"item-1.png")).SequenceEqual(original),"Crop transform undo restores exact original");
  await Open(folder);await SetMode("grid");View.Selected.Clear();View.Selected.Add(0);NavigateSelection(1);check(View.Selected.SetEquals(new[]{1}),"Thumbnail Next advances one selection");string chosen=GridPaths[1];await SetMode("viewer");check(current==chosen,"Returning from thumbnails opens selected image");
  var giant=Directory.GetFiles(Path.Combine(Paths.Test,"bigimage")).First(Paths.Media);var clock=Stopwatch.StartNew();var raster=await Task.Run(()=>LargeRaster.Open(giant));check(raster!=null&&raster.Width==65000&&raster.Height==6469,"Panorama native dimensions retained");check(raster!.Preview.PixelWidth<=8192,"Panorama overview is bounded");report.Add(new{metric="panorama_overview_ms",value=clock.Elapsed.TotalMilliseconds});clock.Restart();var detail=await Task.Run(()=>raster.Region(new Rect(30000,2400,1920,1200),1,default));check(detail.Image.PixelWidth==1920&&detail.Image.PixelHeight==1200,"Panorama full-resolution viewport decodes without full bitmap");report.Add(new{metric="panorama_detail_ms",value=clock.Elapsed.TotalMilliseconds,workingSetMB=Process.GetCurrentProcess().WorkingSet64/1048576});
  await Open(giant);check(View.IsFit&&largeRaster!=null,"Panorama opens centered and fitted");await Open(seed);await VerifyLatest21(report,check,work);
 }
}
