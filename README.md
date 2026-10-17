# Long Shot

Score from your own half. Aim past a line of defenders, bank shots off the boards, bend them around a goalkeeper, and see how far you get before someone gets a foot to it.

**[Play it in your browser](https://mohamedoulal.vercel.app/projects/LongShot)** or **[download it for Android](https://github.com/ElAyachie/Long-Shot/releases/latest/download/LongShot.apk)** (Android 6.0 or newer; every version is on the [Releases page](https://github.com/ElAyachie/Long-Shot/releases)).

The APK isn't installed from Google Play, so Android will ask you to allow installs from your browser the first time. Each release also has `LongShot.apk.sha256`, the file's SHA-256 checksum, if you want to check the download.

Long Shot started as a one-tap game I made in college. In 2026 it was rebuilt on Unity 6 with aimed shots, physics, progression and a lot more polish, keeping the original sprites and sounds.

## How to play

- **Pull back and let go** to shoot. Pull further for more power. A dotted guide shows the path through the first bounce.
- **Tap** for a quick straight shot.
- **Swipe sideways** while the ball is in the air to curve it.
- On a computer, use the mouse, or **Space** to shoot straight.

## What's in it

- **Shot physics:** the ball slows as it travels, bounces off the side boards and posts, curves when swiped, and drifts with the wind.
- **Scoring:** bank shots, close calls (grazing a defender), Top Bins on a moving goal, pinball bumpers, and trick shot combos for doing several in one shot. Quick shots build a streak, and five in a row puts you On Fire for double points.
- **Getting harder:** more defenders, a goalkeeper, wind, bumpers, a sliding goal, defenders that feint, and doubled-up lanes, each announced as it arrives.
- **Pickups:** coins, shields, slow motion and double points drift across the pitch.
- **Progression:** coins, XP and levels with rank titles, three rotating missions, a seven-day daily bonus, and a paid continue once per match.
- **Shop:** nine balls with physics perks (keeps its pace, bouncy walls, wind proof, big curve, and more) and seven pitch themes.

All new art, the pitch, the menus and most sound effects are generated in code; see `Assets/Scripts/PixelArt.cs`, `Pitch.cs`, `GameUI.cs` and `Sfx.cs`.

## Project layout

The game runs from `Assets/Scenes/MainScene.unity`. `GameManager` starts itself when the scene loads and takes over the scene's goal, defenders and ball spawn.

| Script | What it does |
| --- | --- |
| `GameManager.cs` | Game states, input and aiming, scoring, pickups, continues, end of match |
| `Ball.cs` | Shot physics, collisions, close calls, trails and the aim guide's prediction |
| `Defender.cs`, `Goalkeeper.cs`, `Goal.cs`, `Bumper.cs`, `Boards.cs` | The things on the pitch |
| `Difficulty.cs` | Everything that changes with goals scored |
| `Progression.cs`, `SaveData.cs`, `Catalog.cs` | Levels, missions, daily bonus, saves, balls and pitches |
| `GameUI.cs`, `UIKit.cs` | Every screen, built at runtime |
| `Fx.cs`, `Sfx.cs`, `PixelArt.cs`, `Pitch.cs` | Particles, sound, generated sprites and the pitch |

## Building

Open the `Long-Shot` folder with **Unity 6000.0.65f1**.

The browser version is built from **Long Shot > Build WebGL** (`Assets/Editor/WebGLBuild.cs`), which uses the template in `Assets/WebGLTemplates/LongShot` and writes to `Builds/WebGL`. It uses gzip with Unity's decompression fallback, so it runs on any static host.

On some Windows machines Emscripten's optimizer crashes when it runs multithreaded. If the build fails while linking, set the environment variable `BINARYEN_CORES=1` before starting Unity and build again.

### Android releases

Android releases are built and signed by GitHub Actions (`.github/workflows/release.yml`, using [GameCI](https://game.ci)); no signing key is kept in the repository. To publish a version, push a tag such as `git tag v2.0.0 && git push origin v2.0.0`, or run **Release APK** from the Actions tab. The version name and code come from the tag.

The workflow needs these repository secrets (Settings > Secrets and variables > Actions):

| Secret | |
| --- | --- |
| `UNITY_LICENSE` | The contents of your Unity license file (`C:\ProgramData\Unity\Unity_lic.ulf` on Windows after signing in to Unity Hub) |
| `UNITY_EMAIL`, `UNITY_PASSWORD` | Your Unity account |
| `ANDROID_KEYSTORE_BASE64` | The release keystore, base64-encoded |
| `ANDROID_KEYSTORE_PASSWORD` | Its password |
| `ANDROID_KEY_ALIAS` | Optional, defaults to `release` |

To build a signed APK in the editor instead, put the release keystore at `Long-Shot/user.keystore` (Git ignores it) and enter its password under Player Settings > Publishing Settings.

The original version of the game was on Google Play.
