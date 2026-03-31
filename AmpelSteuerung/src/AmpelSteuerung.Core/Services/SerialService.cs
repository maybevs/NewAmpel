using System.IO.Ports;
using AmpelSteuerung.Core.Configuration;
using AmpelSteuerung.Core.Models;
using Microsoft.Extensions.Logging;

namespace AmpelSteuerung.Core.Services;

public class SerialService : ISerialService
{
    private readonly IAmpelStateService _stateService;
    private readonly ILogger<SerialService> _logger;
    private readonly string _clockFormat;
    private SerialPort? _serialPort;
    private System.Timers.Timer? _broadcastTimer;
    private readonly object _lock = new();
    private bool _broadcasting;

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
    }

    public string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }

    public void Connect(string portName, int baudRate = 9600)
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
            if (_broadcasting) return;

            _broadcastTimer = new System.Timers.Timer(100); // 10x per second
            _broadcastTimer.Elapsed += OnBroadcastTick;
            _broadcastTimer.AutoReset = true;
            _broadcastTimer.Start();
            _broadcasting = true;
            _logger.LogInformation("RS485 broadcast started (10 Hz)");
        }
    }

    public void StopBroadcast()
    {
        lock (_lock)
        {
            _broadcastTimer?.Stop();
            _broadcastTimer?.Dispose();
            _broadcastTimer = null;
            _broadcasting = false;
            _logger.LogInformation("RS485 broadcast stopped");
        }
    }

    private void OnBroadcastTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        lock (_lock)
        {
            if (_serialPort is not { IsOpen: true }) return;

            try
            {
                var state = _stateService.CurrentState;
                string json;

                if (state.Status == TimerStatus.Stopped && state.IdleMode != IdleDisplayMode.Off)
                {
                    // Idle mode: send message/clock JSON
                    var clock = DateTime.Now.ToString(_clockFormat);
                    var idleJson = AmpelState.ToIdleSerialJson(state.IdleMode, state.IdleMessage, state.IdleMessageScroll, clock);
                    json = $"{{\"d1\":{idleJson},\"d2\":{idleJson}}}\n";
                }
                else
                {
                    // Normal mode: apply display swap for RS485 output
                    var d1 = _stateService.Display1Side == "left" ? state.Display1 : state.Display2;
                    var d2 = _stateService.Display1Side == "left" ? state.Display2 : state.Display1;
                    json = $"{{\"d1\":{d1.ToSerialJson()},\"d2\":{d2.ToSerialJson()}}}\n";
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
