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
        .displays {{
            display: flex;
            gap: 8px;
            padding: 12px;
        }}
        .display-panel {{
            flex: 1;
            text-align: center;
            padding: 16px;
            border-radius: 12px;
            transition: background-color 0.5s;
        }}
        .display-panel.red {{ background: #cc0000; }}
        .display-panel.green {{ background: #00aa00; }}
        .display-panel.yellow {{ background: #ddaa00; }}
        .display-label {{
            font-size: 11px;
            opacity: 0.7;
            text-transform: uppercase;
            font-weight: bold;
        }}
        .timer {{
            font-size: clamp(32px, 12vw, 80px);
            font-weight: bold;
            font-family: 'Consolas', 'Courier New', monospace;
            letter-spacing: 2px;
        }}
        .display-group {{
            font-size: clamp(14px, 4vw, 24px);
            font-weight: bold;
            opacity: 0.9;
        }}
        .info-row {{
            display: flex;
            justify-content: space-around;
            padding: 8px 16px;
            font-size: 14px;
            color: #aaa;
        }}
        .info-row .phase {{ color: #e8a030; font-weight: bold; }}
        .controls {{
            flex: 1;
            padding: 12px;
            display: flex;
            flex-direction: column;
            gap: 10px;
        }}
        .btn-row {{
            display: flex;
            gap: 8px;
            justify-content: center;
            flex-wrap: wrap;
        }}
        .btn {{
            border: none;
            border-radius: 10px;
            padding: 14px 22px;
            font-size: clamp(13px, 3.5vw, 18px);
            font-weight: bold;
            color: white;
            cursor: pointer;
            min-width: 80px;
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
        .btn-emergency {{ background: #cc0000; font-size: clamp(15px, 4vw, 20px); }}
        .btn-skip {{ background: #6b4c9a; }}
        .btn-group {{ background: #3d5a80; min-width: 70px; }}
        .btn-color {{ min-width: 60px; }}
        .btn-color.red {{ background: #cc0000; }}
        .btn-color.green {{ background: #00aa00; }}
        .btn-color.yellow {{ background: #ddaa00; color: #333; }}
        .btn-nav {{ background: #3b3b5c; }}
        .btn-side {{ background: #3d5a80; }}
        .section {{
            background: #2b2b40;
            border-radius: 10px;
            padding: 12px;
        }}
        .section-title {{
            font-size: 11px;
            text-transform: uppercase;
            color: #888;
            font-weight: bold;
            margin-bottom: 8px;
        }}
        .final-section {{ display: none; }}
        .final-section.active {{ display: block; }}
        .idle-section {{ display: none; }}
        .idle-section.active {{ display: block; }}
        .idle-modes {{ display: flex; gap: 6px; justify-content: center; flex-wrap: wrap; }}
        .idle-modes label {{ background: #2b2b40; padding: 8px 14px; border-radius: 8px; cursor: pointer; font-size: 13px; }}
        .idle-modes input[type=radio] {{ display: none; }}
        .idle-modes input[type=radio]:checked + span {{ color: #5B6BF5; font-weight: bold; }}
        .idle-input {{ display: flex; gap: 6px; margin-top: 8px; }}
        .idle-input input {{ flex: 1; background: #1e1e30; border: 1px solid #3a3a58; border-radius: 8px; padding: 10px; color: white; font-size: 14px; }}
        .idle-input input::placeholder {{ color: #555; }}
        .btn-clear {{ background: #3b3b5c; min-width: 44px; border-radius: 8px; font-size: 18px; }}
        .quick-msgs {{ display: flex; gap: 6px; flex-wrap: wrap; justify-content: center; margin-top: 8px; }}
        .btn-quick {{ background: #252545; border: 1px solid #3a3a58; border-radius: 8px; padding: 8px 14px; font-size: 12px; color: #aaa; cursor: pointer; }}
        .btn-quick:active {{ background: #3a3a58; }}
        .idle-scroll-info {{ font-size: 11px; color: #888; margin-top: 4px; text-align: center; }}
        .status {{
            font-size: 11px;
            color: #888;
            text-align: center;
            padding: 4px;
        }}
    </style>
</head>
<body>
    <div class=""displays"">
        <div class=""display-panel red"" id=""d1Panel"">
            <div class=""display-label"">Display 1</div>
            <div class=""timer"" id=""d1Timer"">00:00</div>
            <div class=""display-group"" id=""d1Group"">--</div>
        </div>
        <div class=""display-panel red"" id=""d2Panel"">
            <div class=""display-label"">Display 2</div>
            <div class=""timer"" id=""d2Timer"">00:00</div>
            <div class=""display-group"" id=""d2Group"">--</div>
        </div>
    </div>

    <div class=""info-row"">
        <span class=""phase"" id=""phaseText"">Bereit</span>
        <span>Passe: <span id=""end"">--</span></span>
        <span>Status: <span id=""statusText"">Gestoppt</span></span>
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
            <div class=""btn-row"" style=""margin-top:8px"">
                <button class=""btn btn-skip"" onclick=""api('skip')"">&#9193; SKIP</button>
                <button class=""btn btn-emergency"" onclick=""api('emergency-stop')"">&#9888; NOTFALL-STOPP</button>
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

        <div class=""section final-section"" id=""finalSection"">
            <div class=""section-title"">Final-Modus</div>
            <div class=""btn-row"">
                <button class=""btn btn-side"" onclick=""api('start-side/left')"">&#9664; Links</button>
                <button class=""btn btn-side"" onclick=""api('start-side/right')"">Rechts &#9654;</button>
                <button class=""btn btn-skip"" onclick=""api('switch-side')"">&#8596; Seitenwechsel</button>
            </div>
            <div style=""text-align:center;margin-top:8px;font-size:13px;color:#aaa"">
                Pfeile &#8212; L: <span id=""arrowL"">0</span> | R: <span id=""arrowR"">0</span>
            </div>
        </div>

        <div class=""section idle-section"" id=""idleSection"">
            <div class=""section-title"">Anzeige im Leerlauf</div>
            <div class=""idle-modes"">
                <label><input type=""radio"" name=""idleMode"" value=""clock"" onchange=""setIdleMode('clock')""><span>&#128339; Uhrzeit</span></label>
                <label><input type=""radio"" name=""idleMode"" value=""message"" onchange=""setIdleMode('message')""><span>&#128172; Nachricht</span></label>
                <label><input type=""radio"" name=""idleMode"" value=""both"" onchange=""setIdleMode('both')""><span>&#128339;+&#128172; Beides</span></label>
                <label><input type=""radio"" name=""idleMode"" value=""off"" onchange=""setIdleMode('off')""><span>&#10006; Aus</span></label>
            </div>
            <div class=""idle-input"">
                <input type=""text"" id=""idleMsg"" placeholder=""Nachricht eingeben..."" oninput=""sendIdleMsg(this.value)"" maxlength=""200""/>
                <button class=""btn btn-clear"" onclick=""clearIdleMsg()"">&#10005;</button>
            </div>
            <div class=""idle-scroll-info"" id=""idleScrollInfo""></div>
            <div class=""quick-msgs"" id=""quickMsgs""></div>
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
        let idleMsgTimer = null;

        function api(endpoint) {{
            fetch(BASE + '/api/' + endpoint, {{ method: 'POST' }})
                .catch(e => console.error('API error:', e));
        }}

        function setIdleMode(mode) {{
            fetch(BASE + '/api/idle/mode/' + mode, {{ method: 'POST' }});
        }}

        function sendIdleMsg(text) {{
            clearTimeout(idleMsgTimer);
            idleMsgTimer = setTimeout(() => {{
                fetch(BASE + '/api/idle/message', {{ method: 'POST', body: text }});
            }}, 300);
        }}

        function clearIdleMsg() {{
            document.getElementById('idleMsg').value = '';
            fetch(BASE + '/api/idle/message', {{ method: 'DELETE' }});
        }}

        function setQuickMsg(text) {{
            document.getElementById('idleMsg').value = text;
            fetch(BASE + '/api/idle/message', {{ method: 'POST', body: text }});
        }}

        function loadQuickMessages() {{
            fetch(BASE + '/api/idle')
                .then(r => r.json())
                .then(data => {{
                    const container = document.getElementById('quickMsgs');
                    container.innerHTML = '';
                    (data.quickMessages || []).forEach(msg => {{
                        const btn = document.createElement('button');
                        btn.className = 'btn-quick';
                        btn.textContent = msg;
                        btn.onclick = () => setQuickMsg(msg);
                        container.appendChild(btn);
                    }});
                }});
        }}

        function pad(n) {{ return n.toString().padStart(2, '0'); }}
        function fmtTime(t) {{ return pad(Math.floor(t/60)) + ':' + pad(t%60); }}

        const phaseMap = {{
            'Idle': 'Bereit',
            'PreparationGroup1': 'Vorbereitung',
            'ShootingGroup1': 'Schießzeit',
            'PreparationGroup2': 'Vorbereitung Gr.2',
            'ShootingGroup2': 'Schießzeit Gr.2',
            'EndCompleted': 'Passe beendet',
            'EmergencyStopped': '\u26A0 NOTFALL-STOPP'
        }};

        function updateState() {{
            fetch(BASE + '/api/state')
                .then(r => r.json())
                .then(s => {{
                    document.getElementById('d1Timer').textContent = fmtTime(s.display1.timeRemaining);
                    document.getElementById('d1Group').textContent = s.display1.group;
                    document.getElementById('d1Panel').className = 'display-panel ' + s.display1.color;

                    document.getElementById('d2Timer').textContent = fmtTime(s.display2.timeRemaining);
                    document.getElementById('d2Group').textContent = s.display2.group;
                    document.getElementById('d2Panel').className = 'display-panel ' + s.display2.color;

                    document.getElementById('end').textContent = s.currentEnd;
                    document.getElementById('phaseText').textContent = phaseMap[s.phase] || s.phase;

                    const statusMap = {{ running: 'Läuft', paused: 'Pausiert', stopped: 'Gestoppt' }};
                    document.getElementById('statusText').textContent = statusMap[s.status] || s.status;

                    // Final mode
                    const isFinal = s.mode === 'final';
                    document.getElementById('finalSection').className = 'section final-section' + (isFinal ? ' active' : '');
                    if (isFinal) {{
                        document.getElementById('arrowL').textContent = s.arrowCountLeft;
                        document.getElementById('arrowR').textContent = s.arrowCountRight;
                    }}

                    // Idle section — show when stopped
                    const isStopped = s.status === 'stopped';
                    document.getElementById('idleSection').className = 'section idle-section' + (isStopped ? ' active' : '');
                    if (isStopped && s.idle) {{
                        const radios = document.querySelectorAll('input[name=idleMode]');
                        radios.forEach(r => {{ r.checked = r.value === s.idle.mode; }});
                        // Only update msg input if not focused
                        const msgEl = document.getElementById('idleMsg');
                        if (document.activeElement !== msgEl) {{
                            msgEl.value = s.idle.message || '';
                        }}
                        document.getElementById('idleScrollInfo').textContent = s.idle.message
                            ? (s.idle.scroll ? '\u2190 Scrollend' : 'Statisch')
                            : '';
                    }}

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
        loadQuickMessages();
    </script>
</body>
</html>";
    }
}
