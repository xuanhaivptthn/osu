# Feature: Mod-Adjusted Star Rating for Tournament Client

Branch: `feature/more-map-stats`

## Overview
In the osu! tournament client (`osu.Game.Tournament`), the `SongBar` previously displayed an unadjusted base star rating accompanied by a hardcoded asterisk (`*`) when difficulty- or rate-affecting mods (like HardRock or DoubleTime) were applied:
```csharp
if (convertedMods.Any(x => x is ModHardRock) || convertedMods.Any(x => x is ModDoubleTime))
    srExtra = "*";
```

This branch implements mod-adjusted star rating calculation in `SongBar` by querying the official osu! v2 difficulty attributes endpoint when mods are active.

---

## Changes Made

### 1. `osu.Game/Online/API/Requests/GetBeatmapAttributesRequest.cs` (New File)
- Implemented `GetBeatmapAttributesRequest`, targeting `POST /api/v2/beatmaps/{beatmap}/attributes`.
- Parameters sent in the request body:
  - `ruleset_id`: The ID of the current ruleset (e.g., 0 for osu!, 1 for Taiko, 2 for Catch, 3 for Mania).
  - `mods`: The legacy mods bitmask (`LegacyMods`).
- Deserializes the response into a `DifficultyAttributes` object (containing `StarRating`, `MaxCombo`, etc.).

### 2. `osu.Game.Tournament/Components/SongBar.cs`
- Injected `IAPIProvider` to allow queuing online API requests.
- Replaced the hardcoded `srExtra = "*"` logic with a dynamic query to `GetBeatmapAttributesRequest`:
  - When `mods != LegacyMods.None`, queues an attribute lookup request for the active beatmap and ruleset.
  - Upon success, asynchronously swaps the star rating display (`starRatingContainer.Child`) to show the accurate mod-adjusted star rating.
  - Automatically cancels pending attribute requests when switching maps or mods to prevent stale updates.

---

## Technical Context: Stat Calculations in Tournament Mode

| Stat | How It Is Calculated with Mods | Needs Local `.osu` File? |
| :--- | :--- | :--- |
| **BPM / Length** | Calculated client-side using playback rate (`BPM * rate`, `Length / rate`). | No |
| **CS / HP** | Calculated client-side using `IApplicableToDifficulty` mod multipliers (`CS * 1.3`). | No |
| **AR / OD** | Calculated client-side via `Ruleset.GetAdjustedDisplayDifficulty` by converting AR/OD to millisecond hit windows, dividing by rate, and mapping back. | No |
| **Star Rating** | Requires running the strain algorithm across every hit object, spacing, and rhythm pattern. Because tournament mode does not download or store local `.osu` files, mod-adjusted star rating is fetched from the API attributes endpoint. | Yes (hence API lookup) |

---

## Verification
- Built: `dotnet build osu.Game.Tournament/osu.Game.Tournament.csproj` (0 errors, 0 warnings).
- Tested: `dotnet test osu.Game.Tournament.Tests/osu.Game.Tournament.Tests.csproj --filter "FullyQualifiedName~SongBar"` (passed).
