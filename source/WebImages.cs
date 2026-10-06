using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;
namespace gazegallery;
public static class WebImages {
 public static void SaveWebP(BitmapSource source,string path){var image=new FormatConvertedBitmap(source,PixelFormats.Bgra32,null,0);int stride=image.PixelWidth*4;var bytes=new byte[stride*image.PixelHeight];image.CopyPixels(bytes,stride,0);using var bmp=new SKBitmap(new SKImageInfo(image.PixelWidth,image.PixelHeight,SKColorType.Bgra8888,SKAlphaType.Unpremul));System.Runtime.InteropServices.Marshal.Copy(bytes,0,bmp.GetPixels(),bytes.Length);using var sk=SKImage.FromBitmap(bmp);using var data=sk.Encode(SKEncodedImageFormat.Webp,100);using var file=File.Create(path);data.SaveTo(file);}
 public static bool Animated(string path){if(!Path.GetExtension(path).Equals(".webp",StringComparison.OrdinalIgnoreCase))return false;try{using var f=File.OpenRead(path);Span<byte> header=stackalloc byte[21];return f.Read(header)==21&&header[12]==(byte)'V'&&header[15]==(byte)'X'&&(header[20]&2)!=0;}catch{return false;}}
 public static BitmapSource Read(string path,int max){using var stream=File.OpenRead(path);using var codec=SKCodec.Create(stream)??throw new IOException("Unsupported image");var info=codec.Info;using var bmp=new SKBitmap(new SKImageInfo(info.Width,info.Height,SKColorType.Bgra8888,SKAlphaType.Premul));var result=codec.GetPixels(bmp.Info,bmp.GetPixels());if(result!=SKCodecResult.Success&&result!=SKCodecResult.IncompleteInput)throw new IOException("Image decode failed: "+result);return Convert(bmp,max);}
 public static BitmapSource Convert(SKBitmap bmp,int max){if(max>0&&Math.Max(bmp.Width,bmp.Height)>max){double s=max/(double)Math.Max(bmp.Width,bmp.Height);using var scaled=new SKBitmap(new SKImageInfo(Math.Max(1,(int)(bmp.Width*s)),Math.Max(1,(int)(bmp.Height*s)),SKColorType.Bgra8888,SKAlphaType.Premul));using var canvas=new SKCanvas(scaled);canvas.DrawBitmap(bmp,new SKRect(0,0,scaled.Width,scaled.Height));return Convert(scaled,0);}var output=BitmapSource.Create(bmp.Width,bmp.Height,96,96,PixelFormats.Pbgra32,null,bmp.GetPixels(),bmp.ByteCount,bmp.RowBytes);output.Freeze();return output;}
}
public sealed class AnimatedImage:IDisposable {
 readonly object sync=new();SKCodec codec;SKBitmap bitmap;SKCodecFrameInfo[] frames;int index=-1;bool disposed;DateTime next=DateTime.MinValue;public bool Busy;bool playing=true;public bool Playing {get=>playing;set {lock(sync){playing=value;if(value)next=DateTime.UtcNow.AddMilliseconds(frames.Length==0?100:Math.Max(10,frames[Math.Max(0,index)].Duration));}}}public int Width=>bitmap.Width;public int Height=>bitmap.Height;
 public AnimatedImage(string path){codec=SKCodec.Create(File.OpenRead(path))??throw new IOException("Cannot decode animation");frames=codec.FrameInfo;bitmap=new SKBitmap(new SKImageInfo(codec.Info.Width,codec.Info.Height,SKColorType.Bgra8888,SKAlphaType.Premul));}
 public bool Due=>!Busy&&Playing&&DateTime.UtcNow>=next;
 public Task<BitmapSource?> Advance()=>Task.Run(()=>{lock(sync){if(disposed)return null;index=(index+1)%Math.Max(1,frames.Length);codec.GetPixels(bitmap.Info,bitmap.GetPixels(),new SKCodecOptions(index,index>0?index-1:-1));next=(next==DateTime.MinValue||DateTime.UtcNow-next>TimeSpan.FromSeconds(1)?DateTime.UtcNow:next).AddMilliseconds(frames.Length==0?100:Math.Max(10,frames[index].Duration));return WebImages.Convert(bitmap,1920);}});
 public void Dispose(){lock(sync){disposed=true;codec.Dispose();bitmap.Dispose();}}
}



