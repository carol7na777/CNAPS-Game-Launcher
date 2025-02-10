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
            // Pr�fen, ob die linke Maustaste gedr�ckt ist
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                // BeginMoveDrag ben�tigt e als Argument
                BeginMoveDrag(e);
            }
        }
    }
}