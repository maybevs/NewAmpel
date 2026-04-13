using System.IO.Ports;
using System.Threading;
using AmpelSteuerung.Core.Configuration;
using AmpelSteuerung.Core.Models;
using Microsoft.Extensions.Logging;

namespace AmpelSteuerung.Core.Services;

public class SerialService : ISerialService
{
    private readonly IAmpelStateService _stateService;
    private readonly ILogger<SerialService> _logger;
    private readonly string _clockFormat;
    private readonly AmpelConfiguration _config;
    private SerialPort? _serialPort;
    private Thread? _broadcastThread;
    private volatile bool _broadcastRunning;
    private readonly object _lock = new();

    public bool IsConnected
    {
        get
        {
            lock (_lock) return _serialPort?.IsOpen == true;
        }
    }

    public string? CurrentPort
    {
        get
        {
            lock (_lock) return _serialPort?.PortName;
        }
    }

    public string? ErrorMessage { get; private set; }

    public event EventHandler<bool>? ConnectionChanged;

    public SerialService(IAmpelStateService stateService, AmpelConfiguration config, ILogger<SerialService> logger)
    {
        _stateService = stateService;
        _logger = logger;
        _clockFormat = config.Idle.ClockFormat;
        _config = config;
    }

    public string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }

    public void Connect(string portName, int baudRate = 115200)
    {
        lock (_lock)
        {
            try
            {
                Disconnect();

                _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    WriteTimeout = 500,
                    ReadTimeout = 500
                };
                _serialPort.Open();
                ErrorMessage = null;
                _logger.LogInformation("Connected to {Port} at {BaudRate} baud", portName, baudRate);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fehler beim Verbinden mit {portName}: {ex.Message}";
                _logger.LogError(ex, "Failed to connect to {Port}", portName);
                _serialPort?.Dispose();
                _serialPort = null;
            }
        }

        ConnectionChanged?.Invoke(this, IsConnected);
    }

    public void Disconnect()
    {
        lock (_lock)
        {
            if (_serialPort is { IsOpen: true })
            {
                try
                {
                    _serialPort.Close();
                    _logger.LogInformation("Disconnected from {Port}", _serialPort.PortName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while disconnecting");
                }
            }
            _serialPort?.Dispose();
            _serialPort = null;
        }

        ConnectionChanged?.Invoke(this, false);
    }

    public void StartBroadcast()
    {
        lock (_lock)
        {
            if (_broadcastRunning) return;

            _broadcastRunning = true;
            _broadcastThread = new Thread(BroadcastLoop)
            {
                IsBackground = true,
                Name = "RS485Broadcast",
                Priority = ThreadPriority.AboveNormal
            };
            _broadcastThread.Start();
            _logger.LogInformation("RS485 broadcast started (50 Hz, dedicated thread)");
        }
    }

    public void StopBroadcast()
    {
        _broadcastRunning = false;
        _broadcastThread?.Join(500);
        _broadcastThread = null;
        _logger.LogInformation("RS485 broadcast stopped");
    }

    public void SendRaw(string data)
    {
        lock (_lock)
        {
            if (_serialPort is not { IsOpen: true }) return;
            try
            {
                _serialPort.Write(data);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RS485 SendRaw write error");
            }
        }
    }

    private int _lastLoggedTime = -1;

    private void BroadcastLoop()
    {
        while (_broadcastRunning)
        {
            BroadcastOnce();
            Thread.Sleep(20); // ~50 Hz
        }
    }

    private void BroadcastOnce()
    {
        lock (_lock)
        {
            if (_serialPort is not { IsOpen: true }) return;

            try
            {
                var state = _stateService.CurrentState;
                string json;

                if (state.Status == TimerStatus.Stopped && state.Phase == MatchPhase.Idle && state.IdleMode != IdleDisplayMode.Off)
                {
                    // Idle mode: send message/clock JSON (only when explicitly stopped, not between ends)
                    var clock = DateTime.Now.ToString(_clockFormat);
                    var idleJson = AmpelState.ToIdleSerialJson(state.IdleMode, state.IdleMessage, state.IdleMessageScroll, clock);
                    json = $"{{\"d1\":{idleJson},\"d2\":{idleJson}}}\n";
                }
                else
                {
                    // Normal mode: apply display swap for RS485 output
                    var d1 = _stateService.Display1Side == "left" ? state.Display1 : state.Display2;
                    var d2 = _stateService.Display1Side == "left" ? state.Display2 : state.Display1;
                    var split = _config.FinalsSingleDisplay && state.Mode == OperatingMode.Final
                        ? ",\"split\":true"
                        : "";
                    json = $"{{\"d1\":{d1.ToSerialJson(state.TimeFormat)},\"d2\":{d2.ToSerialJson(state.TimeFormat)}{split}}}\n";

                    // Debug: log time value transitions
                    var t = d1.TimeRemaining;
                    if (t != _lastLoggedTime && state.Status == TimerStatus.Running)
                    {
                        _logger.LogInformation("[TX] t={Time} phase={Phase} fmt={Fmt}", t, state.Phase, state.TimeFormat);
                        _lastLoggedTime = t;
                    }
                }

                _serialPort.Write(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RS485 broadcast write error");
                ErrorMessage = $"Sendefehler: {ex.Message}";

                // Try to reconnect
                TryReconnect();
            }
        }
    }

    private void TryReconnect()
    {
        if (_serialPort == null) return;

        var portName = _serialPort.PortName;
        var baudRate = _serialPort.BaudRate;

        try
        {
            _serialPort.Close();
            _serialPort.Dispose();
            _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                WriteTimeout = 500,
                ReadTimeout = 500
            };
            _serialPort.Open();
            ErrorMessage = null;
            _logger.LogInformation("Reconnected to {Port}", portName);
            ConnectionChanged?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reconnect to {Port} failed", portName);
            ErrorMessage = $"Reconnect fehlgeschlagen: {ex.Message}";
            ConnectionChanged?.Invoke(this, false);
        }
    }

    public void Dispose()
    {
        StopBroadcast();
        Disconnect();
    }
}
