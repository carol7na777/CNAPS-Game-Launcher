using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Dave.ViewModels
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Event-Handler f�r den Minimieren-Button
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Event-Handler f�r den Vollbildmodus-Button
        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
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