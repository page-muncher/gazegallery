# gazegallery 0.40.0

- Unselected thumbnail keyboard-focus outlines are now 65% opaque and 3 px thick, removing one inward pixel while keeping the outer edge in place. Selected-image highlights are unchanged.
- Held-arrow navigation waits two seconds when wrapping between the first and last image files. Ordinary row/column wrapping and separate key presses stay immediate. Releasing the arrow or switching away resets the wait.
- The configured thumbnail-mode hotkey now works in collage, using the existing leave confirmation and returning focus to the image that started the collage.
- Ctrl+F opens and focuses the thumbnail filter. If it is already open, the same input is focused and selected instead of opening another popup. The shortcut can be changed in Bindings.
- Video duration appears after file size in the default viewer title: mm:ss, or h:mm:ss for hour-long videos. Video duration can be disabled or reordered in Title bar settings.

Validation: 344 basic checks pass; app/checks build with zero warnings/errors. Checks cover end-wrap timing, outline opacity/geometry, title formatting/toggles, collage-origin restoration and existing thumbnail/view lifecycle. Packaged video decoding and ZIP/source/runtime/plugin integrity verified. No desktop/control testing. Guides unchanged.

Extract the complete portable ZIP or use About > Check for updates > Update to latest from version 0.38.1 or newer. The updater preserves user settings. Version 0.38.0 requires a one-time manual repair due to its original installation-path bug.
