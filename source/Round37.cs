using System;using System.Runtime.InteropServices;using System.Windows;using System.Windows.Controls;using System.Windows.Media;
namespace gazegallery;
public partial class MainWindow {
 bool forwardPending37;int forwardGeneration37;
 Border peekSkeleton37=new(){IsHitTestVisible=false,Background=new SolidColorBrush(Color.FromArgb(40,239,240,225)),BorderBrush=new SolidColorBrush(Color.FromArgb(50,239,240,225)),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(7),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Visibility=Visibility.Collapsed};
 void SizePeekSkeleton37(string path){double ratio=GridRatios.TryGetValue(path,out var known)?known:1;double w=Math.Max(40,root.ActualWidth*.95-24),h=Math.Max(40,root.ActualHeight*.95-24);if(ratio*w>h)w=h/Math.Max(.01,ratio);else h=w*ratio;peekSkeleton37.Width=w;peekSkeleton37.Height=h;}
 static void TintBinding37(Button button,bool assigned){if(assigned)button.Background=KeyTint35();}
 [DllImport("user32.dll",EntryPoint="RedrawWindow")]static extern bool RedrawWindow37(IntPtr hwnd,IntPtr rect,IntPtr region,uint flags);
}
