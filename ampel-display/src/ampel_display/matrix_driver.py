"""Wrapper around rpi-rgb-led-matrix for LED panel configuration."""

from rgbmatrix import RGBMatrix, RGBMatrixOptions


class MatrixDriver:
    """Configures and manages the RGB LED matrix hardware."""

    def __init__(self, panel_type: str = "p4", brightness: int = 80,
                 chain_length: int = 4):
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

        # Adafruit HAT supports only 1 parallel chain.
        # Use U-mapper with 4 panels to fold into 2-row grid.
        # With 2 panels: simple horizontal strip, no U-mapper needed.
        options.chain_length = chain_length
        options.parallel = 1
        if chain_length >= 4:
            options.pixel_mapper_config = "U-mapper"
        options.brightness = max(0, min(100, brightness))
        # drop_privileges=False because RGBMatrix changes user to 'daemon'
        # which can't read font files under /home/pi/
        options.drop_privileges = False

        self.matrix = RGBMatrix(options=options)
        self.width: int = self.matrix.width   # 128 for P4, 64 for P8
        self.height: int = self.matrix.height  # 64 for P4, 32 for P8

    def set_brightness(self, value: int) -> None:
        """Set display brightness (0–100)."""
        self.matrix.brightness = max(0, min(100, value))
