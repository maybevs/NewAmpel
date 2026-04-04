"""Color bar element — fills the bottom of the display with the traffic-light color."""


class ColorBar:
    """Renders a solid color bar representing the current signal."""

    COLORS = {
        "R": (255, 0, 0),
        "G": (0, 255, 0),
        "Y": (255, 255, 0),
    }

    def draw(self, canvas, region: tuple, color_code: str, is_ttf: bool = False) -> None:
        """Fill the region with the signal color.

        Works on both rgbmatrix canvas (SetPixel) and PIL Image (putpixel).
        """
        r, g, b = self.COLORS.get(color_code, (0, 0, 0))
        if r == 0 and g == 0 and b == 0:
            return

        x, y, w, h = region
        if is_ttf:
            # PIL Image
            for px in range(x, x + w):
                for py in range(y, y + h):
                    canvas.putpixel((px, py), (r, g, b))
        else:
            # rgbmatrix canvas
            for px in range(x, x + w):
                for py in range(y, y + h):
                    canvas.SetPixel(px, py, r, g, b)
