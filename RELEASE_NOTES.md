# gazegallery 0.41.1

- Dotted thumbnail focus now leaves a 1 px hollow gap around the image container, retaining its 3 px stroke and 65% opacity. Selected-image highlighting is unchanged.
- The thumbnail hotkey (default Z) opens thumbnails from view/collage mode. In thumbnails, it goes up one folder; at a drive root it stays put. Collage still uses the existing leave confirmation.
- Trashing a focused thumbnail moves focus to the next surviving image, or the previous image if it was last. Focus survives folder rescans and skips deleted placeholders, preventing arrow navigation from restarting at the top.

Validation: 358 code-level checks passed; app/checks build without warnings/errors. Offscreen outline geometry, thumbnail navigation, focus continuity and packaged video decoding verified. No desktop testing. The existing Windows Recycle Bin undo roundtrip remains excluded after the OS item-lookup failure recorded in 0.41.0; recycling code is unchanged. Guides unchanged.

Update through About > Check for updates > Update to latest, or extract the complete portable release archive. The updater preserves settings.
