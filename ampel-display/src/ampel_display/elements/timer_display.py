"""Timer display element — shows remaining time as M:SS or SS."""


class TimerDisplay:
    """Renders the countdown timer."""

    @staticmethod
    def format_time(seconds: int) -> str:
        """Format seconds into a display string."""
        if seconds < 0:
            seconds = 0
        if seconds >= 60:
            return f"{seconds // 60}:{seconds % 60:02d}"
        return str(seconds)

    def draw(self, canvas, renderer, region: tuple, seconds: int, color: tuple,
             is_ttf: bool = False) -> None:
        """Draw the timer centred in the given region."""
        x, y, w, h = region
        text = self.format_time(seconds)
        font_name = "large"

        text_w = renderer.measure_text(text, font_name)
        text_h = renderer.font_height(font_name)

        # Centre horizontally and vertically
        draw_x = x + max(0, (w - text_w) // 2)
        # BDF fonts use baseline coordinates; TTF uses top-left
        if is_ttf:
            draw_y = y + max(0, (h - text_h) // 2)
        else:
            draw_y = y + max(0, (h + text_h) // 2)

        renderer.draw_text(canvas, font_name, draw_x, draw_y, color, text)
