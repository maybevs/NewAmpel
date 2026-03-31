# 🏹 Bogensport-Ampel

Eine Open-Source-Ampelsteuerung für den Bogensport — DIY-Alternative zu kommerziellen Systemen für unter 900€.

Das System zeigt Schießzeit, Schützengruppe und Ampelfarbe auf zwei synchronisierten LED-Displays am Schießstand an. Die Steuerung erfolgt über eine Windows-App auf dem Laptop, optional ergänzt durch Tablet-Fernbedienung oder Streamdeck.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/UI-WPF-0078D4)
![License](https://img.shields.io/badge/License-MIT-green)

---

## Features

**Steuerung**
- Native Windows-App (WPF / .NET 8) mit Systemtray-Integration
- Tablet-Fernbedienung über eingebauten Webserver (lokales Netzwerk)
- Streamdeck-Anbindung über REST-API
- Keyboard-Shortcuts für schnelle Bedienung
- Beamer-Modus (Vollbild-Anzeige für Zuschauer)

**Wettkampf-Modi**
- WA 720 (Outdoor) — 240s, AB/CD-Wechsel
- WA Indoor — 120s, AB/CD-Wechsel
- Einzelfinale — Alternierend, 1 Pfeil pro Seite, 20s
- Mixed Team Finale — 2 Pfeile pro Team, 20s
- Team Finale — 3 Pfeile pro Team, 30s
- Eigene Presets über einfache YAML-Dateien

**Signale**
- Automatische Signaltöne nach World Archery Reglement (2× Vorbereitung, 1× Start, 3× Ende)
- Notfall-Stopp mit 5× Signal
- "Weiter"-Taste zum Überspringen der Restzeit
- Konfigurierbarer Passenablauf mit automatischem Gruppenwechsel

**Hardware**
- Zwei synchronisierte LED-Displays über RS485-Bus (50–100m Kabelstrecke)
- Unabhängige Anzeigen im Finalmodus (Links/Rechts)
- P4/P8 HUB75 LED-Panels, angesteuert über Raspberry Pi 4
- Zuverlässige Kabelverbindung statt fehleranfälligem WLAN

---

## Systemarchitektur

```
┌─────────────────┐         RS485 Bus (Twisted Pair)
│  Windows-Laptop │───USB───[USB-RS485]───///───[Anzeige 1]───///───[Anzeige 2]
│                 │                              Pi 4 + LEDs        Pi 4 + LEDs
│  - WPF App      │
│  - REST API     │◄──WiFi──[Tablet / Handy]
│  - RS485 Master │◄──USB───[Streamdeck]
└─────────────────┘
```

Die Steuer-App sendet den aktuellen Zustand 10× pro Sekunde als JSON über RS485. Die Anzeigen halten keinen eigenen Timer — sie rendern nur den zuletzt empfangenen State. Dadurch sind beide Displays immer perfekt synchron.

---

## Schnellstart

### Voraussetzungen

- Windows 10/11 mit [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- USB-zu-RS485-Adapter
- 1–2× Raspberry Pi 4 mit LED-Panels (siehe [Hardware-Dokumentation](docs/hardware.md))

### Installation

```bash
# Repository klonen
git clone https://github.com/maybevs/bogensport-ampel.git
cd bogensport-ampel

# Bauen und starten
dotnet build
dotnet run --project src/AmpelSteuerung.App
```

### Erster Test (ohne Hardware)

Die App startet auch ohne angeschlossenen RS485-Adapter. Der Timer, die UI und die REST-API funktionieren vollständig — nur der RS485-Broadcast zeigt einen Verbindungsfehler an.

1. App starten
2. COM-Port auf "Kein Gerät" lassen
3. Preset "WA 720" auswählen
4. Start drücken — der Timer läuft

### Test mit Tablet

1. Laptop und Tablet im selben WLAN
2. App starten
3. Auf dem Tablet im Browser: `http://LAPTOP-IP:5000`
4. Die Fernbedienungs-UI erscheint

---

## Hardware

### Kosten

| Komponente | Preis (ca.) |
|---|---|
| 2× Anzeigeeinheit (je 4× P8-Panels, Pi 4, Netzteil, Gehäuse) | 430–580€ |
| Steuereinheit (USB-RS485-Adapter) | 10–15€ |
| Verkabelung (150m RS485-Kabel, XLR-Stecker) | 170–230€ |
| **Gesamt** | **610–825€** |

Zum Vergleich: Kommerzielle Ampelsysteme kosten 2.000–4.000€+.

### Prototyp (Schreibtisch-Version)

Für die Entwicklung und Tests gibt es eine günstige Prototyp-Variante mit P4-Indoor-Panels:

| Komponente | Preis (ca.) |
|---|---|
| 4× P4 Indoor LED-Panel (256×128mm) | 32–60€ |
| Raspberry Pi 4 + SD + Netzteil + HAT | 88–103€ |
| RS485-Module + Adapter | 16–23€ |
| Netzteil + Kleinteile | 21–26€ |
| **Gesamt Prototyp** | **157–212€** |

Detaillierte Einkaufslisten und Aufbauanleitungen: siehe [`docs/`](docs/).

---

## Presets

Presets definieren den Wettkampfablauf als YAML-Datei im Ordner `Presets/`. Beispiel:

```yaml
# Presets/wa720.yaml
name: "WA 720 (70m Outdoor)"
description: "6 Pfeile pro Passe, 240 Sekunden Schießzeit"

timer:
  shootingTime: 240
  preparationTime: 10
  warningTime: 30

groups:
  mode: "alternating"     # AB/CD wechseln sich ab
  names: ["AB", "CD"]
  alternateStartOrder: true

match:
  totalEnds: 12
  arrowsPerEnd: 6
```

Für Finals gibt es einen speziellen Modus mit unabhängigen Anzeigen:

```yaml
# Presets/final_individual.yaml
name: "Einzelfinale"
type: "final"

timer:
  shootingTime: 20
  preparationTime: 10
  warningTime: 5

final:
  arrowsPerSide: 1
  totalArrowsPerEnd: 3
  sides: ["Links", "Rechts"]
  startSide: "manual"
```

Eigene Presets: Einfach eine neue `.yaml`-Datei in den `Presets/`-Ordner legen und die App neu starten.

---

## REST-API

Die eingebaute REST-API läuft auf `http://0.0.0.0:5000` und ermöglicht die Steuerung über Tablet, Streamdeck oder eigene Tools.

| Methode | Endpoint | Beschreibung |
|---|---|---|
| `GET` | `/api/state` | Aktueller Zustand (beide Displays) |
| `POST` | `/api/start` | Timer starten |
| `POST` | `/api/stop` | Timer stoppen |
| `POST` | `/api/pause` | Timer pausieren |
| `POST` | `/api/resume` | Fortsetzen (auch nach Notfall) |
| `POST` | `/api/reset` | Timer zurücksetzen |
| `POST` | `/api/group/{group}` | Gruppe setzen (AB, CD) |
| `POST` | `/api/duration/{seconds}` | Timer-Dauer setzen |
| `POST` | `/api/next-end` | Nächste Passe |
| `POST` | `/api/switch` | Seitenwechsel (Finalmodus) |
| `POST` | `/api/emergency-stop` | Notfall: 5× Signal + Stopp |
| `GET` | `/` | Tablet-Fernbedienungs-UI |

---

## RS485-Protokoll

JSON über Seriell, 9600 Baud, `\n`-terminiert, 10× pro Sekunde:

```json
{"d1":{"t":87,"g":"AB","c":"G","e":"1/10"},"d2":{"t":87,"g":"AB","c":"G","e":"1/10"}}
```

| Feld | Beschreibung |
|---|---|
| `d1`, `d2` | State für Display 1 bzw. 2 |
| `t` | Restzeit in Sekunden |
| `g` | Gruppe / Seite |
| `c` | Farbe: `R` (Rot), `G` (Grün), `Y` (Gelb) |
| `e` | Passe (z.B. "1/10") |

Im Standard-Modus sind `d1` und `d2` identisch. Im Finalmodus zeigen sie unterschiedliche Inhalte (z.B. eine Seite Grün mit Countdown, die andere Rot).

---

## Projektstruktur

```
bogensport-ampel/
├── src/
│   ├── AmpelSteuerung.App/        # WPF-Hauptanwendung
│   ├── AmpelSteuerung.Core/       # Geschäftslogik (State, Timer, Serial, Presets)
│   └── AmpelSteuerung.Api/        # Eingebettete REST-API + Tablet-UI
├── Presets/                        # YAML-Preset-Dateien
├── docs/
│   ├── hardware.md                # Einkaufslisten & Aufbauanleitungen
│   ├── hardware-prototype.md      # Prototyp-Version
│   └── display-software.md        # Raspberry Pi Display-Software
└── tests/
    └── AmpelSteuerung.Core.Tests/ # Unit Tests
```

---

## Keyboard-Shortcuts

| Taste | Aktion |
|---|---|
| `Leertaste` | Start / Pause |
| `Escape` | Stop |
| `Enter` | Weiter / Seitenwechsel |
| `1` / `2` | Gruppe AB / CD |
| `←` / `→` | Vorherige / Nächste Passe |
| `L` / `R` | Startseite Links / Rechts (Finalmodus) |
| `F5` | Notfall-Stopp |
| `F8` | Fortsetzen nach Notfall |

---

## Stream Deck Plugin

Das Projekt enthält ein natives Stream Deck Plugin, das die Ampelsteuerung direkt vom Elgato Stream Deck aus ermöglicht — Timer-Anzeige, Start/Pause, Notfall-Stopp, Gruppenwahl, Passenwechsel und Presets.

### Voraussetzungen

- Elgato Stream Deck Software ≥ 6.0
- .NET 8 Runtime auf dem PC
- Die Ampel-App muss laufen (das Plugin steuert über die REST-API auf `localhost:5000`)

### Installation

Ein PowerShell-Installationsskript erledigt alles automatisch:

```powershell
cd AmpelSteuerung
.\Install-StreamDeckPlugin.ps1
```

Das Skript führt vier Schritte aus:

1. **Build** — Kompiliert das Plugin-Projekt (`Release`)
2. **Icons generieren** — Erstellt die Action-Icons für das Stream Deck
3. **Installieren** — Kopiert alle Dateien nach `%APPDATA%\Elgato\StreamDeck\Plugins\com.ampelsteuerung.sdPlugin`
4. **Stream Deck neustarten** — Beendet und startet die Stream Deck Software, damit das Plugin erkannt wird

### Optionen

```powershell
# Debug-Build statt Release
.\Install-StreamDeckPlugin.ps1 -Configuration Debug

# Ohne automatischen Stream Deck Neustart
.\Install-StreamDeckPlugin.ps1 -SkipRestart
```

### Verfügbare Actions

Nach der Installation findet sich in der Stream Deck Software die Kategorie **Ampel Steuerung** mit folgenden Actions:

| Action | Beschreibung |
|---|---|
| **Timer-Anzeige** | Zeigt den aktuellen Timer mit Farbe, Gruppe und Passe an. Tippen = Start/Pause |
| **Start / Pause** | Start, Pause oder Weiter — passt sich dem aktuellen Status an |
| **Stop** | Timer stoppen |
| **Notfall-Stopp** | 5× Signal + sofortiger Stopp |
| **Skip** | Restzeit überspringen |
| **Gruppe AB / CD** | Gruppe setzen |
| **Nächste / Vorherige Passe** | Passenwechsel |
| **Preset laden** | Ein konfiguriertes Preset anwenden |

### Fehlerbehebung

- **Plugin erscheint nicht**: Stream Deck Software manuell neustarten oder PC neu starten
- **Actions zeigen "Keine Verbindung"**: Prüfen, ob die Ampel-App läuft und die REST-API auf Port 5000 erreichbar ist
- **Erneute Installation**: Das Skript ersetzt automatisch eine vorhandene Installation

---

## Roadmap

- [x] Systemarchitektur & Hardware-Design
- [x] Einkaufslisten & Aufbauanleitungen
- [x] WPF-Steuerungsapp (Core, UI, API)
- [ ] Raspberry Pi Display-Software
- [x] Streamdeck-Plugin (nativ)
- [x] Beamer-Modus mit Split-Screen für Finals
- [ ] Dark Mode
- [ ] Wettkampf-Log (Export der Passenzeiten)

---

## Mitmachen

Beiträge sind willkommen! Besonders gesucht:

- **Bogenschützen / Schießleitung**: Feedback zu Abläufen und Presets
- **Raspberry Pi / LED-Panel Erfahrung**: Display-Software und Pixelmapping
- **WPF / .NET Entwickler**: UI und Features
- **Hardware-Bastler**: Gehäusebau und Montage-Ideen

Bitte vor größeren Änderungen ein Issue erstellen, damit wir die Richtung besprechen können.

---

## Lizenz

MIT License — siehe [LICENSE](LICENSE).

---

## Danksagungen

- [hzeller/rpi-rgb-led-matrix](https://github.com/hzeller/rpi-rgb-led-matrix) — LED-Panel-Treiber für Raspberry Pi
- [Adafruit RGB Matrix HAT](https://www.adafruit.com/product/2345) — Hardware-Interface
- World Archery — Regelwerk und Signalton-Definitionen