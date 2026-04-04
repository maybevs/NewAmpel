"""Tests for the resolution-independent layout system."""

import pytest
from ampel_display.layout import Layout, LayoutRegion


class TestLayoutRegion:
    def test_dataclass_creation(self):
        r = LayoutRegion(0.1, 0.2, 0.5, 0.3)
        assert r.x == 0.1
        assert r.y == 0.2
        assert r.width == 0.5
        assert r.height == 0.3


class TestLayoutResolve:
    def test_resolve_full_canvas(self):
        region = LayoutRegion(0.0, 0.0, 1.0, 1.0)
        assert Layout.resolve(region, 128, 64) == (0, 0, 128, 64)
        assert Layout.resolve(region, 64, 32) == (0, 0, 64, 32)

    def test_resolve_partial_p4(self):
        """Timer region on P4 (128x64)."""
        region = Layout.ACTIVE["timer"]
        x, y, w, h = Layout.resolve(region, 128, 64)
        assert x == 0
        assert y == 0
        assert w == 83  # int(0.65 * 128)
        assert h == 35  # int(0.55 * 64)

    def test_resolve_partial_p8(self):
        """Timer region on P8 (64x32)."""
        region = Layout.ACTIVE["timer"]
        x, y, w, h = Layout.resolve(region, 64, 32)
        assert x == 0
        assert y == 0
        assert w == 41  # int(0.65 * 64)
        assert h == 17  # int(0.55 * 32)

    def test_color_bar_at_bottom(self):
        region = Layout.ACTIVE["color_bar"]
        x, y, w, h = Layout.resolve(region, 128, 64)
        assert x == 0
        assert y == 48  # int(0.75 * 64)
        assert w == 128
        assert h == 16  # int(0.25 * 64)


class TestIdleLayouts:
    def test_clock_layout(self):
        layout = Layout.get_idle_layout("clock")
        assert "clock" in layout
        assert "text" not in layout

    def test_text_layout(self):
        layout = Layout.get_idle_layout("text")
        assert "text" in layout
        assert "clock" not in layout

    def test_both_layout(self):
        layout = Layout.get_idle_layout("both")
        assert "clock" in layout
        assert "text" in layout

    def test_unknown_defaults_to_both(self):
        layout = Layout.get_idle_layout("unknown")
        assert "clock" in layout
        assert "text" in layout
