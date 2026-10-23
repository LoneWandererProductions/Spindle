# Split notes: Grimm -> Spindle

## What was done
Built from the `Grimm-master.zip` snapshot (no `.git` folder, so no history).

* Copied the software-render stack: `FolkTales`, `Dungeons`, `Ortho`, `Terrain` plus `Imaging*`, `Core.MemoryLog`, `Extended.*`, `MemoryManager*`, `Mathematics.Constants`.
* Removed stale project references that were never used in code:
  `FolkTales -> RenderEngine`, `Dungeons -> RenderEngine`, `Ortho -> RenderEngine`, `Terrain -> GlRender`.
  (`grep` finds no OpenTK / GlRender / RenderEngine type use in these four projects, only comments.)
* Added `Spindle.Viewer`, a software-only host derived from `Grimoire` (which mixed both pipelines and used the GL-side `*Viewport(New|Gl)` classes).
* New `Spindle.sln`. Namespaces and assembly names were left unchanged on purpose, to keep the split low-risk. Rename them later if you like.

## Not copied (GL side stays in Grimm)
`GlRender`, `GlRender.Core`, `GlRender.Scenery`, `GlRender.Unity`, `RenderEngine`, `Grimoire`, `Tales`, `Pathfinding`, `DataFormatter`, `Imaging.Plugins` (WebP decoder, loaded at runtime; add it if you need .webp textures), `Imaging.Cifs/Compare/Tests/Texture.Tests`, the stray GUID-named PNGs at the repo root and the Skara Brae notes.

## Caveats (please read)
1. **Not compiled.** The sandbox had no .NET SDK. Do `dotnet build Spindle.sln` first; if anything fails, the likely cause is a type that quietly lived in a project I did not copy.
2. **Tests not carried over.** `FolkTales.Tests` links source files from `Tales/` (some of which use GlRender) and references `GlRender.Core`. 20 of its 50 files are GL-free (map, light, settings tests) and could be ported once the builders/map JSON are decoupled.
3. **Drift.** Comments in `FolkTales` say the GL viewports use shared services in `GlRender.Core.Shared` (e.g. `DungeonVisibility`, collision, perception). The software viewports do not use them, so newer features (fog/visibility, hybrid worlds, scenery, multi-level surfaces) are probably not rendered here. `FolkTales` is the shared model and has moved on with the GL branch; decide whether Spindle keeps its own copy or pins an older version.
4. **Scenery.** `Spindle.Viewer` shows no scenery: the software viewports expose no `AddScenery` (Grimoire's scenery calls were on the GL-side viewports).

## Keep the git history (recommended)
Do this on a fresh clone of the **software** branch, not from the zip, so you get that branch's own versions of the files:

```
pip install git-filter-repo
git clone --branch <software-branch> <repo-url> spindle
cd spindle
git filter-repo \
  --path FolkTales --path Dungeons --path Ortho --path Terrain \
  --path Imaging --path Imaging.Objects --path Imaging.Texture --path Imaging.Plugins.Interface \
  --path Core.MemoryLog --path Extended.Extensions --path Extended.Unmanaged \
  --path Mathematics.Constants \
  --path MemoryManager --path MemoryManager.Core --path MemoryManager.Lanes --path MemoryManager.Types \
  --path LICENSE.txt --path .gitignore --path .gitattributes
```
Then copy in `Spindle.Viewer/`, `Spindle.sln`, `README.md`, and re-apply the four csproj reference removals above, commit, and add your new remote.
