using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Dave.ViewModels
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnDragWindow(object? sender, PointerPressedEventArgs e)
        {
            // Prüfen, ob die linke Maustaste gedrückt ist
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                // BeginMoveDrag benötigt e als Argument
                BeginMoveDrag(e);
            }
        }
    }
}