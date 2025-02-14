using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Dave.Modules;
using Dave.Modules.Abstract;
using Dave.Modules.Model;
using Dave.Modules.Steam;
using Steam.Models.SteamCommunity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dave.ViewModels
{
    public partial class MainWindow : Window
    {
        private readonly GameManager m_GameManager;
        private List<Game> m_AllGames = new();

        public MainWindow()
        {
            InitializeComponent();

            // Ermöglicht Dragging überall auf dem Fenster
            this.PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    BeginMoveDrag(e);
                }
            };

            StartSteamLogin();

            List<IGameLauncherModule> modules = new List<IGameLauncherModule>
            {
                new SteamModule()
            };

            m_GameManager = new GameManager(modules);
            LoadGamesAsync();
            DisplayFriends();
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

        private void OnSearchTextChanged(object sender, KeyEventArgs e)
        {
            string searchText = SearchBox.Text?.ToLower().Trim() ?? "";

            var filteredGames = m_AllGames
                .Where(game => !string.IsNullOrEmpty(game.Name) && game.Name.ToLower().Contains(searchText))
                .ToList();

            DisplayGames(filteredGames);
        }

        private void OnSteamClicked(object sender, RoutedEventArgs e)
        {
            Logger.Logger.Debug("Click!");
        }

        private async void LoadGamesAsync()
        {
            m_AllGames = await m_GameManager.GetAllGamesAsync();
            m_AllGames = [.. m_AllGames.OrderByDescending(game => game.Playtime)];
            DisplayGames(m_AllGames);
        }
        private async void DisplayGames(List<Game> games)
        {
            SteamGamesContainer.Children.Clear(); // Clear that list
            foreach (Game game in games)
            {
                var button = await CreateGameButton(game);
                SteamGamesContainer.Children.Add(button);
            }
        }

        private async void DisplayFriends()
        {
            SteamFriendsController.Children.Clear();
            List<Friend> friends = await m_GameManager.GetAllFriendsAsync();

            var sortedFriends = friends.OrderBy(f => f.UserStatus switch
                {
                    UserStatus.Online => 1,
                    UserStatus.Away => 2,
                    UserStatus.Busy => 2,
                    UserStatus.Snooze => 2,
                    _ => 3
                }).ThenBy(f => f.Username).ToList();

            foreach (Friend friend in sortedFriends)
            {
                var button = await CreateFriendButton(friend);
                SteamFriendsController.Children.Add(button);
            }
        }

        private SolidColorBrush GetStatusBrush(UserStatus status)
        {
            switch (status)
            {
                case UserStatus.Online:
                    return new SolidColorBrush(Avalonia.Media.Color.FromRgb(0, 255, 0)); // green
                case UserStatus.Away:
                case UserStatus.Busy:
                case UserStatus.Snooze:
                    return new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 255, 0)); // yellow
                default: // offline/other
                    return new SolidColorBrush(Avalonia.Media.Color.FromRgb(128, 128, 128)); // grey
            }
        }

        private async Task<Button> CreateFriendButton(Friend friend)
        {
            var button = new Button
            {
                Classes = { "friend-button" },
                Background = Avalonia.Media.Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Margin = new Avalonia.Thickness(0, 5, 0, 5)
            };

            var stackPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };

            string iconPath = await DownloadFriendIconAsync(friend);

            var image = new Image
            {
                Source = new Avalonia.Media.Imaging.Bitmap(iconPath), // Add Image here
                Width = 40,
                Height = 40,
                Stretch = Avalonia.Media.Stretch.Uniform
            };

            var textBlock = new TextBlock
            {
                Text = friend.Username,
                Foreground = Avalonia.Media.Brushes.White,
                FontSize = 14,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            var statusDot = new Avalonia.Controls.Shapes.Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = GetStatusBrush(friend.UserStatus),
                Margin = new Avalonia.Thickness(5, 0, 0, 0)
            };
            stackPanel.Children.Add(image);
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(statusDot);
            button.Content = stackPanel;

            return button;
        }

        private async Task<string> DownloadFriendIconAsync(Friend friend)
        {
            string cacheFolder = Path.Combine(AppContext.BaseDirectory, "FriendIcons");
            Directory.CreateDirectory(cacheFolder); // Ensure the folder exists

            string localIconPath = Path.Combine(cacheFolder, $"{friend.SteamId}.jpg");

            if (!System.IO.File.Exists(localIconPath))
            {
                using var httpClient = new HttpClient();
                try
                {
                    byte[] imageData = await httpClient.GetByteArrayAsync(friend.AvatarUrl);
                    await System.IO.File.WriteAllBytesAsync(localIconPath, imageData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to download icon for {friend.Username}: {ex.Message}");
                    return "assets/gtav.png"; // Fallback icon
                }
            }

            return localIconPath;
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
                Width = 32,
                Height = 32,
                Stretch = Avalonia.Media.Stretch.None,
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

            button.DoubleTapped += (_, _) => m_GameManager.LaunchGame(game);
            button.Click += (_, _) => ShowGameDetailsAsync(game);

            return button;
        }

        private async Task ShowGameDetailsAsync(Game game)
        {
            MainContentArea.Children.Clear();

            game.Achievements = await m_GameManager.GetAchievementsForGame(game);

            var container = new StackPanel
            {
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            // Game Title
            var gameTitle = new TextBlock
            {
                Text = game.Name,
                Foreground = Brushes.White,
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            // Time Played
            var timePlayed = new TextBlock
            {
                Text = $"Time Played: {Math.Round(game.Playtime, 2)} hours",
                Foreground = Brushes.Gray,
                FontSize = 14,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            // Play Button
            var playButton = new Button
            {
                Content = "Play",
                Background = Brushes.Green,
                Foreground = Brushes.White,
                FontSize = 18,
                Padding = new Thickness(10, 5),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            playButton.Click += (sender, args) => m_GameManager.LaunchGame(game);

            // Game Description
            var gameDescription = new TextBlock
            {
                Text = game.Description ?? "No description available.",
                Foreground = Brushes.LightGray,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 400,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            // Achievements Section (Header)
            var achievementsHeader = new TextBlock
            {
                Text = "Achievements",
                Foreground = Brushes.White,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 10, 0, 5),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            // Create the achievements panel with wrapping and spacing
            var achievementsPanel = new WrapPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            // Add achievements to the panel
            foreach (var achievement in game.Achievements)
            {
                var achievementItem = new Border
                {
                    Width = 200,
                    Height = 120,
                    BorderThickness = new Thickness(2),
                    BorderBrush = achievement.Unlocked ? Brushes.Green : Brushes.Red,
                    Padding = new Thickness(10),
                    Margin = new Thickness(5),
                    CornerRadius = new CornerRadius(8),
                    Background = Brushes.Transparent,
                    Child = new StackPanel
                    {
                        Spacing = 5,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = achievement.Name,
                                Foreground = Brushes.White,
                                FontSize = 14,
                                FontWeight = FontWeight.Bold,
                                TextAlignment = TextAlignment.Center
                            },
                            new TextBlock
                            {
                                Text = achievement.Description,
                                Foreground = Brushes.Gray,
                                FontSize = 12,
                                MaxWidth = 180,
                                TextWrapping = TextWrapping.Wrap,
                                TextAlignment = TextAlignment.Center
                            },
                            new TextBlock
                            {
                                Text = achievement.Unlocked
                                    ? $"✅ {achievement.UnlockDate?.ToString("yyyy-MM-dd")}"
                                    : "❌ Locked",
                                Foreground = Brushes.LightGray,
                                FontSize = 12,
                                TextAlignment = TextAlignment.Center
                            }
                        }
                    }
                };

                achievementsPanel.Children.Add(achievementItem);
            }

            // Now wrap the panel in a ScrollViewer to add vertical scrolling
            var scrollContainer = new ScrollViewer
            {
                Content = achievementsPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, // Enable vertical scrolling
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, // Disable horizontal scrolling
                MaxHeight = 400
            };


            // Add everything to the container
            container.Children.Add(gameTitle);
            container.Children.Add(timePlayed);
            container.Children.Add(playButton);
            container.Children.Add(gameDescription);
            container.Children.Add(achievementsHeader);
            container.Children.Add(scrollContainer);

            MainContentArea.Children.Add(container);
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
                    byte[] imageData = await httpClient.GetByteArrayAsync(downloadUrl);
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
        private async void StartSteamLogin()
        {
            string steamLoginUrl = "https://steamcommunity.com/openid/login"; // Steam-Login-Seite

            // Öffne den Standardbrowser für den Login
            Process.Start(new ProcessStartInfo
            {
                FileName = steamLoginUrl,
                UseShellExecute = true
            });

            // Simuliertes Warten auf Login (5 Sekunden)
            await Task.Delay(5000);

            // Nach Login Inhalte der App laden
            ShowMainContent();
        }

        private void ShowMainContent()
        {
            this.Content = new Grid
            {
                Children =
        {
            new TextBlock
            {
                Text = "Willkommen! Deine Steam-Spiele werden geladen...",
                Foreground = Avalonia.Media.Brushes.White,
                FontSize = 20,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            }
        }
            };

            // Lade Steam-Spiele
            LoadGamesAsync();
        }
    }
}