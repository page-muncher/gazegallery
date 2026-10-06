using System;using System.Linq;using System.Windows;
namespace gazegallery;
public sealed partial class Surface {
 public double GridViewport35=>Host.Config.River?ActualWidth:ActualHeight;
 public double GridStart35(Rect tile)=>Host.Config.River?tile.Left:tile.Top;
 public double GridEnd35(Rect tile)=>Host.Config.River?tile.Right:tile.Bottom;
 public Rect ScreenTile35(Rect tile){if(Host.Config.River)tile.X-=Scroll;else tile.Y-=Scroll;return tile;}
 void LayoutRiver35(bool append){const double gap=10,margin=14;ActualColumns=Math.Clamp(Host.Config.Columns,2,9);double label=Host.Config.Names?38:0;double height=Math.Max(16,(ActualHeight-Host.ToolbarHeight-margin*2-gap*(ActualColumns-1))/ActualColumns-label);string signature=$"river|{ActualHeight:R}|{ActualColumns}|{label}|{Host.ToolbarHeight:R}";int count=Host.GridLayoutCount;bool extend=append&&ReferenceEquals(layoutPaths,Host.GridPaths)&&layoutSignature==signature&&Tiles.Count<=count&&layoutHeights.Length==ActualColumns;if(!extend){Tiles.Clear();layoutHeights=Enumerable.Repeat(margin,ActualColumns).ToArray();GridVersion++;}for(int i=Tiles.Count;i<count;i++){int row=Array.IndexOf(layoutHeights,layoutHeights.Min());double ratio=Host.GridRatios.TryGetValue(Host.GridPaths[i],out double known)?known:1;double width=Host.IsFolderTile(i)||ratio>1?height*4/3:height/Math.Max(.05,ratio);width=Math.Clamp(width,24,height*8);Tiles.Add(new Rect(layoutHeights[row],Host.ToolbarHeight+margin+row*(height+label+gap),width,height));layoutHeights[row]+=width+gap;}layoutPaths=Host.GridPaths;layoutSignature=signature;GridBottom=layoutHeights.Max()+margin;Scroll=Math.Clamp(Scroll,0,Math.Max(0,GridBottom-ActualWidth));GridVersion++;InvalidateVisual();}
}
