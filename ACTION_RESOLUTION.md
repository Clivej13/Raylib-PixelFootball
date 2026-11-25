# Action resolution architecture

The game currently runs a kickoff return with 22 players on the field and 75 animated sideline players. The returner is controlled by the player. FieldScene owns the play, player entities, ball sprite, camera and optional F3 overlay.

## Resolution

`ActionResolver` evaluates rated skills (1–10), two independent dice rolls and named situational modifiers. It returns an `ActionResult` with the score, outcome, skills, rolls and modifiers. It has no movement, animation or ball code. A seeded `IActionRandom` makes headless tests repeatable.

`ActionProfiles` loads separate profiles for blocking, catching, tackling and ball security from `action_weights.json` at startup. Each profile defines dice, thresholds, skill weights, difficulty, timing and consequences. The default resolver thresholds implement the seven grades in the specification. The blocking profile widens the neutral band and uses d10 rolls to permit rare critical outcomes at equal ratings.

`BlockContest` owns continuous half-second checks and pending displacement. Blocking skill versus shedding skill sets the initial hold duration; graded checks extend or shorten it. A critical success pancakes the defender. A critical failure or an expired hold gives the defender a rush win. `KickoffBlocking` pairs agents, applies the same clamped shift to each participant, handles recovery, and prevents the same pair from re-engaging during the play. Losers stay down through their recovery and while a standing player occupies their feet space; standing players can run over downed players. Blockers stay near their assigned lateral lanes, while defenders steer around standing blockers. Strength scales physical displacement separately from technical resolution. The current kickoff has no quarterback or pass protection assignments, so a quarterback pocket is not yet a live play feature.

`CatchHandler` resolves a catch when `KickoffPlay` presents an eligible returner. `BallPossession` owns flight, deflection, bobble, unsecured/securing/secured possession, loose ball and dead ball. Catch attempts are capped; a pass that lands before possession is incomplete, while a ball lost after possession is a fumble. `FieldScene` hides the separate ball sprite while carried or dead and selects the run-with-ball animation for the carrier. The kickoff currently routes catches to the user returner; catching or intercepting by other on-field players needs a carrier/assignment update.

`TackleHandler` uses the same resolver for initial contact and timed wrap struggles. A dominant tackle invokes `BallSecurityHandler` as a separate check. A completed tackle ends the play immediately and starts the carrier's tackled sheet; frame 4 cues the tackler's tackle sheet while the play stays dead. The carrier also ends the play on either sideline or at either end-zone line. The F3 overlay shows ball state, recovery and the latest action score, dice and total modifier.

## Tuning

Edit `action_weights.json` and restart the game to change action odds. The file also contains situational modifiers, contact thresholds, blocking motion, tackle effects, recovery timing, ball behavior, kickoff timing and initial player rating patterns. All seven outcomes must appear in each profile's `Consequences` object, even when an outcome has no added consequence. Invalid or missing values fail startup rather than silently changing the balance. Run `dotnet run -- --check-config` to validate the file without opening the game.

## First balance sample

`BalanceSimulationTests` runs 5,000 seeded matched blocks and 5,000 routine catches without a window. With the current defender-favoured profile, matched blocks averaged 1.156 seconds, with 94.50% rush wins, 5.50% pancakes and -0.028 yard average displacement. The rush-win rate includes holds that expire without a critical roll. Catching 7 versus routine difficulty 4 completed 97.40%, with 0% critical drops in this sample and 0.285 seconds average recovery among catches. These are first-pass estimates; multi-player play testing and contested catch simulations remain necessary.
