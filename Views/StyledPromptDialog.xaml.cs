using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace POS_in_NET.Views
{
    public partial class StyledPromptDialog : ContentView
    {
        private TaskCompletionSource<string?>? _taskCompletionSource;
        private Grid? _parentGrid;

        public StyledPromptDialog()
        {
            InitializeComponent();
        }

        public void SetDialog(string title, string message, string placeholder, Keyboard? keyboard = null, string initialValue = "")
        {
            TitleLabel.Text = title;
            MessageLabel.Text = message;
            InputEntry.Placeholder = placeholder;
            InputEntry.Keyboard = keyboard ?? Keyboard.Default;
            InputEntry.Text = initialValue;
        }

        public async Task<string?> ShowAsync()
        {
            _taskCompletionSource = new TaskCompletionSource<string?>();
            
            // Find the GiftCardPage's content grid
            if (Application.Current?.MainPage is Shell shell)
            {
                var currentPage = shell.CurrentPage;
                
                // Try to find the content grid in the current page
                if (currentPage != null)
                {
                    var pageContent = FindPageContent(currentPage);
                    if (pageContent is Grid mainGrid)
                    {
                        _parentGrid = mainGrid;
                        
                        // Make sure dialog fills entire grid to center properly
                        Grid.SetRowSpan(this, mainGrid.RowDefinitions.Count > 0 ? mainGrid.RowDefinitions.Count : 1);
                        Grid.SetColumnSpan(this, mainGrid.ColumnDefinitions.Count > 0 ? mainGrid.ColumnDefinitions.Count : 1);
                        
                        // Add dialog overlay to the grid
                        mainGrid.Children.Add(this);
                        
                        // Focus on the entry field
                        await Task.Delay(100);
                        InputEntry.Focus();
                    }
                }
            }

            return await _taskCompletionSource.Task;
        }

        private Element? FindPageContent(Element element)
        {
            // For ContentPage, find the Content property
            var contentProperty = element.GetType().GetProperty("Content");
            if (contentProperty != null)
            {
                return contentProperty.GetValue(element) as Element;
            }
            return null;
        }

        private void OnOkClicked(object sender, EventArgs e)
        {
            _taskCompletionSource?.TrySetResult(InputEntry.Text);
            CloseDialog();
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            _taskCompletionSource?.TrySetResult(null);
            CloseDialog();
        }

        private void CloseDialog()
        {
            // Remove from parent
            if (_parentGrid != null)
            {
                _parentGrid.Children.Remove(this);
            }
        }
    }
}
