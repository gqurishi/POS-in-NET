using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace POS_in_NET.Views
{
    public partial class ModernConfirmDialog : ContentView
    {
        private TaskCompletionSource<bool>? _taskCompletionSource;
        private Grid? _parentGrid;

        public ModernConfirmDialog()
        {
            InitializeComponent();
        }

        public void SetConfirm(string title, string message, string yesText = "Yes", string noText = "No", string icon = "!", string iconBgColor = "#F59E0B")
        {
            TitleLabel.Text = title;
            MessageLabel.Text = message;
            YesButton.Text = yesText;
            NoButton.Text = noText;
            
            IconBorder.BackgroundColor = Color.FromArgb(iconBgColor);
            var iconLabel = (IconBorder.Content as Label);
            if (iconLabel != null)
            {
                iconLabel.Text = icon;
            }
        }

        public async Task<bool> ShowAsync()
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

        private void OnYesClicked(object sender, EventArgs e)
        {
            _taskCompletionSource?.TrySetResult(true);
            CloseDialog();
        }

        private void OnNoClicked(object sender, EventArgs e)
        {
            _taskCompletionSource?.TrySetResult(false);
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
