using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
namespace gazegallery;
public partial class MainWindow {
 async Task VerifyPlayback(){var report=new List<object>();verifyNoLoop=true;try{foreach(var path in Directory.GetFiles(Path.Combine(Paths.Test,"troublesome")).Where(Paths.Motion)){Paths.Log("playback-check-start",Path.GetFileName(path));await Open(path);var timer=Stopwatch.StartNew();while(video!=null&&!video.Ended&&timer.Elapsed.TotalSeconds<35)await Task.Delay(10);report.Add(new{file=Path.GetFileName(path),ended=video?.Ended,decoded=video?.DecodedFrames,elapsed=timer.Elapsed.TotalMilliseconds,frames=video?.PresentationSamples.ToArray(),ids=video?.PresentationFrameIds.ToArray()});File.WriteAllText(Path.Combine(Paths.Logs,"playback-check.json"),System.Text.Json.JsonSerializer.Serialize(report));await StopVideo();}}catch(Exception ex){Paths.Log("playback-check-error",ex.ToString());}finally{shutdown=true;Close();}}
}
