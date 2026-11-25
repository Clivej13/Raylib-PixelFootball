# Pixel Football

A .NET 9 and Raylib pixel American football prototype. The current playable scenario is one kickoff return: control the Away returner, field or recover the ball, and run until tackled, out of bounds, or into an end zone.

## What this branch adds

| Feature | Implemented behavior | Why it exists |
| --- | --- | --- |
| Two teams and kit colors | Home supplies the 11 coverage players and left sideline; Away supplies the 11 return-team players and right sideline. The Teams menu recolors jerseys with a palette swap and saves each choice. | Teams are visually distinct without maintaining a separate sheet for every kit. |
| Playable kickoff and ball | A kick travels toward the returner. The ball can be caught, deflected, bobbled, dropped, or recovered after a bounce. Possession has unsecured, securing, secured, loose, and dead states. The separate ball sprite is hidden while carried. | A return can develop from the ball's state instead of beginning with automatic possession. |
| Shared action resolution | Blocking, catching, tackling, and ball security use the same graded seven-outcome resolver, rated skills, independent dice rolls, and situational modifiers. Seeded random rolls support repeatable tests. Each mechanic interprets the result in its own handler. | One probability engine can be balanced without tying animation or physics to the dice calculation. |
| JSON gameplay tuning | [action_weights.json](action_weights.json) supplies all live action profiles, thresholds, skill weights, dice ranges, consequences, situational modifiers, block and tackle timing, recovery, ball behavior, kickoff timing, and initial rating patterns. The game validates it at startup. | Balance changes can be made without editing and recompiling C#; restart after changing the file. |
| Dynamic blocking and rush lanes | Nearby players form individual block engagements. Checks use elapsed simulation time; results shift both players smoothly, extend or shorten the hold, produce rush wins or pancakes, and prevent the same pair immediately re-engaging. Strength affects displacement separately from blocking technique. Defenders steer around standing blockers. | The return lane develops from player interactions rather than a fixed blocking animation or timer. |
| Tackle and break-tackle outcomes | Contact can miss, break, glance, start a wrap struggle, or complete a tackle. Baseline kickoff Tackling and BreakTackle are both 5, with a roughly 61% completion rate after contact and any resulting struggle. A dominant tackle makes a separate ball-security check. Carrier reactions are juke for a miss, truck for a broken tackle, stiff arm for a glance, and spin after escaping a wrap. | Contact has varied football outcomes while a rating change can later shift the odds. |
| Physical player space | Players have a circular footprint at their feet approximating shoulder width. Standing players do not overlap in normal movement; a fallen or committed tackle player stops blocking the lane. Failed contacts can be retried after a timed cooldown, and contact with an engaged defender can interrupt the block for a tackle. | Defenders can reach the carrier without players stacking or a missed tackler clogging the lane. |
| Fall, recovery, and post-play exit | A completed tackle ends the play. At the carrier's fourth tackled frame, the tackler moves onto the carrier and plays their tackle animation. A pile queue lets the top player rise first; the player below waits until the upper player has stood and moved clear. Tackled carriers get up hurt, successful or missed tacklers get up fast, and block or pass-rush losers get up slowly. After the play, all 22 field players walk toward their own sidelines because both next-play rosters are currently empty. | The result of contact remains visible and grounded players have an orderly way to leave the field. |
| Sprites and diagnostics | The player catalog includes the four catch sheets, run-with-ball, tackle/tackled, lying/get-up, and juke/spin/truck/stiff-arm sheets, alongside idle, run, kick, engage, bull rush, and block-loss. Sheets use eight directions and eight frames per direction, except the six-frame kick. The ball animation sheet is loaded separately. F3 shows action results, skills, dice, modifiers, ball state, recovery, and control mappings. | Art reflects gameplay outcomes, and the debug overlay makes balancing observable. |

The starter foundation also provides the skyline-to-stadium-to-field introduction, portrait field artwork, 22 field and 75 sideline sprites, camera tracking from the airborne ball to the carrier, a tunable screen-edge buffer, scalable render/window resolutions, menu and controller input, and rotating logs. The 2D viewport and sprite code remain internal to this repository so they can be extracted into a shared library later.

## Run and controls

From the repository root:

```text
dotnet restore
dotnet run
```

Choose **Start** to watch the introduction. Enter or controller A skips it. Move the Away returner with WASD, arrow keys, or the controller left stick. The camera follows the ball in flight and then the carrier. Press **K** after the play ends to restart the kickoff. Escape or controller Start pauses.

The Throw, Catch, Kick, Bull Rush, Engage, and Block Loss bindings can preview their player animations during a return; Throw is not yet a playable pass. F3 toggles the debug overlay. The Controls menu supports session-only rebinding. Keyboard, controller, and mouse can navigate the menus.

```text
dotnet build
dotnet test tests/PixelFootball.Tests.csproj
dotnet run -- --check-config
```

The config check validates the startup files without opening a window.

## Configuration and code layout

- [action_weights.json](action_weights.json): action odds, outcome consequences, contextual modifiers, and timing. Edit and restart to apply changes.
- [config.json](config.json): window/render resolution, VSync, fullscreen, debug overlay, scroll buffer, and initial kit colors.
- [input.json](input.json) and [menu.json](menu.json): bindings and menus.
- [logging.json](logging.json): rotating log destination and limits.
- [ACTION_RESOLUTION.md](ACTION_RESOLUTION.md): resolver, possession, and first balance sample details.

Window options span 640×360 to 2560×1440 when the monitor supports them. Render options are 320×180, 640×360, and 1280×720; the field keeps the same 320×180 world composition. `ScrollEdgeBuffer` controls when the camera begins following near a screen edge. Display choices and kit colors are saved under `%LOCALAPPDATA%/RaylibPixelFootball/settings.json`.

`Game/` owns play rules, entities, AI, possession, and action handlers. `TwoD/` owns the viewport, sprite sheet slicing, and per-entity playback. The game uses the sibling RaylibGameFramework Logging project and the Configuration, Input, and Menus packages.

## Follow-on work — not implemented

- **Next-play selection and substitutions.** Both next-play rosters are empty, so everyone exits after this kickoff. There is no next snap or automatic play transition yet.
- **A full offensive play.** There is no quarterback, pass protection pocket, route running, playable throw, interception, or playbook. The Throw binding previews its animation only.
- **Other receivers and live fumbles.** The kickoff catch handler currently offers catches to the user returner. A dominant tackle checks ball security, but the play ends immediately, so teammates cannot recover that resulting fumble during a live continuation.
- **Player-specific tackle ratings and jersey numbers.** The resolver supports individual ratings, but the kickoff uses equal baseline Tackling and BreakTackle values. Kits have colors, with no reliable number overlay yet.
- **Assisted tackles and chosen carrier moves.** A wrap is a one-defender struggle. Juke, spin, truck, and stiff arm are selected from resolved outcomes; the player cannot choose one as a separate move.
- **Additional sprite hooks.** The branch includes `football_idle_braced_7_8x.png` and `football_kick_left_7_8x.png`, but neither sheet is selected by the player animation catalog yet.
- **Visual and balance validation on target hardware.** Automated tests cover assets, state transitions, collision, timing, and sampled distributions. Tackle feel, blocking frequency, animation alignment, and frame pacing still need hands-on game checks.
- **Shared 2D library extraction.** The internal viewport and sprite code is separated from gameplay, but it has not been published as its own library.
