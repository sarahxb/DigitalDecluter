# Disk Reclaimer

A Windows desktop app that scans your drives and **recommends** files and folders that are safe to
remove. It never deletes anything on its own — it's a research tool, not a cleaner.

Built with .NET 8, C#, and WPF (MVVM).

## Features

### Implemented

**Scanning & categorization**
- Recursive filesystem scan of any folder or drive, resilient to inaccessible directories and
  reparse points (no symlink cycles)
- Per-file categorization (Document, Media, Archive, Installer, CodeProject, Log, Temp, SystemJunk, ...)
  via pluggable rules
- Folder-pattern detection: Git repos, Visual Studio / IntelliJ projects, Docker contexts,
  `node_modules`, Python virtualenvs, build output folders — with aggregate size/file-count/activity
  rolled up per folder, and nested folders of the same kind de-duplicated automatically
- Configurable exclusion rules (exact paths or glob patterns), always including a hardcoded system
  floor (Windows, Program Files, ProgramData, AppData system folders) that can't be overridden

**Detectors** (each reports raw findings; nothing is scored or merged until the recommendation engine)
- **Large files** — flags files at or above a size threshold
- **Duplicate files** — three-stage pipeline: group by size, then by content hash (xxHash), then
  confirm every hash match with an actual byte-for-byte comparison
- **Stale files** — flags files untouched for a long time, using whichever of last-modified /
  last-accessed is more recent (works around NTFS access-time tracking being off by default)
- **Temp/cache/junk folders** — recognizes `%TEMP%`, `Downloads`, browser/package-manager caches,
  `__pycache__`, etc., and reports the whole folder as one finding
- **Installers** — `.msi`/`.iso` always; `.exe`/`.zip` only when the filename looks like an installer
  (avoids flagging ordinary program binaries)
- **Old projects** — Git repos / IDE projects / Docker contexts with no activity anywhere in the
  tree for a long time

**Recommendations**
- Findings from multiple detectors on the same file are merged into one recommendation; the number
  of detectors that independently agree on a target drives its confidence tier (Low/Medium/High)
- Recommendations are prioritized by reclaimable space
- **Reveal in Explorer** — jump straight to any file/folder from the Files, Folders, Recommendations,
  or Insights grid
- **CSV export** of the current recommendation list

**Folder insights**
- A descriptive (non-prescriptive) view alongside recommendations: per detected folder, total size,
  file count, and a breakdown of what's in it by category

**History & persistence**
- Every scan's summary (root path, timing, file/folder/recommendation counts, total reclaimable
  bytes) is recorded to a local SQLite database and browsable in a History tab

**Safety**
- Recommendations only — the app never deletes or moves anything automatically

### Not yet implemented
- Actually deleting anything (even routed through the Recycle Bin with confirmation — the original
  design intent, still just a recommendation list today)
- Packaging/installer for distribution (run from source or a dev build only)
- Incremental/always-on indexing (every scan is a full rescan of the chosen root)
- Photo organization, iPhone/iCloud integration, backup/export beyond CSV, AI-assisted
  recommendations (explicitly out of scope for v1)

## How to use

### Prerequisites
- Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build & run
```
git clone git@github.com:sarahxb/DigitalDecluter.git
cd DigitalDecluter
dotnet build DigitalDecluter2.sln
dotnet run --project DiskReclaimer.UI
```
(Or open `DigitalDecluter2.sln` in Rider / Visual Studio and run the `DiskReclaimer.UI` project.)

### Using the app
1. **Browse...** to pick a folder or drive to scan, then **Scan**. Progress and results stream into
   the tabs below; **Cancel** stops an in-progress scan.
2. **Recommendations** tab — the prioritized, confidence-scored list of what could be removed and
   why. Click **Reveal** on any row to open it in Explorer.
3. **Files** / **Detected Folders** tabs — the full scoped file list and every detected project/junk
   folder, for browsing independent of any recommendation.
4. **Insights** tab — "what do I have" per detected folder (size, file count, category breakdown),
   as opposed to the Recommendations tab's "what should I do."
5. **History** tab — every past scan's summary, most recent first.
6. **Exclusions** tab — add a path or glob pattern (e.g. `C:\data\*.tmp`) with an optional reason to
   keep it out of future scans; built-in system-protection rules are listed but can't be removed.
7. **Export CSV...** saves the current Recommendations list to a file.

### Where data lives
The app stores its local config, database, and logs under `%LocalAppData%\DiskReclaimer\`:
- `exclusions.json` — user-added exclusion rules
- `diskreclaimer.db` — SQLite scan history
- `log-*.txt` — daily rolling Serilog logs

### Running tests
```
dotnet test DigitalDecluter2.sln
```

## Architecture

Layered, pluggable-detector design:

```
FileScanner (Infrastructure)
    -> FileRecord[]
        -> Categorizer (Application, pure logic)
            -> CategorizedFile[] + DetectedFolder[]
                -> Exclusion filter (applied once, centrally)
                    -> scoped index
                        -> Detectors            -> Finding[]       \
                        -> FolderInsightsService -> InsightSummary  |-> UI
                        -> RecommendationEngine  -> Recommendation[] /
```

- **Core** — models and interfaces only, no dependencies
- **Application** — categorization, detectors, recommendation engine, folder insights, scan
  orchestration
- **Infrastructure** — filesystem scanning, SQLite persistence, config-backed exclusion rules
- **UI** — WPF, MVVM (CommunityToolkit.Mvvm), talks only to Application/Core abstractions

## Roadmap

Next up, roughly in order:
1. A real delete workflow — Recycle Bin, explicit per-item confirmation
2. Packaging (installer / single-file publish) for distribution outside a dev environment
3. Performance passes for very large drives (progress reporting during long scans, parallelism)
4. Revisit the full-rescan model vs. an incremental index for repeat scans of the same root

Further out (originally scoped as post-v1): photo organization, iCloud/iPhone integration, richer
backup/export, AI-assisted recommendations.
