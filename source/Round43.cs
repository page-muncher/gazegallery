using System;using System.Linq;using System.Collections.Generic;using System.Threading.Tasks;
namespace gazegallery;
public static class GridFocus43 {
 public static int Resolve(IReadOnlyList<string> before,IReadOnlyList<string> after,int focus,ISet<string> holes){
  if(focus<0||focus>=before.Count)return -1;
  int Find(string path){for(int i=0;i<after.Count;i++)if(string.Equals(after[i],path,StringComparison.OrdinalIgnoreCase)&&!holes.Contains(after[i]))return i;return -1;}
  int same=Find(before[focus]);if(same>=0)return same;
  for(int i=focus+1;i<before.Count;i++){int next=Find(before[i]);if(next>=0)return next;}
  for(int i=focus-1;i>=0;i--){int previous=Find(before[i]);if(previous>=0)return previous;}
  for(int i=0;i<after.Count;i++)if(!holes.Contains(after[i]))return i;return -1;
 }
}
public partial class MainWindow {
 async Task ThumbnailNavigation43(){if(Mode=="board"){await CollageThumbnails41();return;}if(Mode!="grid"){await SetMode("grid");return;}var parent=System.IO.Directory.GetParent(Folder);if(parent!=null)await NavigateFolder(parent.FullName);}
}
