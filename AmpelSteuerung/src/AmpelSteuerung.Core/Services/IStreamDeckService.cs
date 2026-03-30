namespace AmpelSteuerung.Core.Services;

public interface IStreamDeckService
{
    bool IsConnected { get; }
    void Initialize();
    void UpdateDisplay();
    void Shutdown();
}
