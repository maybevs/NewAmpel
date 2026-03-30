namespace AmpelSteuerung.Api;

public static class WebUiHtml
{
    public static string GetHtml(int apiPort)
    {
        return $@"<!DOCTYPE html>
<html lang=""de"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0, user-scalable=no"">
    <title>Ampelsteuerung</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
            background: #1e1e2e;
            color: white;
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            transition: background-color 0.5s;
        }}
        .header {{
            text-align: center;
            padding: 20px;
            transition: background-color 0.5s;
            border-radius: 0 0 16px 16px;
        }}
        .header.red {{ background: #cc0000; }}
        .header.green {{ background: #00aa00; }}
        .header.yellow {{ background: #ddaa00; }}
        .timer {{
            font-size: clamp(48px, 15vw, 120px);
            font-weight: bold;
            font-family: 'Consolas', 'Courier New', monospace;
            letter-spacing: 4px;
        }}
        .info-row {{
            display: flex;
            justify-content: space-around;
            margin-top: 8px;
            font-size: clamp(18px, 5vw, 32px);
            font-weight: bold;
            opacity: 0.9;
        }}
        .controls {{
            flex: 1;
            padding: 16px;
            display: flex;
            flex-direction: column;
            gap: 12px;
        }}
        .btn-row {{
            display: flex;
            gap: 10px;
            justify-content: center;
            flex-wrap: wrap;
        }}
        .btn {{
            border: none;
            border-radius: 12px;
            padding: 16px 28px;
            font-size: clamp(14px, 4vw, 20px);
            font-weight: bold;
            color: white;
            cursor: pointer;
            min-width: 100px;
            text-align: center;
            transition: transform 0.1s, opacity 0.2s;
            -webkit-tap-highlight-color: transparent;
            user-select: none;
        }}
        .btn:active {{ transform: scale(0.95); }}
        .btn-start {{ background: #2d7d2d; }}
        .btn-pause {{ background: #b8860b; }}
        .btn-resume {{ background: #2d6d9e; }}
        .btn-stop {{ background: #8b2222; }}
        .btn-reset {{ background: #3b3b5c; }}
        .btn-group {{ background: #3d5a80; min-width: 80px; }}
        .btn-color {{ min-width: 70px; }}
        .btn-color.red {{ background: #cc0000; }}
        .btn-color.green {{ background: #00aa00; }}
        .btn-color.yellow {{ background: #ddaa00; color: #333; }}
        .btn-nav {{ background: #3b3b5c; }}
        .section {{
            background: #2b2b40;
            border-radius: 12px;
            padding: 14px;
        }}
        .section-title {{
            font-size: 12px;
            text-transform: uppercase;
            color: #888;
            font-weight: bold;
            margin-bottom: 10px;
        }}
        .status {{
            font-size: 12px;
            color: #888;
            text-align: center;
        }}
        .status-text {{
            text-align: center;
            padding: 6px;
            font-size: 14px;
            color: #aaa;
        }}
    </style>
</head>
<body>
    <div class=""header red"" id=""header"">
        <div class=""timer"" id=""timer"">00:00</div>
        <div class=""info-row"">
            <span>Gruppe: <span id=""group"">--</span></span>
            <span>Passe: <span id=""end"">--</span></span>
        </div>
        <div class=""status-text"" id=""statusText"">Gestoppt</div>
    </div>

    <div class=""controls"">
        <div class=""section"">
            <div class=""section-title"">Steuerung</div>
            <div class=""btn-row"">
                <button class=""btn btn-start"" onclick=""api('start')"">&#9654; START</button>
                <button class=""btn btn-pause"" onclick=""api('pause')"">&#9208; PAUSE</button>
                <button class=""btn btn-resume"" onclick=""api('resume')"">&#9654; WEITER</button>
                <button class=""btn btn-stop"" onclick=""api('stop')"">&#9209; STOP</button>
                <button class=""btn btn-reset"" onclick=""api('reset')"">&#8634; RESET</button>
            </div>
        </div>

        <div class=""section"">
            <div class=""section-title"">Gruppe</div>
            <div class=""btn-row"">
                <button class=""btn btn-group"" onclick=""api('group/AB')"">AB</button>
                <button class=""btn btn-group"" onclick=""api('group/CD')"">CD</button>
            </div>
        </div>

        <div class=""section"">
            <div class=""section-title"">Passe</div>
            <div class=""btn-row"">
                <button class=""btn btn-nav"" onclick=""api('prev-end')"">&#9664; Zurück</button>
                <button class=""btn btn-nav"" onclick=""api('next-end')"">Vor &#9654;</button>
            </div>
        </div>

        <div class=""section"">
            <div class=""section-title"">Farbe (manuell)</div>
            <div class=""btn-row"">
                <button class=""btn btn-color red"" onclick=""api('color/red')"">ROT</button>
                <button class=""btn btn-color green"" onclick=""api('color/green')"">GRÜN</button>
                <button class=""btn btn-color yellow"" onclick=""api('color/yellow')"">GELB</button>
            </div>
        </div>

        <div class=""section"">
            <div class=""section-title"">Timer-Dauer</div>
            <div class=""btn-row"">
                <button class=""btn btn-reset"" onclick=""api('duration/60')"">60s</button>
                <button class=""btn btn-reset"" onclick=""api('duration/90')"">90s</button>
                <button class=""btn btn-reset"" onclick=""api('duration/120')"">120s</button>
                <button class=""btn btn-reset"" onclick=""api('duration/240')"">240s</button>
            </div>
        </div>
    </div>

    <div class=""status"" id=""connStatus"">Verbunden</div>

    <script>
        const BASE = window.location.origin;

        function api(endpoint) {{
            fetch(BASE + '/api/' + endpoint, {{ method: 'POST' }})
                .catch(e => console.error('API error:', e));
        }}

        function pad(n) {{ return n.toString().padStart(2, '0'); }}

        function updateState() {{
            fetch(BASE + '/api/state')
                .then(r => r.json())
                .then(s => {{
                    const mins = Math.floor(s.timeRemaining / 60);
                    const secs = s.timeRemaining % 60;
                    document.getElementById('timer').textContent = pad(mins) + ':' + pad(secs);
                    document.getElementById('group').textContent = s.group;
                    document.getElementById('end').textContent = s.end;

                    const header = document.getElementById('header');
                    header.className = 'header ' + s.color;

                    const statusMap = {{ running: 'Läuft', paused: 'Pausiert', stopped: 'Gestoppt' }};
                    document.getElementById('statusText').textContent = statusMap[s.status] || s.status;

                    document.getElementById('connStatus').textContent = 'Verbunden';
                    document.getElementById('connStatus').style.color = '#888';
                }})
                .catch(() => {{
                    document.getElementById('connStatus').textContent = 'Verbindung verloren';
                    document.getElementById('connStatus').style.color = '#ff6666';
                }});
        }}

        setInterval(updateState, 1000);
        updateState();
    </script>
</body>
</html>";
    }
}
