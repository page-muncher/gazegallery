using System;using System.IO;using System.IO.Pipes;using System.Linq;using System.Text.Json;using System.Threading;using System.Threading.Tasks;using System.Collections.Generic;
namespace gazegallery;
public static class LaunchBatch42 {
 // The leader can open its first file while Explorer's other processes join.
 // Keep mutex acquisition and release on the same background thread.
 public static Task<string[]?> Begin(string[] paths, string? identity = null, int quietMs = 300)
 {
    var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var result = new TaskCompletionSource<string[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
    var worker = new Thread(() =>
    {
        try { result.SetResult(CollectCore(paths, identity, quietMs, () => ready.TrySetResult())); }
        catch (Exception) { result.SetResult(paths); }
        finally { ready.TrySetResult(); }
    }) { IsBackground = true, Name = "gazegallery launch collection" };
    worker.Start();
    ready.Task.GetAwaiter().GetResult();
    return result.Task;
 }

 public static string[]? Collect(string[] paths, string? identity = null, int quietMs = 300) =>
     CollectCore(paths, identity, quietMs, null);

 private static string[]? CollectCore(string[] paths,string? identity,int quietMs,Action? leaderReady){
  string id=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity??((Environment.ProcessPath??"")+Environment.UserName))))[..24];string name="gazegallery-launch-"+id;
  using var mutex=new Mutex(false,"Local\\"+name);bool leader;try{leader=mutex.WaitOne(0);}catch(AbandonedMutexException){leader=true;}
  if(!leader){try{using var client=new NamedPipeClientStream(".",name,PipeDirection.InOut,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);client.Connect(2000);using var writer=new StreamWriter(client,new System.Text.UTF8Encoding(false),1024,true){AutoFlush=true};writer.WriteLine(JsonSerializer.Serialize(paths));using var reader=new StreamReader(client,System.Text.Encoding.UTF8,false,1024,true);if(reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult()=="OK")return null;}catch{}return paths;}
  try{leaderReady?.Invoke();return Task.Run(async()=>{var list=new List<string>(paths);var limit=DateTime.UtcNow.AddSeconds(3);while(DateTime.UtcNow<limit){using var server=new NamedPipeServerStream(name,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);using var timeout=new CancellationTokenSource(Math.Max(1,Math.Min(quietMs,(int)(limit-DateTime.UtcNow).TotalMilliseconds)));try{await server.WaitForConnectionAsync(timeout.Token);using var reader=new StreamReader(server,System.Text.Encoding.UTF8,false,1024,true);string? line=await reader.ReadLineAsync(timeout.Token);var files=JsonSerializer.Deserialize<string[]>(line??"[]")??Array.Empty<string>();list.AddRange(files.Where(File.Exists));using var writer=new StreamWriter(server,new System.Text.UTF8Encoding(false),1024,true){AutoFlush=true};await writer.WriteLineAsync("OK");}catch(OperationCanceledException){break;}catch(IOException){}}return list.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();}).GetAwaiter().GetResult();}finally{mutex.ReleaseMutex();}
 }
}

public partial class MainWindow
{
    private async Task OpenLaunchPath(string firstFile, Task<string[]?>? batch)
    {
        var opening = Open(firstFile);
        int transition = modeTransition;
        int media = generation;
        await opening;
        if (batch == null)
            return;
        var files = await batch;
        if (files is not { Length: > 1 } || shutdown || transition != modeTransition ||
            media != generation || Mode != "viewer")
            return;
        collageReturnMode = "viewer";
        collageOrigin = files[0];
        await SetMode("board");
        if (Mode == "board")
            await ImportBoard(files);
    }
}

