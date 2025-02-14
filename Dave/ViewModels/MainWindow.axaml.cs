using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Dave.Modules;
using Dave.Modules.Model;
using Steam.Models.SteamCommunity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using AvaloniaWebView;

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

            m_GameManager = new GameManager();
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

            if (SteamGamesContainer.Children.Count == 0)
            {
                SteamGamesContainer.Children.Add(new TextBlock()
                {
                    Text = "It seems you are either not logged in\nor your library is empty"
                });
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

            if (SteamFriendsController.Children.Count == 0)
            {
                SteamFriendsController.Children.Add(new TextBlock()
                {
                    Text = "It seems you are either not logged in,\nor you do not have friends.\nIf it's the latter, then damn\nI'm sorry :("
                });
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
                    return "assets/default_icon.png"; // Fallback icon
                }
            }

            return localIconPath;
        }

        private async Task<Button> CreateGameButton(Game game)
        {
            var button = new Button
            {
                Classes = { "game-button" },
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 5, 0, 5)
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

            string iconPath = await DownloadGameIconAsync(game);

            var image = new Image
            {
                Source = new Bitmap(iconPath), // Add Image here
                Width = 32,
                Height = 32,
                Stretch = Stretch.None,
            };

            var textBlock = new TextBlock
            {
                Text = game.Name,
                Foreground = Brushes.White,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
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

            // Fetch game details and achievements asynchronously
            var gameDetails = await m_GameManager.GetGameStoreDetailsAsync(game);
            game.Achievements = await m_GameManager.GetAchievementsForGame(game);

            // Main border container with padding and rounded corners
            var mainBorder = new Border
            {
                Padding = new Thickness(20),
                CornerRadius = new CornerRadius(10),
                Background = Brushes.Transparent,
                Margin = new Thickness(20)
            };

            // Main grid: two rows (header, content) and two columns for content row
            var mainGrid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*"),
                ColumnDefinitions = new ColumnDefinitions("*,*")
            };

            // --- Row 0: Header Section ---
            Control headerSection;
            if (!string.IsNullOrEmpty(gameDetails.HeaderImage))
            {
                // Load banner image from URL asynchronously
                Logger.Logger.Warning(gameDetails.HeaderImage);
                string bannerBitmap = await DownloadGameBannerAsync(gameDetails);

                var headerImage = new Image
                {
                    Source = new Bitmap(bannerBitmap),
                    Stretch = Stretch.UniformToFill
                };

                var overlay = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Background = Brushes.Transparent,
                };

                overlay.Children.Add(new TextBlock
                {
                    Text = game.Name,
                    Foreground = Brushes.White,
                    FontSize = 36,
                    FontWeight = FontWeight.Bold,
                    TextWrapping = TextWrapping.Wrap
                });

                // Combine image and overlay in a grid
                var headerGrid = new Grid();
                headerGrid.Children.Add(headerImage);
                headerGrid.Children.Add(overlay);

                headerSection = new Border
                {
                    Child = headerGrid,
                    Height = 250,
                    CornerRadius = new CornerRadius(10),
                    Margin = new Thickness(0, 0, 0, 20)
                };
            }
            else
            {
                headerSection = new TextBlock
                {
                    Text = game.Name,
                    Foreground = Brushes.White,
                    FontSize = 36,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                };
            }
            // Header spans both columns in row 0
            Grid.SetColumnSpan(headerSection, 2);
            Grid.SetRow(headerSection, 0);
            mainGrid.Children.Add(headerSection);

            // --- Row 1, Column 0: Game Details Panel ---
            var detailsPanel = new StackPanel
            {
                Spacing = 10,
                Margin = new Thickness(10)
            };

            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Time Played: {Math.Round(game.Playtime, 2)} hours",
                Foreground = Brushes.LightGray,
                FontSize = 16
            });

            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Developer: {string.Join(", ", gameDetails.Developers ?? ["Unknown"])}",
                Foreground = Brushes.White,
                FontSize = 14
            });

            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Publisher: {string.Join(", ", gameDetails.Publishers ?? ["Unknown"])}",
                Foreground = Brushes.White,
                FontSize = 14
            });

            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"Website: {gameDetails.Website}",
                Foreground = Brushes.LightBlue,
                FontSize = 14
            });

            detailsPanel.Children.Add(new TextBlock
            {
                Text = gameDetails.ShortDescription ?? "No description available.",
                Foreground = Brushes.WhiteSmoke,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 400
            });

            var playButton = new Button
            {
                Content = "Play",
                Background = Brushes.LimeGreen,
                Foreground = Brushes.White,
                FontSize = 20,
                Padding = new Thickness(15, 8),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 10, 0, 0)
            };
            playButton.Click += (sender, args) => m_GameManager.LaunchGame(game);
            detailsPanel.Children.Add(playButton);

            Grid.SetRow(detailsPanel, 1);
            Grid.SetColumn(detailsPanel, 0);
            mainGrid.Children.Add(detailsPanel);

            // --- Row 1, Column 1: Compact Achievements Panel ---
            var achievementsPanel = new StackPanel
            {
                Spacing = 10,
                Margin = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Stretch
            };

            achievementsPanel.Children.Add(new TextBlock
            {
                Text = "Achievements",
                Foreground = Brushes.White,
                FontSize = 24,
                FontWeight = FontWeight.Bold
            });

            // Use a UniformGrid for a tidy, compact achievements display
            var achievementsGrid = new UniformGrid
            {
                Columns = 3,  // This will put 3 items per row
                Margin = new Thickness(0, 10, 0, 0),
            };

            foreach (var achievement in game.Achievements)
            {
                var achievementItem = new Border
                {
                    Width = 180,
                    Height = 90,
                    BorderThickness = new Thickness(1),
                    BorderBrush = achievement.Unlocked ? Brushes.Lime : Brushes.DarkRed,
                    Padding = new Thickness(3),
                    CornerRadius = new CornerRadius(5),
                    Background = Brushes.Transparent,
                    Child = new StackPanel
                    {
                        Spacing = 5,
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

                achievementsGrid.Children.Add(achievementItem);
            }

            // Optionally, wrap the achievements grid in a ScrollViewer if needed
            var achievementsScroll = new ScrollViewer
            {
                Content = achievementsGrid,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Height = 500
            };

            achievementsPanel.Children.Add(achievementsScroll);

            Grid.SetRow(achievementsPanel, 1);
            Grid.SetColumn(achievementsPanel, 1);
            mainGrid.Children.Add(achievementsPanel);

            // Set the main grid as the content of the border and add to MainContentArea
            mainBorder.Child = mainGrid;
            MainContentArea.Children.Add(mainBorder);
        }

        public async Task<string> DownloadGameBannerAsync(StoreDetails details)
        {
            // Cache directory for banners
            string cacheFolder = Path.Combine(AppContext.BaseDirectory, "GameBanners");
            Directory.CreateDirectory(cacheFolder); // Ensure the folder exists

            // Path to save the banner image
            string localBannerPath = Path.Combine(cacheFolder, $"{details.Id}_banner.jpg");
            string downloadUrl = details.HeaderImage;

            // If the banner hasn't been downloaded yet
            if (File.Exists(localBannerPath))
            {
                // Return the local path of the downloaded banner
                return localBannerPath;
            }

            using var httpClient = new HttpClient();
            try
            {
                // Download the banner image bytes
                byte[] imageData = await httpClient.GetByteArrayAsync(downloadUrl);

                // Save the banner image bytes to the local file system
                await File.WriteAllBytesAsync(localBannerPath, imageData);
            }
            catch (Exception)
            {
                return "assets/default_banner.png"; // Fallback banner image
            }

            // Return the local path of the downloaded banner
            return localBannerPath;
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
                    return "assets/default_icon.png"; // Fallback icon
                }
            }

            return localIconPath;
        }

        private async void StartSteamLogin()
        {
            string steamLoginUrl = "https://steamcommunity.com/openid/login"; // Steam-Login-Seite
            Logger.Logger.Info(steamLoginUrl);

        }

        // Event handler for Steam Login button
        private void SteamLoginButton_Click(object sender, RoutedEventArgs e)
        {
            StartSteamLogin();
        }
    }
}