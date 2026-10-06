using System;using System.Linq;using System.Windows.Controls;
namespace gazegallery;
public partial class MainWindow {
 Slider volume=new();
 string? ResolveViewerAction(string key)=>Config.Keys.FirstOrDefault(k=>!k.Key.EndsWith("collage")&&!k.Key.EndsWith("thumbnails")&&k.Value.Any(value=>NormalizeKey(value).Equals(NormalizeKey(key),StringComparison.OrdinalIgnoreCase))).Key;
}
