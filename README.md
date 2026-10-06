# gazegallery

A portable Windows image viewer for browsing art, sorting folders and arranging reference collages. Fast everyday navigation, still images, GIF/WebP animation and MP4/WebM playback in one window.

**[Download the portable ZIP](https://github.com/page-muncher/gazegallery/releases/latest)** · [Complete feature guide](docs/Full_Guide.html) · [Quick start](docs/Readme.txt)

Extract the whole ZIP to a writable folder and run **gazegallery.exe**. The release includes the Windows x64 runtime and three plugins; no separate .NET installation is needed. Open the bundled Full_Guide.html in a browser for the offline three-column guide.

## Features

- Grid, masonry and horizontal river thumbnails, filters, autoscroll, Alt previews and batch selection/dragging.
- Mouse-centred zoom, fit presets, fullscreen/image-sized windows, film strip, bookmarks and customizable overlays.
- Quick-sort destinations and sets, file-conflict previews, clipboard support and five-operation session undo.
- Video seeking, frame stepping and A/B looping; slideshows with selectable pan, zoom, blur and transition effects.
- Live image adjustments/cropping and a collage canvas with packing, normalization and JPG/PNG export.
- Metadata, histogram, configurable shortcuts and portable settings export/import.
- Bundled Simple Draw, Wallpaper/Lock Screen and Color Picker plugins; documented C# plugin API.

## Supported media

JPG/JPEG, PNG, BMP, GIF, WebP, SVG, HEIC/HEIF, AVIF, JP2, TIFF/TIF, ICO, MP4 and WebM.

## Build and plugins

Install the .NET 10 SDK on Windows. Follow [BuildSource.txt](docs/BuildSource.txt). Native mpv provenance and build links are in [ThirdPartyNotices.txt](docs/ThirdPartyNotices.txt). Plugin authors: [PluginGuide.md](docs/PluginGuide.md).

Prepared portable packages can be published with `scripts/Publish-Release.ps1` using an authenticated GitHub CLI. Generated binaries and personal settings do not belong in Git history.

## Feedback and validation

Report issues with version, format and reproduction steps. Avoid posting private media. Version 0.37 passed 248 code-level checks; desktop testing was not performed. The development environment blocked Windows Recycle Bin round-trip verification, so verify recovery using disposable copies before relying on it.

## License and credits

gazegallery is licensed **GPL-3.0-or-later**. See [LICENSE](LICENSE). Bundled third-party components retain their own licenses and notices.

Coder: Codex. QA #1 and Ideas Guy: **pagemuncher**.
