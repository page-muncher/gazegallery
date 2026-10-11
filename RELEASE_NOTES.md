# gazegallery 0.38.1

- Fixed “DLL was not found” when opening videos in the 0.38.0 portable release. The single-file executable now finds its bundled video runtime beside the actual executable, rather than inside .NET’s temporary extraction folder.
- Corrected the same portable path handling for plugins, settings, clipboard temporary files and opening another viewer.
- Added a headless native video diagnostic and regression checks for single-file portable paths.

Validation: reproduced the original DLL error; the patched portable executable decodes audio MP4, silent MP4 and WebM fixtures with an unrelated working directory. All 281 basic format/behavior checks pass; build has zero warnings/errors. Archive integrity and bundled runtime/plugins verified. No desktop control testing was performed.

Extract the entire ZIP, retaining the runtime and plugins folders alongside gazegallery.exe. Keep your existing data folder when replacing application files.
