"""Timer display element — shows remaining time as M:SS, SS, or raw seconds."""


class TimerDisplay:
    """Renders the countdown timer."""

    @staticmethod
    def format_time(seconds: int, fmt: str = "m") -> str:
        """Format seconds into a display string.
        
        fmt='m' → M:SS or SS (minutes mode)
        fmt='s' → raw seconds always (e.g. 120)
        fmt='f' → SS:FF (seconds:centiseconds) — finals mode
                   Here 'seconds' is actually centiseconds (e.g. 2050 = 20.50s)
        """
        if seconds < 0:
            seconds = 0
        if fmt == "f":
            secs = seconds // 100
            cs = seconds % 100
            return f"{secs:02d}:{cs:02d}"
        if fmt == "s":
            return str(seconds)
        if seconds >= 60:
            return f"{seconds // 60}:{seconds % 60:02d}"
        return str(seconds)

    def draw(self, canvas, renderer, region: tuple, seconds: int, color: tuple,
             is_ttf: bool = False, fmt: str = "m") -> None:
        """Draw the timer centred in the given region."""
        x, y, w, h = region

        if fmt == "f":
            # Finals mode: large seconds + smaller centiseconds
            self._draw_finals(canvas, renderer, x, y, w, h, seconds, color, is_ttf)
            return

        text = self.format_time(seconds, fmt)
        font_name = "large_colon" if ":" in text else "large"

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

    def _draw_finals(self, canvas, renderer, x: int, y: int, w: int, h: int,
                     centiseconds: int, color: tuple, is_ttf: bool) -> None:
        """Draw finals time: large SS with smaller :FF alongside, bottom-aligned."""
        if centiseconds < 0:
            centiseconds = 0
        secs = centiseconds // 100
        cs = centiseconds % 100
        sec_text = f"{secs:02d}"
        frac_text = f".{cs:02d}"

        sec_w = renderer.measure_text(sec_text, "large")
        sec_h = renderer.font_height("large")
        frac_w = renderer.measure_text(frac_text, "finals_frac")
        frac_h = renderer.font_height("finals_frac")

        total_w = sec_w + frac_w + 1  # 1px gap

        # Centre the combined block horizontally in the region
        draw_x = x + max(0, (w - total_w) // 2)

        if is_ttf:
            # Vertically centre the large part; align fraction baseline to large baseline
            sec_y = y + max(0, (h - sec_h) // 2)
            # Align bottoms: fraction baseline = sec bottom - frac descender offset
            frac_y = sec_y + sec_h - frac_h
        else:
            sec_y = y + max(0, (h + sec_h) // 2)
            frac_y = sec_y  # BDF baseline alignment

        renderer.draw_text(canvas, "large", draw_x, sec_y, color, sec_text)
        renderer.draw_text(canvas, "finals_frac", draw_x + sec_w + 1, frac_y, color, frac_text)
