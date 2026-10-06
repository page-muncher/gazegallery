using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace gazegallery;

public static class Paths {
 public static readonly string Root = Path.GetFullPath(AppContext.BaseDirectory);
 public static readonly string DevelopmentRoot=Directory.Exists(Path.Combine(Root,"..","source"))?Path.GetFullPath(Path.Combine(Root,"..")):Root;
 public static readonly string Test = Path.Combine(DevelopmentRoot,"TestFiles");
 public static readonly string Data = Environment.GetCommandLineArgs().Any(a=>a is "--verify" or "--playback-check" or "--smoke")?Path.Combine(Root,"diagnostics","test-profile"):Path.Combine(Root,"data");
 public static readonly string Logs = Path.Combine(Root,"diagnostics");
 public static void Init(){Directory.CreateDirectory(Data);if(!Environment.GetCommandLineArgs().Any(a=>a.StartsWith("--"))&&!File.Exists(Path.Combine(Data,"settings.json"))&&DevelopmentRoot!=Root){string legacy=Path.Combine(DevelopmentRoot,"data","settings.json");if(File.Exists(legacy))File.Copy(legacy,Path.Combine(Data,"settings.json"));}Directory.CreateDirectory(Logs);Directory.CreateDirectory(Path.Combine(Data,"recovery"));}
 // Validate file targets; normal user-operated folders are now supported.
 public static void Guard(string path){var full=Path.GetFullPath(path);if(string.IsNullOrWhiteSpace(Path.GetFileName(full)))throw new IOException("Choose a file path, not a drive root.");}
 public static bool Media(string p)=>new[]{".jpg",".jpeg",".png",".gif",".mp4",".webm",".bmp",".webp"}.Contains(Path.GetExtension(p).ToLowerInvariant())||ExtendedImages.Supports(p)||PluginCatalog.Supports(p);
 public static bool Motion(string p)=>(new[]{".gif",".mp4",".webm"}.Contains(Path.GetExtension(p).ToLowerInvariant())||WebImages.Animated(p));
 public static void Log(string kind,object value){try{lock(typeof(Paths))File.AppendAllText(Path.Combine(Logs,"events.jsonl"),JsonSerializer.Serialize(new{time=DateTime.UtcNow,kind,value})+Environment.NewLine);}catch{}}
}
public class Destination { public string Folder{get;set;}="";public string Key{get;set;}=""; }
public class Settings {
 public bool PauseMotionOnBlur{get;set;}=false;public bool FilmstripShrink{get;set;}=false;public List<string> HiddenOverlays{get;set;}=new(){"Metadata","Histogram"};public string ViewerBackground{get;set;}="Solid Color";public bool Background32{get;set;}=false;public double FilmstripSize{get;set;}=100;public System.Collections.Generic.List<string> ViewerTitle{get;set;}=new(){"Image position","Filename","File extension","Dimensions","File size","Folder name","Program name"};public System.Collections.Generic.List<string> CollageTitle{get;set;}=new(){"Image count","Collage","Program name"};public bool Grayscale{get;set;}=false;public bool Histogram{get;set;}=false;public bool HistogramRed{get;set;}=true;public bool HistogramGreen{get;set;}=true;public bool HistogramBlue{get;set;}=true;public bool HistogramGray{get;set;}=true;public System.Collections.Generic.List<string> SlideBackgrounds{get;set;}=new(){"Plain"};public System.Collections.Generic.List<string> SlideTransitions{get;set;}=new(){"Soft Zoom Fade"};public System.Collections.Generic.List<string> SlideViews{get;set;}=new(){"None"};public bool Effects31{get;set;}=false;public int LoaderThreads{get;set;}=3;public bool Checkerboard{get;set;}=false;public bool Filmstrip{get;set;}=false;public int FilmstripCount{get;set;}=15;public bool MetadataOverlay{get;set;}=false;public string SlideEffect{get;set;}="Plain";public bool SlideTimeline{get;set;}=false;public List<string> AssociationTypes{get;set;}=new();public bool Menu30{get;set;}=false;
 public bool OverlayMenu26{get;set;}=false;public bool WheelNavigation{get;set;}=false;public bool TimerAutoHide{get;set;}=false;public bool CropRatioLock{get;set;}=false;public double CropRatio{get;set;}=1;public double CollageGap{get;set;}=8;public int DefaultVolume{get;set;}=25;public Dictionary<string,Dictionary<string,string>> CustomPalettes{get;set;}=new();public bool PositionIndicator{get;set;}public string StartupFolder{get;set;}="";public bool StartupThumbnails{get;set;}=true;public bool HideQuickStart{get;set;}=false;public bool ConfirmEscape{get;set;}=true;public bool AutoHideCursor{get;set;}=true;public bool SlideshowTimer{get;set;}=false;public string DefaultFit{get;set;}="Fit window";public Dictionary<string,List<ExternalProgram>> ExternalSets{get;set;}=new();public bool CropFillOutside{get;set;}=false;public bool StartFullscreen{get;set;} public bool SiblingNavigation{get;set;} public bool ZoomIndicator{get;set;} public string SmoothFilter{get;set;}="Bicubic"; public string LastDestination{get;set;}=""; public Dictionary<string,List<Destination>> DestinationSets{get;set;}=new(); public Dictionary<string,List<ExternalProgram>> ExternalPrograms{get;set;}=new(); public Dictionary<string,string> Colors{get;set;}=new(); public List<string> DisabledPlugins{get;set;}=new(); public bool Oled{get;set;} public bool CopyAliasMigration{get;set;} public bool DailyMenuMigration{get;set;} public bool InputMigration{get;set;}=false;public bool FadeCollage{get;set;}=false;public bool ShowSubfolders{get;set;}=true;public bool FullscreenExit{get;set;}=true;public double SlideSeconds{get;set;}=8;public double ScrollSpeed{get;set;}=30;public Dictionary<string,List<string>> MenuLayouts{get;set;}=new();
 public bool CompactMenuMigrated{get;set;}=false;public bool FadeToolbar{get;set;}=false;public bool RespectAspect{get;set;}=true;public List<string> RecentFolders{get;set;}=new();
 public int Columns{get;set;}=5;public int ThumbnailPixels{get;set;}=480;
 public bool Mute{get;set;}=false;[System.Text.Json.Serialization.JsonIgnore] public int Volume{get;set;}=25;public bool TrashConfirm{get;set;}=true;public bool Arrows{get;set;}=false;public bool Nearest{get;set;}=false;public bool Masonry{get;set;}=true;public bool River{get;set;}=false;public bool BorderlessFitsWindow{get;set;}=false;public bool ThumbnailTransition{get;set;}=true;public bool Names{get;set;}=false;public double Thumb{get;set;}=230;public string Sort{get;set;}="Newest";public int CacheMB{get;set;}=384;
 public List<string> Pins{get;set;}=new(); public List<string> HiddenActions{get;set;}=new();
 public List<Destination> Global{get;set;}=Enumerable.Range(0,10).Select(i=>new Destination{Key=i==9?"D0":"D"+(i+1)}).ToList();
 public List<Destination> GlobalSecond{get;set;}=Enumerable.Range(0,10).Select(_=>new Destination()).ToList();
 public bool QuickSecondUniversal{get;set;}=true;public bool QuickExpanded{get;set;}=false;
 public Dictionary<string,List<Destination>> Local{get;set;}=new(StringComparer.OrdinalIgnoreCase);

 public Dictionary<string,string[]> Keys{get;set;}=new(){["Next"]=new[]{"F","D","Right","MouseForward"},["Previous"]=new[]{"A","S","Space","Left","MouseBack"},["Quick sort"]=new[]{"Q"},["Thumbnails"]=new[]{"Z"},["Copy"]=new[]{"Shift+C"},["Trash"]=new[]{"B"},["Undo"]=new[]{"Ctrl+Z"},["Crop"]=new[]{"C"},["Fullscreen"]=new[]{"Enter"},["Exit"]=new[]{"Escape"},["Shuffle"]=new[]{"J"},["Filtering"]=new[]{"P"},["Save as"]=new[]{"Ctrl+S"},["Show in Explorer"]=new[]{"K"},["Play / pause"]=new[]{"V"},["Mute"]=new[]{"M"},["Diagnostics"]=new[]{"F12"}};
 System.Text.Json.Nodes.JsonNode? baseline;
 public static Settings Load(){Settings value;try{value=JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path.Combine(Paths.Data,"settings.json")))??new();}catch{value=new();}value.baseline=JsonSerializer.SerializeToNode(value);return value;}
 public void Save(){foreach(var key in Local.Where(p=>p.Value.All(d=>string.IsNullOrWhiteSpace(d.Folder))).Select(p=>p.Key).ToArray())Local.Remove(key);lock(typeof(Settings)){using var mutex=new Mutex(false,"Local\\gazegallerySettings-"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Paths.Data))).Substring(0,16));bool held=false;try{try{held=mutex.WaitOne(5000);}catch(AbandonedMutexException){held=true;}if(!held)throw new IOException("Settings are busy; try again.");var p=Path.Combine(Paths.Data,"settings.json");var current=JsonSerializer.SerializeToNode(this)!;System.Text.Json.Nodes.JsonNode? disk=null;try{disk=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(p));}catch{}var merged=MergeSettings(current,baseline,disk);if(merged is System.Text.Json.Nodes.JsonObject clean){clean.Remove("Borderless");clean.Remove("Resume");}string json=merged!.ToJsonString(new JsonSerializerOptions{WriteIndented=true});if(File.Exists(p))File.Copy(p,p+".bak",true);File.WriteAllText(p+".tmp",json);File.Move(p+".tmp",p,true);baseline=current.DeepClone();}finally{if(held)mutex.ReleaseMutex();}}}
 static System.Text.Json.Nodes.JsonNode? MergeSettings(System.Text.Json.Nodes.JsonNode? value,System.Text.Json.Nodes.JsonNode? before,System.Text.Json.Nodes.JsonNode? disk){if(System.Text.Json.Nodes.JsonNode.DeepEquals(value,before))return disk?.DeepClone()??value?.DeepClone();if(value is System.Text.Json.Nodes.JsonArray array&&before is System.Text.Json.Nodes.JsonArray oldArray&&disk is System.Text.Json.Nodes.JsonArray diskArray&&array.Count==oldArray.Count&&array.Count==diskArray.Count){var result=new System.Text.Json.Nodes.JsonArray();for(int i=0;i<array.Count;i++)result.Add(MergeSettings(array[i],oldArray[i],diskArray[i]));return result;}if(value is System.Text.Json.Nodes.JsonObject obj&&before is System.Text.Json.Nodes.JsonObject old){var result=disk?.DeepClone() as System.Text.Json.Nodes.JsonObject??new();foreach(var key in obj.Select(x=>x.Key).Union(old.Select(x=>x.Key)).ToArray()){if(!obj.ContainsKey(key)){result.Remove(key);continue;}result[key]=MergeSettings(obj[key],old[key],result[key]);}return result;}return value?.DeepClone();}
 public List<Destination> LocalDestinations(string folder){if(!Local.ContainsKey(folder))Local[folder]=Enumerable.Range(0,10).Select(_=>new Destination()).ToList();return Local[folder];}
 public List<Destination> Destinations(string folder)=>Global.Concat(QuickSecondUniversal?GlobalSecond:LocalDestinations(folder)).ToList();
 public void SetSecondDestinations(string folder,List<Destination> items){if(QuickSecondUniversal)GlobalSecond=items;else Local[folder]=items;}
}
public sealed class ImageCache {
 record Entry(BitmapSource Image,long Bytes);
 readonly Dictionary<string,Entry> items=new();readonly LinkedList<string> lru=new();readonly Dictionary<string,Task<BitmapSource>> pending=new();readonly object sync=new();
 public long Bytes{get;private set;}public int Count{get{lock(sync)return items.Count;}}public int Hits,Misses;public long Budget;
 public ImageCache(long budget){Budget=budget;}
 static string Id(string path,int size){var f=new FileInfo(path);return path+"|"+f.Length+"|"+f.LastWriteTimeUtc.Ticks+"|"+size;}
 public Task<BitmapSource> Get(string path,int max=2048,Func<bool>? valid=null){string key=Id(path,max);lock(sync){if(items.TryGetValue(key,out var e)){Hits++;lru.Remove(key);lru.AddLast(key);return Task.FromResult(e.Image);}if(pending.TryGetValue(key,out var t))return t;Misses++;var work=Task.Run(async()=>{using var permit=await LoaderPool.Acquire();try{
    if(valid!=null&&!valid())throw new OperationCanceledException();var watch=Stopwatch.StartNew();BitmapSource b=Read(path,max);lock(sync){long bytes=(long)b.PixelWidth*b.PixelHeight*4;items[key]=new(b,bytes);Bytes+=bytes;lru.AddLast(key);while(Bytes>Budget&&lru.Count>1){var old=lru.First!.Value;lru.RemoveFirst();if(items.Remove(old,out var prior))Bytes-=prior.Bytes;}}Paths.Log("decode",new{path=Path.GetFileName(path),max,ms=watch.Elapsed.TotalMilliseconds});return b;
   }finally{lock(sync)pending.Remove(key);}});pending[key]=work;return work;}}
 public static BitmapSource Read(string path,int max){if(ExtendedImages.Supports(path))return ExtendedImages.Read(path,max);var custom=PluginCatalog.Decode(path,max);if(custom!=null)return custom;if(Path.GetExtension(path).Equals(".webp",StringComparison.OrdinalIgnoreCase))return WebImages.Read(path,max);using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);int width=0;if(max>0){var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.DelayCreation,BitmapCacheOption.None);var frame=decoder.Frames[0];width=(int)(frame.PixelWidth*Math.Min(1,max/(double)Math.Max(frame.PixelWidth,frame.PixelHeight)));stream.Position=0;}var b=new BitmapImage();b.BeginInit();b.CacheOption=BitmapCacheOption.OnLoad;b.StreamSource=stream;if(width>0)b.DecodePixelWidth=width;b.EndInit();b.Freeze();return b;}
 public void Clear(){lock(sync){items.Clear();lru.Clear();Bytes=0;}}
}
public sealed class Natural : IComparer<string>{public int Compare(string? a,string? b)=>StrCmpLogicalW(a??"",b??"");[System.Runtime.InteropServices.DllImport("shlwapi.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern int StrCmpLogicalW(string a,string b);}
public sealed class Telemetry {
 readonly Queue<double> frames=new(); readonly Queue<double> loads=new();public double LastLoad;public bool CacheHit;public long LateFrames;public double LastFrame;public int RenderCount;
 public void Frame(double ms){LastFrame=ms;RenderCount++;if(ms>33.4)LateFrames++;frames.Enqueue(ms);while(frames.Count>240)frames.Dequeue();}
 public void Load(double ms,bool cached){LastLoad=ms;CacheHit=cached;loads.Enqueue(ms);while(loads.Count>300)loads.Dequeue();}
 public double[] Frames=>frames.ToArray();public double P95=>loads.Count==0?0:loads.Order().ElementAt(Math.Min(loads.Count-1,(int)(loads.Count*.95)));
}






