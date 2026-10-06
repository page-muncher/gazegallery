using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
namespace gazegallery;
public partial class MainWindow {
 public async Task ImportBoard(IEnumerable<string> paths,Point? drop=null){if(importsRunning)return;importsRunning=true;int epoch=boardEpoch;Point origin=drop??View.BoardPoint(new Point(View.ActualWidth/2,View.ActualHeight/2));View.Snapshot();View.BoardSelection.Clear();int added=0,rejected=0;try{foreach(var path in paths){if(epoch!=boardEpoch||Mode!="board")break;if(Paths.Motion(path)){rejected++;continue;}if(!Paths.Media(path))continue;if(!EnsureBoardCapacity())break;string copy=Path.Combine(Paths.Data,"collage",Guid.NewGuid().ToString("N")+Path.GetExtension(path));try{Directory.CreateDirectory(Path.GetDirectoryName(copy)!);await Task.Run(()=>File.Copy(path,copy));boardCopies.Add(copy);var b=await LoaderPool.Run(()=>ImageCache.Read(copy,1600));if(epoch!=boardEpoch||Mode!="board"){File.Delete(copy);boardCopies.Remove(copy);break;}using var info=SkiaSharp.SKCodec.Create(copy);var extra=ExtendedImages.Supports(copy)?new ImageMagick.MagickImageInfo(copy):null;double nw=info?.Info.Width??(int?)extra?.Width??b.PixelWidth,nh=info?.Info.Height??(int?)extra?.Height??b.PixelHeight;double factor=Math.Min(1,Math.Min(View.ActualWidth*.6/nw,View.ActualHeight*.6/nh)/View.BoardScale);double dw=nw*factor,dh=nh*factor;View.Board.Add(new(){Path=copy,Image=b,ImportOrder=++importSequence,NaturalWidth=nw,NaturalHeight=nh,Rect=new Rect(origin.X-dw/2+added*24/View.BoardScale,origin.Y-dh/2+added*24/View.BoardScale,dw,dh)});View.BoardSelected=-1;added++;View.InvalidateVisual();}catch(Exception e){if(File.Exists(copy)){File.Delete(copy);boardCopies.Remove(copy);}Error(e);}}Toast(rejected>0?"Moving pictures aren't allowed here":$"Added {added} — {View.Board.Count}/{boardLimit} images");}finally{importsRunning=false;}}
 public static BitmapSource RenderBoard(BoardItem[] items,Rect bounds,int longest,bool oled=false){double factor=longest/Math.Max(bounds.Width,bounds.Height);int w=Math.Max(1,(int)Math.Round(bounds.Width*factor)),h=Math.Max(1,(int)Math.Round(bounds.Height*factor));if((long)w*h>64_000_000)throw new IOException("Export exceeds 64 million pixels.");var drawing=new DrawingVisual();using(var dc=drawing.RenderOpen()){dc.DrawRectangle(new SolidColorBrush(oled?Colors.Black:Ui.CanvasColor),null,new Rect(0,0,w,h));foreach(var i in items){var r=new Rect((i.Rect.X-bounds.X)*factor,(i.Rect.Y-bounds.Y)*factor,i.Rect.Width*factor,i.Rect.Height*factor);var image=i.Edited?i.Image:ImageCache.Read(i.Path,(int)Math.Ceiling(Math.Max(r.Width,r.Height)));dc.DrawImage(image,r);}}var bmp=new RenderTargetBitmap(w,h,96,96,PixelFormats.Pbgra32);bmp.Render(drawing);bmp.Freeze();return bmp;}
}
