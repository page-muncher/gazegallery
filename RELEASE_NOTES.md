# gazegallery 0.42.1

- View crop editing now places Adjust image at the upper left, Hide / Show in a fixed position at the upper right, and ratio options in a vertical stack below it. Rotation stays near the bottom center and save actions at the bottom right.
- The mouse Back button retraces visited folders in thumbnail mode.
- Explorer launch grouping runs while the first image opens. Folder indexing and settings writes follow the first image. Revisiting cached images skips another header probe, and folders with removed thumbnail placeholders use faster file lookups.

379 automated checks passed. In an offscreen benchmark, median first-image readiness fell from 814 ms with blocking grouping to 459 ms with overlapping grouping across four paired samples. Separate launches still grouped into one batch. Use About → Check for updates → Update to latest.
