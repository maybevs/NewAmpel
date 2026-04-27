using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using AmpelSteuerung.App.ViewModels;

namespace AmpelSteuerung.App;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private BeamerWindow? _beamerWindow;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void SetViewModel(MainViewModel vm)
    {
        _viewModel = vm;
        DataContext = vm;
        SetupTrayIcon();
        vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsBeamerOpen))
        {
            if (_viewModel!.IsBeamerOpen)
                OpenBeamerWindow();
            else
                CloseBeamerWindow();
        }
    }

    private void OpenBeamerWindow()
    {
        if (_beamerWindow != null) return;

        _beamerWindow = new BeamerWindow { DataContext = _viewModel };
        _beamerWindow.Closed += (_, _) =>
        {
            _beamerWindow = null;
            if (_viewModel != null) _viewModel.IsBeamerOpen = false;
        };

        // Place on the user-selected screen
        var screens = System.Windows.Forms.Screen.AllScreens;
        var selectedIdx = _viewModel?.SelectedScreenIndex ?? 0;
        if (selectedIdx >= 0 && selectedIdx < screens.Length)
        {
            var target = screens[selectedIdx];
            _beamerWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            _beamerWindow.Left = target.Bounds.Left;
            _beamerWindow.Top = target.Bounds.Top;
            _beamerWindow.Width = target.Bounds.Width;
            _beamerWindow.Height = target.Bounds.Height;
        }

        _beamerWindow.Show();
        _beamerWindow.WindowState = WindowState.Maximized;
    }

    private void CloseBeamerWindow()
    {
        _beamerWindow?.Close();
        _beamerWindow = null;
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
        // Minimize to taskbar (not tray) — no special handling needed
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (_viewModel == null) return;

        // Don't intercept keys when a TextBox has focus (let the user type freely)
        if (e.OriginalSource is System.Windows.Controls.TextBox)
            return;

        switch (e.Key)
        {
            case Key.Space:
                _viewModel.ToggleStartPauseCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Enter:
                _viewModel.SkipTimerCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Escape:
                _viewModel.StopTimerCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F5:
                _viewModel.EmergencyStopCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F8:
                _viewModel.ResumeTimerCommand.Execute(null);
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
            case Key.L:
                _viewModel.SetStartSideLeftCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.R:
                _viewModel.SetStartSideRightCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F11:
                _viewModel.ToggleBeamerCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.M:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // Ctrl+M: Focus the idle message textbox
                    IdleMessageTextBox?.Focus();
                }
                else if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    _viewModel.CycleIdleModeCommand.Execute(null);
                }
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
        CloseBeamerWindow();
        _viewModel?.SaveConfiguration();
        _notifyIcon?.Dispose();
    }
}