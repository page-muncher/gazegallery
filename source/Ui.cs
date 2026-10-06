using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace gazegallery;
public static class Ui {
 static readonly System.Collections.Generic.HashSet<ContextMenu> openMenus=new();
 static Ui(){
  EventManager.RegisterClassHandler(typeof(ContextMenu),ContextMenu.OpenedEvent,new RoutedEventHandler((sender,e)=>openMenus.Add((ContextMenu)sender)));
  EventManager.RegisterClassHandler(typeof(ContextMenu),ContextMenu.ClosedEvent,new RoutedEventHandler((sender,e)=>openMenus.Remove((ContextMenu)sender)));
  EventManager.RegisterClassHandler(typeof(ContextMenu),System.Windows.Input.Keyboard.PreviewKeyDownEvent,new System.Windows.Input.KeyEventHandler((sender,e)=>{if(IsCancelKey(e)){e.Handled=true;((ContextMenu)sender).IsOpen=false;}}),true);
  EventManager.RegisterClassHandler(typeof(Separator),FrameworkElement.LoadedEvent,new RoutedEventHandler((sender,e)=>((Separator)sender).SetResourceReference(FrameworkElement.StyleProperty,typeof(Separator))));
 }
 public static bool IsExitAlias(System.Windows.Input.KeyEventArgs e)=>IsExitAlias(e.Key==System.Windows.Input.Key.System?e.SystemKey:e.Key,System.Windows.Input.Keyboard.Modifiers);
 public static bool IsExitAlias(System.Windows.Input.Key key,System.Windows.Input.ModifierKeys modifiers)=>key==System.Windows.Input.Key.F4&&modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt)||key==System.Windows.Input.Key.W&&modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control);
 public static bool DismissMenus(){var menus=openMenus.Where(m=>m.IsOpen).ToArray();foreach(var menu in menus)menu.IsOpen=false;return menus.Length>0;}
 public static bool IsCancelKey(System.Windows.Input.KeyEventArgs e)=>e.Key==System.Windows.Input.Key.Escape||IsExitAlias(e);

 public static bool Confirm(Window owner,string title,string message,string yes,bool primaryFirst=false){var w=Dialog(owner,title,480,220);w.ResizeMode=ResizeMode.NoResize;var p=new StackPanel{Margin=new Thickness(22)};p.Children.Add(Label(message));var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=primaryFirst?HorizontalAlignment.Center:HorizontalAlignment.Right,Margin=new Thickness(0,22,0,0)};var cancel=Button("Cancel",()=>w.DialogResult=false);var accept=Button(yes,()=>w.DialogResult=true);buttons.Children.Add(primaryFirst?accept:cancel);buttons.Children.Add(primaryFirst?cancel:accept);if(yes=="Exit")w.PreviewKeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Space){e.Handled=true;w.DialogResult=true;}};p.Children.Add(buttons);w.Content=p;return w.ShowDialog()==true;}
 public static Brush Selection=new SolidColorBrush(Color.FromRgb(239,240,225));public static Brush SelectionTint=new SolidColorBrush(Color.FromArgb(85,239,240,225));public static Brush Canvas=new SolidColorBrush(Color.FromRgb(24,24,24));public static Brush Muted=new SolidColorBrush(Color.FromRgb(163,171,179));public static Color CanvasColor=Color.FromRgb(24,24,24);public static Brush Bg=new SolidColorBrush(Color.FromRgb(30,32,36));public static Brush Fg=new SolidColorBrush(Color.FromRgb(228,230,232));
 public static Button FlatButton(string label,Action action){var b=Button(label,action);b.Tag="Flat";return b;}
 public static Button Button(string label,Action action){var b=new Button{Content=label,Margin=new Thickness(3),Padding=new Thickness(11,6,11,6),Background=Brushes.Transparent,Foreground=Fg,BorderThickness=new Thickness(0),Focusable=false,HorizontalAlignment=HorizontalAlignment.Left,HorizontalContentAlignment=HorizontalAlignment.Center};b.Click+=(_,_)=>action();return b;}
 public static TextBlock Label(string text,int size=14)=>new(){Text=text,FontSize=size,Foreground=Fg,Margin=new Thickness(5),TextWrapping=TextWrapping.Wrap};
 public static Window Dialog(Window owner,string title,double width=500,double height=400){var w=new Window{Owner=owner,Title=title,Width=width,Height=height,Background=Bg,Foreground=Fg,WindowStartupLocation=WindowStartupLocation.CenterOwner,ShowInTaskbar=false};w.PreviewKeyDown+=(_,e)=>{if(IsCancelKey(e)){e.Handled=true;if(!DismissMenus())w.Close();}};return w;}
 public static TextBox Input(string text)=>new(){Text=text,Margin=new Thickness(5),Padding=new Thickness(7),Background=new SolidColorBrush(Color.FromRgb(44,48,53)),Foreground=Fg,BorderBrush=Brushes.DimGray};
 public static string? Prompt(Window owner,string title,string text){var w=Dialog(owner,title,560,180);var p=new StackPanel{Margin=new Thickness(15)};var input=Input(text);p.Children.Add(input);p.Children.Add(Button("OK",()=>w.DialogResult=true));w.Content=p;w.Loaded+=(_,_)=>{input.Focus();input.SelectAll();};return w.ShowDialog()==true?input.Text:null;}
 public static void Check(Panel p,string title,bool value,Action<bool> changed){var c=new CheckBox{Content=title,IsChecked=value,Foreground=Fg,Margin=new Thickness(8)};c.Checked+=(_,_)=>changed(true);c.Unchecked+=(_,_)=>changed(false);p.Children.Add(c);}
}

