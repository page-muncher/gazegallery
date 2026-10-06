using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;
using gazegallery.PluginApi;
using SkiaSharp;

namespace gazegallery;
public partial class MainWindow {
	private bool hasFolder;

	private bool seekDragging;

	private bool clipBusy;

	private bool panGesture;

	private string? viewerReturn;

	private int resizeHandle = -1;

	private Point gestureStart;

	private Rect selectionStart;

	private BitmapSource? sourceImage;

	private int rotation;

	private bool flipH;

	private bool flipV;

	private readonly string windowId = Guid.NewGuid().ToString("N");

	private Border playerPanel = new Border();

	private Border cropPanel = new Border();

	private Border dropPanel = new Border();

	private System.Windows.Controls.Button previousFrameButton=null!;

	private System.Windows.Controls.Button playButton=null!;

	private System.Windows.Controls.Button stepButton=null!;

	private System.Windows.Controls.Button muteButton=null!;

	private Slider seek = new Slider();

	private TextBlock playbackTime = new TextBlock();

	private DateTime playerActiveUntil;

	private Border viewTarget = new Border();

	private Border moveTarget = new Border();

	private int dropChoice;

	private readonly ImageCache ThumbCache = new ImageCache(268435456L);

	private int boardEpoch;

	private bool importsRunning;

	private readonly HashSet<string> boardCopies = new HashSet<string>();

	private int modeTransition;

	public double ContentWidth => Math.Max(1.0, root.ActualWidth);

	private string? ActionImage
	{
		get
		{
			if (!(Mode == "grid"))
			{
				return current;
			}
			return Selection().FirstOrDefault();
		}
	}

	public static string TempFolder => System.IO.Path.Combine(AppContext.BaseDirectory, "temp");

	private void SetupExperience()
	{
		Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();
		dictionary.Add("Folder tree", new string[2] { "Tab", "X" });
		dictionary.Add("Flip horizontal", new string[1] { "H" });
		dictionary.Add("Flip vertical", new string[1] { "N" });
		dictionary.Add("Rotate 90°", new string[1] { "R" });
		dictionary.Add("Rename", new string[1] { "T" });
		dictionary.Add("Paste", new string[1] { "Ctrl+V" });
		dictionary.Add("Navigation arrows", Array.Empty<string>());
		foreach (KeyValuePair<string, string[]> item in dictionary)
		{
			if (!Config.Keys.ContainsKey(item.Key))
			{
				Config.Keys[item.Key] = item.Value;
			}
		}
		RenderOptions.SetBitmapScalingMode((DependencyObject)(object)View, ScalingMode);
		toolbar.Height = 48.0;
		toolbar.Margin = new Thickness(8.0, 0.0, 8.0, 0.0);
		folderPanel.Width = 280.0;
		folderPanel.Margin = new Thickness(0.0, 48.0, 0.0, 0.0);
		cropPanel.Background = Ui.Bg;
		cropPanel.CornerRadius = new CornerRadius(6.0);
		cropPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
		cropPanel.VerticalAlignment = VerticalAlignment.Bottom;
		cropPanel.Margin = new Thickness(20.0);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal
		};
		stackPanel.HorizontalAlignment=HorizontalAlignment.Right;AddEditControls(stackPanel);AddCropRatios(stackPanel);Ui.Check(stackPanel,"Allow overcropping (Uses canvas color)",Config.CropFillOutside,v=>{Config.CropFillOutside=v;Config.Save();ConstrainViewerCrop();});
		stackPanel.Children.Add(Ui.Button("Cancel", CancelCrop));
		stackPanel.Children.Add(Ui.Button("Save as copy", () =>
		{
			Run(SaveCropCopy);
		}));
		stackPanel.Children.Add(Ui.Button("Apply and save", () =>
		{
			Run(ApplyCrop);
		}));
		cropPanel.Child = stackPanel;
		cropPanel.Visibility = Visibility.Collapsed;
		root.Children.Add(cropPanel);
		Grid grid = new Grid
		{
			Margin = new Thickness(7.0)
		};
		for (int num = 0; num < 7; num++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = ((num == 2) ? new GridLength(1.0, GridUnitType.Star) : GridLength.Auto)
			});
		}
		playButton = Ui.Button("Pause", () =>
		{
			video?.Pause();
		});
		stepButton = Ui.Button(">", () =>
		{
			video?.Step();
		});
		muteButton = Ui.Button(Config.Mute ? "Unmute" : "Mute", () =>
		{
			Run(() => Action("Mute"));
		});
		muteButton.Width = 82.0;
		SetupSeek();
		playbackTime = Ui.Label("0:00 / 0:00", 11);
		playbackTime.VerticalAlignment = VerticalAlignment.Center;
		volume = new Slider
		{
			IsMoveToPointEnabled = true,
			Minimum = 0.0,
			Maximum = 100.0,
			Value = Config.Volume,
			Width = 90.0,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(6.0)
		};
		volume.ValueChanged += (object _, RoutedPropertyChangedEventArgs<double> _) =>
		{
			if(updatingVolume)return;Config.Volume = (int)volume.Value;
			video?.Volume(Config.Volume);
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal
		};
		previousFrameButton = Ui.Button("<", () =>
		{
			video?.PreviousFrame();
		});
		previousFrameButton.ToolTip = "Step through recent decoded frames. Seek earlier when the buffer ends.";
		stackPanel2.Children.Add(previousFrameButton);
		stackPanel2.Children.Add(stepButton);stackPanel2.Children.Add(Ui.Button("Loop",LoopMenu35));
		UIElement[] array = new UIElement[7]
		{
			playButton,
			stackPanel2,
			seek,
			playbackTime,
			muteButton,
			new TextBlock
			{
				Text = "Volume",
				FontSize = 11.0,
				Foreground = Ui.Fg,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(5.0, 0.0, 5.0, 0.0)
			},
			volume
		};
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			Grid.SetColumn(array[num2], num2);
			grid.Children.Add(array[num2]);
		}
		playerPanel.Child = grid;stepButton.ToolTip="Next frame";previousFrameButton.ToolTip="Previous frame";
		playerPanel.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(245, 34, 38, 42));
		playerPanel.CornerRadius = new CornerRadius(7.0);
		playerPanel.Margin = new Thickness(24.0, 0.0, 24.0, 22.0);
		playerPanel.VerticalAlignment = VerticalAlignment.Bottom;
		playerPanel.Visibility = Visibility.Collapsed;
		playerPanel.MouseMove += (object _, System.Windows.Input.MouseEventArgs _) =>
		{
			playerActiveUntil = DateTime.Now.AddSeconds(2.0);
		};
		root.Children.Add(playerPanel);
		dropPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
		dropPanel.VerticalAlignment = VerticalAlignment.Center;
		Grid grid2 = new Grid();
		grid2.RowDefinitions.Add(new RowDefinition());
		grid2.RowDefinitions.Add(new RowDefinition());
		viewTarget = Target("View this image\nOpen in its original folder");
		moveTarget = Target("Move image to current folder\nDo not view");
		grid2.Children.Add(viewTarget);
		Grid.SetRow(moveTarget, 1);
		grid2.Children.Add(moveTarget);
		dropPanel.Child = grid2;
		dropPanel.IsHitTestVisible = false;
		dropPanel.Visibility = Visibility.Collapsed;
		root.Children.Add(dropPanel);
		SetupRevision();
		DragLeave += (object _, System.Windows.DragEventArgs e) =>
		{
			if (!IsMouseOver)
			{
				dropPanel.Visibility = Visibility.Collapsed;
				dropChoice = 0;
			}
		};
		static Border Target(string label)
		{
			return new Border
			{
				Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(72, 80, 87)),
				Opacity = 0.9,
				CornerRadius = new CornerRadius(10.0),
				Padding = new Thickness(24.0),
				Margin = new Thickness(8.0),
				Child = new TextBlock
				{
					Text = label,
					FontSize = 26.0,
					Foreground = Ui.Fg,
					TextAlignment = TextAlignment.Center,
					TextWrapping = TextWrapping.Wrap,
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = System.Windows.HorizontalAlignment.Center
				}
			};
		}
	}

	private void RefreshPlayer()
	{
		if(videoActive)SyncVolumeUi();
		if (!videoActive || !(Mode == "viewer") || Cropping || Quick)
		{
			playerPanel.Visibility = Visibility.Collapsed;
			return;
		}
		bool flag = DateTime.Now < playerActiveUntil || playerPanel.IsMouseOver || seekDragging;
		playerPanel.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		if (flag)
		{
			SyncVolumeUi();bool playing = video!.Playing;volume.Opacity=video.HasAudio?1:.4;if(playerPanel.Child is Grid mediaControls&&mediaControls.Children.Count>5)mediaControls.Children[5].Opacity=video.HasAudio?1:.4;
			playButton.Content = (playing ? "Pause" : "Play");
			stepButton.IsEnabled = !playing;
			previousFrameButton.IsEnabled = !playing && video.CanPrevious;
			muteButton.Content = (Config.Mute ? "Unmute" : "Mute");
			if (!seekDragging)
			{
				seek.Maximum = Math.Max(1L, video.Length);
				seek.Value = Math.Min(seek.Maximum, video.Time);
			}
			playbackTime.Text = $"{TimeSpan.FromMilliseconds(video.Time):m\\:ss} / {TimeSpan.FromMilliseconds(video.Length):m\\:ss}";
		}
	}

	private void UpdateTitle(){var image=sourceImage??View.Image;Title=TitleBar32.Build(Config,Mode,current,Folder,Index,Files.Count,View.Board.Count,largeRaster?.Width??image?.PixelWidth??0,largeRaster?.Height??image?.PixelHeight??0);}

	private void PanelLayout()
	{
		bool flag = folderPanel.Visibility == Visibility.Visible;
		double num = (flag ? Math.Min(folderWidth, Math.Max(160.0, ContentWidth * 0.65)) : 0.0);
		folderPanel.Width = Math.Max(160.0, num);
		treeResize.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		treeResize.Margin = new Thickness(Math.Max(0.0, num - 3.0), 0.0, 0.0, 0.0);
		View.Margin = new Thickness(num, 0.0, 0.0, FilmstripInset33);
		toolbar.Margin = new Thickness(0.0);
		folderPanel.Margin = new Thickness(0.0, 0.0, 0.0, 0.0);
		RefreshTreeHeader();ResponsiveToolbar();View.UpdateLayout();
		if (Mode == "grid")
		{
			View.LayoutTiles();
		}
		else if (!Cropping && !resizingImageWindow33)
		{
			FitViewer33();
		}
	}

	private void ColumnStep(int step)
	{
		Config.Columns = Math.Clamp(Config.Columns + step, 2, 9);
		Config.Save();
		View.LayoutTiles();
	}

	private void ShowFolders()
	{
		if (!(Mode == "board") && !Quick && !Cropping)
		{
			BuildFolders();
			PanelLayout();
		}
	}

	private async Task Empty()
	{
		ClearLarge();
		hasFolder = false;
		current = null;
		Files.Clear();
		Index = 0;
		sourceImage = null;
		View.Image = null;
		await StopVideo();
		UpdateTitle();
		View.InvalidateVisual();
	}

	private bool LeaveBoard()
	{
		if (Mode != "board")
		{
			return true;
		}
		if (!Ui.Confirm(this, "Leave collage?", "The collage will be unloaded from memory. Export it first if you want to keep a JPG.", "Unload and leave",true))
		{
			return false;
		}
		ReleaseBoard();
		return true;
	}

	private void ReleaseBoard()
	{
		CancelBoardCrop();
		focusedBoard = null;
		boardEpoch++;
		groupStart.Clear();
		View.Board.Clear();
		View.BoardSelection.Clear();
		View.BoardUndo.Clear();
		View.BoardSelected = -1;
		View.BoardDirty = false;
		Cache.Clear();
		string[] array = boardCopies.ToArray();
		foreach (string text in array)
		{
			try
			{
				File.Delete(text);
				boardCopies.Remove(text);
			}
			catch
			{
			}
		}
		View.InvalidateVisual();
		Paths.Log("collage-unloaded", new
		{
			items = 0,
			undo = 0
		});
	}


 void BuildModeToolbar(){toolbar.Children.Clear();toolbar.Visibility=Mode=="viewer"?Visibility.Collapsed:Visibility.Visible;
 if(Mode=="grid"){AddFilter34(toolbar);toolbar.Children.Add(Ui.Button(ThumbnailLayoutName35,()=>CycleLayout35()));toolbar.Children.Add(Ui.Button("Names",ToggleNames));toolbar.Children.Add(Ui.Button("Sort",SortMenu));toolbar.Children.Add(Ui.Button("Select all",ToggleSelectAll));toolbar.Children.Add(Ui.Button("Move selected",ToggleQuick));toolbar.Children.Add(Ui.Button("Create new folder",CreateSubfolder));}
 else if(Mode=="board"){toolbar.Children.Add(Ui.Button("Open another viewer",LaunchCompanion));toolbar.Children.Add(Ui.Button("Select All",()=>{ToggleBoardSelection();}));toolbar.Children.Add(Ui.Button("Equal area",NormalizeBoard));toolbar.Children.Add(Ui.Button("Pack",PackBoard));toolbar.Children.Add(Ui.Button("Gap Size",GapMenu));toolbar.Children.Add(Ui.Button("Export",()=>Run(ExportBoard)));toolbar.Children.Add(Ui.Button("Clear",ClearBoard));Ui.Check(toolbar,"Respect aspect ratio",Config.RespectAspect,v=>{Config.RespectAspect=v;Config.Save();});}
 AddLatestToolbar();PanelLayout();ResponsiveToolbar();View.InvalidateVisual();}
 public async Task SetMode(string mode){if(mode=="grid"&&!hasFolder){Directory.CreateDirectory(TempFolder);await NavigateFolder(TempFolder);return;}if(mode==Mode&&!GridLoading)return;if(!LeaveBoard())return;EndPluginOverlay();if(Cropping)CancelCrop();string previous=Mode;PrepareReverse36(previous,mode);if(previous=="grid"&&mode=="viewer"&&SelectedGridImages().Length==1)viewerReturn=SelectedGridImages()[0];if(mode=="viewer")ClearGridHoles();if(previous=="viewer"&&mode=="grid")viewerReturn=current;
 int token=++modeTransition;navigatingTree=false;PauseScroll();if(mode=="board"){Quick=false;quickPanel.Visibility=Visibility.Collapsed;}Mode=mode;GridLoading=mode=="grid";GridMetadataLoading=mode=="grid";View.ToolTip=null;View.HoverTile=-1;generation++;thumbGeneration++;playerPanel.Visibility=Visibility.Collapsed;BuildModeToolbar();
 if(mode=="grid"){await StopVideo();if(token!=modeTransition)return;View.Image=null;sourceImage=null;await PrepareGrid();if(token!=modeTransition)return;GridLoading=false;View.Selected.Clear();View.LayoutTiles();int index=GridPaths.IndexOf(current??"");if(index>=0&&index<View.Tiles.Count){View.Anchor=index;View.Scroll=Math.Max(0,View.GridStart35(View.Tiles[index])-(Config.River?14:ToolbarHeight));FlashThumbnail(index);}}
 else if(mode=="board"){await StopVideo();if(token!=modeTransition)return;View.Image=null;sourceImage=null;boardLimit=50;View.BoardScale=1;View.BoardPan=new(50,80);}
 else{GridLoading=false;if(viewerReturn!=null&&Files.Contains(viewerReturn))Index=Files.IndexOf(viewerReturn);viewerReturn=null;if(hasFolder)await ShowIndex(Index,false);else await Empty();}
 if(token!=modeTransition)return;FinishReverse36(previous,mode);if(previous=="grid"&&mode=="viewer")ArmThumbnailReturn35();else if(mode!="viewer")thumbnailReturn35=null;View.Focus();View.InvalidateVisual();}

	private void ApplyTransform(string action)
	{
		if (Cropping)
		{
			TransformEdit(action);
		}
		else if (current != null && !Paths.Motion(current) && sourceImage != null)
		{
			if (action == "Flip horizontal")
			{
				flipH = !flipH;
			}
			if (action == "Flip vertical")
			{
				flipV = !flipV;
			}
			if (action == "Rotate 90°")
			{
				rotation = (rotation + 90) % 360;
			}
			TransformGroup transformGroup = new TransformGroup();
			transformGroup.Children.Add(new ScaleTransform((!flipH) ? 1 : (-1), (!flipV) ? 1 : (-1)));
			transformGroup.Children.Add(new RotateTransform(rotation));
			TransformedBitmap transformedBitmap = new TransformedBitmap(sourceImage, transformGroup);
			((Freezable)transformedBitmap).Freeze();
			View.Image = transformedBitmap;
			View.Fit();
		}
	}

	private void StartCrop()
	{


		StackPanel stackPanel = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal
		};
		stackPanel.HorizontalAlignment=HorizontalAlignment.Right;AddEditControls(stackPanel);AddCropRatios(stackPanel);Ui.Check(stackPanel,"Allow overcropping (Uses canvas color)",Config.CropFillOutside,v=>{Config.CropFillOutside=v;Config.Save();ConstrainViewerCrop();});
		stackPanel.Children.Add(Ui.Button("Cancel", CancelCrop));
		stackPanel.Children.Add(Ui.Button("Save as copy", () =>
		{
			Run(SaveCropCopy);
		}));
		stackPanel.Children.Add(Ui.Button("Apply and save", () =>
		{
			Run(ApplyCrop);
		}));
		var editTools=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};while(stackPanel.Children.Count>0){var child=stackPanel.Children[0];stackPanel.Children.RemoveAt(0);editTools.Children.Add(child);}cropPanel.Child=editTools;cropPanel.MaxWidth=Math.Max(240,ActualWidth-40);
		if (current != null && !Paths.Motion(current) && !(Mode != "viewer"))
		{
			if (largeRaster != null)
			{
				Toast("Crop is unavailable for images above 64 megapixels; save a resized copy first.");
				return;
			}
			if (Cropping)
			{
				CancelCrop();
				return;
			}
			flipH = (flipV = false);
			rotation = 0;
			View.Image = sourceImage ?? View.Image;
			cropOriginal = sourceImage;
			View.Fit();
			Cropping = true;BeginEditing();
			View.CropRect = Rect.Empty;
			cropPanel.Visibility = Visibility.Visible;
			playerPanel.Visibility = Visibility.Collapsed;
			View.InvalidateVisual();
		}
	}

	private void CancelCrop()
	{


		if (Cropping && cropOriginal != null)
		{
			sourceImage = cropOriginal;
			View.Image = cropOriginal;
			View.Fit();
		}
		motionLineMode=motionLineDragging=false;adjustmentWindow?.Close();editEpoch++;editSession++;editPreviewSource=null;cropOriginal = null;
		Cropping = false;
		dragging = false;
		View.ReleaseMouseCapture();
		View.CropRect = Rect.Empty;
		cropPanel.Visibility = Visibility.Collapsed;
		View.InvalidateVisual();
	}

	public static BitmapSource CropPixels(BitmapSource original, Rect area, bool oled = false)
	{


		int num = Math.Max(1, (int)Math.Ceiling(area.Width));
		int num2 = Math.Max(1, (int)Math.Ceiling(area.Height));
		if ((long)num * (long)num2 > 100000000)
		{
			throw new IOException("Crop is too large (100 million pixels maximum).");
		}
		DrawingVisual drawingVisual = new DrawingVisual();
		using (DrawingContext drawingContext = drawingVisual.RenderOpen())
		{
			drawingContext.DrawRectangle(new SolidColorBrush(oled ? Colors.Black : Ui.CanvasColor), null, new Rect(0.0, 0.0, (double)num, (double)num2));
			drawingContext.DrawImage(original, new Rect(0.0 - Math.Floor(area.X), 0.0 - Math.Floor(area.Y), (double)original.PixelWidth, (double)original.PixelHeight));
		}
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(num, num2, 96.0, 96.0, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(drawingVisual);
		((Freezable)renderTargetBitmap).Freeze();
		return renderTargetBitmap;
	}

	private async Task ApplyCrop()
	{
		if (!Cropping || sourceImage == null || current == null || Operations.Busy)
		{
			return;
		}
		string path = current;
		Rect rect = FullEditArea();if(rect.IsEmpty){Toast("Crop must overlap the image");return;}
		Operations.Busy = true;
		try
		{
			BitmapSource source=await EditedFullImage();
			BitmapSource image = await Sta.Run(() =>
			{

				return CropPixels(source, rect, Config.Oled);
			});
			UndoEntry entry = new UndoEntry
			{
				Navigation = navigation,
				Viewed = current
			};
			List<Change> changes = entry.Changes;
			changes.Add(await Operations.Crop(path, image));
			Operations.Commit(entry);
			RememberCropNeighbors();
			CancelCrop();
			Cache.Clear();
			ThumbCache.Clear();
			View.Thumbs.Remove(path);
			await ShowIndex(Index, manual: false);
			Toast("Edit saved — Ctrl+Z restores the original");
		}
		finally
		{
			Operations.Busy = false;
		}
	}

	private async Task Rename()
	{
		if (ActionImage == null || Operations.Busy)
		{
			return;
		}
		string src = ActionImage;
		string? text = RenamePrompt(src);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (text.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
		{
			throw new IOException("Invalid filename");
		}
		string dst = System.IO.Path.Combine(Folder, text + System.IO.Path.GetExtension(src));
		if (string.Equals(src, dst, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		if (File.Exists(dst))
		{
			dst = FileNames.Next(dst);
		}
		Operations.Busy = true;
		try
		{
			await StopVideo();
			UndoEntry entry = new UndoEntry
			{
				Navigation = navigation,
				Viewed = src
			};
			List<Change> changes = entry.Changes;
			changes.Add(await Operations.Move(src, dst, overwrite: false));
			Operations.Commit(entry);
			if (current == src)
			{
				current = dst;
			}
			Cache.Clear();
			await Scan();
			if (Mode == "viewer")
			{
				await ShowIndex(Files.IndexOf(dst), manual: false);
			}
			else
			{
				View.LayoutTiles();
			}
		}
		finally
		{
			Operations.Busy = false;
		}
	}

	public static void CleanupTemp(string? keep)
	{
		if (!Directory.Exists(TempFolder))
		{
			return;
		}
		DirectoryInfo directoryInfo = new DirectoryInfo(TempFolder);
		if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) != 0)
		{
			return;
		}
		FileInfo[] array = (from f in directoryInfo.GetFiles()
			where Paths.Media(f.FullName) && (f.Attributes & FileAttributes.ReparsePoint) == 0
			orderby f.CreationTimeUtc
			select f).ToArray();
		long num = array.Sum((FileInfo f) => f.Length);
		FileInfo[] array2 = array;
		foreach (FileInfo fileInfo in array2)
		{
			if (num <= 50000000)
			{
				break;
			}
			if (!string.Equals(fileInfo.FullName, keep, StringComparison.OrdinalIgnoreCase))
			{
				try
				{
					long length = fileInfo.Length;
					fileInfo.Delete();
					num -= length;
				}
				catch
				{
				}
			}
		}
	}

	private async Task Paste()
	{
		if(Mode=="board"){await PasteBoard();return;}
		if (clipBusy || Mode != "viewer" || Quick || Cropping)
		{
			return;
		}
		clipBusy = true;
		try
		{
			string[] array = (System.Windows.Clipboard.ContainsFileDropList() ? (from string p in System.Windows.Clipboard.GetFileDropList()
				where Paths.Media(p) && File.Exists(p)
				select p).ToArray() : Array.Empty<string>());
			BitmapSource? bitmap = ((array.Length == 0 && System.Windows.Clipboard.ContainsImage()) ? System.Windows.Clipboard.GetImage() : null);
			if (array.Length == 0 && bitmap == null)
			{
				Toast("Clipboard contains no supported image");
				return;
			}
			if (bitmap != null)
			{
				((Freezable)bitmap).Freeze();
			}
			string dir = (hasFolder ? Folder : TempFolder);
			Directory.CreateDirectory(dir);
			List<string> outputs = new List<string>();
			string[] array2 = array;
			foreach (string src in array2)
			{
				string dest = FileNames.Available(System.IO.Path.Combine(dir, System.IO.Path.GetFileName(src)));
				Paths.Guard(dest);
				await Task.Run(() =>
				{
					File.Copy(src, dest);
				});
				outputs.Add(dest);
			}
			if (bitmap != null)
			{
				string dest2 = FileNames.Available(System.IO.Path.Combine(dir, "Paste_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png"));
				Paths.Guard(dest2);
				await Task.Run(() =>
				{
					PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder
					{
						Frames = { BitmapFrame.Create(bitmap) }
					};
					using FileStream stream = File.Create(dest2);
					pngBitmapEncoder.Save(stream);
				});
				outputs.Add(dest2);
			}
			if (outputs.Count > 0)
			{
				await Open(outputs[0]);
				CleanupTemp(current);
				Toast($"Pasted {outputs.Count} file(s)");
			}
		}
		finally
		{
			clipBusy = false;
		}
	}

	private void DragHover(object sender, System.Windows.DragEventArgs e)
	{




		if(Mode=="grid"&&e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)&&IsFolderTile(View.HitTile(e.GetPosition(View)))){dropPanel.Visibility=Visibility.Collapsed;e.Effects=DragDropEffects.Move;e.Handled=true;return;}
		if (e.Data.GetData("gazegallery.SourceWindow") as string == windowId || !e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
		{
			dropPanel.Visibility = Visibility.Collapsed;
			e.Effects = System.Windows.DragDropEffects.None;
			e.Handled = true;
			return;
		}
		if (Mode == "board")
		{
			e.Effects = System.Windows.DragDropEffects.Copy;
			e.Handled = true;
			return;
		}
		if ((Mode != "viewer" && Mode != "grid") || Cropping)
		{
			e.Effects = System.Windows.DragDropEffects.None;
			e.Handled = true;
			return;
		}
		dropPanel.Width = Math.Max(1.0, root.ActualWidth * 0.9);
		dropPanel.Height = Math.Max(1.0, root.ActualHeight * 0.9);
		dropPanel.Visibility = Visibility.Visible;
		dropPanel.UpdateLayout();
		moveTarget.Opacity = (hasFolder ? 0.9 : 0.2);
		Point position = e.GetPosition(viewTarget);
		bool flag = position.X >= 0.0 && position.Y >= 0.0 && position.X < viewTarget.ActualWidth && position.Y < viewTarget.ActualHeight;
		Point position2 = e.GetPosition(moveTarget);
		bool flag2 = hasFolder && position2.X >= 0.0 && position2.Y >= 0.0 && position2.X < moveTarget.ActualWidth && position2.Y < moveTarget.ActualHeight;
		dropChoice = (flag ? 1 : (flag2 ? 2 : 0));
		viewTarget.BorderBrush = (flag ? System.Windows.Media.Brushes.LightGray : System.Windows.Media.Brushes.Transparent);
		moveTarget.BorderBrush = (flag2 ? System.Windows.Media.Brushes.LightGray : System.Windows.Media.Brushes.Transparent);
		Border border = viewTarget;
		Thickness borderThickness = (moveTarget.BorderThickness = new Thickness(1.0));
		border.BorderThickness = borderThickness;
		e.Effects = ((dropChoice != 0) ? ((dropChoice != 2) ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.Move) : System.Windows.DragDropEffects.None);
		e.Handled = true;
	}

	private async void ReceiveDrop(object sender, System.Windows.DragEventArgs e)
	{
		_ = 2;
		try
		{
			dropPanel.Visibility = Visibility.Collapsed;
			int tile=Mode=="grid"?View.HitTile(e.GetPosition(View)):-1;if(IsFolderTile(tile)&&e.Data.GetData(DataFormats.FileDrop) is string[] dropped){await MoveSelection(GridPaths[tile],dropped,true);return;}
			if (!(e.Data.GetData("gazegallery.SourceWindow") as string == windowId) && e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] array)
			{
				if (Mode == "board")
				{
					await ImportBoard(array, View.BoardPoint(e.GetPosition(View)));
				}
				else if (dropChoice == 1 && array.Length != 0)
				{
					await Open(array[0]);
				}
				else if (dropChoice == 2 && hasFolder)
				{
					await MoveSelection(Folder, array, preserveView: true);
				}
			}
		}
		catch (Exception e2)
		{
			Error(e2);
		}
		finally
		{
			dropChoice = 0;
			e.Handled = true;
		}
	}

	private void ExportDrag()
	{
		if (dragFiles == null)
		{
			return;
		}
		System.Windows.DataObject dataObject = new System.Windows.DataObject(System.Windows.DataFormats.FileDrop, dragFiles);
		dataObject.SetData("gazegallery.SourceWindow", windowId);
		dragging = false;
		exportCandidate = false;
		View.ReleaseMouseCapture();
		bool flag = true;
		try
		{
			string[] array = dragFiles;
			for (int i = 0; i < array.Length; i++)
			{
				Paths.Guard(array[i]);
			}
		}
		catch
		{
			flag = false;
		}
		DragDrop.DoDragDrop((DependencyObject)(object)View, dataObject, (!flag) ? System.Windows.DragDropEffects.Copy : (System.Windows.DragDropEffects.Copy | System.Windows.DragDropEffects.Move));
		View.InvalidateVisual();
	}

	private void MouseDownHandler(object sender, MouseButtonEventArgs e)
	{






















































		View.Focus();
		pointerTime = DateTime.Now;
		View.Cursor = System.Windows.Input.Cursors.Arrow;
		MouseButton changedButton = e.ChangedButton;
		if ((changedButton == MouseButton.Middle || (uint)(changedButton - 3) <= 1u) ? true : false)
		{
			if (Mode == "grid")
			{
				return;
			}
			string key;
			if (e.ChangedButton == MouseButton.XButton1)
			{
				key = "MouseBack";
			}
			else
			{
				key = ((e.ChangedButton == MouseButton.XButton2) ? "MouseForward" : "MouseMiddle");
			}
			if(key=="MouseBack"&&TryThumbnailBack35()){e.Handled=true;return;}string? action = ResolveViewerAction(key);
			if (Mode == "board" && focusedBoard != null)
			{
				TryBoardNavigate(key);
			}
			else if (action != null && Mode != "board" && (!Quick || new ReadOnlySpan<string>(new string[5] { "Next", "Previous", "Trash", "Undo", "Quick sort" }).Contains(action)))
			{
				Run(() => Action(action));
			}
			return;
		}
		if (e.ChangedButton != MouseButton.Left || Operations.Busy)
		{
			return;
		}
		Point position = e.GetPosition(View);if(MotionDown32(position)){e.Handled=true;return;}if(Mode=="viewer"&&Config.SlideshowTimer&&SlideOverlay.Contains(position)){SetSlideTime();e.Handled=true;return;}if(Mode=="viewer"&&Config.ZoomIndicator&&ZoomOverlay.Contains(position)&&View.Image!=null){View.Scale=largeRaster==null?1:largeRaster.Width/(double)View.Image.PixelWidth;View.Pan=new((View.ActualWidth-View.Image.PixelWidth*View.Scale)/2,(View.ActualHeight-View.Image.PixelHeight*View.Scale)/2);View.ZoomUsed=true;View.InvalidateVisual();e.Handled=true;return;}
		Rect val;
		if (Mode == "viewer" && Config.PositionIndicator)
		{
			val = View.SortOverlay;
			if (val.Contains(position))
			{
				PositionMenu();
				View.InvalidateVisual();
				e.Handled = true;
				return;
			}
		}
		if (BoardCropItem != null)
		{
			BoardCropDown(position);
			e.Handled = true;
			return;
		}
		if (Mode == "grid")
		{
			int idx = View.HitTile(position);
			if (idx < 0||GridHoles.Contains(GridPaths[idx]))
			{
				return;
			}
            if(idx==0&&IsFolderTile(idx)){if(e.ClickCount==2)Run(()=>NavigateFolder(GridPaths[idx]));return;}
            if(!(Keyboard.Modifiers==ModifierKeys.None&&View.Selected.Count>1&&View.Selected.Contains(idx)))SelectGridTile(idx,Keyboard.Modifiers.HasFlag(ModifierKeys.Control),Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
            if(IsFolderTile(idx)){if(e.ClickCount==2)Run(async()=>{await NavigateFolder(GridPaths[idx]);Toast(Folder,2);});return;}

			View.InvalidateVisual();
			if (e.ClickCount == 2 && (int)Keyboard.Modifiers == 0)
			{
				viewerReturn = GridPaths[idx];
				Run(() => EnterThumbnailView35(idx));
			}
			else
			{
				press = position;
				dragFiles = Selection();
				exportCandidate = true;
			}
			return;
		}
		if (Config.Arrows && Mode == "viewer" && !Cropping)
		{
			val = View.Arrow(next: true);
			bool next = val.Contains(position);
			if (!next)
			{
				val = View.Arrow(next: false);
				if (!val.Contains(position))
				{
					goto IL_051d;
				}
			}
			if (next)
			{
				View.RightArrowAlpha = 0.75;
			}
			else
			{
				View.LeftArrowAlpha = 0.75;
			}
			Run(async () =>
			{
				await NavigateViewer(next ? 1 : -1);
				await Task.Delay(200);
				if (next)
				{
					View.RightArrowAlpha = 0.33;
				}
				else
				{
					View.LeftArrowAlpha = 0.33;
				}
				View.InvalidateVisual();
			});
			return;
		}
		goto IL_051d;
		IL_051d:
		if (Mode == "viewer" && !Cropping && e.ClickCount == 2)
		{
			dragging = (exportCandidate = false);
			View.ReleaseMouseCapture();
			if (!View.IsFit)
			{
				View.Fit();
			}
			else
			{
				ToggleFullscreen();
			}
			return;
		}
		if (Mode == "board" && e.ClickCount == 2)
		{
			int num2 = View.HitBoard(position);
			if (num2 < 0)
			{
				ToggleFullscreen();
			}
			else
			{
				FocusBoard(View.Board[num2]);
			}
			return;
		}
		if (Mode == "viewer" && !Cropping && !fullscreen && WindowStyle == WindowStyle.None && ((!View.ZoomUsed&&(imageWindow||View.IsFit))||Keyboard.Modifiers.HasFlag(ModifierKeys.Control)||Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)))
		{
			DragMove();
			return;
		}
		press = (last = position);
		dragging = true;
		exportCandidate = false;
		resizeHandle = -1;
		panGesture = Keyboard.IsKeyDown((Key)18);
		if (Mode == "board")
		{
			if (!panGesture)
			{
				int num3 = View.HitBoard(position);
				if (num3 >= 0)
				{
					BoardItem boardItem = View.Board[num3];
					resizeHandle = GeometryRules.Handle(View.BoardScreen(boardItem.Rect), position, 14.0);
					if ((((Enum)Keyboard.Modifiers).HasFlag((Enum)(object)(ModifierKeys)2) || ((Enum)Keyboard.Modifiers).HasFlag((Enum)(object)(ModifierKeys)4)) && resizeHandle < 0)
					{
						if (!View.BoardSelection.Add(boardItem))
						{
							View.BoardSelection.Remove(boardItem);
						}
					}
					else if (!View.BoardSelection.Contains(boardItem))
					{
						View.BoardSelection.Clear();
						View.BoardSelection.Add(boardItem);
					}
					View.Snapshot();
					View.Board.RemoveAt(num3);
					View.Board.Add(boardItem);
					View.BoardSelected = View.Board.Count - 1;
					selectionStart = boardItem.Rect;
					gestureStart = View.BoardPoint(position);
					groupStart = View.BoardSelection.ToDictionary((BoardItem i) => i, (BoardItem i) =>
					{

						return i.Rect;
					});
				}
				else
				{
					View.BoardSelected = -1;
					View.BoardSelection.Clear();
					panGesture = true;
				}
			}
		}
		else if (Cropping)
		{
			if (!panGesture)
			{
				resizeHandle = (View.CropRect.IsEmpty ? (-1) : GeometryRules.Handle(View.CropScreen, position, 14.0));
				selectionStart = View.CropRect;
				gestureStart = View.ImagePoint(position);
				if (resizeHandle < 0)
				{
					val = View.CropScreen;
					if (!val.Contains(position))
					{
						resizeHandle = -2;
						View.CropRect = new Rect(gestureStart, gestureStart);
					}
				}
			}
		}
		else if (!fullscreen && !View.ZoomUsed)
		{
			val = View.ImageRect;
			if (val.Contains(position))
			{
				exportCandidate = true;
				dragFiles = ((current == null) ? null : new string[1] { current });
				dragging = false;
				return;
			}
		}
		View.CaptureMouse();
		View.InvalidateVisual();
	}

	private void MouseMoveHandler(object sender, System.Windows.Input.MouseEventArgs e)
	{



































































		pointerTime = DateTime.Now;
		View.Cursor = System.Windows.Input.Cursors.Arrow;
		Point position = e.GetPosition(View);
		if (videoActive && position.Y > View.ActualHeight - 100.0)
		{
			playerActiveUntil = DateTime.Now.AddSeconds(2.0);
		}
		if (Mode == "grid" && e.LeftButton != MouseButtonState.Pressed)
		{
			int num = View.HitTile(position);
			if (num != View.HoverTile)
			{
				View.HoverTile = num;
				View.ToolTip = null;
			}
		}
		if (e.LeftButton != MouseButtonState.Pressed)
		{
			return;
		}
		if(MotionMove32(position)){e.Handled=true;return;}if (BoardCropItem != null)
		{
			BoardCropMove(position);
		}
		else if (exportCandidate)
		{
			Vector val = position - press;
			if (val.Length > 8.0)
			{
				ExportDrag();
			}
		}
		else
		{
			if (!dragging)
			{
				return;
			}
			Vector val2 = position - last;
			last = position;
			if (Mode == "board")
			{
				if (panGesture || Keyboard.IsKeyDown((Key)18) || View.BoardSelected < 0)
				{
					Surface view = View;
					view.BoardPan += val2;
				}
				else
				{
					BoardItem boardItem = View.Board[View.BoardSelected];
					Point val3 = View.BoardPoint(position);
					if (resizeHandle >= 0)
					{
						boardItem.Rect = GeometryRules.Resize(selectionStart, gestureStart, val3, resizeHandle, Config.RespectAspect || ((Enum)Keyboard.Modifiers).HasFlag((Enum)(object)(ModifierKeys)4), 8.0 / View.BoardScale);
					}
					else
					{
						foreach (KeyValuePair<BoardItem, Rect> item in groupStart)
						{
							BoardItem key = item.Key;
							Rect value = item.Value;
							Point val4 = value.Location + (val3 - gestureStart);
							value = item.Value;
							key.Rect = new Rect(val4, value.Size);
						}
					}
					View.BoardDirty = true;
				}
			}
			else if (Cropping)
			{
				if (panGesture || Keyboard.IsKeyDown((Key)18))
				{
					Surface view2 = View;
					view2.Pan += val2;
				}
				else
				{
					Point val5 = View.ImagePoint(position);
					if (resizeHandle == -2)
					{
						View.CropRect = CropDrag(gestureStart, val5);
					}
					else if (resizeHandle >= 0)
					{
						View.CropRect = CropResize(selectionStart, gestureStart, val5, resizeHandle);
					}
					else if (!selectionStart.IsEmpty)
					{
						View.CropRect = new Rect(selectionStart.Location + (val5 - gestureStart), selectionStart.Size);
					}
				}
			}
			else
			{
				Surface view3 = View;
				view3.Pan += val2;
				View.ZoomUsed = true;
			}
			if(Cropping)ConstrainViewerCrop();
		View.InvalidateVisual();
		}
	}

	private void MouseUpHandler(object sender, MouseButtonEventArgs e)
	{
 if(Mode=="grid"&&e.ChangedButton==MouseButton.Left&&Keyboard.Modifiers==ModifierKeys.None&&exportCandidate&&(e.GetPosition(View)-press).Length<=8){SelectGridTile(View.HitTile(e.GetPosition(View)));}
 if(MotionUp32()){e.Handled=true;return;}
		boardCropDrag = false;
		
		dragging = (exportCandidate = false);
		View.ReleaseMouseCapture();
	}

	private void WheelHandler(object sender, MouseWheelEventArgs e)
	{













		if(imageWindow&&!fullscreen&&Mode=="viewer"&&!Cropping&&(Keyboard.Modifiers.HasFlag(ModifierKeys.Control)||Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))){ResizeImageWindow(Math.Pow(1.15,e.Delta/120.0),false);e.Handled=true;return;}
		if (Quick&&Mode!="grid")
		{
			return;
		}
		if (Mode == "grid")
		{

			if (((Enum)Keyboard.Modifiers).HasFlag((Enum)(object)(ModifierKeys)2))
			{
				ColumnStep((e.Delta <= 0) ? 1 : (-1));
			}
			else
			{
				ScrollGridBy(-e.Delta*2);
				View.InvalidateVisual();
			}
			e.Handled = true;
			return;
		}
		if(videoActive&&Config.WheelNavigation&&Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)&&!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)){Run(()=>NavigateViewer(e.Delta>0?-1:1));e.Handled=true;return;}if(Mode=="viewer"&&!videoActive&&!Cropping&&Config.WheelNavigation&&!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)){Run(()=>NavigateViewer(e.Delta>0?-1:1));e.Handled=true;return;}if (videoActive && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
		{
			if(video?.HasAudio!=true){Toast("Current video has no audio");toast.Margin=new Thickness(10,0,10,90);e.Handled=true;return;}Config.Volume = Math.Clamp(Config.Volume + ((e.Delta > 0) ? 5 : (-5)), 0, 100);
			video?.Volume(Config.Volume);
			volume.Value=Config.Volume;Toast($"Volume {Config.Volume}%");toast.Margin=new Thickness(10,0,10,90);
			return;
		}
		Point position = e.GetPosition(View);
		double num = Math.Pow(1.15, (double)e.Delta / 120.0);
		if (Mode == "board")
		{
			Point val = View.BoardPoint(position);
			View.BoardScale = Math.Clamp(View.BoardScale * num, 0.001, 10000.0);
			View.BoardPan = new Vector(position.X - val.X * View.BoardScale, position.Y - val.Y * View.BoardScale);
		}
		else if (View.Image != null)
		{
			(double, double) tuple = GeometryRules.Zoom(View.Image.PixelWidth, View.Image.PixelHeight, View.ActualWidth, View.ActualHeight);
			double num2 = Math.Clamp(View.Scale * num, tuple.Item1, tuple.Item2 * ((largeRaster == null) ? 1.0 : ((double)largeRaster.Width / (double)View.Image.PixelWidth)));
			num = num2 / View.Scale;
			if (e.Delta < 0)
			{
				double num3 = View.ActualWidth / 2.0;
				double num4 = View.ActualHeight / 2.0;
				View.Pan = new Vector(num3 + (View.Pan.X + (double)View.Image.PixelWidth * View.Scale / 2.0 - num3) * num * num - (double)View.Image.PixelWidth * num2 / 2.0, num4 + (View.Pan.Y + (double)View.Image.PixelHeight * View.Scale / 2.0 - num4) * num * num - (double)View.Image.PixelHeight * num2 / 2.0);
			}
			else
			{
				View.Pan = new Vector(position.X - (position.X - View.Pan.X) * num, position.Y - (position.Y - View.Pan.Y) * num);
			}
			View.Scale = num2;
			View.ZoomUsed = true;
		}
		View.InvalidateVisual();
	}

	private void KeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
	{
 if(Ui.IsCancelKey(e)&&Ui.DismissMenus()){e.Handled=true;return;}if(ThumbnailAlt35(e,true))return;bool exitAlias=Ui.IsExitAlias(e);Key pressedKey=exitAlias?Key.Escape:e.Key;if(exitAlias)e.Handled=true;











		if (pluginOverlay != null)
		{
 if(e.OriginalSource is System.Windows.Controls.TextBox && pressedKey!=Key.Escape)return;
 if(PluginShortcut(KeyName(e))){EndPluginOverlay();e.Handled=true;return;}
 if(pressedKey==Key.Z&&Keyboard.Modifiers.HasFlag(ModifierKeys.Control)){pluginUndo?.Invoke();e.Handled=true;return;}
			if ((int)pressedKey == 13)
			{
				EndPluginOverlay();
			}
			e.Handled = true;
		}
		else
		{
			if(e.OriginalSource==filterInput34&&pressedKey==Key.Escape){filterInput34.Text="";e.Handled=true;return;}
			if (e.OriginalSource is System.Windows.Controls.TextBox && pressedKey!=Key.Escape)
			{
				return;
			}
			string key = exitAlias?"Escape":KeyName(e);if(Round31Key(key)){e.Handled=true;return;}if(Mode=="board"&&pressedKey==Key.Enter){ToggleFullscreen();e.Handled=true;return;}if(Mode=="board"&&key=="Ctrl+A"){ToggleBoardSelection();e.Handled=true;return;}if(Mode=="grid"&&!Quick&&key=="W"){if(SelectedGridImages().Length==1)Run(()=>SetMode("viewer"));e.Handled=true;return;}if(Mode=="grid"&&key=="Ctrl+A"){ToggleSelectAll();e.Handled=true;return;}if(Quick&&pressedKey==Key.Tab){if(!e.IsRepeat)ToggleQuickCopy();e.Handled=true;return;}
			if (Mode == "grid" && !Quick)
			{
				if (Config.Keys["Autoscroll thumbnails"].Contains(key))
				{
					if (!e.IsRepeat)
					{
						ToggleAutoscroll();
					}
					e.Handled = true;
					return;
				}
				if (Config.Keys["Copy thumbnails"].Contains(key))
				{
					CopyGridFiles();
					e.Handled = true;
					return;
				}
				if (Config.Keys["Scroll up thumbnails"].Contains(key) || Config.Keys["Scroll down thumbnails"].Contains(key))
				{
					PauseScroll();
					scrollKey = (Config.Keys["Scroll down thumbnails"].Contains(key) ? 1 : (-1));
					scrollHeldKey = pressedKey;
					e.Handled = true;
					return;
				}
			}
			if (BoardCropItem != null)
			{
				if ((int)pressedKey == 13 || Config.Keys["Crop"].Contains(key))
				{
					CancelBoardCrop();
				}
				e.Handled = true;
				return;
			}
			if (Cropping)
			{
				string key2 = Config.Keys.FirstOrDefault((KeyValuePair<string, string[]> k) =>
				{
					bool flag3;
					switch (k.Key)
					{
					case "Rotate 90°":
					case "Flip horizontal":
					case "Flip vertical":
						flag3 = true;
						break;
					default:
						flag3 = false;
						break;
					}
					return flag3 && k.Value.Contains(key);
				}).Key;
				if (key2 != null)
				{
					TransformCrop(key2);
					e.Handled = true;
				}
				else if ((int)pressedKey == 13 || Config.Keys["Crop"].Contains(key))
				{
					CancelCrop();
					e.Handled = true;
				}
				else if ((int)pressedKey == 18)
				{
					e.Handled = true;
				}
				return;
			}
			if (Mode == "board")
			{
                if(key=="Escape"&&Config.ConfirmEscape&&!ConfirmEscapeExit()){e.Handled=true;return;}
                if(Config.Keys["Trash"].Contains(key)){RemoveBoardSelection();e.Handled=true;return;}
				if (Config.Keys["Crop"].Contains(key))
				{
					Run(StartBoardCrop);
					e.Handled = true;
					return;
				}
				if (focusedBoard != null && TryBoardNavigate(key))
				{
					e.Handled = true;
					return;
				}
				switch (Config.Keys.FirstOrDefault((KeyValuePair<string, string[]> k) => k.Key.EndsWith("collage") && k.Value.Contains(key)).Key)
				{
				case "Select all collage":
					View.BoardSelection = View.Board.ToHashSet();
					View.InvalidateVisual();
					break;
				case "Equal area collage":
					NormalizeBoard();
					break;
				case "Pack collage":
					PackBoard();
					break;
				default:
					if(key=="Ctrl+V"){Run(PasteBoard);}
					else if (key == "Ctrl+S")
					{
						Run(ExportBoard);
					}
					else if (key == "Ctrl+Z")
					{
						Run(Undo);
					}
					else if (key == "Escape")
					{
						Close();
					}
					break;
				}
				e.Handled = true;
				return;
			}
			if(key=="Escape"&&!Quick&&Config.ConfirmEscape&&!ConfirmEscapeExit()){e.Handled=true;return;}
			if (Mode=="viewer"&&!Quick&&TryPluginKey(key)){e.Handled=true;return;}
 if(key=="MouseBack"&&TryThumbnailBack35()){e.Handled=true;return;}string? action = ResolveViewerAction(key);
			bool flag = Mode == "grid";
			if (flag)
			{
				string? text = action;
				bool flag2 = ((text == "Next" || text == "Previous") ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				NavigateSelection((action == "Next") ? 1 : (-1));
				e.Handled = true;
			}
			else if (Quick)
			{
                if(pressedKey==Key.Escape){ToggleQuick();e.Handled=true;return;}
				if (action != null && new ReadOnlySpan<string>(new string[5] { "Next", "Previous", "Trash", "Undo", "Quick sort" }).Contains(action))
				{
					flag = !e.IsRepeat;
					if (!flag)
					{
						string? text = action;
						bool flag2 = ((text == "Next" || text == "Previous") ? true : false);
						flag = flag2;
					}
					if (flag)
					{
						Run(() => Action(action));
					}
				}
				else
				{
					Destination? dest = Config.Destinations(Folder).FirstOrDefault((Destination d) => d.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && d.Folder != "");
					if (dest != null && !e.IsRepeat)
					{
						Run(() => QuickTransfer(dest.Folder));
					}
				}
				e.Handled = true;
			}
			else
			{
				if (action == null)
				{
					return;
				}
				e.Handled = true;
				flag = !e.IsRepeat;
				if (!flag)
				{
					string? text = action;
					bool flag2 = ((text == "Next" || text == "Previous") ? true : false);
					flag = flag2;
				}
				if (flag)
				{
					Run(() => Action(action));
				}
			}
		}
	}

	private void OnClosing(object? sender, CancelEventArgs e)
	{
		if (shutdown)
		{
			return;
		}
		if (Operations.Busy || importsRunning)
		{
			Toast("Wait for the operation to finish.");
			e.Cancel = true;
			return;
		}
		if (Mode == "board" && !LeaveBoard())
		{
			e.Cancel = true;
			return;
		}
		if (Cropping)
		{
			if (!Ui.Confirm(this, "Exit gazegallery?", "Discard the unfinished crop and exit?", "Exit"))
			{
				e.Cancel = true;
				return;
			}
		}
		
		shutdown = true;
		Paths.Log("performance-summary", new
		{
			navigationP95ms = Stats.P95,
			Hits = Cache.Hits,
			Misses = Cache.Misses,
			memoryBytes = Process.GetCurrentProcess().WorkingSet64
		});
	}


}

