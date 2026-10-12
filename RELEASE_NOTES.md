# gazegallery 0.39.0

- Thumbnail toolbar now uses compact Display, Navigation and Actions menus, retaining Folders/View mode on the left and To top/Fade Away on the right. Fixed fullscreen toolbar vertical padding in thumbnail and collage modes.
- Arrow keys navigate thumbnails spatially in Grid, Masonry and River, with wrapping and automatic reveal. Enter opens the focused image or folder; Ctrl+Space toggles selection. Focus is distinct from selection and selected/focused thumbnails receive a stronger highlight. Space controls autoscroll without activating toolbar controls.
- Space on a still image starts a slideshow, then pauses/resumes it. Animated media retains playback controls outside slideshows. Shift+G toggles folder timeline.
- Filmstrip follows the presented filename and reconciles changed file positions after moves/rescans instead of highlighting a stale index. Older overlapping folder scans cannot replace newer results.
- Right-click Image-sized borderless reliably opens Window fits image / Image fits window choices.
- Set ordering shows numbered thumbnail/filename rows and suppresses the collage toolbar/fullscreen exit while the dialog is open, including during dragging.
- Removed unnecessary separators from Diagnostics and Navigation Arrows overlay menus.

Validation: 304 basic format/behavior checks pass; app/checks build with zero warnings/errors. Portable native decoder and ZIP/source/runtime/plugin integrity checks completed. No desktop/control testing was performed. Guides unchanged for this feedback round.

Extract the complete portable ZIP or use About > Check for updates > Update to latest from version 0.38.1 or newer. User settings are retained by the updater. Version 0.38.0 requires a one-time manual repair because its original updater resolves the wrong installation folder.
