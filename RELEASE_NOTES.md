# gazegallery 0.38.0

- Folder timeline works independently of film strip, including while the strip auto-hides.
- Fixed thumbnail toolbar vertical spacing and clipping.
- Same-folder moves/copies show “Already in folder: FOLDERNAME” without changing files.
- About includes stable GitHub update checks and a verified portable updater that preserves settings/custom plugins, backs up replaced files, rolls back installation failures and relaunches.
- Quarter-turn viewing uses the existing image texture instead of copying the bitmap.
- Added auto-hide for Zoom, Image Position and Film Strip; film-strip auto-hide is unavailable in shrink-image mode.
- Restored image-sized borderless context-menu entry with right-click sizing options.
- Quick Sort’s Trash control is clickable.
- Collage supports custom row/column alignment and a thumbnail ordering list with drag/reorder controls.
- Overlay right-click options stay in the existing menu cascade; toast opacity is consistent.

Extract the entire portable ZIP and run gazegallery.exe. All three plugins and runtime dependencies are included. Existing users can import exported settings into a fresh extraction; future versions can use the new updater.

Validation: 279 code-level checks passed, including disposable updater installation and rollback tests. No desktop control testing was performed. Recycle Bin round-trip verification remains blocked by the restricted development shell.
