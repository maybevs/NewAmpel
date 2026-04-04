"""Group display element — shows the archer group (AB, CD, Links, Rechts)."""


class GroupDisplay:
    """Renders the group/side identifier."""

    def draw(self, canvas, renderer, region: tuple, group: str, color: tuple,
             is_ttf: bool = False) -> None:
        """Draw the group text centred in the region."""
        x, y, w, h = region
        if not group:
            return

        # Use medium font for short labels, small for longer ones
        font_name = "medium" if len(group) <= 4 else "small"

        text_w = renderer.measure_text(group, font_name)
        text_h = renderer.font_height(font_name)

        draw_x = x + max(0, (w - text_w) // 2)
        if is_ttf:
            draw_y = y + max(0, (h - text_h) // 2)
        else:
            draw_y = y + max(0, (h + text_h) // 2)

        renderer.draw_text(canvas, font_name, draw_x, draw_y, color, group)
