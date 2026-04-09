"""Diagnostic: shows quadrant labels on the 2x2 grid with U-mapper."""
import sys
sys.path.insert(0, "/home/pi/ampel-display/src")

from rgbmatrix import RGBMatrix, RGBMatrixOptions, graphics

options = RGBMatrixOptions()
options.hardware_mapping = "adafruit-hat"
options.gpio_slowdown = 4
options.rows = 32
options.cols = 64
options.chain_length = 4
options.parallel = 1
options.pixel_mapper_config = "U-mapper"
options.drop_privileges = False
options.brightness = 60

matrix = RGBMatrix(options=options)
canvas = matrix.CreateFrameCanvas()

# Canvas should now be 128x64
print(f"Canvas size: {canvas.width}x{canvas.height}")

font = graphics.Font()
font.LoadFont("/home/pi/rpi-rgb-led-matrix/fonts/6x10.bdf")

red = graphics.Color(255, 0, 0)
green = graphics.Color(0, 255, 0)
blue = graphics.Color(0, 100, 255)
yellow = graphics.Color(255, 255, 0)

# Draw labels in each virtual quadrant
# Top-left quadrant (0,0)-(63,31)
graphics.DrawText(canvas, font, 10, 18, red, "V:TL")
# Top-right quadrant (64,0)-(127,31)
graphics.DrawText(canvas, font, 74, 18, green, "V:TR")
# Bottom-left quadrant (0,32)-(63,63)
graphics.DrawText(canvas, font, 10, 50, blue, "V:BL")
# Bottom-right quadrant (64,32)-(127,63)
graphics.DrawText(canvas, font, 74, 50, yellow, "V:BR")

# Draw borders around each quadrant
for x in range(128):
    canvas.SetPixel(x, 0, 100, 100, 100)
    canvas.SetPixel(x, 31, 100, 100, 100)
    canvas.SetPixel(x, 32, 100, 100, 100)
    canvas.SetPixel(x, 63, 100, 100, 100)
for y in range(64):
    canvas.SetPixel(0, y, 100, 100, 100)
    canvas.SetPixel(63, y, 100, 100, 100)
    canvas.SetPixel(64, y, 100, 100, 100)
    canvas.SetPixel(127, y, 100, 100, 100)

matrix.SwapOnVSync(canvas)

print("You should see V:TL (red), V:TR (green), V:BL (blue), V:BR (yellow)")
print("Tell me which label appears in which PHYSICAL position (TL/TR/BL/BR)")
print()
input("Press Enter to exit...")
