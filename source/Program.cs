using System;
using System.Linq;
using System.IO;
using System.Windows;
namespace gazegallery;
public static class Program {
 [STAThread]public static void Main(string[] args){if(args.Length>=2&&args[0]=="--video-runtime-check"){Environment.ExitCode=VideoRuntimeCheck.Run(args[1],args.Length>2?args[2]:null);return;}Paths.Init();var app=new Application();app.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("/gazegallery;component/Theme.xaml",UriKind.Relative)});app.DispatcherUnhandledException+=(_,e)=>{Paths.Log("unhandled",e.Exception.ToString());MessageBox.Show(e.Exception.Message,"gazegallery error");e.Handled=true;};AppDomain.CurrentDomain.UnhandledException+=(_,e)=>Paths.Log("fatal",e.ExceptionObject.ToString()??"");app.Run(new MainWindow(args.FirstOrDefault(a=>!a.StartsWith("--"))));}
}
