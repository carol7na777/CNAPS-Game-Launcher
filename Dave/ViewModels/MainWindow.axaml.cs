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

        // Event-Handler für den Minimieren-Button
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Event-Handler für den Vollbildmodus-Button
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
            // Prüfen, ob die linke Maustaste gedrückt ist
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                // BeginMoveDrag benötigt e als Argument
                BeginMoveDrag(e);
            }
        }
    }
}