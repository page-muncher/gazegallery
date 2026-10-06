using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
namespace gazegallery.PluginApi;
public interface IGazegalleryPlugin {string Id{get;}string Name{get;}string Description{get;}int ApiVersion=>1;void Initialize(IPluginHost host);}
public interface IImageDecoder {IReadOnlyList<string> Extensions{get;}BitmapSource Decode(string path,int maximumEdge);}
public interface IPluginHost {
 string? CurrentFile{get;}BitmapSource? CurrentImage{get;}
 void RegisterCommand(string title,Action execute,string shortcut="",string help="");void RegisterDecoder(IImageDecoder decoder);
 Point ImagePoint(Point viewerPoint);Point ViewerPoint(Point imagePoint);double ImageScale{get;}
 void ZoomAt(Point viewerPoint,int wheelDelta);void PanBy(Vector displacement);
 void BeginOverlay(UIElement overlay,Panel controls,Action cancelled);void EndOverlay();void SetOverlayUndo(Action undo);
 Task<bool> SaveCopyAsync(BitmapSource image);void Toast(string text);
}
