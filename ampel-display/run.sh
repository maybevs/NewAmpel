#!/bin/bash
# Bogensport-Ampel Display Start Script
# Must run with sudo (GPIO access for LED matrix)

DISPLAY_ID=${1:-1}
PANEL_TYPE=${2:-p4}
FONT_MODE=${3:-bdf}
BRIGHTNESS=${4:-80}
RS485_MODE=${5:-waveshare}
CHAIN_LENGTH=${6:-4}
SERIAL_PORT=${7:-auto}

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

# Auto-detect serial port: prefer USB adapter (immune to matrix DMA),
# fall back to GPIO UART only if no USB adapter found.
if [ "$SERIAL_PORT" = "auto" ]; then
    USB_PORT=$(ls /dev/ttyUSB* 2>/dev/null | head -1)
    ACM_PORT=$(ls /dev/ttyACM* 2>/dev/null | head -1)
    if [ -n "$USB_PORT" ]; then
        SERIAL_PORT=$USB_PORT
    elif [ -n "$ACM_PORT" ]; then
        SERIAL_PORT=$ACM_PORT
    else
        SERIAL_PORT=/dev/serial0
        echo "WARNING: No USB-to-RS485 adapter found, using GPIO UART ($SERIAL_PORT)."
        echo "         LED matrix DMA will corrupt serial data. Plug in a USB adapter."
    fi
fi
echo "Using serial port: $SERIAL_PORT"

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
        --chain-length $CHAIN_LENGTH \
        --serial-port $SERIAL_PORT
"
