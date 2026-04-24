"""Entry point: python3 -m ampel_display"""

import signal
import sys
import time
import os

from ampel_display.config import parse_args
from ampel_display.protocol import DisplayState, ConfigMessage
from ampel_display.matrix_driver import MatrixDriver
from ampel_display.fonts.font_manager import FontManager
from ampel_display.layout import Layout
from ampel_display.renderer import AmpelRenderer
from ampel_display.serial_receiver import SerialReceiver

# Timeout threshold: show standby after this many seconds without data
_TIMEOUT_SECONDS = 3.0

# Debug: set AMPEL_DEBUG=1 to enable time-value transition logging
_DEBUG = os.environ.get("AMPEL_DEBUG", "") == "1"


def main() -> None:
    args = parse_args()
    display_key = f"d{args.display_id}"

    # Initialise hardware
    matrix_driver = MatrixDriver(args.panel_type, args.brightness, args.chain_length)
    font_manager = FontManager(args.font_mode, matrix_driver.width, matrix_driver.height)
    layout = Layout()
    renderer = AmpelRenderer(matrix_driver.matrix, layout, font_manager, args.panel_type)
    receiver = SerialReceiver(args.serial_port, args.baudrate, args.rs485_mode)

    # Current display state (updated from serial thread)
    current_state = DisplayState()
    split_state: tuple[DisplayState, DisplayState] | None = None
    _dbg_last_t = -1
    _dbg_last_c = ""
    _dbg_rx_count = 0
    _dbg_json_err = 0

    def on_display_message(msg: dict) -> None:
        nonlocal current_state, split_state, _dbg_last_t, _dbg_last_c, _dbg_rx_count
        _dbg_rx_count += 1
        if msg.get("split"):
            # Single-display finals: both sides on one panel
            d1 = DisplayState.from_dict(msg["d1"]) if "d1" in msg else DisplayState()
            d2 = DisplayState.from_dict(msg["d2"]) if "d2" in msg else DisplayState()
            split_state = (d1, d2)
        else:
            split_state = None
            if display_key in msg:
                current_state = DisplayState.from_dict(msg[display_key])

        if _DEBUG:
            t = current_state.time
            c = current_state.color
            if t != _dbg_last_t or c != _dbg_last_c:
                print(f"[RX] t={t} c={c} f={current_state.format} rx#{_dbg_rx_count} @{time.time():.3f}")
                _dbg_last_t = t
                _dbg_last_c = c

    def on_config_message(cfg: dict) -> None:
        config = ConfigMessage.from_dict(cfg)
        if config.font_mode:
            font_manager.set_mode(config.font_mode)
            print(f"Font mode changed to: {config.font_mode}")
        if config.brightness is not None:
            matrix_driver.set_brightness(config.brightness)
            print(f"Brightness changed to: {config.brightness}")

    receiver.on_display(on_display_message)
    receiver.on_config(on_config_message)
    receiver.start()

    # Graceful shutdown
    def signal_handler(sig, frame):
        print("\nShutting down...")
        receiver.stop()
        sys.exit(0)

    signal.signal(signal.SIGINT, signal_handler)
    signal.signal(signal.SIGTERM, signal_handler)

    print(
        f"Ampel Display {args.display_id} started "
        f"({args.panel_type}, {matrix_driver.width}x{matrix_driver.height})"
    )

    # Render loop (~30 FPS for smooth scrolling)
    _dbg_render_t = -1
    try:
        while True:
            elapsed = time.time() - receiver.last_receive_time
            if elapsed > _TIMEOUT_SECONDS:
                renderer.render_standby()
            elif split_state is not None:
                renderer.render_split(split_state[0], split_state[1])
            else:
                renderer.render(current_state)
                if _DEBUG:
                    t = current_state.time
                    if t != _dbg_render_t:
                        print(f"[RENDER] t={t} c={current_state.color} @{time.time():.3f}")
                        _dbg_render_t = t
            time.sleep(1 / 30)
    except KeyboardInterrupt:
        receiver.stop()


if __name__ == "__main__":
    main()
