"""Scan-rate diagnostic: tries common 1/8 scan configurations for P5 panels.

Usage (on Pi):
    sudo /home/pi/ampel-venv/bin/python3 /home/pi/ampel-display/scan_test.py
"""
import time
import sys

from rgbmatrix import RGBMatrix, RGBMatrixOptions, graphics

CONFIGS = [
    # (rows, cols, multiplexing, row_addr_type, description)
    (32, 64, 0, 0, "Standard 1/16 scan (default)"),
    (16, 64, 0, 0, "rows=16, no multiplexing"),
    (16, 64, 1, 0, "rows=16, multiplexing=1 (Stripe)"),
    (16, 64, 2, 0, "rows=16, multiplexing=2 (Checker)"),
    (16, 64, 3, 0, "rows=16, multiplexing=3 (Spiral)"),
    (16, 64, 4, 0, "rows=16, multiplexing=4 (ZStripe08)"),
    (32, 64, 1, 0, "rows=32, multiplexing=1"),
    (32, 64, 2, 0, "rows=32, multiplexing=2"),
    (32, 64, 3, 0, "rows=32, multiplexing=3"),
    (32, 64, 4, 0, "rows=32, multiplexing=4"),
    (32, 64, 0, 1, "rows=32, row_addr_type=1 (AB addr)"),
    (32, 64, 0, 2, "rows=32, row_addr_type=2"),
    (32, 64, 0, 3, "rows=32, row_addr_type=3"),
]


def test_config(rows, cols, multiplexing, row_addr_type, desc):
    """Try one configuration and display a test pattern."""
    print(f"\n>>> Testing: {desc}")
    print(f"    rows={rows} cols={cols} mux={multiplexing} addr_type={row_addr_type}")

    try:
        options = RGBMatrixOptions()
        options.hardware_mapping = "adafruit-hat"
        options.gpio_slowdown = 4
        options.rows = rows
        options.cols = cols
        options.chain_length = 1  # single panel for testing
        options.parallel = 1
        options.brightness = 60
        options.drop_privileges = False
        options.multiplexing = multiplexing
        options.row_address_type = row_addr_type

        matrix = RGBMatrix(options=options)
        canvas = matrix.CreateFrameCanvas()

        # Fill with a simple test pattern: color bands
        w, h = canvas.width, canvas.height
        for x in range(w):
            for y in range(h):
                if y < h // 3:
                    canvas.SetPixel(x, y, 255, 0, 0)      # Red top
                elif y < 2 * h // 3:
                    canvas.SetPixel(x, y, 0, 255, 0)       # Green middle
                else:
                    canvas.SetPixel(x, y, 0, 0, 255)        # Blue bottom

        # Draw "OK" text if font is available
        try:
            font = graphics.Font()
            font.LoadFont("/home/pi/rpi-rgb-led-matrix/fonts/6x10.bdf")
            white = graphics.Color(255, 255, 255)
            graphics.DrawText(canvas, font, 2, 10, white, "OK")
        except Exception:
            pass

        matrix.SwapOnVSync(canvas)
        return matrix  # Keep alive so LEDs stay on

    except Exception as e:
        print(f"    ERROR: {e}")
        return None


def main():
    print("P5 Panel Scan Rate Diagnostic")
    print("=" * 50)
    print("This will cycle through common 1/8 scan configurations.")
    print("Watch the panels — type the number of the config that works.")
    print("Press Enter to try next config, or enter number to select.\n")

    for i, (rows, cols, mux, addr, desc) in enumerate(CONFIGS):
        matrix = test_config(rows, cols, mux, addr, desc)
        if matrix is None:
            continue

        answer = input(f"  [{i}] Can you see color bands (R/G/B)? "
                       f"Enter=next, number=select: ").strip()
        # Cleanup matrix before next test
        del matrix

        if answer.isdigit():
            idx = int(answer)
            if 0 <= idx < len(CONFIGS):
                winner = CONFIGS[idx]
                print(f"\n*** Selected config #{idx}: {winner[4]}")
                print(f"    rows={winner[0]} cols={winner[1]} "
                      f"multiplexing={winner[2]} row_addr_type={winner[3]}")
                return
        elif answer.lower() in ("y", "yes", "this"):
            print(f"\n*** This config works: {desc}")
            print(f"    rows={rows} cols={cols} "
                  f"multiplexing={mux} row_addr_type={addr}")
            return

        time.sleep(0.5)  # Brief pause between configs

    print("\nNone of the standard configs worked.")
    print("Check panel wiring - is the ribbon cable fully seated?")
    print("Try: --led-multiplexing values 5-18")


if __name__ == "__main__":
    main()
