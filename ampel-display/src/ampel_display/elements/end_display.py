"""End/Passe display element — shows current end info like '3/12'."""


class EndDisplay:
    """Renders the end/passe information."""

    def draw(self, canvas, renderer, region: tuple, end_text: str, color: tuple,
             is_ttf: bool = False) -> None:
        """Draw end info centred in the region."""
        x, y, w, h = region
        if not end_text:
            return

        font_name = "small"

        # Prefix with "E " if there is enough room
        label = f"E {end_text}"
        label_w = renderer.measure_text(label, font_name)
        if label_w > w:
            label = end_text  # Drop the prefix
            label_w = renderer.measure_text(label, font_name)

        text_h = renderer.font_height(font_name)

        draw_x = x + max(0, (w - label_w) // 2)
        if is_ttf:
            draw_y = y + max(0, (h - text_h) // 2)
        else:
            draw_y = y + max(0, (h + text_h) // 2)

        renderer.draw_text(canvas, font_name, draw_x, draw_y, color, label)
