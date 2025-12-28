using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace POS_in_NET.Views
{
    public partial class ModernAlertDialog : ContentView
    {
        private TaskCompletionSource<bool>? _taskCompletionSource;
        private Grid? _parentGrid;

        public ModernAlertDialog()
        {
            InitializeComponent();
        }

        public void SetAlert(string title, string message, string icon = "i", string iconBgColor = "#3B82F6", string buttonColor = "#E5E7EB", string buttonTextColor = "#374151")
        {
            TitleLabel.Text = title;
            MessageLabel.Text = message;
            IconBorder.BackgroundColor = Color.FromArgb(iconBgColor);
            
            // Set icon text - use simple characters instead of emojis
            var iconLabel = (IconBorder.Content as Label);
            if (iconLabel != null)
            {
                iconLabel.Text = icon;
            }
            
            OkButton.BackgroundColor = Color.FromArgb(buttonColor);
            OkButton.TextColor = Color.FromArgb(buttonTextColor);
        }

        public async Task ShowAsync()
        {
            _taskCompletionSource = new TaskCompletionSource<bool>();
            
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

            await _taskCompletionSource.Task;
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

        private void OnOkClicked(object sender, EventArgs e)
        {
            _taskCompletionSource?.TrySetResult(true);
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
