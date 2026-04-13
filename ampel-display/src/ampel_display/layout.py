"""Resolution-independent layout system using proportional regions."""

from dataclasses import dataclass


@dataclass
class LayoutRegion:
    """A rectangular region defined as fractions of the total canvas (0.0–1.0)."""

    x: float
    y: float
    width: float
    height: float


class Layout:
    """Defines UI regions for all display modes."""

    # Active mode (timer running)
    # 1px border drawn separately; timer fills most of the space,
    # group label on the far right.
    ACTIVE = {
        "timer": LayoutRegion(0.02, 0.02, 0.75, 0.96),
        "group": LayoutRegion(0.77, 0.02, 0.21, 0.96),
    }

    # Finals mode: timer uses full width, no group label
    ACTIVE_FINALS = {
        "timer": LayoutRegion(0.02, 0.02, 0.96, 0.96),
    }

    # Idle modes
    IDLE_CLOCK = {
        "clock": LayoutRegion(0.0, 0.1, 1.0, 0.8),
    }

    IDLE_TEXT = {
        "text": LayoutRegion(0.0, 0.1, 1.0, 0.8),
    }

    IDLE_BOTH = {
        "clock": LayoutRegion(0.0, 0.05, 1.0, 0.45),
        "text": LayoutRegion(0.0, 0.55, 1.0, 0.40),
    }

    # Standby mode (no RS485 data)
    STANDBY = {
        "message": LayoutRegion(0.0, 0.1, 1.0, 0.8),
    }

    # Split mode (single display, two shooters side by side)
    # Regions computed directly in renderer for precise pixel control.

    @staticmethod
    def resolve(region: LayoutRegion, canvas_width: int, canvas_height: int) -> tuple[int, int, int, int]:
        """Convert a proportional region to pixel coordinates (x, y, w, h)."""
        return (
            int(region.x * canvas_width),
            int(region.y * canvas_height),
            int(region.width * canvas_width),
            int(region.height * canvas_height),
        )

    @staticmethod
    def get_idle_layout(mode: str) -> dict[str, LayoutRegion]:
        """Return the appropriate idle layout for the given mode."""
        if mode == "clock":
            return Layout.IDLE_CLOCK
        elif mode == "text":
            return Layout.IDLE_TEXT
        else:
            return Layout.IDLE_BOTH
