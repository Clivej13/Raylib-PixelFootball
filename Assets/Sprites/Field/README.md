# Stadium and field artwork

The pre-game sequence uses these PNGs in order:

1. `football_stadium_512_8x.png` — skyline/stadium view, 4096 × 2304 source (512 × 288 at native pixel size).
2. `football_stadium_topdown_1024_8x.png` — overhead stadium, 8192 × 4608 source (1024 × 576 native).
3. `football_field_portrait_8x.png` — portrait field, 4096 × 6656 source (512 × 832 native).

The loader reduces each 8× image to its native pixel size before GPU upload. The portrait image starts fitted to the 320 × 180 world and zooms until it fills the width. Gameplay draws that same final crop behind the player entities.
