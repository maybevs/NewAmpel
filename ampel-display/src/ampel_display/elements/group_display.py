"""Group display element — shows the archer group (AB, CD, Links, Rechts)."""

_ACTIVE_COLOR = (255, 255, 255)   # bright white
_INACTIVE_COLOR = (60, 60, 60)    # faint grey


class GroupDisplay:
    """Renders the group/side identifier."""

    def draw(self, canvas, renderer, region: tuple, group: str, color: tuple,
             is_ttf: bool = False) -> None:
        """Draw group labels in the region.

        For AB/CD groups: always show both labels stacked (AB top-right,
        CD bottom-right), active in bright white, inactive in faint grey.
        For other groups: centred single label as before.
        """
        x, y, w, h = region
        if not group:
            return

        if group in ("AB", "CD"):
            self._draw_ab_cd(canvas, renderer, region, group, is_ttf)
        else:
            self._draw_single(canvas, renderer, region, group, color, is_ttf)

    def _draw_ab_cd(self, canvas, renderer, region: tuple, active: str,
                    is_ttf: bool) -> None:
        """Draw AB in top-right, CD in bottom-right of the region."""
        x, y, w, h = region
        font_name = "medium"
        text_h = renderer.font_height(font_name)

        # AB — top half, right-aligned
        ab_color = _ACTIVE_COLOR if active == "AB" else _INACTIVE_COLOR
        ab_w = renderer.measure_text("AB", font_name)
        ab_x = x + w - ab_w
        ab_y = y + 2 if is_ttf else y + text_h + 2
        renderer.draw_text(canvas, font_name, ab_x, ab_y, ab_color, "AB")

        # CD — bottom half, right-aligned
        cd_color = _ACTIVE_COLOR if active == "CD" else _INACTIVE_COLOR
        cd_w = renderer.measure_text("CD", font_name)
        cd_x = x + w - cd_w
        cd_y = y + h - text_h - 2 if is_ttf else y + h - 2
        renderer.draw_text(canvas, font_name, cd_x, cd_y, cd_color, "CD")

    def _draw_single(self, canvas, renderer, region: tuple, group: str,
                     color: tuple, is_ttf: bool) -> None:
        """Draw a single centred label (for Links/Rechts/etc.)."""
        x, y, w, h = region
        font_name = "medium" if len(group) <= 4 else "small"

        text_w = renderer.measure_text(group, font_name)
        text_h = renderer.font_height(font_name)

        draw_x = x + max(0, (w - text_w) // 2)
        if is_ttf:
            draw_y = y + max(0, (h - text_h) // 2)
        else:
            draw_y = y + max(0, (h + text_h) // 2)

        renderer.draw_text(canvas, font_name, draw_x, draw_y, color, group)
