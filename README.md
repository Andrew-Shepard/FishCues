# FishCues

Minimal mod to add audio fishing cues for Valheim

- a plink when a fish bites the hook
- a sound plays when the hooked fish struggles

## Install

Copy `FishCues.dll` into `<game>\BepInEx\plugins\`. Needs BepInEx 5.4.2350
(BepInExPack_Valheim). One DLL, no assets, no dependencies. Client-side only, so it is safe on
vanilla servers.

## Config

`<game>\BepInEx\config\online.buddycloud.fishcues.cfg`, created on first launch. Restart after
editing. `PlinkClip` and `StruggleClip` accept any vanilla sound name, case-insensitive; the two
volume keys take `0` to silence one cue.

[37 second demo](https://github.com/Andrew-Shepard/FishCues/releases/download/v0.1.0/FishCues-demo.mp4)
— play it with sound.

## AI disclosure

The code, patches and docs were written by an AI coding agent running **Qwen3.8-Flash-Next**, served
locally by Radiance, and reviewed and tested in-game by a human. Candidate sounds were found in the
game's own asset manifest and measured before anything was played, but the sounds were picked by ear.
