using Microsoft.Maui.Controls;
using POS_in_NET.Services;
using System;
using System.Threading.Tasks;

namespace POS_in_NET.Controls
{
    public partial class ToastNotification : ContentView
    {
        private bool _isShowing = false;

        public ToastNotification()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Show toast notification with automatic styling and dismiss
        /// </summary>
        public async Task ShowAsync(string title, string message, NotificationType type, int durationMs = 2000)
        {
            // Don't show if already showing
            if (_isShowing) return;
            
            _isShowing = true;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Set content
                TitleLabel.Text = title;
                MessageLabel.Text = message;

                // Set colors and icon based on type
                switch (type)
                {
                    case NotificationType.Success:
                        ToastBorder.BackgroundColor = Color.FromArgb("#10B981"); // Green
                        IconLabel.Text = "✓";
                        break;

                    case NotificationType.Error:
                        ToastBorder.BackgroundColor = Color.FromArgb("#EF4444"); // Red
                        IconLabel.Text = "✕";
                        break;

                    case NotificationType.Warning:
                        ToastBorder.BackgroundColor = Color.FromArgb("#F59E0B"); // Orange
                        IconLabel.Text = "⚠";
                        break;

                    case NotificationType.Info:
                        ToastBorder.BackgroundColor = Color.FromArgb("#3B82F6"); // Blue
                        IconLabel.Text = "ℹ";
                        break;
                }

                // Animate in (slide from right + fade in)
                ToastBorder.Opacity = 0;
                ToastBorder.TranslationX = 400;
                
                await Task.WhenAll(
                    ToastBorder.FadeTo(1, 350, Easing.CubicOut),
                    ToastBorder.TranslateTo(0, 0, 350, Easing.CubicOut)
                );

                // Wait for duration
                await Task.Delay(durationMs);

                // Animate out (slide to right + fade out)
                await Task.WhenAll(
                    ToastBorder.FadeTo(0, 300, Easing.CubicIn),
                    ToastBorder.TranslateTo(100, 0, 300, Easing.CubicIn)
                );

                _isShowing = false;
            });
        }

        /// <summary>
        /// Close button handler
        /// </summary>
        private async void OnCloseTapped(object? sender, EventArgs e)
        {
            if (!_isShowing) return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Quick slide out to the right
                await Task.WhenAll(
                    ToastBorder.FadeTo(0, 200, Easing.CubicIn),
                    ToastBorder.TranslateTo(100, 0, 200, Easing.CubicIn)
                );

                _isShowing = false;
            });
        }
    }
}
