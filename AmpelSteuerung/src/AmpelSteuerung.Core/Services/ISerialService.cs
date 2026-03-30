namespace AmpelSteuerung.Core.Services;

public interface ISerialService : IDisposable
{
    bool IsConnected { get; }
    string? CurrentPort { get; }
    string? ErrorMessage { get; }
    event EventHandler<bool>? ConnectionChanged;

    string[] GetAvailablePorts();
    void Connect(string portName, int baudRate = 9600);
    void Disconnect();
    void StartBroadcast();
    void StopBroadcast();
}
