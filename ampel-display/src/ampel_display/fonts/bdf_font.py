"""BDF pixel font renderer using rgbmatrix.graphics."""

import os
from typing import Protocol

from rgbmatrix import graphics


# Font search paths (rpi-rgb-led-matrix default install + local fonts dir)
_FONT_SEARCH_PATHS = [
    os.path.join(os.path.dirname(__file__), "..", "..", "..", "fonts"),
    "/usr/share/fonts/misc",
    "/usr/local/share/fonts",
    os.path.expanduser("~/rpi-rgb-led-matrix/fonts"),
]

# Mapping from logical font name to preferred BDF files per resolution tier.
# Each list is tried in order; the first existing file wins.
_FONT_MAP = {
    "large": {
        "high": ["10x20.bdf", "9x18.bdf", "8x13B.bdf"],
        "low": ["7x13B.bdf", "7x13.bdf", "6x13.bdf", "6x10.bdf"],
    },
    "medium": {
        "high": ["7x13B.bdf", "7x13.bdf", "6x13.bdf"],
        "low": ["6x10.bdf", "5x8.bdf", "5x7.bdf"],
    },
    "small": {
        "high": ["6x10.bdf", "5x8.bdf", "5x7.bdf"],
        "low": ["5x7.bdf", "5x8.bdf", "4x6.bdf"],
    },
}


def _find_font_file(name: str) -> str:
    """Search for a BDF font file in known paths."""
    for base in _FONT_SEARCH_PATHS:
        path = os.path.join(base, name)
        if os.path.isfile(path):
            return path
    raise FileNotFoundError(f"BDF font '{name}' not found in search paths: {_FONT_SEARCH_PATHS}")


def _load_font(candidates: list[str]) -> graphics.Font:
    """Try loading the first available font from a list of candidates."""
    for name in candidates:
        try:
            path = _find_font_file(name)
            font = graphics.Font()
            font.LoadFont(path)
            return font
        except FileNotFoundError:
            continue
    raise FileNotFoundError(f"No BDF font found from candidates: {candidates}")


class BdfFontRenderer:
    """Renders text using BDF bitmap fonts via rgbmatrix.graphics."""

    def __init__(self, canvas_height: int):
        tier = "high" if canvas_height >= 64 else "low"
        self.font_large = _load_font(_FONT_MAP["large"][tier])
        self.font_medium = _load_font(_FONT_MAP["medium"][tier])
        self.font_small = _load_font(_FONT_MAP["small"][tier])

    def _get_font(self, font_name: str) -> graphics.Font:
        return getattr(self, f"font_{font_name}")

    def draw_text(self, canvas, font_name: str, x: int, y: int,
                  color: tuple, text: str) -> int:
        """Draw text on the canvas. Returns the pixel width drawn."""
        font = self._get_font(font_name)
        gfx_color = graphics.Color(*color)
        return graphics.DrawText(canvas, font, x, y, gfx_color, text)

    def measure_text(self, text: str, font_name: str) -> int:
        """Measure the pixel width of text without drawing."""
        font = self._get_font(font_name)
        # Each character in a BDF font has a fixed advance width.
        # CharacterWidth returns -1 if the glyph is missing.
        total = 0
        for ch in text:
            w = font.CharacterWidth(ord(ch))
            if w > 0:
                total += w
        return total

    def font_height(self, font_name: str) -> int:
        """Return the baseline height of the named font."""
        font = self._get_font(font_name)
        return font.height
