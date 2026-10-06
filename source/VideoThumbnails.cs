using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
namespace gazegallery;
// A single silent decoder, separate from viewer playback and the still-image workers.
// Only one small frame is retained. The bounded disk cache is invalidated by file metadata.
public static class VideoThumbnails {
 static readonly SemaphoreSlim worker=new(1);
 static DateTime lastPrune;
 public static bool IsVideo(string path)=>Path.GetExtension(path).ToLowerInvariant() is ".mp4" or ".webm";
 public static async Task<BitmapSource?> Get(string path,int size,Func<bool> valid){
  await worker.WaitAsync();try{
   if(!valid())return null;
   var info=new FileInfo(path);string directory=Path.Combine(Paths.Data,"video-thumbnails");
   string key=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path+"|"+info.Length+"|"+info.LastWriteTimeUtc.Ticks+"|"+size)));
   string cache=Path.Combine(directory,key+".png");
   if(File.Exists(cache)){try{return ImageCache.Read(cache,size);}catch{File.Delete(cache);}}
   using var player=new Video(true,Math.Clamp(size,160,960));
   player.Play(path,true,0,false);var watch=System.Diagnostics.Stopwatch.StartNew();WriteableBitmap? frame=null;
   while(watch.Elapsed.TotalSeconds<8&&valid()){
    if(player.Update(ref frame)&&frame!=null){var image=frame.Clone();image.Freeze();Directory.CreateDirectory(directory);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var stream=File.Create(cache))encoder.Save(stream);
     if((DateTime.UtcNow-lastPrune).TotalMinutes>1){lastPrune=DateTime.UtcNow;var files=new DirectoryInfo(directory).GetFiles("*.png").OrderByDescending(f=>f.LastWriteTimeUtc).ToArray();long total=0;foreach(var file in files){total+=file.Length;if(total>128L*1024*1024)try{file.Delete();}catch{}}}
     return image;
    }await Task.Delay(20);
   }return null;
  }finally{worker.Release();}
 }
}
