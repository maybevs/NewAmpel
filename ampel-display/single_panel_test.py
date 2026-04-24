"""Minimal single-panel test: fills one panel solid red.

Usage (on Pi):
    sudo /home/pi/ampel-venv/bin/python3 /home/pi/ampel-display/single_panel_test.py
"""
from rgbmatrix import RGBMatrix, RGBMatrixOptions

options = RGBMatrixOptions()
options.hardware_mapping = "adafruit-hat"
options.gpio_slowdown = 4
options.rows = 32
options.cols = 64
options.chain_length = 1
options.parallel = 1
options.brightness = 40
options.drop_privileges = False

print("Initializing matrix (rows=32, cols=64, chain=1)...")
matrix = RGBMatrix(options=options)
canvas = matrix.CreateFrameCanvas()

print(f"Canvas: {canvas.width}x{canvas.height}")

# Fill entire panel solid red
for x in range(canvas.width):
    for y in range(canvas.height):
        canvas.SetPixel(x, y, 255, 0, 0)

matrix.SwapOnVSync(canvas)
print("Panel should be SOLID RED.")
print("If dark: check power supply and ribbon cable orientation.")
input("Press Enter to exit...")
