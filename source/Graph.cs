using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Globalization;

namespace gazegallery;
public sealed class Graph:FrameworkElement {
 public MainWindow Host=null!;protected override void OnRender(DrawingContext dc){if(Host==null)return;dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(235,28,32,37)),new Pen(new SolidColorBrush(Color.FromRgb(63,72,80)),1),new Rect(RenderSize),8,8);var s=Host.View;var t=Host.Stats;double mem=System.Diagnostics.Process.GetCurrentProcess().WorkingSet64/1048576.0;s.Text(dc,$"PERFORMANCE · F12\nLoad {t.LastLoad:0.0} ms  /  p95 {t.P95:0.0} ms",new(14,10),12,Brushes.White);s.Text(dc,$"Cache {Host.Cache.Bytes/1048576} MB · {Host.Cache.Count} images\nHits {Host.Cache.Hits} / misses {Host.Cache.Misses}\nProcess {mem:0} MB · visible tiles {Host.VisibleTiles}\nFrame {t.LastFrame:0.0} ms · >33 ms: {t.LateFrames}",new(14,60),11,Brushes.LightGray);
  var data=t.Frames;double y=190;dc.DrawLine(new Pen(Brushes.Gray,1),new(12,y-8.33),new(298,y-8.33));for(int i=1;i<data.Length;i++){double x=12+i*286.0/240;dc.DrawLine(new Pen(data[i]>33?Brushes.OrangeRed:Brushes.MediumAquamarine,1),new(x-286.0/240,y-Math.Min(60,data[i-1])),new(x,y-Math.Min(60,data[i])));}s.Text(dc,"Frame intervals · grey line 8.3 ms (120 Hz)\nFive-step undo · local diagnostics",new(14,200),10,Brushes.LightGray);
 }
}

