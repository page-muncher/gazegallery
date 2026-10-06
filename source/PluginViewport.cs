using System;using System.Windows;using gazegallery.PluginApi;
namespace gazegallery;
public partial class MainWindow {
 void IPluginHost.PanBy(Vector displacement){View.Pan+=displacement;View.ZoomUsed=true;View.InvalidateVisual();}
 void IPluginHost.ZoomAt(Point point,int delta){if(View.Image==null)return;var limits=GeometryRules.Zoom(View.Image.PixelWidth,View.Image.PixelHeight,View.ActualWidth,View.ActualHeight);double next=Math.Clamp(View.Scale*Math.Pow(1.15,delta/120.0),limits.Item1,limits.Item2);double factor=next/View.Scale;View.Pan=new Vector(point.X-(point.X-View.Pan.X)*factor,point.Y-(point.Y-View.Pan.Y)*factor);View.Scale=next;View.ZoomUsed=true;View.InvalidateVisual();}
}
