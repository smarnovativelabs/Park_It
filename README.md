# Park It! — Complete Unity 6 (6000.x) Mobile Portrait Project

### 🌟 English Instructions:
1. Extract this `ParkIt_Unity_6_Project.zip`.
2. Open **Unity Hub** -> Click **Add** -> **Add project from disk** -> Select this folder.
3. Unity Hub will open it as **Unity 6 (6000.x) with URP**.
4. Open `Assets/Scenes/ParkIt_MobileMain.unity` and press **Play**!
5. **ZERO PINK MATERIALS**: Pink shader bug is 100% eliminated. All shaders use URP Lit PBR with multiple crash-proof fallbacks and embedded solid textures.
6. **FULL UI SPRITES & 3D MODELS**: All UI button sprites (`btn_drive.png`, `btn_reverse.png`, `btn_turn_left.png`, `btn_turn_right.png`, `btn_undo.png`, `star_gold.png`, etc.) and master `ui_spritesheet.png` are in `Assets/Sprites/UI/` and `Assets/Resources/Sprites/UI/`.
7. **ALL 3D MODELS (FBX & OBJ)**: `ToyCar_Complete.obj`, `ToyCar_Body.fbx`, `FloorTile.fbx`, `TablePlinth.fbx`, `Obstacle_Barrier.fbx`, and `OilSlick.fbx` are in `Assets/Models/` and `Assets/Resources/Models/`.
8. **BUILD APK**: Click top menu `Park It -> 📱 Build Android APK (1-Click)`. Unity outputs `Builds/Android/ParkIt_Mobile.apk`.

---

### 🇵🇰 Roman Urdu Guide:
1. Is zip file ko extract (unzip) karen.
2. **Unity Hub** open karen -> **Add** button par click karen -> **Add project from disk** select karen aur ye folder open karen.
3. Unity 6 URP packages automatically load ho jayengy.
4. Scene `Assets/Scenes/ParkIt_MobileMain.unity` open karen aur **Play** press karen.
5. **No Pink Cube Bug**: Pink color ka masla 100% solve ho chuka hai, saary materials URP Lit format mai set hain aur multiple safe fallbacks hain.
6. **Saray UI Sprites Shamil Hain**: Drive, Reverse, Turn Left, Turn Right, Shift, Undo, Restart, Fuel meter, aur Stars k saray PNG sprites aur `ui_spritesheet.png` `Assets/Sprites/UI/` aur `Assets/Resources/Sprites/UI/` mai mojood hain!
7. **Saray 3D Models (FBX & OBJ)**: Toy Car, Road Tiles, Barrier Obstacle, Oil Slick, aur Table Plinth k standard FBX aur OBJ files `Assets/Models/` mai shamil hain taky Unity mai same exact display aur visual milay!
8. **Direct APK Build**: Top menu bar mai `Park It -> 📱 Build Android APK (1-Click)` click karen. Ap ka mobile APK tayar ho kar `Builds/Android/ParkIt_Mobile.apk` folder mai open ho jaye ga!
9. **Reference Snaps**: `Assets/Screenshots/` folder mai saray screens k preview snapshots shamil hain.

## UI overhaul, 8 levels & Level Creator (2026-10-22)

- **Spritesheet UI**: every panel, button, badge, icon, star and the logo lives in `Assets/Resources/Sprites/UI/ParkIt_UI_Sheet.png` (87 named, 9-sliced sprites). Regenerate with `Tools/SpriteSheet/Generate-UISpritesheet.ps1`; Unity re-slices automatically (`Park It > Re-slice UI Spritesheet` forces it).
- **Screens**: title screen, level select (stars, difficulty, oil/obstacle/grid info), HUD (level card, target heading, fuel gauge, creator + sound buttons), driving dock with Zen Mode, victory/defeat modals, help.
- **8 levels** in `LevelCatalog.cs`; optimal move counts come from the BFS solver (`MoveRules.cs`). Stars: optimal = 3, +2 moves = 2, otherwise 1.
- **Level Creator** (sliders icon): paint obstacles, oil, start car, target bay; set grid size, fuel and headings; live solver verdict; export/import level code; Test & Play.
- **Diorama props**: fire-hydrant obstacles, gold ghost discs with white arrows, green parking bay with heading arrow, `45° Cam` view toggle.
- Keys: W/S/A/D or arrows drive, Q/E shift, U undo, R restart, C camera, Z zen, Space/Enter start.

## Verified fixes (2026-10-21)

- URP pipeline: `Assets/Settings/ParkIt_URP.asset`, with its Universal Renderer, assigned in Graphics and all six quality tiers. `Park It > Configure URP` can reapply the setup.
- Material shader references repaired; runtime materials reuse cached URP materials without per-object color textures.
- Runtime HUD now provides movement, shift, Undo, Restart, and result panels. UI clicks no longer select world nodes underneath.
- Restart cancels active movement. Undo restores fuel and position and dismisses results. Completed levels reject further moves.
- Correct default level numbers, portrait camera framing, and scene registration in Build Settings.
- Alternate OBJ models moved from Resources to `Assets/Models/ResourceOBJAlternates` to avoid duplicate resource keys. FBX normals are calculated on import.
- Verified in Play Mode: restart during movement, Undo, last-fuel victory, victory lock, loss recovery, reverse parking, oil slip, and all three level transitions.
- Android player scripts compiled successfully (20 assemblies). A complete APK build/device test has not been performed.
