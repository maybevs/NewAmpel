using System.Windows;
using AmpelSteuerung.Core.Models;

namespace AmpelSteuerung.App.Views;

public partial class PresetEditorWindow : Window
{
    public PresetEditorWindow(PresetEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public PresetEditorViewModel ViewModel => (PresetEditorViewModel)DataContext;

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.Name))
        {
            System.Windows.MessageBox.Show("Bitte einen Namen eingeben.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

public class PresetEditorViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private string _name = "";
    private string _description = "";
    private bool _isStandard = true;
    private bool _isFinal;
    private int _shootingTime = 120;
    private int _preparationTime = 10;
    private int _warningTime = 30;
    private int _totalEnds = 10;
    private int _arrowsPerEnd = 3;
    private bool _groupSwitchEnabled = true;
    private bool _alternateStartOrder = true;
    private int _arrowsPerSide = 1;
    private int _totalArrowsPerEnd = 3;

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public bool IsStandard { get => _isStandard; set { SetProperty(ref _isStandard, value); if (value) IsFinal = false; } }
    public bool IsFinal { get => _isFinal; set { SetProperty(ref _isFinal, value); if (value) IsStandard = false; OnPropertyChanged(nameof(IsFinal)); } }
    public int ShootingTime { get => _shootingTime; set => SetProperty(ref _shootingTime, value); }
    public int PreparationTime { get => _preparationTime; set => SetProperty(ref _preparationTime, value); }
    public int WarningTime { get => _warningTime; set => SetProperty(ref _warningTime, value); }
    public int TotalEnds { get => _totalEnds; set => SetProperty(ref _totalEnds, value); }
    public int ArrowsPerEnd { get => _arrowsPerEnd; set => SetProperty(ref _arrowsPerEnd, value); }
    public bool GroupSwitchEnabled { get => _groupSwitchEnabled; set => SetProperty(ref _groupSwitchEnabled, value); }
    public bool AlternateStartOrder { get => _alternateStartOrder; set => SetProperty(ref _alternateStartOrder, value); }
    public int ArrowsPerSide { get => _arrowsPerSide; set => SetProperty(ref _arrowsPerSide, value); }
    public int TotalArrowsPerEnd { get => _totalArrowsPerEnd; set => SetProperty(ref _totalArrowsPerEnd, value); }

    public static PresetEditorViewModel FromPreset(Preset preset)
    {
        var vm = new PresetEditorViewModel
        {
            Name = preset.Name,
            Description = preset.Description,
            IsStandard = !preset.IsFinalMode,
            IsFinal = preset.IsFinalMode,
            ShootingTime = preset.Timer.ShootingTime,
            PreparationTime = preset.Timer.PreparationTime,
            WarningTime = preset.Timer.WarningTime,
            TotalEnds = preset.Match.TotalEnds,
            ArrowsPerEnd = preset.Match.ArrowsPerEnd,
            GroupSwitchEnabled = preset.Options.GroupSwitchEnabled,
            AlternateStartOrder = preset.Groups.AlternateStartOrder,
        };
        if (preset.Final != null)
        {
            vm.ArrowsPerSide = preset.Final.ArrowsPerSide;
            vm.TotalArrowsPerEnd = preset.Final.TotalArrowsPerEnd;
        }
        return vm;
    }

    public Preset ToPreset()
    {
        var preset = new Preset
        {
            Name = Name,
            Description = Description,
            Type = IsFinal ? "final" : "standard",
            Timer = new PresetTimerSettings
            {
                ShootingTime = ShootingTime,
                PreparationTime = PreparationTime,
                WarningTime = WarningTime,
            },
            Groups = new PresetGroupSettings
            {
                Mode = GroupSwitchEnabled ? "alternating" : "single",
                Names = IsFinal ? ["1", "2"] : (GroupSwitchEnabled ? ["AB", "CD"] : ["AB"]),
                AlternateStartOrder = AlternateStartOrder,
            },
            Match = new PresetMatchSettings
            {
                TotalEnds = TotalEnds,
                ArrowsPerEnd = ArrowsPerEnd,
            },
            Options = new PresetOptions
            {
                GroupSwitchEnabled = GroupSwitchEnabled,
                SkipEnabled = true,
            },
        };

        if (IsFinal)
        {
            preset.Final = new PresetFinalSettings
            {
                ArrowsPerSide = ArrowsPerSide,
                TotalArrowsPerEnd = TotalArrowsPerEnd,
                Sides = ["1", "2"],
                StartSide = "manual",
                SkipEnabled = true,
            };
        }

        return preset;
    }
}
