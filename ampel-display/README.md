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
- Raspberry Pi OS (Debian Trixie/Bookworm, 64-bit)

---

## Installation (Schritt für Schritt)

### 1. Projekt auf den Pi kopieren

Vom Windows-PC:

```bash
scp -r .\ampel-display\ pi@<PI-HOSTNAME>:/home/pi/ampel-display
```

### 2. System-Pakete installieren

Auf dem Pi (SSH):

```bash
sudo apt-get update
sudo apt-get install -y git build-essential python3-dev cython3 python3-venv
```

### 3. Python Virtual Environment erstellen

Raspberry Pi OS (Debian Trixie) erlaubt keine system-weite pip-Installation.
Alle Python-Pakete werden in einem venv installiert:

```bash
python3 -m venv ~/ampel-venv
```

### 4. rpi-rgb-led-matrix bauen & installieren

```bash
cd ~
git clone https://github.com/hzeller/rpi-rgb-led-matrix.git
cd ~/rpi-rgb-led-matrix/bindings/python
~/ampel-venv/bin/pip install .
```

### 5. Python-Abhängigkeiten installieren

```bash
~/ampel-venv/bin/pip install -r ~/ampel-display/requirements.txt
~/ampel-venv/bin/pip install RPi.GPIO
```

### 6. BDF-Fonts kopieren

```bash
cp ~/rpi-rgb-led-matrix/fonts/{10x20,9x18,7x13B,7x13,6x10,5x8,5x7}.bdf ~/ampel-display/fonts/
```

### 7. Startscript ausführbar machen

```bash
chmod +x ~/ampel-display/run.sh
```

### 8. Testen

```bash
cd ~/ampel-display

# 2 Panels (Prototyp): chain-length=2
./run.sh 1 p4 bdf 80 max485 2

# 4 Panels (Final): chain-length=4
./run.sh 1 p4 bdf 80 max485 4
```

Wenn das Display "---" zeigt, läuft die Software korrekt im Standby-Modus
(keine RS485-Daten empfangen). Sobald die WPF-App sendet, erscheint die Anzeige.

---

## run.sh Parameter

```
./run.sh <display-id> <panel-type> <font-mode> <brightness> <rs485-mode> <chain-length>
```

| # | Parameter | Default | Beschreibung |
|---|-----------|---------|--------------|
| 1 | display-id | `1` | 1 oder 2 — filtert aus der Broadcast-Nachricht |
| 2 | panel-type | `p4` | `p4` (64×32 Panels) oder `p8` (32×16 Panels) |
| 3 | font-mode | `bdf` | `bdf` (Pixel-Fonts) oder `ttf` (Pillow) |
| 4 | brightness | `80` | Helligkeit 0–100 |
| 5 | rs485-mode | `waveshare` | `waveshare` (auto DE/RE) oder `max485` (GPIO4) |
| 6 | chain-length | `4` | Anzahl verketteter Panels (2 oder 4) |

**Beispiele:**

```bash
# Prototyp: Display 1, P4, 2 Panels, MAX485
./run.sh 1 p4 bdf 80 max485 2

# Final: Display 2, P8 Outdoor, 4 Panels, Waveshare HAT
./run.sh 2 p8 bdf 80 waveshare 4
```

---

## Updates deployen

Vom Windows-PC einzelne Dateien kopieren (nicht `scp -r src/` — das erzeugt
verschachtelte Ordner):

```bash
# Alle Source-Dateien
scp -r .\ampel-display\src\ampel_display\ pi@<PI-HOSTNAME>:/home/pi/ampel-display/src/ampel_display/

# Oder einzelne Dateien
scp .\ampel-display\src\ampel_display\renderer.py pi@<PI-HOSTNAME>:/home/pi/ampel-display/src/ampel_display/

# run.sh separat
scp .\ampel-display\run.sh pi@<PI-HOSTNAME>:/home/pi/ampel-display/run.sh
```

> **Wichtig:** Falls nach einem Update alte Fehler auftreten, Bytecode-Cache löschen:
> ```bash
> sudo find /home/pi/ampel-display -name __pycache__ -exec rm -rf {} +
> ```

---

## Autostart (systemd)

```bash
sudo cp ~/ampel-display/ampel-display.service /etc/systemd/system/
# Parameter in der .service-Datei anpassen (Display-ID, Panel-Typ, Chain-Length, etc.)
sudo nano /etc/systemd/system/ampel-display.service
sudo systemctl daemon-reload
sudo systemctl enable ampel-display
sudo systemctl start ampel-display

# Status / Logs prüfen
sudo systemctl status ampel-display
sudo journalctl -u ampel-display -f
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
