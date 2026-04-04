"""Main renderer: orchestrates all display elements into frames."""

from PIL import Image

from ampel_display.protocol import DisplayState
from ampel_display.layout import Layout
from ampel_display.fonts.font_manager import FontManager
from ampel_display.elements.timer_display import TimerDisplay
from ampel_display.elements.group_display import GroupDisplay
from ampel_display.elements.end_display import EndDisplay
from ampel_display.elements.color_bar import ColorBar
from ampel_display.elements.idle_display import IdleDisplay


# RGB colours for the active signal states
_SIGNAL_COLORS = {
    "R": (255, 0, 0),
    "G": (0, 255, 0),
    "Y": (255, 255, 0),
}

# Dimmed white for standby text
_STANDBY_COLOR = (100, 100, 100)


class AmpelRenderer:
    """Renders the current DisplayState onto the LED matrix."""

    def __init__(self, matrix, layout: Layout, font_manager: FontManager):
        self.matrix = matrix
        self.layout = layout
        self.font_manager = font_manager
        self.canvas = matrix.CreateFrameCanvas()
        self.width: int = self.canvas.width
        self.height: int = self.canvas.height

        # Element renderers
        self.timer_display = TimerDisplay()
        self.group_display = GroupDisplay()
        self.end_display = EndDisplay()
        self.color_bar = ColorBar()
        self.idle_display = IdleDisplay()

        # Track previous idle text for scroll reset
        self._prev_idle_text: str = ""

    def render(self, state: DisplayState) -> None:
        """Render a complete frame based on the current state."""
        is_ttf = self.font_manager.is_ttf

        if is_ttf:
            self._render_ttf(state)
        else:
            self._render_bdf(state)

    def render_standby(self) -> None:
        """Render the standby screen (no RS485 data)."""
        is_ttf = self.font_manager.is_ttf
        renderer = self.font_manager.get_renderer()
        w, h = self.width, self.height

        if is_ttf:
            frame = renderer.create_frame()
            region = self.layout.resolve(Layout.STANDBY["message"], w, h)
            text = "---"
            text_w = renderer.measure_text(text, "large")
            text_h = renderer.font_height("large")
            x, y, rw, rh = region
            draw_x = x + max(0, (rw - text_w) // 2)
            draw_y = y + max(0, (rh - text_h) // 2)
            renderer.draw_text(frame, "large", draw_x, draw_y, _STANDBY_COLOR, text)
            self._copy_image_to_canvas(frame)
        else:
            self.canvas.Clear()
            region = self.layout.resolve(Layout.STANDBY["message"], w, h)
            text = "---"
            text_w = renderer.measure_text(text, "large")
            text_h = renderer.font_height("large")
            x, y, rw, rh = region
            draw_x = x + max(0, (rw - text_w) // 2)
            draw_y = y + max(0, (rh + text_h) // 2)
            renderer.draw_text(self.canvas, "large", draw_x, draw_y, _STANDBY_COLOR, text)
            self.canvas = self.matrix.SwapOnVSync(self.canvas)

    # --- BDF rendering (draw directly on rgbmatrix canvas) ---

    def _render_bdf(self, state: DisplayState) -> None:
        self.canvas.Clear()
        if state.color == "I":
            self._render_idle_bdf(state)
        else:
            self._render_active_bdf(state)
        self.canvas = self.matrix.SwapOnVSync(self.canvas)

    def _render_active_bdf(self, state: DisplayState) -> None:
        renderer = self.font_manager.get_renderer()
        color = _SIGNAL_COLORS.get(state.color, (255, 255, 255))
        w, h = self.width, self.height

        region = self.layout.resolve(Layout.ACTIVE["timer"], w, h)
        self.timer_display.draw(self.canvas, renderer, region, state.time, color)

        region = self.layout.resolve(Layout.ACTIVE["group"], w, h)
        self.group_display.draw(self.canvas, renderer, region, state.group, color)

        region = self.layout.resolve(Layout.ACTIVE["separator"], w, h)
        self._draw_separator_bdf(region, color)

        region = self.layout.resolve(Layout.ACTIVE["end_info"], w, h)
        self.end_display.draw(self.canvas, renderer, region, state.end, color)

        region = self.layout.resolve(Layout.ACTIVE["color_bar"], w, h)
        self.color_bar.draw(self.canvas, region, state.color)

    def _render_idle_bdf(self, state: DisplayState) -> None:
        renderer = self.font_manager.get_renderer()
        idle = state.idle
        if idle is None:
            return

        self._check_scroll_reset(idle.text)
        idle_layout = self.layout.get_idle_layout(idle.mode)
        w, h = self.width, self.height

        if "clock" in idle_layout:
            region = self.layout.resolve(idle_layout["clock"], w, h)
            self.idle_display.draw_clock(self.canvas, renderer, region, idle.clock)

        if "text" in idle_layout:
            region = self.layout.resolve(idle_layout["text"], w, h)
            self.idle_display.draw_text(self.canvas, renderer, region, idle.text, idle.scroll)

    def _draw_separator_bdf(self, region: tuple, color: tuple) -> None:
        x, y, w, h = region
        r, g, b = color
        for px in range(x, x + w):
            for py in range(y, y + max(1, h)):
                self.canvas.SetPixel(px, py, r, g, b)

    # --- TTF rendering (draw on PIL Image, then copy to canvas) ---

    def _render_ttf(self, state: DisplayState) -> None:
        renderer = self.font_manager.get_renderer()
        frame = renderer.create_frame()

        if state.color == "I":
            self._render_idle_ttf(frame, renderer, state)
        else:
            self._render_active_ttf(frame, renderer, state)

        self._copy_image_to_canvas(frame)

    def _render_active_ttf(self, frame: Image.Image, renderer, state: DisplayState) -> None:
        color = _SIGNAL_COLORS.get(state.color, (255, 255, 255))
        w, h = self.width, self.height

        region = self.layout.resolve(Layout.ACTIVE["timer"], w, h)
        self.timer_display.draw(frame, renderer, region, state.time, color, is_ttf=True)

        region = self.layout.resolve(Layout.ACTIVE["group"], w, h)
        self.group_display.draw(frame, renderer, region, state.group, color, is_ttf=True)

        region = self.layout.resolve(Layout.ACTIVE["separator"], w, h)
        self._draw_separator_ttf(frame, region, color)

        region = self.layout.resolve(Layout.ACTIVE["end_info"], w, h)
        self.end_display.draw(frame, renderer, region, state.end, color, is_ttf=True)

        region = self.layout.resolve(Layout.ACTIVE["color_bar"], w, h)
        self.color_bar.draw(frame, region, state.color, is_ttf=True)

    def _render_idle_ttf(self, frame: Image.Image, renderer, state: DisplayState) -> None:
        idle = state.idle
        if idle is None:
            return

        self._check_scroll_reset(idle.text)
        idle_layout = self.layout.get_idle_layout(idle.mode)
        w, h = self.width, self.height

        if "clock" in idle_layout:
            region = self.layout.resolve(idle_layout["clock"], w, h)
            self.idle_display.draw_clock(frame, renderer, region, idle.clock, is_ttf=True)

        if "text" in idle_layout:
            region = self.layout.resolve(idle_layout["text"], w, h)
            self.idle_display.draw_text(frame, renderer, region, idle.text, idle.scroll, is_ttf=True)

    @staticmethod
    def _draw_separator_ttf(frame: Image.Image, region: tuple, color: tuple) -> None:
        x, y, w, h = region
        for px in range(x, x + w):
            for py in range(y, y + max(1, h)):
                frame.putpixel((px, py), color)

    def _copy_image_to_canvas(self, image: Image.Image) -> None:
        """Copy a PIL Image to the matrix canvas via double-buffering."""
        self.canvas.Clear()
        # Use SetImage if available (faster), otherwise pixel-by-pixel
        try:
            self.canvas.SetImage(image)
        except (AttributeError, TypeError):
            for x_pos in range(self.width):
                for y_pos in range(self.height):
                    r, g, b = image.getpixel((x_pos, y_pos))
                    if r or g or b:
                        self.canvas.SetPixel(x_pos, y_pos, r, g, b)
        self.canvas = self.matrix.SwapOnVSync(self.canvas)

    def _check_scroll_reset(self, text: str) -> None:
        """Reset scroll offset when idle text changes."""
        if text != self._prev_idle_text:
            self.idle_display.reset_scroll()
            self._prev_idle_text = text
