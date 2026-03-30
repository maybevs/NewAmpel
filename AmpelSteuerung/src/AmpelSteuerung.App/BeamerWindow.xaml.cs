using System.Windows;
using System.Windows.Input;

namespace AmpelSteuerung.App;

public partial class BeamerWindow : Window
{
    public BeamerWindow()
    {
        InitializeComponent();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape or Key.F11)
        {
            Close();
            e.Handled = true;
        }
    }
}
