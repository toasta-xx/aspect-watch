# Aspect Shadow Watch

A BepInEx mod menu for Gorilla Tag, brought back from the dead.

Aspect Shadow Watch is a revival of the original **Aspect Shadow Watch** menu. The goal was
never to redesign it. The original look, the mod names, the text, and the layout are all
preserved as they were. What changed is everything underneath: the code was modernized and
updated so the menu works on current game builds again.

---

## What this is

- A faithful revival of the original Aspect Shadow Watch wrist menu
- Same visual identity, same mod list, same in-game watch interface
- Rewritten and repaired for the current game version
- Bundled with a standalone Harmony patch library that adds safety and security

## What changed from the original

The menu's presentation is untouched. The internals are not.

- **Fully modernized code.** Menu logic, input handling, and the mod handler were rewritten
  against current game APIs rather than patched in place.
- **Most mods preserved and repaired.** Mods that broke with game updates were fixed; a small
  number that no longer worked at all were removed rather than left in as dead entries.
- **Standalone patch library.** Every patch is now an independent, drop-in `.cs` file with its
  own `Patch()` / `Unpatch()` pair, so patches can be used separately from this menu.
- **Safety stack added.** The menu ships with a set of protective patches that did not exist
  in the original (see below).

---

## Usage

The menu lives on your wrist. Open it, scroll pages with the left joystick, and click the
joystick to toggle the highlighted mod.

### Installation

1. Install **BepInEx** for Gorilla Tag if you have not already.
2. Drop `Aspect Shadow Watch.dll` into:
   ```
   Gorilla Tag\BepInEx\plugins\
   ```
3. Launch the game.

The menu loads automatically. No configuration is required.

### Pages

The menu is a single-button pager. Each page is one mod, toggled on or off.

| Page | Mod |
| --- | --- |
| 0 | Disconnect On Grip |
| 1 | Iron Monke |
| 2 | Platforms |
| 3 | Noclip |
| 4 | ESP |
| 5 | Tag Aura |
| 6 | Tag Gun |
| 7 | Tag All |
| 8 | RGB |
| 9 | Strobe |
| 10 | Watch RGB |
| 11 | Board RGB |
| 12 | Invisibility |
| 13 | Ghost Cam |
| 14 | Teleport Gun |
| 15 | Fly |
| 16 | Sticky Platforms |
| 17 | Platform RGB |
| 18 | Slow + Vibrate Gun `[MASTER]` |
| 19 | Slow + Vibrate All `[MASTER]` |
| 20 | Sound Spammer `[MASTER]` |
| 21 | Anti Ban |
| 22 | Anti Report |
| 23 | Report Log |
| 24 | Day / Night |

Mods marked `[MASTER]` require you to be the room's master client.

---

## Safety and security

This menu ships with a protective patch stack that runs alongside it. These patches are not
part of the original menu. They exist to keep you and your session stable.

**Report and detection handling**
- Suppresses the local report pipeline, from `SendReport` through `DispatchReport`
- Blocks the client-side report audit loop and RPC table rebuild
- Prevents forced teardown and quit-on-ban paths from executing
- Blocks both public-join grace period checks
- Rate-limit bypass so features that fire RPCs rapidly do not trip the 50/second limit

**Telemetry and tracking**
- Blocks Mothership telemetry event queuing and flushing
- Blocks PlayFab device fingerprinting and screen-time tracking
- Blocks Gorillanalytics uploads and crash reporting

**Ban containment**
- Converts ban-shaped PlayFab error responses into generic errors so the client stays stable
- Suppresses ban failure screens and auth error screens
- Prevents the cosmetics ban teardown and VR constraint latch

**Crash protection**
- Validates rope indices, bounds-checks bone lookups
- Rejects malformed critters grab packets and cyclic actor graphs
- Rate-limits incoming critters RPCs per sender
- Blocks known kick payloads and table-data packets that force an app quit

**Privacy**
- Prevents mod state from being written to Photon custom player properties
- Blocks name-list validation suppression paths

All of these run locally on your own client. None of them change what other players see or how
their clients behave.

---

## Project layout

```
aspect watch/
├── aspect watch/
│   └── Aspect Shadow Watch/
│       ├── WatchLibs/           menu host, input, update loop
│       ├── WatchModHandler/     mod loader and dispatch
│       ├── WatchTutorialBoard/  tutorial board UI
│       ├── Spoof/               position serialization helpers
│       ├── Mods/                standalone mods (day/night, etc.)
│       └── AntiBan/             legacy ban patches
└── Patches/
    ├── Menu/                    menu-side patches
    ├── Safety/                  anti-cheat, telemetry, crash patches
    └── PATCHES.txt              patch reference
```

### Building

Requires the .NET SDK and a local Gorilla Tag install.

```powershell
dotnet build -c Release
```

The output is `bin\Release\netstandard2.1\Aspect Shadow Watch.dll`.

The project targets `netstandard2.1` and references game assemblies from your Gorilla Tag
install, with `Assembly-CSharp` publicized via `BepInEx.AssemblyPublicizer.MSBuild`.

---

## Credits

**Original menu — Aspect Shadow Watch.**
All credit for the original design, layout, mod selection, and concept goes to the original
Aspect Shadow Watch authors. This project is a revival, not an original work.

**iiMenu.**
The patch library that powers this menu's safety and security stack is derived from
[iiMenu](https://github.com/iimenu/project). Their work on report suppression, telemetry
blocking, ban containment, RPC limit bypass, and crash protection is what makes running this
menu materially safer. Substantial credit belongs to them.

**Community.**
Gorilla Tag modding, decompiled API reference, and the tooling that makes projects like this
possible.

---

## Notes

- This project has no affiliation with Another Axiom or the Gorilla Tag development team.
- Use at your own risk. Modding multiplayer games carries inherent risk regardless of any
  protective patches.
- The safety patches reduce exposure to several automated detection paths. They are not a
  guarantee against moderation.
