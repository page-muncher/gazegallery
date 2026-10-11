using System;
using System.IO;
using System.Text.Json;
using System.Threading;
namespace gazegallery;
// Headless diagnostic: exercises the real portable executable and native decoder.
public static class VideoRuntimeCheck {
 public static int Run(string report,string? media){
  bool ok=false;long frames=0;string error="";
  try{using var video=new Video(true,320);if(media!=null){video.Play(Path.GetFullPath(media),true,0,false);var end=DateTime.UtcNow.AddSeconds(15);while(video.DecodedFrames==0&&DateTime.UtcNow<end)Thread.Sleep(20);frames=video.DecodedFrames;if(frames==0)throw new IOException("No video frame decoded within 15 seconds.");}ok=true;}catch(Exception ex){error=ex.ToString();}
  File.WriteAllText(report,JsonSerializer.Serialize(new{ok,baseDirectory=AppContext.BaseDirectory,root=Paths.Root,process=Environment.ProcessPath,frames,error},new JsonSerializerOptions{WriteIndented=true}));return ok?0:1;
 }
}
