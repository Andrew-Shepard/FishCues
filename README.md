# FishCues

Two fishing audio cues for Valheim, nothing else.

- a **plink** when a fish bites — the moment you can hook it
- a **loop** while the hooked fish struggles (`Fish.IsEscaping()`); **silence means it is calm**

No HUD, no panels, no keybinds, no auto-hooking. Client-side and read-only: `TryToHook`, stamina,
line length and catches are untouched, so fishing plays like vanilla. Works on vanilla servers, and
cues on your own float only (float ownership via the `s_rodOwner` ZDO).

## Demo

[37 seconds of gameplay](https://github.com/Andrew-Shepard/FishCues/releases/download/v0.1.0/FishCues-demo.mp4)
— the cues are the point, so play it with sound.

## Install

Copy `FishCues.dll` into `<game>\BepInEx\plugins\`. Needs BepInEx 5.4.2350 (BepInExPack_Valheim).
One DLL, no assets, no dependencies. `deploy.ps1 -PluginsDir <path>` does the copy.

## Config

`<game>\BepInEx\config\online.buddycloud.fishcues.cfg`, created on first launch. Restart after
editing — BepInEx reads config once at startup.

| Key | Default | |
|-----|---------|---|
| `Enabled` | `true` | |
| `PlinkClip` | `Splash_Water_Small2` | any vanilla sound name, case-insensitive |
| `PlinkVolume` | `0.15` | `0` mutes the plink |
| `StruggleClip` | `Items_Bathtub_Bubbles_Loop` | 21 s long, and only the bathtub plays it |
| `StruggleVolume` | `0.25` | `0` mutes the loop |
| `StrugglePitch` | `1.0` | 0.5–2 |

An unknown sound name leaves that cue silent and logs one line naming the key to
`BepInEx\LogOutput.txt`. Each cue tries two alternates first, so a typo is not a silent failure.

## Build

```powershell
dotnet build -c Release                                   # .NET SDK + Valheim installed
dotnet build -c Release -p:VALHEIM_INSTALL="D:\SteamLibrary\steamapps\common\Valheim"
```

Non-obvious parts of `FishCues.csproj` are explained in its own comments.

## Limits

- The bite cue hooks `RPC_Nibble`, which only fires for a nibble offered the right bait. That matches
  the vanilla hook window, so there is no plink when the bait is wrong.
- Volumes are the author's by-ear levels against the game's mix, not tuned values.
- Untraced: what else plays `Splash_Water_Small2`. The bathtub loop's seam is measured from the
  file (head and tail within 0.0025 RMS), not proven from the prefab.

MIT — see `LICENSE`. Uses no game assets: it plays whatever sounds the player's own install has.
