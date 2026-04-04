"""JSON protocol definitions and parsing for RS485 communication."""

from dataclasses import dataclass
from typing import Optional


@dataclass
class IdleState:
    """State for idle display mode."""

    mode: str = "clock"  # "clock", "text", "both"
    text: str = ""
    scroll: bool = False
    clock: str = "00:00"


@dataclass
class DisplayState:
    """Current state of a single display."""

    time: int = 0  # Seconds remaining
    group: str = ""  # "AB", "CD", "Links", "Rechts"
    color: str = "R"  # "R", "G", "Y", "I"
    end: str = ""  # "1/10", "2/5"
    idle: Optional[IdleState] = None

    @classmethod
    def from_dict(cls, data: dict) -> "DisplayState":
        idle = None
        if "idle" in data:
            idle_data = data["idle"]
            idle = IdleState(
                mode=idle_data.get("mode", "clock"),
                text=idle_data.get("text", ""),
                scroll=idle_data.get("scroll", False),
                clock=idle_data.get("clock", "00:00"),
            )
        return cls(
            time=data.get("t", 0),
            group=data.get("g", ""),
            color=data.get("c", "R"),
            end=data.get("e", ""),
            idle=idle,
        )


@dataclass
class ConfigMessage:
    """Configuration message received over RS485."""

    font_mode: Optional[str] = None  # "bdf" or "ttf"
    brightness: Optional[int] = None  # 0-100

    @classmethod
    def from_dict(cls, data: dict) -> "ConfigMessage":
        return cls(
            font_mode=data.get("font_mode"),
            brightness=data.get("brightness"),
        )
