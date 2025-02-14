using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia;
using Dave.Modules.Model;
using System;
using System.Threading.Tasks;
using Steam.Models.SteamCommunity;

namespace Dave.Utility
{
    public class ElementCreator
    {
        public static async Task<Control> CreateHeaderSection(Game game, StoreDetails gameDetails)
        {
            if (!string.IsNullOrEmpty(gameDetails.HeaderImage))
            {
                string bannerBitmap = await ResourceLoader.DownloadGameBannerAsync(gameDetails);
                var headerImage = new Image
                {
                    Source = new Bitmap(bannerBitmap),
                    Stretch = Stretch.Fill,
                };

                var overlay = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Background = Brushes.Transparent,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = game.Name,
                            Foreground = Brushes.White,
                            FontSize = 36,
                            FontWeight = FontWeight.Bold,
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                };

                var headerGrid = new Grid();
                headerGrid.Children.Add(headerImage);
                headerGrid.Children.Add(overlay);

                return new Border
                {
                    Child = headerGrid,
                    Height = 250,
                    CornerRadius = new CornerRadius(10),
                    Margin = new Thickness(0, 0, 0, 20)
                };
            }

            return new TextBlock
            {
                Text = game.Name,
                Foreground = Brushes.White,
                FontSize = 36,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
        }

        public static async Task<Button> CreateGameButton(Game game, Func<Game, Task> onClickCallback)
        {
            var button = new Button
            {
                Classes = { "game-button" },
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 5, 0, 5)
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

            string iconPath = await ResourceLoader.DownloadGameIconAsync(game);

            var image = new Image
            {
                Source = new Bitmap(iconPath),
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

            button.Click += (_, _) => onClickCallback(game);

            return button;
        }

        public static async Task<Button> CreateFriendButton(Friend friend)
        {
            var button = new Button
            {
                Classes = { "friend-button" },
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 5, 0, 5)
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

            string iconPath = await ResourceLoader.DownloadFriendIconAsync(friend);

            var image = new Image
            {
                Source = new Bitmap(iconPath), // Add Image here
                Width = 40,
                Height = 40,
                Stretch = Stretch.Uniform
            };

            var textBlock = new TextBlock
            {
                Text = friend.Username,
                Foreground = Brushes.White,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            var statusDot = new Avalonia.Controls.Shapes.Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = GetStatusBrush(friend.UserStatus),
                Margin = new Thickness(5, 0, 0, 0)
            };
            stackPanel.Children.Add(image);
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(statusDot);
            button.Content = stackPanel;

            return button;
        }

        private static SolidColorBrush GetStatusBrush(UserStatus status)
        {
            return status switch
            {
                UserStatus.Online => new SolidColorBrush(Color.FromRgb(0, 255, 0)),// green
                UserStatus.Away or UserStatus.Busy or UserStatus.Snooze => new SolidColorBrush(Color.FromRgb(255, 255, 0)),// yellow
                _ => new SolidColorBrush(Color.FromRgb(128, 128, 128)),// grey offline/other
            };
        }
    }
}
