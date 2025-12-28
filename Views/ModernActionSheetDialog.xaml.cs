using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace POS_in_NET.Views
{
    public partial class ModernActionSheetDialog : ContentView
    {
        private TaskCompletionSource<string?>? _taskCompletionSource;
        private Grid? _parentGrid;

        public ModernActionSheetDialog()
        {
            InitializeComponent();
        }

        public void SetActionSheet(string title, List<string> options, string icon = "📋")
        {
            TitleLabel.Text = title;
            IconLabel.Text = icon;
            OptionsContainer.Children.Clear();

            foreach (var option in options)
            {
                var button = new Button
                {
                    Text = option,
                    BackgroundColor = Color.FromArgb("#F9FAFB"),
                    TextColor = Color.FromArgb("#1F2937"),
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    CornerRadius = 12,
                    HeightRequest = 50,
                    BorderColor = Color.FromArgb("#E5E7EB"),
                    BorderWidth = 1
                };
                
                button.Clicked += (s, e) =>
                {
                    _taskCompletionSource?.TrySetResult(option);
                    CloseDialog();
                };

                OptionsContainer.Children.Add(button);
            }
        }

        public async Task<string?> ShowAsync()
        {
            _taskCompletionSource = new TaskCompletionSource<string?>();
            
            if (Application.Current?.MainPage != null)
            {
                var pageContent = GetPageContent(Application.Current.MainPage);
                if (pageContent is Grid mainGrid)
                {
                    _parentGrid = mainGrid;
                    
                    Grid.SetRowSpan(this, mainGrid.RowDefinitions.Count > 0 ? mainGrid.RowDefinitions.Count : 1);
                    Grid.SetColumnSpan(this, mainGrid.ColumnDefinitions.Count > 0 ? mainGrid.ColumnDefinitions.Count : 1);
                    Grid.SetRow(this, 0);
                    Grid.SetColumn(this, 0);
                    
                    mainGrid.Children.Add(this);
                }
            }

            return await _taskCompletionSource.Task;
        }

        private View? GetPageContent(Page page)
        {
            if (page is Shell shell && shell.CurrentPage is ContentPage currentPage)
            {
                return currentPage.Content;
            }
            else if (page is ContentPage contentPage)
            {
                return contentPage.Content;
            }
            return null;
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            _taskCompletionSource?.TrySetResult(null);
            CloseDialog();
        }

        private void CloseDialog()
        {
            if (_parentGrid != null)
            {
                _parentGrid.Children.Remove(this);
            }
        }
    }
}
