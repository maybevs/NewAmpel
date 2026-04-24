"""Main renderer: orchestrates all display elements into frames."""

from PIL import Image

from ampel_display.protocol import DisplayState
from ampel_display.layout import Layout
from ampel_display.fonts.font_manager import FontManager
from ampel_display.fonts.ttf_font import TtfFontRenderer
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

    def __init__(self, matrix, layout: Layout, font_manager: FontManager,
                 panel_type: str = "p4"):
        self.matrix = matrix
        self.layout = layout
        self.font_manager = font_manager
        self.panel_type = panel_type
        self.canvas = matrix.CreateFrameCanvas()
        self.width: int = self.canvas.width
        self.height: int = self.canvas.height

        # PIL-based renderer (needed for panel quadrant remap)
        self._pil_renderer = TtfFontRenderer(self.width, self.height)

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
        renderer = self._pil_renderer
        frame = renderer.create_frame()

        if state.color == "I":
            self._render_idle(frame, renderer, state)
        else:
            self._render_active(frame, renderer, state)

        self._copy_image_to_canvas(frame)

    def render_split(self, left: DisplayState, right: DisplayState) -> None:
        """Render split-screen finals: left half = display1, right half = display2."""
        renderer = self._pil_renderer
        frame = renderer.create_frame()
        w, h = self.width, self.height
        half_w = w // 2
        pad = 2  # inner padding from border

        # Left side: border + timer in the left half
        color_l = _SIGNAL_COLORS.get(left.color, (255, 255, 255))
        self._draw_half_border(frame, 0, 0, half_w, h, color_l)
        left_region = (pad, pad, half_w - pad * 2 - 1, h - pad * 2)
        self._draw_split_timer(frame, renderer, left_region, left, color_l)

        # Right side: border + timer in the right half
        color_r = _SIGNAL_COLORS.get(right.color, (255, 255, 255))
        self._draw_half_border(frame, half_w, 0, half_w, h, color_r)
        right_x = half_w + pad + 1
        right_region = (right_x, pad, half_w - pad * 2 - 1, h - pad * 2)
        self._draw_split_timer(frame, renderer, right_region, right, color_r)

        # Small abbreviated side labels in bottom corners
        label_color = (60, 60, 60)  # dim so it doesn't distract
        if left.group:
            label = left.group[0]  # "L" or "R" or "1" etc.
            label_h = renderer.font_height("small")
            renderer.draw_text(frame, "small", pad + 1, h - pad - label_h, label_color, label)
        if right.group:
            label = right.group[0]
            label_w = renderer.measure_text(label, "small")
            label_h = renderer.font_height("small")
            renderer.draw_text(frame, "small", w - pad - 1 - label_w, h - pad - label_h, label_color, label)

        # Center divider
        for py in range(h):
            frame.putpixel((half_w, py), (40, 40, 40))

        self._copy_image_to_canvas(frame)

    def _draw_split_timer(self, frame: Image.Image, renderer, region: tuple,
                          state: DisplayState, color: tuple) -> None:
        """Draw timer sized for a split-mode half-panel."""
        x, y, w, h = region
        if state.format == "f":
            cs = max(0, state.time)
            secs = cs // 100
            frac = cs % 100
            text = f"{secs:02d}.{frac:02d}"
        else:
            text = TimerDisplay.format_time(state.time, state.format)

        # Choose the largest font that fits the half-panel width
        font = "medium"
        for candidate in ("large_colon", "finals_frac", "medium"):
            if renderer.measure_text(text, candidate) <= w:
                font = candidate
                break

        text_w = renderer.measure_text(text, font)
        text_h = renderer.font_height(font)
        draw_x = x + max(0, (w - text_w) // 2)
        draw_y = y + max(0, (h - text_h) // 2)
        renderer.draw_text(frame, font, draw_x, draw_y, color, text)

    def render_standby(self) -> None:
        """Render the standby screen (no RS485 data)."""
        renderer = self._pil_renderer
        frame = renderer.create_frame()
        w, h = self.width, self.height
        region = self.layout.resolve(Layout.STANDBY["message"], w, h)
        text = "---"
        text_w = renderer.measure_text(text, "large")
        text_h = renderer.font_height("large")
        x, y, rw, rh = region
        draw_x = x + max(0, (rw - text_w) // 2)
        draw_y = y + max(0, (rh - text_h) // 2)
        renderer.draw_text(frame, "large", draw_x, draw_y, _STANDBY_COLOR, text)
        self._copy_image_to_canvas(frame)

    def _render_active(self, frame: Image.Image, renderer, state: DisplayState) -> None:
        color = _SIGNAL_COLORS.get(state.color, (255, 255, 255))
        w, h = self.width, self.height

        # 1px border in ampel color
        self._draw_border(frame, w, h, color)

        if state.format == "f":
            # Finals mode: use full-width layout, no group label
            region = self.layout.resolve(Layout.ACTIVE_FINALS["timer"], w, h)
            self.timer_display.draw(frame, renderer, region, state.time, color, is_ttf=True, fmt=state.format)
        else:
            region = self.layout.resolve(Layout.ACTIVE["timer"], w, h)
            self.timer_display.draw(frame, renderer, region, state.time, color, is_ttf=True, fmt=state.format)

            region = self.layout.resolve(Layout.ACTIVE["group"], w, h)
            self.group_display.draw(frame, renderer, region, state.group, color, is_ttf=True)

    def _render_idle(self, frame: Image.Image, renderer, state: DisplayState) -> None:
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
    def _draw_border(frame: Image.Image, w: int, h: int, color: tuple) -> None:
        for px in range(w):
            frame.putpixel((px, 0), color)
            frame.putpixel((px, h - 1), color)
        for py in range(h):
            frame.putpixel((0, py), color)
            frame.putpixel((w - 1, py), color)

    @staticmethod
    def _draw_half_border(frame: Image.Image, x: int, y: int, w: int, h: int, color: tuple) -> None:
        """Draw a 1px border around a rectangular sub-region."""
        for px in range(x, x + w):
            frame.putpixel((px, y), color)
            frame.putpixel((px, y + h - 1), color)
        for py in range(y, y + h):
            frame.putpixel((x, py), color)
            frame.putpixel((x + w - 1, py), color)

    def _remap_quadrants(self, image: Image.Image) -> Image.Image:
        """Remap quadrants to compensate for physical panel wiring.

        The U-mapper mapping depends on how the panels are chained.
        We pre-shuffle content so it ends up at the intended physical
        panel with correct orientation.
        """
        w, h = image.size
        qw, qh = w // 2, h // 2

        tl = image.crop((0, 0, qw, qh))
        tr = image.crop((qw, 0, w, qh))
        bl = image.crop((0, qh, qw, h))
        br = image.crop((qw, qh, w, h))

        remapped = Image.new("RGB", (w, h))

        if self.panel_type == "p5":
            # P5 (1/8 scan, mux=1) U-mapper mapping:
            #   V:TL → Phys BR ↕   V:TR → Phys BL ↕
            #   V:BL → Phys TR ↕   V:BR → Phys TL ↕
            remapped.paste(br.rotate(180), (0, 0))    # V:TL ← BR↕ → Phys BR right-side-up
            remapped.paste(bl.rotate(180), (qw, 0))   # V:TR ← BL↕ → Phys BL right-side-up
            remapped.paste(tr.rotate(180), (0, qh))   # V:BL ← TR↕ → Phys TR right-side-up
            remapped.paste(tl.rotate(180), (qw, qh))  # V:BR ← TL↕ → Phys TL right-side-up
        else:
            # P4/P8 wiring: HAT→TR→BR(flipped)→BL(flipped)→TL
            #   V:TL → Phys BL ↕   V:TR → Phys TL ✓
            #   V:BL → Phys BR ✓   V:BR → Phys TR ↕
            remapped.paste(bl.rotate(180), (0, 0))    # V:TL ← BL↕
            remapped.paste(tl, (qw, 0))               # V:TR ← TL as-is
            remapped.paste(br, (0, qh))               # V:BL ← BR as-is
            remapped.paste(tr.rotate(180), (qw, qh))  # V:BR ← TR↕

        return remapped

    def _copy_image_to_canvas(self, image: Image.Image) -> None:
        """Copy a PIL Image to the matrix canvas via double-buffering."""
        image = self._remap_quadrants(image)
        self.canvas.Clear()
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
