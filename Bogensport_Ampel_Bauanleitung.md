# Bogensport-Ampel — Systemarchitektur & Bauanleitung

## Überblick

Ein DIY-Ampelsystem für den Bogensport mit zwei vollständig synchronisierten, outdoor-tauglichen LED-Anzeigen, gesteuert per Tablet über WLAN. Die Kommunikation zwischen Steuerung und Anzeigen erfolgt über RS485 auf Twisted-Pair-Kabel — maximal zuverlässig über 50m+ Distanz.

---

## Systemdesign

### Architektur im Überblick

Das System besteht aus drei Knoten, verbunden über einen einzigen RS485-Bus:

- **Steuerstation** (am Stromanschluss): Ein Raspberry Pi mit Webserver. Man verbindet sich per Smartphone oder Tablet über WLAN und bedient die Ampel. Dieser Pi ist der Master — er sendet den aktuellen Zustand ~10× pro Sekunde über RS485.
- **Anzeigeeinheit 1** (am Schießstand): Ein Raspberry Pi, der 4× P8-LED-Panels im 2×2-Raster über einen HUB75-Adapter-HAT ansteuert. Er lauscht auf dem RS485-Bus und zeigt den aktuellen Countdown, die Schützengruppe (AB/CD) und die Ampelfarbe an.
- **Anzeigeeinheit 2** (30–50m von Anzeige 1 entfernt): Identische Hardware. Gleicher RS485-Bus, durchgeschleift.

### Warum RS485?

RS485 ist ein differentielles Signalprotokoll, das für industrielle Umgebungen entwickelt wurde. Es funktioniert zuverlässig über 1200m+ auf einem einfachen Twisted-Pair-Kabel, ist immun gegen WLAN-Störungen und braucht keine Netzwerkinfrastruktur. Ein einzelnes Kabel verbindet den Master mit Anzeige 1 und Anzeige 2 in Reihe. Die Latenz liegt unter einer Millisekunde — die Anzeigen sind praktisch perfekt synchron.

### Synchronisationskonzept

Der Master-Pi sendet nicht „starte einen 120-Sekunden-Timer" — er sendet 10× pro Sekunde den *aktuellen Zustand*: `{"t":87,"g":"AB","c":"G","e":1}`. Jede Anzeige rendert einfach den zuletzt empfangenen Zustand. Geht ein Paket verloren, kommt 100ms später das nächste. Kein Drift, keine Uhrensynchronisation, keine Fehlerbehandlung nötig.

---

## Display-Konfiguration

### Panel-Wahl: P8 (256×128mm)

- **Modulgröße**: 256mm × 128mm (25,6cm × 12,8cm)
- **Auflösung pro Panel**: 32×16 Pixel
- **Pixelabstand**: 8mm
- **Helligkeit**: ≥5500 Nits (outdoor-tauglich)
- **Anschluss**: HUB75, 1/4-Scan
- **Verfügbarkeit**: Gut — Standardgröße, leicht auf AliExpress/eBay zu finden

### Anordnung: 2×2 Raster

```
+------------------+------------------+
|     Panel 1      |     Panel 2      |
|   (32×16 px)     |   (32×16 px)     |
|   256×128mm      |   256×128mm      |
+------------------+------------------+
|     Panel 3      |     Panel 4      |
|   (32×16 px)     |   (32×16 px)     |
|   256×128mm      |   256×128mm      |
+------------------+------------------+

Gesamt: 64×32 Pixel = 51,2cm × 25,6cm
```

### Vorteile dieser Konfiguration

- **64×32 Pixel** — 33% mehr horizontale Auflösung als 3× P10 (48×32)
- **Einfaches Rechteck** — Gehäusebau deutlich einfacher als L-Form
- **P8 256×128mm ist ein Standardformat** — günstig und überall erhältlich
- **Schärfere Darstellung** — 8mm Pixelabstand statt 10mm bei gleicher Pixelzahl
- **Kompakt**: 51,2cm × 25,6cm — gut transportabel

### Lesbarkeit bei verschiedenen Entfernungen

Bei 64×32 Pixel auf 51,2cm × 25,6cm:

| Entfernung | Countdown (~12,8cm hoch) | Gruppe/Passe (~6–8cm) | Ampelfarbe |
|---|---|---|---|
| 20m (Halle) | Sehr gut | Sehr gut | Sehr gut |
| 30m | Gut | Gut | Sehr gut |
| 40m | Lesbar | Gerade noch | Sehr gut |
| 50m | Knapp lesbar | Schwer | Gut sichtbar |

Die Ampelfarbe (als Hintergrundfarbe oder großer Farbblock) ist das primäre Signal und aus jeder Entfernung sofort erkennbar. Die Countdown-Ziffern sind das zweitwichtigste Element.

### Display-Layout (optimiert für maximale Schriftgröße)

```
+================================================+
|                                                |
|           9 2              ████████            |
|      (Countdown, groß)     ████████            |
|                            (Ampel-             |
+--------------------------------farbe)----------+
|                            ████████            |
|    A/B         1/10        ████████            |
|   (Gruppe)    (Passe)                          |
|                                                |
+================================================+
     Panel 1+3                  Panel 2+4
```

- **Obere Hälfte**: Countdown-Ziffern so groß wie möglich (nutzen ~40 Pixel Breite, ~14 Pixel Höhe = 11,2cm). Rechts daneben: Farbblock.
- **Untere Hälfte**: Schützengruppe (A/B, C/D) links, Passe/Scheibe (z.B. 1/10) mittig. Farbblock rechts.
- **Farbblock rechts**: Durchgehend über beide Reihen — Rot/Grün/Gelb. Sofort erkennbar.

Alternative: Der gesamte Hintergrund wechselt die Farbe (maximale Sichtbarkeit aus der Ferne).

---

## Stückliste

### Pro Anzeigeeinheit (×2)

| Bauteil | Spezifikation | Ca. Preis | Hinweise |
|---|---|---|---|
| P8 Outdoor-LED-Panel | 32×16 Pixel, 8mm Pitch, 256×128mm, HUB75, RGB, ≥5500 Nits | ~30–40€/Stk. | **4 Stück** pro Anzeige. Outdoor-Version kaufen (IP65 Front, SMD3535, vergossen). Suchbegriff: „P8 outdoor LED module 256x128 HUB75". |
| Raspberry Pi 4 Model B | 2GB RAM reicht aus | ~45–55€ | Pi 5 geht auch, aber Pi 4 hat aktuell bessere HUB75-Bibliotheksunterstützung. |
| Adafruit RGB Matrix HAT | oder Electrodragon Active-3 HUB75 HAT | ~25–30€ | Schnittstelle zwischen Pi-GPIO und den HUB75-Panels. Der Adafruit HAT ist am besten dokumentiert. |
| RS485-HAT/Modul | Waveshare RS485 CAN HAT oder MAX485-Breakout | ~10–15€ | Wird an den Pi-UART angeschlossen. Waveshare HAT ist Plug-and-Play. |
| 5V-Netzteil | 5V 30A (150W), Meanwell-Typ | ~20–30€ | 4 P8-Panels bei Volllast ziehen ~60–70W. 150W gibt Reserven. Z.B. Meanwell LRS-150-5. |
| Wetterfestes Gehäuse | IP65, Aluminium oder Kunststoff, ~350×250×120mm | ~20–35€ | Pi + Netzteil hinein, LED-Panels vorne im 2×2-Raster montiert. Einfaches Rechteck. |
| Panel-Montagerahmen | Alu-Profil oder Winkelbleche | ~10–15€ | Hält die 4 Panels im 2×2-Raster. |
| Micro-SD-Karte | 32GB Class 10 | ~8€ | Für Pi OS + Software. |
| Diverses | Kabel, Stecker, Sicherungen, Klemmen | ~15–20€ | Feinsicherung auf der 5V-Leitung nicht vergessen. |

**Zwischensumme pro Anzeigeeinheit: ~215–290€**

### Steuerstation (×1)

| Bauteil | Spezifikation | Ca. Preis | Hinweise |
|---|---|---|---|
| Raspberry Pi 4 Model B | 2GB RAM | ~45–55€ | Betreibt die Web-UI und die RS485-Master-Logik. |
| USB-auf-RS485-Adapter | FTDI-basierter USB-Dongle | ~10–15€ | Einfacher als ein HAT, da kein GPIO für Panels nötig. |
| Kleines Gehäuse | Beliebige Projektbox | ~10€ | |
| Netzteil | Offizielles Pi USB-C-Netzteil | ~10€ | |
| SD-Karte | 32GB | ~8€ | |

**Zwischensumme Steuerstation: ~85–100€**

### Verkabelung

| Bauteil | Spezifikation | Ca. Preis | Hinweise |
|---|---|---|---|
| RS485-Kabel | Belden 9841 o.ä., geschirmtes Twisted Pair, 2-adrig + Schirm | ~1–2€/m | Ca. 130m nötig (50m zur Anzeige 1 + 50m zwischen Anzeigen + Reserve). 150m kaufen. |
| Kabelstecker | 3-polige XLR oder Schraubklemmen | ~10–20€ | XLR: verriegelnd, wetterfest. Pin 1 = GND/Schirm, Pin 2 = A, Pin 3 = B. |
| 120Ω Abschlusswiderstände | 2× 120Ω ¼W | ~1€ | Je einer an jedem Busende. |
| Kabelschutz für Außen | Kabelkanal oder UV-beständige Kabelbinder | ~10–15€ | |

**Zwischensumme Verkabelung: ~170–230€**

### Optionales Zubehör

| Bauteil | Zweck | Ca. Preis |
|---|---|---|
| USV / Akkupuffer | Anzeigen laufen bei kurzem Stromausfall weiter | ~40–60€ |
| Signalhorn / Summer | Akustisches Signal bei Start/Ende der Schießzeit | ~5–10€ pro Anzeige |
| Dediziertes Tablet | Fest montiert an der Steuerstation | ~100–200€ |
| Streamdeck Mini | Physische Tasten statt/zusätzlich zum Tablet | ~70–80€ |

---

## Gesamtkosten (geschätzt)

| Bereich | Niedrig | Hoch |
|---|---|---|
| 2× Anzeigeeinheiten (je 4× P8) | 430€ | 580€ |
| 1× Steuerstation | 85€ | 100€ |
| Verkabelung | 170€ | 230€ |
| **Gesamt (Kernsystem)** | **685€** | **910€** |
| Optionales Zubehör | 115€ | 350€ |

Zum Vergleich: Kommerzielle Ampelsysteme kosten typischerweise 2.000–4.000€+.

---

## Verkabelungsanleitung

### RS485-Bus-Topologie

```
Steuerstation            Anzeige 1              Anzeige 2
[Pi + USB-RS485]---///---[Pi + RS485 HAT]---///---[Pi + RS485 HAT]
       |                        |                        |
    [120Ω]                 (Durchleitung)             [120Ω]
  Abschluss                                         Abschluss
```

Einfache Daisy-Chain. **Keine Stern-Topologie** — RS485 erfordert einen linearen Bus mit Abschlusswiderständen nur an beiden Enden.

### RS485-Anschluss an jedem Knoten (XLR)

- **Pin 1**: GND / Kabelschirm
- **Pin 2**: RS485 A (nicht-invertierend)
- **Pin 3**: RS485 B (invertierend)

An Steuerstation und Anzeige 2 (Busenden): 120Ω-Widerstand zwischen Pin 2 und 3 einlöten.

### HUB75 LED-Panel-Verkabelung

Der Adafruit RGB Matrix HAT steckt direkt auf den GPIO-Header des Pi. Verkettung:

```
HAT ──IDC──▶ Panel 1 ──▶ Panel 2
                              (obere Reihe, Chain 1)

HAT ──IDC──▶ Panel 3 ──▶ Panel 4
  (2. Port)       (untere Reihe, Chain 2)
```

Der Adafruit HAT hat Anschlüsse für 2 parallele Ketten — ideal für ein 2×2-Raster. Konfiguration in `rpi-rgb-led-matrix`:
```
--led-rows=16 --led-cols=32 --led-chain=2 --led-parallel=2
```

Strom: 5V + GND vom Netzteil direkt an jedes Panel (rote/schwarze Schraubklemmen). **Panels NICHT über den Pi versorgen.** Pi vom gleichen Netzteil über GPIO oder USB-C speisen.

### Panel-Anordnung (2×2)

```
+------------------+------------------+
|     Panel 1      |     Panel 2      |
|   256×128mm      |   256×128mm      |
+------------------+------------------+
|     Panel 3      |     Panel 4      |
|   256×128mm      |   256×128mm      |
+------------------+------------------+

Gesamt: 51,2cm × 25,6cm
```

---

## Software-Übersicht

### Master-Pi (Steuerstation)

- **OS**: Raspberry Pi OS Lite
- **Webserver**: Python Flask oder Node.js Express
- **Web-UI**: Responsive HTML/JS — funktioniert auf jedem Browser
- **RS485**: Python `pyserial`, JSON-Pakete mit 10 Hz
- **Protokoll**: `{"t":87,"g":"AB","c":"G","e":1}\n` bei 9600 Baud

### Anzeige-Pi (×2)

- **OS**: Raspberry Pi OS Lite
- **LED-Treiber**: `rpi-rgb-led-matrix` (hzeller), konfiguriert für 2×2 Panel-Raster
- **RS485-Listener**: Python `pyserial`
- **Rendering**: Python mit `rgbmatrix`-Modul

### Funktionen der Steuer-UI

- Timer starten / stoppen / pausieren
- Timer-Dauer einstellen (120s, 90s, benutzerdefiniert)
- Gruppe wählen (AB / CD / Training / benutzerdefiniert)
- Manuelle Farbüberschreibung
- Passe/Scheibe anzeigen (z.B. „1/10")
- Signalton auslösen (falls Summer installiert)
- Voreingestellte Abläufe (z.B. Wettkampf: AB 120s → CD 120s → wiederholen)

---

## Nächste Schritte

1. **Teile bestellen** — LED-Panels haben 2–4 Wochen Lieferzeit aus China. Zuerst bestellen.
2. **Ein Display prototypen** — Einzelner Pi + 1 Panel mit `rpi-rgb-led-matrix` testen.
3. **Web-UI bauen** — Steuer-Oberfläche auf dem Master-Pi entwickeln.
4. **RS485 einrichten** — Bus verdrahten und mit beiden Anzeigen testen.
5. **Gehäuse bauen** — Wetterschutz für Außeneinsatz.
6. **Feldtest** — Am Schießstand mit realen Entfernungen testen.

---

## Empfohlene Bezugsquellen (Deutschland/EU)

- **LED-Panels**: AliExpress (Suche: „P8 outdoor LED module 256x128 HUB75 SMD3535"), eBay
- **Raspberry Pi**: BerryBase.de, Reichelt.de, Conrad.de
- **Adafruit HAT**: BerryBase.de, Exp-tech.de
- **RS485-Module**: Reichelt.de, Conrad.de, Amazon.de
- **Kabel (Belden 9841)**: Voelkner.de, RS-Online.de
- **XLR-Stecker**: Thomann.de
- **Gehäuse & Netzteile**: Reichelt.de, Conrad.de
