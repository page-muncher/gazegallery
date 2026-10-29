# gazegallery 0.41.0

- Quick Sort preserves an already-hidden cursor. Up/Down starts keyboard destination highlighting, skips unset slots, wraps, and Enter uses the selected destination in the current Move/Copy mode.
- Thumbnail copy, trash, Quick Sort and C-to-collage use explicit image selections first, falling back to the dotted-focus image when nothing is selected.
- Held-arrow thumbnail navigation stops at wrap boundaries. Release and press again to wrap. Existing scrolling/autoscroll timing is unchanged.
- Short Space presses toggle slideshow/playback on release. Holding Space for half a second ends the slideshow without first starting or pausing it.
- Slideshow options now separate End Slideshow from Close Overlay; closing the timer alone leaves playback running.
- Minus/plus (including the number pad) increase/decrease thumbnail counts using existing column/row limits.
- Multiple files opened together from Explorer are grouped into a new collage. Single-file opening remains a normal viewer. Supports both multi-argument launches and closely spaced separate Explorer processes.
- Space accepts the collage Unload and leave confirmation.

Validation: 351 code-level checks passed, with zero build warnings/errors. Separate headless processes verified launch aggregation and single-file isolation; portable native-video decoding and archive integrity verified. The existing Windows Recycle Bin folder-undo roundtrip was blocked by an OS item-lookup failure (0x80070002); recycling code is unchanged. No desktop testing. Guides unchanged.

Use About > Check for updates > Update to latest from 0.38.1 or newer, or extract the complete release archive. User settings are preserved by the updater.
