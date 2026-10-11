using System;using System.IO;using System.IO.Compression;using System.Linq;using System.Net.Http;using System.Security.Cryptography;using System.Text.Json;using System.Threading;using System.Threading.Tasks;using System.Diagnostics;using System.Windows;using System.Windows.Controls;
namespace gazegallery;
public sealed record ReleaseUpdate38(string Version,string Download,string Checksum,string Digest,long Bytes);
public static class UpdateService38 {
 public const string Latest="https://api.github.com/repos/page-muncher/gazegallery/releases/latest";
 static readonly HttpClient http=CreateClient();
 static HttpClient CreateClient(){var c=new HttpClient{Timeout=TimeSpan.FromMinutes(10)};c.DefaultRequestHeaders.UserAgent.ParseAdd("gazegallery-updater/1.0");c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");return c;}
 public static bool Newer(string tag,Version current)=>Version.TryParse(tag.TrimStart('v','V'),out var v)&&v>new Version(current.Major,current.Minor,Math.Max(0,current.Build));
 public static ReleaseUpdate38? Parse(string json,Version current){using var d=JsonDocument.Parse(json);var root=d.RootElement;if(root.GetProperty("draft").GetBoolean()||root.GetProperty("prerelease").GetBoolean())return null;string tag=root.GetProperty("tag_name").GetString()??"";if(!Newer(tag,current))return null;string name="gazegallery-"+tag.TrimStart('v','V')+"-win-x64-portable.zip";var assets=root.GetProperty("assets").EnumerateArray().ToArray();var zip=assets.FirstOrDefault(a=>a.GetProperty("name").GetString()==name);if(zip.ValueKind==JsonValueKind.Undefined)throw new IOException("The latest release has no Windows x64 portable ZIP.");var sha=assets.FirstOrDefault(a=>a.GetProperty("name").GetString()==name+".sha256.txt");string digest=zip.TryGetProperty("digest",out var hash)?hash.GetString()??"":"";string checksum=sha.ValueKind==JsonValueKind.Undefined?"":sha.GetProperty("browser_download_url").GetString()??"";if(!digest.StartsWith("sha256:")&&checksum.Length==0)throw new IOException("The update has no SHA-256 checksum.");return new(tag.TrimStart('v','V'),zip.GetProperty("browser_download_url").GetString()!,checksum,digest,zip.GetProperty("size").GetInt64());}
 public static async Task<ReleaseUpdate38?> Check(Version current,CancellationToken token=default){using var r=await http.GetAsync(Latest,token);if(r.StatusCode==System.Net.HttpStatusCode.NotFound)return null;r.EnsureSuccessStatusCode();return Parse(await r.Content.ReadAsStringAsync(token),current);}
 public static bool IsManagedPath(string path){string clean=path.Replace('\\','/');if(clean.StartsWith('/')||clean.Split('/').Any(s=>s is ".." or "." or ""||s.Contains(':')))return false;string first=clean.Split('/')[0];return clean is "gazegallery.exe" or "Readme.txt" or "Full_Guide.html" || clean.Contains('/') && first is "runtime" or "plugins" or "Support";}
 public static string ValidateEntry(string zipName){string name=zipName.Replace('\\','/');if(!name.StartsWith("gazegallery/",StringComparison.Ordinal))throw new IOException("Unexpected update archive structure.");string rel=name.Substring("gazegallery/".Length);if(!IsManagedPath(rel))throw new IOException("Unsafe or unexpected update archive entry: "+zipName);return rel;}
 public static void ExtractValidated(string archive,string destination){using var zip=ZipFile.OpenRead(archive);if(zip.Entries.Count>10000||zip.Entries.Sum(e=>e.Length)>4L*1024*1024*1024)throw new IOException("Update archive is too large.");var names=new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(var e in zip.Entries){if(e.FullName.EndsWith('/'))continue;string rel=ValidateEntry(e.FullName);if(!names.Add(rel))throw new IOException("Duplicate update archive entry.");string path=Path.GetFullPath(Path.Combine(destination,rel));if(!path.StartsWith(Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Archive path escaped staging.");Directory.CreateDirectory(Path.GetDirectoryName(path)!);e.ExtractToFile(path,false);}foreach(string required in new[]{"gazegallery.exe","runtime/libmpv-2.dll","plugins/SimpleDraw.dll","plugins/Wallpaper.dll","plugins/ColorPicker.dll"})if(!File.Exists(Path.Combine(destination,required)))throw new IOException("Update is missing "+required);}
 static Uri TrustedUrl(string url){var uri=new Uri(url);if(uri.Scheme!="https"||uri.Host!="github.com"||!uri.AbsolutePath.StartsWith("/page-muncher/gazegallery/releases/download/",StringComparison.Ordinal))throw new IOException("Unexpected update download address.");return uri;}
 public static async Task<string> Prepare(ReleaseUpdate38 update,string appRoot,IProgress<double>? progress,CancellationToken token){string work=Path.Combine(appRoot,".updates",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);string zip=Path.Combine(work,"update.zip"),stage=Path.Combine(work,"stage");try{string expected=update.Digest.StartsWith("sha256:")?update.Digest.Substring(7):"";if(expected.Length==0)expected=(await http.GetStringAsync(TrustedUrl(update.Checksum),token)).Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries)[0];if(expected.Length!=64||!expected.All(Uri.IsHexDigit))throw new IOException("Invalid update checksum.");using(var response=await http.GetAsync(TrustedUrl(update.Download),HttpCompletionOption.ResponseHeadersRead,token)){response.EnsureSuccessStatusCode();await using var input=await response.Content.ReadAsStreamAsync(token);await using var output=new FileStream(zip,FileMode.CreateNew,FileAccess.Write,FileShare.None,81920,true);var buffer=new byte[81920];long total=0;int read;while((read=await input.ReadAsync(buffer,token))>0){await output.WriteAsync(buffer.AsMemory(0,read),token);total+=read;if(total>1024L*1024*1024)throw new IOException("Update download exceeds the size limit.");progress?.Report(update.Bytes>0?total/(double)update.Bytes:0);}if(update.Bytes>0&&total!=update.Bytes)throw new IOException("Incomplete update download.");}await using(var stream=File.OpenRead(zip)){string hash=Convert.ToHexString(await SHA256.HashDataAsync(stream,token));if(!hash.Equals(expected,StringComparison.OrdinalIgnoreCase))throw new IOException("Update checksum did not match. Nothing was installed.");}await Task.Run(()=>ExtractValidated(zip,stage),token);File.Delete(zip);return stage;}catch{try{Directory.Delete(work,true);}catch{}throw;}}
 public const string InstallerScript="""
param([int]$TargetPid,[string]$AppRoot,[string]$Stage,[string]$ResumeFile,[switch]$NoRelaunch)
$ErrorActionPreference='Stop'
$AppRoot=[IO.Path]::GetFullPath($AppRoot).TrimEnd('\')
$Stage=[IO.Path]::GetFullPath($Stage).TrimEnd('\')
if (-not $Stage.StartsWith($AppRoot+'\.updates\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid staging location' }
$work=Split-Path -Path $Stage -Parent
$backup=Join-Path $work 'backup'
$log=Join-Path $work 'update.log'
$journal=New-Object System.Collections.Generic.List[object]
function Retry-Move([string]$Source,[string]$Destination) {
  for($attempt=0;$attempt -lt 30;$attempt++) {
    try { Move-Item -LiteralPath $Source -Destination $Destination -Force; return }
    catch { if($attempt -eq 29){throw}; Start-Sleep -Milliseconds 300 }
  }
}
try {
 $running=Get-Process -Id $TargetPid -ErrorAction SilentlyContinue
 if($running -and -not $running.WaitForExit(120000)){throw 'gazegallery did not close; update cancelled'}
 foreach($file in Get-ChildItem -LiteralPath $Stage -File -Recurse | Sort-Object FullName) {
  $relative=$file.FullName.Substring($Stage.Length+1)
  $first=$relative.Split('\')[0]
  if(($relative.Split('\') -contains '..') -or $first -notin @('gazegallery.exe','runtime','plugins','Support','Readme.txt','Full_Guide.html')){throw 'Unexpected staged file'}
  $target=Join-Path $AppRoot $relative
  $saved=Join-Path $backup $relative
  $ancestor=Split-Path -Path $target -Parent
  while($ancestor -and $ancestor.Length -ge $AppRoot.Length) {
   if((Test-Path -LiteralPath $ancestor) -and ((Get-Item -LiteralPath $ancestor).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Refusing a linked update directory'}
   if($ancestor -eq $AppRoot){break};$ancestor=Split-Path -Path $ancestor -Parent
  }
  if(-not $target.StartsWith($AppRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid target'}
  if(Test-Path -LiteralPath $target) {
   if((Get-Item -LiteralPath $target).PSIsContainer){throw 'Expected an application file, found a directory'}
   if((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Refusing a linked update target'}
   New-Item -ItemType Directory -Path (Split-Path $saved -Parent) -Force | Out-Null
   Retry-Move $target $saved
  }
  $journal.Add([pscustomobject]@{Target=$target;Saved=$saved})
  New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
  Retry-Move $file.FullName $target
 }
 'Update installed. Previous application files are in backup; user data was retained.' | Set-Content -LiteralPath $log
} catch {
 $problem=$_.ToString()
 for($i=$journal.Count-1;$i -ge 0;$i--) {
  $entry=$journal[$i]
  try { if(Test-Path -LiteralPath $entry.Target){Remove-Item -LiteralPath $entry.Target -Force};if(Test-Path -LiteralPath $entry.Saved){Retry-Move $entry.Saved $entry.Target} }
  catch { $problem+='; Rollback: '+$_.ToString() }
 }
 ('Update failed; rolled back: '+$problem) | Set-Content -LiteralPath $log
}
$exe=Join-Path $AppRoot 'gazegallery.exe'
if(-not $NoRelaunch -and (Test-Path -LiteralPath $exe)) {
 $args=@()
 if($ResumeFile -and (Test-Path -LiteralPath $ResumeFile)){ $args=@('"'+$ResumeFile.Replace('"','')+'"') }
 if($args.Count){Start-Process -FilePath $exe -ArgumentList $args -WorkingDirectory $AppRoot}
 else {Start-Process -FilePath $exe -WorkingDirectory $AppRoot}
}
""";
}
public partial class MainWindow {
 ReleaseUpdate38? availableUpdate38;
 void AddUpdateControls38(Panel panel){var row=new WrapPanel();var status=Ui.Label("",12);var check=Ui.Button("Check for updates",()=>{});var update=Ui.Button("Update to latest",()=>{});update.IsEnabled=false;check.Click+=async(_,_)=>{check.IsEnabled=false;update.IsEnabled=false;try{status.Text="Checking GitHub…";availableUpdate38=await UpdateService38.Check(typeof(MainWindow).Assembly.GetName().Version!);status.Text=availableUpdate38==null?"You are up to date.":"Available: "+availableUpdate38.Version;update.IsEnabled=availableUpdate38!=null;}catch(Exception e){status.Text="Could not check: "+e.Message;}finally{check.IsEnabled=true;}};update.Click+=async(_,_)=>{if(availableUpdate38==null)return;var owner=Window.GetWindow(panel)??this;if(Operations.Busy||importsRunning){status.Text="Finish the current file operation first.";return;}if(!File.Exists(Path.Combine(Paths.Root,"gazegallery.exe"))||!string.Equals(Environment.ProcessPath,Path.Combine(Paths.Root,"gazegallery.exe"),StringComparison.OrdinalIgnoreCase)){status.Text="Use the portable executable to install updates.";return;}if(!Ui.Confirm(owner,"Update gazegallery?","Download "+availableUpdate38.Version+", keep settings, then close and relaunch? Unfinished edits and collage contents will be discarded.","Update"))return;update.IsEnabled=check.IsEnabled=false;try{var progress=new Progress<double>(v=>status.Text=$"Downloading update… {Math.Clamp(v,0,1):P0}");string stage=await UpdateService38.Prepare(availableUpdate38,Paths.Root,progress,CancellationToken.None);status.Text="Update verified. Restarting…";Config.Save();string helper=Path.Combine(Path.GetTempPath(),"gazegallery-update-"+Guid.NewGuid().ToString("N")+".ps1");await File.WriteAllTextAsync(helper,UpdateService38.InstallerScript);var start=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"System32","WindowsPowerShell","v1.0","powershell.exe")){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};foreach(string arg in new[]{"-NoProfile","-NonInteractive","-ExecutionPolicy","Bypass","-File",helper,"-TargetPid",Environment.ProcessId.ToString(),"-AppRoot",Paths.Root,"-Stage",stage,"-ResumeFile",current??""})start.ArgumentList.Add(arg);if(Process.Start(start)==null)throw new IOException("Could not start the update helper.");shutdown=true;if(owner!=this)owner.Close();Close();}catch(Exception e){status.Text="Update failed; current version kept: "+e.Message;update.IsEnabled=check.IsEnabled=true;}};row.Children.Add(check);row.Children.Add(update);panel.Children.Add(row);panel.Children.Add(status);}
}
