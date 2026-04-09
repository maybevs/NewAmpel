"""RS485/UART serial receiver for JSON messages."""

import json
import time
import serial
import threading
from typing import Callable


class SerialReceiver:
    """Receives and parses newline-terminated JSON messages over RS485/UART."""

    def __init__(self, port: str = "/dev/serial0", baudrate: int = 9600,
                 rs485_mode: str = "waveshare"):
        self.port = port
        self.baudrate = baudrate
        self.rs485_mode = rs485_mode
        self._serial: serial.Serial | None = None
        self._running = False
        self._thread: threading.Thread | None = None
        self._callbacks: dict[str, list[Callable]] = {"display": [], "config": []}
        self.last_receive_time: float = time.time()

    def start(self) -> None:
        """Open serial port and start the reader thread."""
        print(f"[Serial] Opening {self.port} at {self.baudrate} baud (mode: {self.rs485_mode})")
        self._serial = serial.Serial(self.port, self.baudrate, timeout=0.1)

        if self.rs485_mode == "max485":
            import RPi.GPIO as GPIO
            GPIO.setmode(GPIO.BCM)
            GPIO.setup(4, GPIO.OUT)
            GPIO.output(4, GPIO.LOW)  # RX mode

        self._running = True
        self._thread = threading.Thread(target=self._read_loop, daemon=True)
        self._thread.start()

    def _read_loop(self) -> None:
        """Continuously read lines from serial and dispatch messages."""
        buffer = ""
        while self._running:
            try:
                if self._serial is None:
                    break
                data = self._serial.read(self._serial.in_waiting or 1)
                if data:
                    buffer += data.decode("utf-8", errors="ignore")
                    while "\n" in buffer:
                        line, buffer = buffer.split("\n", 1)
                        line = line.strip()
                        if line:
                            self._process_message(line)
            except Exception as e:
                print(f"Serial error: {e}")

    def _process_message(self, line: str) -> None:
        """Parse a JSON line and invoke the appropriate callbacks."""
        try:
            msg = json.loads(line)
        except json.JSONDecodeError:
            return

        self.last_receive_time = time.time()

        if "cfg" in msg:
            for cb in self._callbacks["config"]:
                cb(msg["cfg"])
        elif "d1" in msg or "d2" in msg:
            for cb in self._callbacks["display"]:
                cb(msg)

    def on_display(self, callback: Callable) -> None:
        """Register a callback for display messages."""
        self._callbacks["display"].append(callback)

    def on_config(self, callback: Callable) -> None:
        """Register a callback for config messages."""
        self._callbacks["config"].append(callback)

    def stop(self) -> None:
        """Stop the reader thread and close the serial port."""
        self._running = False
        if self._serial:
            self._serial.close()
            self._serial = None
        if self.rs485_mode == "max485":
            try:
                import RPi.GPIO as GPIO
                GPIO.cleanup(4)
            except Exception:
                pass
