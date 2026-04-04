"""Idle display element — clock, scrolling text, or both."""

import time


class IdleDisplay:
    """Renders the idle-mode screen."""

    # White colour for idle elements
    COLOR = (255, 255, 255)

    def __init__(self):
        self.scroll_offset = 0.0
        self.last_scroll_time = 0.0
        self.scroll_speed = 30  # pixels per second

    def draw_clock(self, canvas, renderer, region: tuple, clock_text: str,
                   is_ttf: bool = False) -> None:
        """Draw the clock with blinking colon."""
        x, y, w, h = region
        font_name = "large"

        # Blink colon every second (even second = show, odd = hide)
        display_text = clock_text
        if int(time.time()) % 2 != 0:
            display_text = clock_text.replace(":", " ")

        text_w = renderer.measure_text(display_text, font_name)
        text_h = renderer.font_height(font_name)

        draw_x = x + max(0, (w - text_w) // 2)
        if is_ttf:
            draw_y = y + max(0, (h - text_h) // 2)
        else:
            draw_y = y + max(0, (h + text_h) // 2)

        renderer.draw_text(canvas, font_name, draw_x, draw_y, self.COLOR, display_text)

    def draw_text(self, canvas, renderer, region: tuple, text: str,
                  scroll: bool, is_ttf: bool = False) -> None:
        """Draw static or scrolling text."""
        x, y, w, h = region
        font_name = "small"

        text_w = renderer.measure_text(text, font_name)
        text_h = renderer.font_height(font_name)

        if is_ttf:
            draw_y = y + max(0, (h - text_h) // 2)
        else:
            draw_y = y + max(0, (h + text_h) // 2)

        if not scroll or text_w <= w:
            # Static: centre
            draw_x = x + max(0, (w - text_w) // 2)
            renderer.draw_text(canvas, font_name, draw_x, draw_y, self.COLOR, text)
        else:
            # Scroll from right to left
            now = time.time()
            if self.last_scroll_time > 0:
                dt = now - self.last_scroll_time
                self.scroll_offset += self.scroll_speed * dt
            self.last_scroll_time = now

            # Reset when text has fully scrolled past
            if self.scroll_offset > text_w + w:
                self.scroll_offset = 0.0

            draw_x = x + w - int(self.scroll_offset)
            renderer.draw_text(canvas, font_name, draw_x, draw_y, self.COLOR, text)

    def reset_scroll(self) -> None:
        """Reset scroll state (e.g., when text changes)."""
        self.scroll_offset = 0.0
        self.last_scroll_time = 0.0
