# Tournament Protects — Feature Notes

This document summarizes the changes introduced in the `feature-protects` branch for the osu! tournament client.

---

## 1. Overview

The **tournament protects** feature introduces a dedicated **Protect phase** prior to the standard Ban and Pick phases in tournament matches. Teams alternate protecting beatmaps from the round's pool to prevent them from being banned.

### Phase Progression Flow
```
Protect Phase (Red/Blue alternate)  ──>  Ban Phase (Red/Blue alternate)  ──>  Pick Phase (Red/Blue alternate)
```

---

## 2. Summary of Modified Files

| File | Key Changes |
|---|---|
| `osu.Game.Tournament/Models/BeatmapChoice.cs` | Added `ChoiceType.Protect` to the choice type enum. |
| `osu.Game.Tournament/Models/TournamentRound.cs` | Added `ProtectCount` (`BindableInt`) to configure the number of protects per team per round. |
| `osu.Game.Tournament/Screens/Editors/RoundEditorScreen.cs` | Added `# of Protects` settings slider in the round editor UI. |
| `osu.Game.Tournament/Screens/MapPool/MapPoolScreen.cs` | Added Protect buttons, implemented the Protect → Ban → Pick phase logic, updated auto-advance and IPC gating. |
| `osu.Game.Tournament/Components/TournamentBeatmapPanel.cs` | Added a protecting team indicator badge beside the mod icon and handled simultaneous protect + pick display. |

---

## 3. Detailed Changes by Component

### 3.1 Data Model
- **`BeatmapChoice.cs`**:
  - `ChoiceType` enum updated to:
    ```csharp
    public enum ChoiceType
    {
        Pick,
        Ban,
        Protect,
    }
    ```
  - Serialized via `StringEnumConverter` so entries in `bracket.json` save cleanly as `"Protect"`.
- **`TournamentRound.cs`**:
  - Added `ProtectCount`:
    ```csharp
    public readonly BindableInt ProtectCount = new BindableInt(1) { Default = 1, MinValue = 0, MaxValue = 3 };
    ```
  - Allows tournament organizers to configure 0 to 3 protects per team (default 1). If set to 0, the protect phase is bypassed automatically.

---

### 3.2 Round Editor UI
- **`RoundEditorScreen.cs`**:
  - Added a `# of Protects` slider directly beside the existing `# of Bans` slider to configure `round.ProtectCount`.

---

### 3.3 MapPool Screen & Match Flow
- **`MapPoolScreen.cs`**:
  - **Reordered Control Buttons:** The mode selection buttons are arranged in phase order:
    1. `Red Protect`
    2. `Blue Protect`
    3. `Red Ban`
    4. `Blue Ban`
    5. `Red Pick`
    6. `Blue Pick`
  - **Phase Auto-Advance (`setNextMode`):**
    - Protect phase continues until `totalProtectsRequired = ProtectCount * 2` protects have been recorded.
    - Ban phase activates once protects are fulfilled and runs until `totalBansRequired = BanCount * 2`.
    - Pick phase runs once all protects and bans are placed.
  - **IPC Auto-Pick Gating (`beatmapChanged`):**
    - Automated selection via osu!stable IPC only triggers after **both** all protects and all bans have been completed.
  - **Screen Transition Safeguard:**
    - The 10-second auto-transition to `GameplayScreen` is scheduled **only** on a `Pick`.
    - If a `Ban` or `Protect` action occurs, any pending countdown to gameplay is explicitly cancelled.
  - **Coexistence of Protect and Pick:**
    - Duplicate check in `addForBeatmap` permits a beatmap to have both a `Protect` entry and a subsequent `Pick` or `Ban` entry.

---

### 3.4 Beatmap Panel Visual Representation
- **`TournamentBeatmapPanel.cs`**:
  - **Protect Badge:** A rectangular pill badge (`RED PROTECT` / `BLUE PROTECT`) is anchored to the bottom-right of the card.
  - **Offset from Mod Icon:** Positioned with a right margin of `74px` when a mod icon is present, ensuring zero overlap with the 60px mod icon container.
  - **Z-Index Layering:** Rendered on top of panel content via scene addition order.
  - **Color & Contrast:**
    - The badge background uses the protecting team's color (`COLOUR_RED` / `COLOUR_BLUE`).
    - The text uses bold white font for crisp readability against the red/blue background.
  - **Simultaneous Protect + Pick Display:**
    - When a team picks a map previously protected by the opposing team (e.g. Red picks Blue's protected map):
      - **Card Border (6px):** Shows the picking team's color (Red).
      - **Corner Badge:** Continues showing the protecting team's color and label (`BLUE PROTECT`).
