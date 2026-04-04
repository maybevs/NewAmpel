"""Tests for the RS485 JSON protocol parsing."""

import pytest
from ampel_display.protocol import DisplayState, IdleState, ConfigMessage


class TestDisplayState:
    def test_from_dict_active(self):
        data = {"t": 87, "g": "AB", "c": "G", "e": "1/10"}
        state = DisplayState.from_dict(data)
        assert state.time == 87
        assert state.group == "AB"
        assert state.color == "G"
        assert state.end == "1/10"
        assert state.idle is None

    def test_from_dict_idle(self):
        data = {
            "c": "I",
            "idle": {
                "mode": "both",
                "text": "Mittagspause",
                "scroll": True,
                "clock": "12:34",
            },
        }
        state = DisplayState.from_dict(data)
        assert state.color == "I"
        assert state.idle is not None
        assert state.idle.mode == "both"
        assert state.idle.text == "Mittagspause"
        assert state.idle.scroll is True
        assert state.idle.clock == "12:34"

    def test_from_dict_defaults(self):
        state = DisplayState.from_dict({})
        assert state.time == 0
        assert state.group == ""
        assert state.color == "R"
        assert state.end == ""
        assert state.idle is None

    def test_from_dict_idle_defaults(self):
        data = {"c": "I", "idle": {}}
        state = DisplayState.from_dict(data)
        assert state.idle is not None
        assert state.idle.mode == "clock"
        assert state.idle.text == ""
        assert state.idle.scroll is False
        assert state.idle.clock == "00:00"

    def test_full_broadcast_message(self):
        """Parse both displays from a full broadcast message."""
        msg = {
            "d1": {"t": 17, "g": "Links", "c": "G", "e": "2/5"},
            "d2": {"t": 0, "g": "Rechts", "c": "R", "e": "2/5"},
        }
        d1 = DisplayState.from_dict(msg["d1"])
        d2 = DisplayState.from_dict(msg["d2"])
        assert d1.time == 17
        assert d1.group == "Links"
        assert d2.time == 0
        assert d2.color == "R"


class TestConfigMessage:
    def test_font_mode(self):
        cfg = ConfigMessage.from_dict({"font_mode": "ttf"})
        assert cfg.font_mode == "ttf"
        assert cfg.brightness is None

    def test_brightness(self):
        cfg = ConfigMessage.from_dict({"brightness": 50})
        assert cfg.font_mode is None
        assert cfg.brightness == 50

    def test_both(self):
        cfg = ConfigMessage.from_dict({"font_mode": "bdf", "brightness": 100})
        assert cfg.font_mode == "bdf"
        assert cfg.brightness == 100

    def test_empty(self):
        cfg = ConfigMessage.from_dict({})
        assert cfg.font_mode is None
        assert cfg.brightness is None
