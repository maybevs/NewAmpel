"""TTF font renderer using Pillow (PIL)."""

from PIL import Image, ImageDraw, ImageFont

# Default TTF font path on Raspberry Pi OS
_DEFAULT_TTF = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"

# Font size mapping per resolution tier
_SIZE_MAP = {
    "large": {"high": 26, "low": 14},
    "medium": {"high": 14, "low": 8},
    "small": {"high": 10, "low": 6},
}


class TtfFontRenderer:
    """Renders text using TrueType fonts via Pillow."""

    def __init__(self, canvas_width: int, canvas_height: int, ttf_path: str = _DEFAULT_TTF):
        self.width = canvas_width
        self.height = canvas_height
        tier = "high" if canvas_height >= 64 else "low"

        self.font_large = ImageFont.truetype(ttf_path, size=_SIZE_MAP["large"][tier])
        self.font_medium = ImageFont.truetype(ttf_path, size=_SIZE_MAP["medium"][tier])
        self.font_small = ImageFont.truetype(ttf_path, size=_SIZE_MAP["small"][tier])

    def _get_font(self, font_name: str) -> ImageFont.FreeTypeFont:
        return getattr(self, f"font_{font_name}")

    def draw_text(self, canvas, font_name: str, x: int, y: int,
                  color: tuple, text: str) -> int:
        """Draw text onto a PIL Image (canvas). Returns pixel width drawn."""
        font = self._get_font(font_name)
        draw = ImageDraw.Draw(canvas)
        bbox = draw.textbbox((x, y), text, font=font)
        draw.text((x, y), text, font=font, fill=color)
        return bbox[2] - bbox[0]

    def measure_text(self, text: str, font_name: str) -> int:
        """Measure pixel width of text without drawing."""
        font = self._get_font(font_name)
        # Use a temporary image for measurement
        bbox = font.getbbox(text)
        return bbox[2] - bbox[0] if bbox else 0

    def font_height(self, font_name: str) -> int:
        """Return the pixel height of the named font."""
        font = self._get_font(font_name)
        bbox = font.getbbox("Ag0")
        return bbox[3] - bbox[1] if bbox else 0

    def create_frame(self) -> Image.Image:
        """Create a blank frame image."""
        return Image.new("RGB", (self.width, self.height), (0, 0, 0))
