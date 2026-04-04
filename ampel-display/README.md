# Bogensport-Ampel — Pi LED Display

Python-Anwendung für Raspberry Pi 4 zur Ansteuerung von HUB75 LED-Panels über die
[rpi-rgb-led-matrix](https://github.com/hzeller/rpi-rgb-led-matrix) Library.

Die Software empfängt JSON-Nachrichten per RS485 von der Windows WPF-Steuerungssoftware
und zeigt Timer, Gruppenanzeige, Ampelfarbe und Ende/Passe auf der LED-Matrix an.

## Unterstützte Panels

| Typ | Auflösung | Panels | Grid |
|-----|-----------|--------|------|
| **P4 Indoor** | 128×64 px | 4× 64×32 | 2×2 |
| **P8 Outdoor** | 64×32 px | 4× 32×16 | 2×2 |

## Voraussetzungen

- Raspberry Pi 4 (Pi 5 wird **nicht** unterstützt)
- Adafruit RGB Matrix HAT
- RS485-Transceiver (Waveshare RS485 CAN HAT oder MAX485 Breadboard)
- Python 3.9+

### rpi-rgb-led-matrix installieren

```bash
git clone https://github.com/hzeller/rpi-rgb-led-matrix.git
cd rpi-rgb-led-matrix
make build-python PYTHON=$(which python3)
sudo make install-python PYTHON=$(which python3)
```

### Python-Abhängigkeiten

```bash
cd ampel-display
pip install -r requirements.txt
```

## Schnellstart

```bash
# P4 Indoor, Display 1, BDF-Fonts, MAX485-Prototyp
./run.sh 1 p4 bdf 80 max485

# P8 Outdoor, Display 2, Waveshare HAT
./run.sh 2 p8 bdf 80 waveshare
```

Oder direkt:

```bash
sudo python3 -m ampel_display \
    --display-id 1 \
    --panel-type p4 \
    --font-mode bdf \
    --brightness 80 \
    --rs485-mode waveshare
```

## CLI-Parameter

| Parameter | Default | Beschreibung |
|-----------|---------|--------------|
| `--display-id` | *(Pflicht)* | 1 oder 2 — filtert das passende Display aus der Broadcast-Nachricht |
| `--panel-type` | `p4` | `p4` (128×64) oder `p8` (64×32) |
| `--serial-port` | `/dev/serial0` | Serieller Port |
| `--baudrate` | `9600` | Baudrate |
| `--rs485-mode` | `waveshare` | `waveshare` (auto DE/RE) oder `max485` (GPIO4 manuell) |
| `--font-mode` | `bdf` | `bdf` (Pixel-Fonts) oder `ttf` (Pillow-Rendering) |
| `--brightness` | `80` | Helligkeit 0–100 |

## Autostart (systemd)

```bash
sudo cp ampel-display.service /etc/systemd/system/
# Ggf. Parameter in der .service-Datei anpassen
sudo systemctl daemon-reload
sudo systemctl enable ampel-display
sudo systemctl start ampel-display
```

## Projektstruktur

```
ampel-display/
├── src/ampel_display/
│   ├── __main__.py           # Entry Point
│   ├── config.py             # CLI-Argumente
│   ├── protocol.py           # JSON-Protokoll Datenklassen
│   ├── serial_receiver.py    # RS485/UART Empfänger
│   ├── matrix_driver.py      # rpi-rgb-led-matrix Wrapper
│   ├── renderer.py           # Haupt-Renderer (State → Canvas)
│   ├── layout.py             # Auflösungsunabhängiges Layout
│   ├── fonts/
│   │   ├── bdf_font.py       # BDF Pixel-Font Renderer
│   │   ├── ttf_font.py       # TTF/Pillow Font Renderer
│   │   └── font_manager.py   # Font-Modus Verwaltung
│   └── elements/
│       ├── timer_display.py   # Countdown-Timer
│       ├── group_display.py   # Gruppen-/Seitenanzeige
│       ├── end_display.py     # Ende/Passe
│       ├── color_bar.py       # Ampelfarb-Balken
│       └── idle_display.py    # Uhr, Scrolltext, beides
├── fonts/                     # BDF/TTF Font-Dateien
├── tests/                     # Unit Tests
├── requirements.txt
├── setup.py
├── run.sh                     # Start-Script
└── ampel-display.service      # systemd Unit
```

## RS485-Protokoll

Broadcast-Nachricht (10 Hz, `\n`-terminiert):

```json
{"d1":{"t":87,"g":"AB","c":"G","e":"1/10"},"d2":{"t":87,"g":"AB","c":"G","e":"1/10"}}
```

Config-Nachricht:

```json
{"cfg":{"font_mode":"bdf","brightness":80}}
```

## Timeout

Wenn 3 Sekunden lang keine gültige Nachricht empfangen wird, zeigt das Display
einen Standby-Modus mit "---" an. Beim nächsten empfangenen Frame wird sofort
wieder der normale Betrieb aufgenommen.
