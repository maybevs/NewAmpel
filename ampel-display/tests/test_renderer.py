"""Tests for the renderer and display elements (no hardware required)."""

import pytest
from ampel_display.elements.timer_display import TimerDisplay
from ampel_display.elements.end_display import EndDisplay
from ampel_display.elements.color_bar import ColorBar


class TestTimerFormat:
    def test_zero(self):
        assert TimerDisplay.format_time(0) == "0"

    def test_under_minute(self):
        assert TimerDisplay.format_time(45) == "45"

    def test_one_minute(self):
        assert TimerDisplay.format_time(60) == "1:00"

    def test_minutes_seconds(self):
        assert TimerDisplay.format_time(87) == "1:27"

    def test_ten_minutes(self):
        assert TimerDisplay.format_time(600) == "10:00"

    def test_large_value(self):
        assert TimerDisplay.format_time(999) == "16:39"

    def test_negative_clamped(self):
        assert TimerDisplay.format_time(-5) == "0"


class TestColorBarColors:
    def test_red(self):
        assert ColorBar.COLORS["R"] == (255, 0, 0)

    def test_green(self):
        assert ColorBar.COLORS["G"] == (0, 255, 0)

    def test_yellow(self):
        assert ColorBar.COLORS["Y"] == (255, 255, 0)

    def test_unknown(self):
        assert ColorBar.COLORS.get("X", (0, 0, 0)) == (0, 0, 0)
