# BDF & TTF Fonts for Ampel Display

This directory holds the bitmap (BDF) and TrueType (TTF) fonts used by the display software.

## BDF Fonts

BDF fonts are sourced from the `rpi-rgb-led-matrix` library. After cloning that
repository on the Pi, the fonts are found at `rpi-rgb-led-matrix/fonts/`.

Copy the required fonts here or symlink them:

```bash
cp ~/rpi-rgb-led-matrix/fonts/10x20.bdf .
cp ~/rpi-rgb-led-matrix/fonts/7x13B.bdf .
cp ~/rpi-rgb-led-matrix/fonts/7x13.bdf .
cp ~/rpi-rgb-led-matrix/fonts/6x10.bdf .
cp ~/rpi-rgb-led-matrix/fonts/5x8.bdf .
cp ~/rpi-rgb-led-matrix/fonts/5x7.bdf .
```

## TTF Fonts

`DejaVuSans-Bold.ttf` is pre-installed on Raspberry Pi OS at
`/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf`.

The software uses that system path by default. To bundle a copy, place it here
and update the path in `ttf_font.py`.
