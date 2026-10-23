# Spindle

> Spinning CPU cycles into pixels: a pure **software-rendered** tile and terrain engine for .NET.

Spindle is the software-rasterizer half of the former *Grimm* project, split out into its own repository. It has **no OpenGL / OpenTK dependency**: every pixel is produced by `SoftRasterizer` (triangle fill, texturing, per-pixel Z-buffer) and shown through a plain `Bitmap`.

## What is in here

| Project | Role |
|---|---|
| `FolkTales` | Core model: maps (`DungeonMap`, `TerrainMap`), actors, camera, culling, `SoftRasterizer` |
| `Dungeons` | First-person perspective dungeon renderer + viewport |
| `Ortho` | Orthographic (parallel projection) dungeon renderer + viewport |
| `Terrain` | Heightmap terrain renderer + viewport |
| `Imaging`, `Imaging.Objects`, `Imaging.Texture`, `Imaging.Plugins.Interface` | Bitmap / texture buffers, `NativeBitmapDisplay` WPF control |
| `Core.MemoryLog`, `Extended.*`, `MemoryManager*`, `Mathematics.Constants` | Shared helper libraries |
| `Spindle.Viewer` | Small WPF host: Terrain, Dungeon (solid / wireframe), Dungeon Ortho |

## Run it

Requires Windows and the .NET 9 SDK (WPF).

```
dotnet build Spindle.sln
dotnet run --project Spindle.Viewer
```

Controls: WASD / arrow keys (movement and look). The renderer resolution is fixed at 800x600 per mode.

## Status

This code was split from a shared repository.

## License

See `LICENSE.txt`.
