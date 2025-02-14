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
using Avalonia.Threading;
using System.Net;
using System.Web;
using Dave.Modules.Steam;
using Dave.Utility;
using System.Threading;
using SteamWebAPI2.Models;
using Dave.Caching;

namespace Dave.ViewModels
{
    public partial class MainWindow : Window
    {
        private readonly GameManager m_GameManager;
        private List<Game> m_AllGames = [];
        private bool m_IsLoggedIntoSteam = false;
        private CancellationTokenSource m_SearchCts = new();
        private ulong m_SteamId;
        private CacheManager m_CacheManager;

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
            m_CacheManager = new CacheManager();
            m_CacheManager.LoadCacheFromDisk("cache.json");
            LoadSteamIdFromCache();

            LoadGamesAsync();
            DisplayFriends();
        }

        private void LoadSteamIdFromCache()
        {
            var steamProfile = m_CacheManager.GetSteamUserProfile();
            if (steamProfile != null && steamProfile.User != 0)
            {
                m_SteamId = steamProfile.User;
                m_IsLoggedIntoSteam = true;
                Console.WriteLine($"Loaded Steam ID from cache: {m_SteamId}");
                m_GameManager.AddModule(new SteamModule(m_SteamId));
            }
            else
            {
                m_SteamId = 0;
                m_IsLoggedIntoSteam = false;
                Console.WriteLine("No valid Steam ID found in cache.");
            }
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

        private async void OnSearchTextChanged(object sender, KeyEventArgs e)
        {
            m_SearchCts.Cancel();
            m_SearchCts = new CancellationTokenSource();

            string searchText = SearchBox.Text?.ToLower().Trim() ?? "";

            await Task.Delay(300, m_SearchCts.Token);  // 300ms debounce delay

            if (m_SearchCts.Token.IsCancellationRequested) return;  // Ignore outdated searches

            var filteredGames = m_AllGames
                .Where(game => !string.IsNullOrEmpty(game.Name) && game.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            await DisplayGames(filteredGames);
        }

        private void OnSteamClicked(object sender, RoutedEventArgs e)
        {
            Logger.Logger.Debug("Click!");
        }

        private async Task LoadGamesAsync()
        {
            m_AllGames = await m_GameManager.GetAllGamesAsync();
            m_AllGames = [.. m_AllGames.OrderByDescending(game => game.Playtime)];
            await DisplayGames(m_AllGames); // Ensure it's awaited
        }

        private async void SteamLoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginManager.LoginToSteam();

            var callbackHandler = new SteamLoginCallbackHandler();
            await callbackHandler.StartListening();

            string steamId = callbackHandler.GetSteamID();
            m_SteamId = ulong.Parse(steamId);
            m_GameManager.AddModule(new SteamModule(m_SteamId));
            m_IsLoggedIntoSteam = true;

            m_CacheManager.AddOrUpdate(m_SteamId);
            m_CacheManager.SaveCacheToDisk("cache.json");

            await LoadGamesAsync();    // Awaited to ensure proper flow
            await DisplayFriends();    // Avoids race conditions
        }

        private async Task DisplayGames(List<Game> games)
        {
            SteamGamesContainer.Children.Clear();

            foreach (var game in games)
                SteamGamesContainer.Children.Add(await ElementCreator.CreateGameButton(game, ShowGameDetailsAsync));

            if (!SteamGamesContainer.Children.Any())
            {
                string message = m_IsLoggedIntoSteam ?
                    "It seems like your library is empty\n" :
                    "It seems you are not logged in\n";

                SteamGamesContainer.Children.Add(new TextBlock { Text = message });
            }

            foreach (var container in new[] { EpicGamesContainer, GOGGamesContainer })
                if (!container.Children.Any())
                    container.Children.Add(new TextBlock { Text = "Coming SoonTM" });
        }

        private async Task DisplayFriends()
        {
            SteamFriendsController.Children.Clear();
            var friends = (await m_GameManager.GetAllFriendsAsync())
                .OrderBy(f => f.UserStatus switch
                {
                    UserStatus.Online => 1,
                    UserStatus.Away or UserStatus.Busy or UserStatus.Snooze => 2,
                    _ => 3
                })
                .ThenBy(f => f.Username);

            foreach (var friend in friends)
                SteamFriendsController.Children.Add(await ElementCreator.CreateFriendButton(friend));

            if (!SteamFriendsController.Children.Any())
            {
                string message = m_IsLoggedIntoSteam ?
                    "It seems like you have no friends...\nI'm sorry :(" :
                    "It seems you are not logged in\n";

                SteamFriendsController.Children.Add(new TextBlock { Text = message });
            }

            foreach (var controller in new[] { EpicFriendsController, GOGFriendsController })
                if (!controller.Children.Any())
                    controller.Children.Add(new TextBlock { Text = "SoonTM" });
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
            var headerSection = await ElementCreator.CreateHeaderSection(game, gameDetails);
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
                Columns = Math.Max(1, Math.Min(3, game.Achievements.Count)),
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
                                    ? $"✅ {achievement.UnlockDate?.ToString("dd-MM-yyyy")}"
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
    }
}