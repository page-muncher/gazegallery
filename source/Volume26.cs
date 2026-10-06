using System.Windows;using System.Windows.Controls;
namespace gazegallery;
public partial class MainWindow {
 bool updatingVolume;
 void SyncVolumeUi(){if(volume==null)return;updatingVolume=true;try{volume.Value=Config.Mute?0:Config.Volume;volume.Opacity=video?.HasAudio==true?1:.4;if(playerPanel.Child is Grid panel&&panel.Children.Count>5&&panel.Children[5] is TextBlock label){label.TextDecorations=Config.Mute?TextDecorations.Strikethrough:null;label.Opacity=video?.HasAudio==true?1:.4;}}finally{updatingVolume=false;}}
}
