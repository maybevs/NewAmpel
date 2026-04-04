"""Font manager: switches between BDF and TTF rendering modes."""

from ampel_display.fonts.bdf_font import BdfFontRenderer
from ampel_display.fonts.ttf_font import TtfFontRenderer


class FontManager:
    """Manages the active font rendering mode (BDF or TTF)."""

    def __init__(self, mode: str, canvas_width: int, canvas_height: int):
        self.mode = mode
        self._canvas_width = canvas_width
        self._canvas_height = canvas_height
        self._bdf_renderer: BdfFontRenderer | None = None
        self._ttf_renderer: TtfFontRenderer | None = None

        # Eagerly init the selected mode
        if mode == "bdf":
            self._bdf_renderer = BdfFontRenderer(canvas_height)
        else:
            self._ttf_renderer = TtfFontRenderer(canvas_width, canvas_height)

    def set_mode(self, mode: str) -> None:
        """Switch font mode. Called by config messages."""
        if mode not in ("bdf", "ttf"):
            return
        self.mode = mode
        # Lazily initialise on first use
        if mode == "bdf" and self._bdf_renderer is None:
            self._bdf_renderer = BdfFontRenderer(self._canvas_height)
        elif mode == "ttf" and self._ttf_renderer is None:
            self._ttf_renderer = TtfFontRenderer(self._canvas_width, self._canvas_height)

    def get_renderer(self) -> BdfFontRenderer | TtfFontRenderer:
        """Return the currently active font renderer."""
        if self.mode == "bdf":
            if self._bdf_renderer is None:
                self._bdf_renderer = BdfFontRenderer(self._canvas_height)
            return self._bdf_renderer
        if self._ttf_renderer is None:
            self._ttf_renderer = TtfFontRenderer(self._canvas_width, self._canvas_height)
        return self._ttf_renderer

    @property
    def is_ttf(self) -> bool:
        return self.mode == "ttf"
