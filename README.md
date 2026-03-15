# AssetStudio - Izan's Version

## Changes in this fork

- Shader parser hardening:
  - guarded count reads to prevent huge allocations on malformed data
  - parser fallback mode instead of hard failure
  - fallback shader text export when conversion fails
- Unreadable object handling:
  - preserve unreadable assets as placeholders instead of dropping them
- Texture and sprite export:
  - managed texture decoder fallback when native decoder is unavailable
  - safe sprite crop bounds handling to avoid crop exceptions
  - faster lossless PNG export settings
- Export pipeline reliability:
  - reduced filesystem overhead in bulk exports
  - sanitized and path-length-safe file names
  - repeated "(Clone)" suffixes compacted in export names
- Model export fallback:
  - Animator can export as OBJ/MTL/textures when FBX native runtime is missing
- Logging improvements:
  - better GUI/console error visibility
  - TypeTree mismatch warning deduplication and cap
- Compatibility and parser updates across core modules:
  - improved Unity version detection and related file parsing robustness

## Build

- Visual Studio 2022+
- .NET Desktop runtimes required by solution targets
- Optional for native FBX export: AssetStudioFBXNative binaries

## License

MIT. See [LICENSE](./LICENSE).

