# Player sprite sheets

- `football_idle_7_8x.png`: idle loop.
- `football_run_7_8x.png`: running loop.
- `football_throw_7_8x.png`: throw action.
- `football_catch_stationary_7_8x.png`: stationary catch action.
- `football_catch_rac_7_8x.png`: catch and run action.
- `football_catch_high_7_8x.png`: high catch action.
- `football_catch_dive_7_8x.png`: diving catch action.

Each PNG is **32768 × 512 RGBA**. It is one horizontal strip containing eight facing-direction animations, each with eight frames. A source cell is 512 × 512 pixels; the art is scaled 8× from a **64 × 64 logical frame**. Direction 0 occupies frames 0–7, direction 1 occupies 8–15, through direction 7 at 56–63.

The strip starts facing north and rotates clockwise in 45-degree steps: north, northeast, east, southeast, south, southwest, west, northwest. Screen-up movement selects north; screen-down movement selects south.

`PlayerSpriteAssets` loads each sheet once. `PlayerEntity` instances share the textures but own separate playback clocks, positions, facing directions, and animation states. The loader crops each direction, reduces the duplicated 8× source pixels to native 64×64 frames, then uploads a 512 × 64 texture for that direction. This keeps seven animation sheets practical in GPU memory.

The field uses a fixed 320 × 180 world. Render resolution changes its drawing scale while preserving positions, proportions, and the number of visible players.
