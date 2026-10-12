# gazegallery 0.39.1

- Fixed fullscreen thumbnail/collage toolbar spacing: symmetric padding across all groups and wrapped rows; removed fullscreen resize chrome that could clip the upper edge.
- Masonry Left/Right always targets the immediately adjacent column, choosing the nearest vertical center and wrapping at the edges.
- Returning to thumbnails gives the last viewed image keyboard focus without selecting it. Enter opens the focused image; the mode-switch hotkey returns to the last viewed image. Dashed focus outlines are 4 px thick at 50% opacity; selected focus remains brighter.
- Thumbnail-to-view entry now uses an explicit file target, dismisses preview/drag state, and completes viewport layout before fitting and handing off the zoom transition. Added lifecycle and rendered-pixel checks after the transition completes.
- Doubled collage Set ordering previews to 96 x 76.

Validation: app/checks build without warnings/errors; code-level checks include offscreen rendered toolbar geometry and thumbnail/view lifecycle. Native portable decoder, archive, source, runtime and plugin integrity checks completed. No desktop/control testing. Guides unchanged.

Extract the complete portable ZIP or use About > Check for updates > Update to latest from version 0.38.1 or newer. The updater preserves user settings. Version 0.38.0 requires a one-time manual repair due to its original installation-path bug.
