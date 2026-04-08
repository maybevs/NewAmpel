#!/bin/bash
# Bogensport-Ampel Display Start Script
# Must run with sudo (GPIO access for LED matrix)

DISPLAY_ID=${1:-1}
PANEL_TYPE=${2:-p4}
FONT_MODE=${3:-bdf}
BRIGHTNESS=${4:-80}
RS485_MODE=${5:-waveshare}
CHAIN_LENGTH=${6:-4}

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

# Activate venv inside sudo so all packages are on sys.path
VENV="${AMPEL_VENV:-/home/pi/ampel-venv}"

sudo bash -c "
    export PYTHONDONTWRITEBYTECODE=1
    source '$VENV/bin/activate'
    PYTHONPATH='$SCRIPT_DIR/src' python3 -m ampel_display \
        --display-id $DISPLAY_ID \
        --panel-type $PANEL_TYPE \
        --font-mode $FONT_MODE \
        --brightness $BRIGHTNESS \
        --rs485-mode $RS485_MODE \
        --chain-length $CHAIN_LENGTH
"
