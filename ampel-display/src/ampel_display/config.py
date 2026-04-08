"""CLI argument parsing and configuration."""

import argparse


def parse_args(argv=None) -> argparse.Namespace:
    """Parse command-line arguments."""
    parser = argparse.ArgumentParser(description="Bogensport-Ampel LED Display")
    parser.add_argument(
        "--display-id",
        type=int,
        required=True,
        choices=[1, 2],
        help="Display ID (1 or 2)",
    )
    parser.add_argument(
        "--panel-type",
        type=str,
        default="p4",
        choices=["p4", "p8"],
        help="Panel type: p4 (128x64) or p8 (64x32)",
    )
    parser.add_argument(
        "--serial-port",
        type=str,
        default="/dev/serial0",
        help="Serial port for RS485",
    )
    parser.add_argument(
        "--baudrate",
        type=int,
        default=9600,
        help="Serial baud rate",
    )
    parser.add_argument(
        "--rs485-mode",
        type=str,
        default="waveshare",
        choices=["waveshare", "max485"],
        help="RS485 transceiver mode",
    )
    parser.add_argument(
        "--font-mode",
        type=str,
        default="bdf",
        choices=["bdf", "ttf"],
        help="Initial font rendering mode",
    )
    parser.add_argument(
        "--brightness",
        type=int,
        default=80,
        help="Initial brightness (0-100)",
    )
    parser.add_argument(
        "--chain-length",
        type=int,
        default=4,
        help="Number of daisy-chained panels (2 or 4)",
    )
    return parser.parse_args(argv)
