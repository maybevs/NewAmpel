"""Wrapper around rpi-rgb-led-matrix for LED panel configuration."""

from rgbmatrix import RGBMatrix, RGBMatrixOptions


class MatrixDriver:
    """Configures and manages the RGB LED matrix hardware."""

    def __init__(self, panel_type: str = "p4", brightness: int = 80):
        options = RGBMatrixOptions()
        options.hardware_mapping = "adafruit-hat"
        options.gpio_slowdown = 4

        if panel_type == "p4":
            options.rows = 32
            options.cols = 64
        elif panel_type == "p8":
            options.rows = 16
            options.cols = 32
        else:
            raise ValueError(f"Unknown panel type: {panel_type}")

        options.chain_length = 2
        options.parallel = 2
        options.brightness = max(0, min(100, brightness))
        options.drop_privileges = True

        self.matrix = RGBMatrix(options=options)
        self.width: int = self.matrix.width   # 128 for P4, 64 for P8
        self.height: int = self.matrix.height  # 64 for P4, 32 for P8

    def set_brightness(self, value: int) -> None:
        """Set display brightness (0–100)."""
        self.matrix.brightness = max(0, min(100, value))
