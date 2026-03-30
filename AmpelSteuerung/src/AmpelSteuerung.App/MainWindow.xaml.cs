using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using AmpelSteuerung.App.ViewModels;

namespace AmpelSteuerung.App;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void SetViewModel(MainViewModel vm)
    {
        _viewModel = vm;
        DataContext = vm;
        SetupTrayIcon();
    }

    private void SetupTrayIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "Bogensport Ampelsteuerung",
            Visible = false
        };

        // Use a default icon
        _notifyIcon.Icon = System.Drawing.SystemIcons.Application;

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Anzeigen", null, (_, _) => ShowFromTray());
        menu.Items.Add("Start", null, (_, _) => _viewModel?.ToggleStartPauseCommand.Execute(null));
        menu.Items.Add("Stop", null, (_, _) => _viewModel?.StopTimerCommand.Execute(null));
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) =>
        {
            _notifyIcon.Visible = false;
            Application.Current.Shutdown();
        });
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        if (_notifyIcon != null) _notifyIcon.Visible = false;
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _notifyIcon != null)
        {
            Hide();
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(1000, "Ampelsteuerung", "Läuft im Hintergrund weiter.", System.Windows.Forms.ToolTipIcon.Info);
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (_viewModel == null) return;

        switch (e.Key)
        {
            case Key.Space:
                _viewModel.ToggleStartPauseCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Escape:
                _viewModel.StopTimerCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.D1:
            case Key.NumPad1:
                _viewModel.SetGroupABCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.D2:
            case Key.NumPad2:
                _viewModel.SetGroupCDCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Right:
                _viewModel.NextEndCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Left:
                _viewModel.PreviousEndCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void SetDuration_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out var seconds))
        {
            if (_viewModel != null)
                _viewModel.TimerDuration = seconds;
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _viewModel?.SaveConfiguration();
        _notifyIcon?.Dispose();
    }
}