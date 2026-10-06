using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
namespace gazegallery;
public static class Smoke {
 public static async Task Run(MainWindow window){var results=new List<object>();string work=Path.Combine(Paths.Test,"_testing",DateTime.Now.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(work);int assertions=0;void Check(bool condition,string label){if(!condition)throw new Exception("FAILED: "+label);assertions++;results.Add(new{test=label,status="passed"});}
  try{
   bool rejected=false;try{Paths.Guard(Path.Combine(Paths.Root,"outside-test.jpg"));}catch(IOException){rejected=true;}Check(rejected,"Reject mutations outside TestFiles");rejected=false;try{Paths.Guard(Paths.Test+"-other\\image.jpg");}catch(IOException){rejected=true;}Check(rejected,"Reject prefix-lookalike folders");
   var files=new FileOperations();string a=Path.Combine(work,"a.txt"),b=Path.Combine(work,"b.txt");File.WriteAllText(a,"incoming");File.WriteAllText(b,"existing");var e=new UndoEntry{Viewed=a};e.Changes.Add(await files.Move(a,b,true));files.Commit(e);Check(!File.Exists(a)&&File.ReadAllText(b)=="incoming","Overwrite moves rather than copies");await files.Undo();Check(File.ReadAllText(a)=="incoming"&&File.ReadAllText(b)=="existing","Overwrite undo restores BOTH originals");
   var batch=new UndoEntry();for(int i=0;i<6;i++){string p=Path.Combine(work,$"batch-{i}.txt");File.WriteAllText(p,"batch "+i);batch.Changes.Add(await files.Move(p,p+".moved",false));}files.Commit(batch);Check(files.Count==1,"Six-file batch uses one undo slot");await files.Undo();Check(Enumerable.Range(0,6).All(i=>File.Exists(Path.Combine(work,$"batch-{i}.txt"))),"One undo restores whole batch");
   for(int i=0;i<6;i++){string p=Path.Combine(work,$"history-{i}.txt");File.WriteAllText(p,"data");var en=new UndoEntry();en.Changes.Add(await files.Move(p,p+".moved",false));files.Commit(en);}Check(files.Count==5,"History capped at five operations");files.Close();
   string trash=Path.Combine(work,"recycle-roundtrip.txt");File.WriteAllText(trash,"recycle identity");var tr=new UndoEntry();tr.Changes.Add(await files.Trash(trash));files.Commit(tr);Check(!File.Exists(trash),"Windows Recycle Bin removes source");await files.Undo();Check(File.Exists(trash)&&File.ReadAllText(trash)=="recycle identity","Undo restores exact recycled item");
   string pic=Directory.GetFiles(Paths.Test,"*.png").First();string crop=Path.Combine(work,"crop.png");File.Copy(pic,crop);var original=ImageCache.Read(crop,0);var cropped=new CroppedBitmap(original,new Int32Rect(0,0,Math.Max(1,original.PixelWidth/2),Math.Max(1,original.PixelHeight/2)));cropped.Freeze();var ce=new UndoEntry();ce.Changes.Add(await files.Crop(crop,cropped));files.Commit(ce);Check(ImageCache.Read(crop,0).PixelWidth==cropped.PixelWidth,"Crop replaces pixels");await files.Undo();Check(ImageCache.Read(crop,0).PixelWidth==original.PixelWidth,"Crop undo restores original");files.Close();
   var stress=Path.Combine(work,"stress-3000");Directory.CreateDirectory(stress);var seeds=Directory.GetFiles(Paths.Test).Where(p=>new[]{".jpg",".png"}.Contains(Path.GetExtension(p))).ToArray();await Task.Run(()=>{for(int i=0;i<3000;i++){var src=seeds[i%seeds.Length];File.Copy(src,Path.Combine(stress,$"image-{i:0000}"+Path.GetExtension(src)));}});var sw=Stopwatch.StartNew();await window.Open(stress);Check(window.Files.Count==3000,"Index 3000-file folder");results.Add(new{metric="folder_first_image_ms",value=sw.Elapsed.TotalMilliseconds});
   for(int i=0;i<35;i++){await window.ShowIndex(i);await Task.Delay(30);}for(int i=34;i>=0;i--){await window.ShowIndex(i);await Task.Delay(15);}Check(window.Cache.Bytes<=window.Cache.Budget||window.Cache.Count==1,"Decoded image cache bounded");await window.SetMode("grid");await Task.Delay(900);Check(window.VisibleTiles<100,"Grid renders only visible tiles");
   await window.SetMode("board");await window.ImportBoard(seeds);window.NormalizeBoard();var areas=window.View.Board.Select(i=>i.Rect.Width*i.Rect.Height).ToArray();Check(areas.Max()-areas.Min()<1,"Collage equal-area normalization");window.PackBoard();Check(window.View.Board.All(i=>i.Rect.Width>0&&i.Rect.Height>0),"Collage packing preserves nonzero bounds");window.View.BoardDirty=false;
   await window.Open(Paths.Test);foreach(var p in window.Files.Where(Paths.Motion).ToArray()){await window.ShowIndex(window.Files.IndexOf(p));await Task.Delay(1800);Check(window.View.Image!=null,"Decoded motion frame: "+Path.GetFileName(p));}
   results.Add(new{metric="navigation_p95_ms",value=window.Stats.P95});results.Add(new{metric="working_set_mb",value=Process.GetCurrentProcess().WorkingSet64/1048576});
   File.WriteAllText(Path.Combine(Paths.Logs,"smoke-results.json"),System.Text.Json.JsonSerializer.Serialize(new{status="passed",assertions,work,results},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));window.Toast($"Smoke checks passed: {assertions}");await window.ShowIndex(0);await window.Action("Diagnostics");
  }catch(Exception ex){Paths.Log("smoke-failure",ex.ToString());File.WriteAllText(Path.Combine(Paths.Logs,"smoke-results.json"),System.Text.Json.JsonSerializer.Serialize(new{status="failed",assertions,error=ex.ToString(),work,results},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));window.Error(ex);}
 }
}
