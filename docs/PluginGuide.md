# gazegallery plugin API — version 1 (gazegallery 0.26)

Plugins are optional local .NET DLLs. They run inside gazegallery with the user's permissions. They are not sandboxed. Install trusted plugins only. The viewer, file operations, video player and settings work without the example plugins.

## Installation and layout

Extract the complete portable folder before running gazegallery.exe. The application directory contains:

- `gazegallery.exe`: application and bundled .NET runtime. `runtime/` contains the native media library; keep it beside the executable.
- `plugins/`: compiled extension DLLs and this guide. Enable/disable in Settings > Plugins. Restart to discover newly installed DLLs.
- `data/settings.json`: preferences, bindings, destination sets, external programs and folder history. Never overwrite this from a plugin.
- `data/recovery/`: temporary recovery material owned by gazegallery's file-operation undo service. Do not edit or clean it from plugins.
- `temp/`: clipboard images received without an open folder. Oldest entries are cleaned when the cache exceeds 50 MB.
- `diagnostics/`: local application diagnostics.

The application directory must be writable. Resolve your plugin-owned files relative to `AppContext.BaseDirectory`, not the process working directory. A suggested location is `data/plugins/<your-unique-id>/`. Do not assume a drive letter, username or installation path. Never store generated files beside a user's source image unless the user explicitly chose that destination.

## Build a plugin

Developers need the .NET 10 SDK; users running the portable app do not. Target `net10.0-windows`, enable WPF, and reference the included `Support/Source/PluginSdk/gazegallery.PluginApi.csproj` or the API DLL in Support/PluginSdk shipped with the target gazegallery version. Implement `IGazegalleryPlugin`. Use a stable unique ID, Name, Description, ApiVersion=1, and Initialize(IPluginHost).

Example project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="gazegallery.PluginApi">
      <HintPath>PATH-TO-GAZEGALLERY/Support/PluginSdk/gazegallery.PluginApi.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

Build to a separate output directory, then copy only your plugin DLL and private dependencies into `plugins/`. Do not publish into a directory containing your source project: the SDK may exclude the source as build output. Do not replace gazegallery's API DLL with another version. Private/native dependency resolution is the plugin's responsibility; avoid globally conflicting assembly names.

## Lifecycle and available API

Initialize is called on the UI thread at startup. Register commands/decoders here; do not perform expensive decoding or change user files. Disabled plugins have their commands and decoders filtered out. This initial host is not a hot-reload or unload system. Assembly-load failures are logged.

| Member | Contract |
|---|---|
| `CurrentFile` | Current viewer file path, or null. Treat it as read-only unless the user explicitly requests an operation. |
| `CurrentImage` | Immutable still-image BitmapSource, or null for unsupported contexts, animated/video content, or region-decoded very large images. |
| `RegisterCommand(title, execute, shortcut = "", help = "")` | Adds an action beneath Plugins in the context menu. Optional viewer shortcut is installed only when no saved binding exists for that plugin command. Help text appears with the action. Bindings are editable in Settings. |
| `RegisterDecoder(decoder)` | Registers an IImageDecoder with Extensions and Decode(path, maximumEdge). Enabled plugin decoders take precedence for registered extensions. |
| `ImagePoint(viewerPoint)` | Converts overlay coordinates into source-image coordinates. |
| `ViewerPoint(imagePoint)` | Converts source-image coordinates into overlay coordinates. |
| `ZoomAt(viewerPoint, wheelDelta)` | Zooms the still-image viewer about a point, respecting viewer limits. |
| `PanBy(displacement)` | Pans the viewer by a vector in display coordinates. |
| `ImageScale` | Current source-to-view scale for the normal still-image viewer. |
| `BeginOverlay(element, controls, cancelled)` | Mounts a viewer tool plus its control panel. Ordinary viewer hotkeys are suspended while it is active. Escape closes it. |
| `SetOverlayUndo(action)` | Call AFTER BeginOverlay to handle Ctrl+Z while this tool is open. The plugin owns its history. |
| `EndOverlay()` | Removes overlay and controls, clears the undo callback, invokes cancellation/cleanup callback. |
| `SaveCopyAsync(bitmap)` | Opens PNG/JPEG Save As. Returns true after a successful save, false for cancellation/failure. Rejects overwriting CurrentFile. Freeze the bitmap before passing it. |
| `Toast(text)` | Displays a short status message. |

Commands currently operate from the still-image viewer. Plugins cannot register thumbnail/collage tool modes, video decoders, settings tabs, folder navigators, or new core toolbar controls through API version 1. There is no InvokeBuiltinCommand API. Do not depend on reflection into MainWindow or internal implementation details; request a future supported API instead.

## File loading, performance and decoder extensions

IImageDecoder.Extensions uses leading dots, for example `.example`. Decode may be called concurrently from worker threads. maximumEdge=0 requests full size; otherwise return a preview whose longest edge does not exceed the limit. Return a frozen or freezable BitmapSource, close streams promptly, and throw a descriptive exception for unsupported/damaged input. Do not manipulate controls from decoding threads. Avoid decoding full-resolution pixels merely to make a small preview.

gazegallery indexes folder metadata, builds stable thumbnail geometry separately from its bitmap cache, prioritizes near-view thumbnails and preloads adjacent still images. Only the active video is played. Navigation can supersede an earlier request; do not assume CurrentFile still equals a captured path after awaiting background work. A plugin should capture the path/pixels it operates on and check its own cancellation/generation before publishing results.

## File moves, trash, crops and undo

Core quick-sort moves, crop replacement and trash are serialized by gazegallery's file-operation service, release playback handles, and record a five-operation in-memory undo history. Batch operations form one undo entry. Copies do not enter that history. Recovery backups expire with their undo entries or the session. Windows recycling has safety checks and does not silently fall back to permanent deletion.

These internal operations are NOT exposed to plugins in API version 1. Plugin filesystem writes are not automatically registered for core undo. Keep tools non-destructive where possible; use SaveCopyAsync for image edits. Never manipulate the Recycle Bin directly. Close handles so gazegallery can move the source file while the plugin is inactive. Do not alter `data/recovery`, internal thumbnails or settings to imitate core commands.

## Included examples

**Simple Draw** registers `Draw on image` and default U. It draws vector ink on a separate overlay, so pointer movement does not copy the full source bitmap. Brush and Line share a size setting and pointer-size indicator; Line previews until release. The eraser removes only overlay strokes. Mouse wheel zooms and Space-drag pans. Manual numeric size edits establish a half-to-double slider range. It keeps five stroke-undo entries, including eraser strokes; Ctrl+Z and Undo stroke use the same history. Apply composites source and layer into a new saved copy. Cancel discards the layer.

**Wallpaper** registers `Set as wallpaper`. Yes opens a preview and layout controls; only Apply changes Windows wallpaper. Fit/Fill/Stretch/Center/Tile/Span are available with selectable aspect ratios. Center/Tile previews assume 1920 pixels across; Span uses the chosen combined-desktop ratio. The plugin stores its generated wallpaper in its own data directory. It does not edit the source image.

The included source projects demonstrate these API calls. They are examples, not required parts of the viewer. Recompile against the shipped API when updating gazegallery; this early API may evolve.
