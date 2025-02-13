using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Dave.Modules;
using Dave.Modules.Abstract;
using Dave.Modules.Model;
using Dave.Modules.Steam;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dave.ViewModels
{
    public partial class MainWindow : Window
    {
        private readonly GameManager m_GameManager;
        public MainWindow()
        {
            InitializeComponent();

            // Erm�glicht Dragging �berall auf dem Fenster
            this.PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    BeginMoveDrag(e);
                }
            };

            List<IGameLauncherModule> modules = new List<IGameLauncherModule>
            {
                new SteamModule()
            };

            m_GameManager = new GameManager(modules);
            LoadGamesAsync();
        }

        private async void LoadGamesAsync()
        {
            List<Game> allGames = await m_GameManager.GetAllGamesAsync();
            SteamGamesContainer.Children.Clear();
            foreach (Game game in allGames)
            {
                var button = CreateGameButton(game);
                SteamGamesContainer.Children.Add(await button);
            }
        }

        private async Task<Button> CreateGameButton(Game game)
        {
            var button = new Button
            {
                Classes = { "game-button" },
                Background = Avalonia.Media.Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Margin = new Avalonia.Thickness(0, 5, 0, 5)
            };

            var stackPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };

            string iconPath = await DownloadGameIconAsync(game);

            var image = new Image
            {
                Source = new Avalonia.Media.Imaging.Bitmap(iconPath), // Add Image here
                Width = 40,
                Height = 40,
                Stretch = Avalonia.Media.Stretch.Uniform
            };

            var textBlock = new TextBlock
            {
                Text = game.Name,
                Foreground = Avalonia.Media.Brushes.White,
                FontSize = 14,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            stackPanel.Children.Add(image);
            stackPanel.Children.Add(textBlock);
            button.Content = stackPanel;

            button.Click += (_, _) => m_GameManager.LaunchGame(game);

            return button;
        }

        private async Task<string> DownloadGameIconAsync(Game game)
        {
            string cacheFolder = Path.Combine(AppContext.BaseDirectory, "GameIcons");
            Directory.CreateDirectory(cacheFolder); // Ensure the folder exists

            string localIconPath = Path.Combine(cacheFolder, $"{game.ID}.jpg");

            string downloadUrl = $"http://media.steampowered.com/steamcommunity/public/images/apps/{game.ID}/{game.IconUrl}.jpg";

            if (!System.IO.File.Exists(localIconPath))
            {
                using var httpClient = new HttpClient();
                try
                {
                    byte[] imageData = await httpClient.GetByteArrayAsync(downloadUrl); // Ensure `game.ImageUrl` is correct
                    await System.IO.File.WriteAllBytesAsync(localIconPath, imageData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to download icon for {game.Name}: {ex.Message}");
                    return "assets/gtav.png"; // Fallback icon
                }
            }

            return localIconPath;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = (this.WindowState == WindowState.Normal) ? WindowState.Maximized : WindowState.Normal;
        }

        private void OnDragWindow(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }
    }
}