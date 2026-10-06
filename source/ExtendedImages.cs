using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageMagick;
namespace gazegallery;
public static class ExtendedImages {
 public static readonly string[] Extensions={".svg",".heic",".heif",".avif",".jp2",".tiff",".tif",".ico"};
 public static bool Supports(string path)=>Extensions.Contains(Path.GetExtension(path).ToLowerInvariant());
 public static double Ratio(string path){var info=new MagickImageInfo(path);return info.Height/(double)Math.Max(1,info.Width);}
 public static BitmapSource Read(string path,int maximumEdge){
  using var images=new MagickImageCollection();
  var settings=new MagickReadSettings{FrameIndex=0,FrameCount=1};
  if(Path.GetExtension(path).Equals(".ico",StringComparison.OrdinalIgnoreCase))images.Read(path);
  else images.Read(path,settings);
  var image=images.OrderByDescending(i=>(long)i.Width*i.Height).First();
  image.AutoOrient();
  if(maximumEdge>0&&Math.Max(image.Width,image.Height)>maximumEdge)image.Resize(new MagickGeometry((uint)maximumEdge,(uint)maximumEdge){IgnoreAspectRatio=false});
  image.ColorSpace=ColorSpace.sRGB;image.Alpha(AlphaOption.On);
  int width=checked((int)image.Width),height=checked((int)image.Height);
  // Raw-format encoding can retain the TIFF's original bit depth. WPF requires
  // exactly four 8-bit channels per pixel, independent of the source depth.
  using var pixelCollection=image.GetPixels();
  var pixels=pixelCollection.ToByteArray(PixelMapping.BGRA)??throw new IOException("No decoded pixels");
  if(pixels.Length!=checked(width*height*4))throw new IOException("Unexpected decoded pixel layout");
  var bitmap=BitmapSource.Create(width,height,96,96,PixelFormats.Bgra32,null,pixels,checked(width*4));bitmap.Freeze();return bitmap;
 }
 public static void Save(BitmapSource bitmap,string target,string extension){
  using var stream=new MemoryStream();var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));encoder.Save(stream);var png=stream.ToArray();
  extension=extension.ToLowerInvariant();
  if(extension==".svg") {File.WriteAllText(target,$"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{bitmap.PixelWidth}\" height=\"{bitmap.PixelHeight}\" viewBox=\"0 0 {bitmap.PixelWidth} {bitmap.PixelHeight}\"><image width=\"{bitmap.PixelWidth}\" height=\"{bitmap.PixelHeight}\" href=\"data:image/png;base64,{Convert.ToBase64String(png)}\"/></svg>");return;}
  using var image=new MagickImage(png);image.Quality=95;
  var format=extension switch {".tif" or ".tiff"=>MagickFormat.Tiff,".heic" or ".heif"=>MagickFormat.Heic,".avif"=>MagickFormat.Avif,".jp2"=>MagickFormat.Jp2,".ico"=>MagickFormat.Ico,_=>throw new IOException("Unsupported output format")};
  image.Write(target,format);
 }
}
