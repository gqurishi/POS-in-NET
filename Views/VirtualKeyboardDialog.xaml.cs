using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace POS_in_NET.Views
{
    public partial class VirtualKeyboardDialog : ContentView
    {
        private TaskCompletionSource<string?>? _tcs;
        private string _searchText = string.Empty;

        public VirtualKeyboardDialog()
        {
            InitializeComponent();
            StartCursorBlink();
        }

        public void SetInitialText(string text)
        {
            _searchText = text ?? string.Empty;
            UpdateDisplay();
        }

        public Task<string?> ShowAsync()
        {
            _tcs = new TaskCompletionSource<string?>();
            
            // Add to page
            if (Application.Current?.MainPage is Page page)
            {
                if (page is Shell shell && shell.CurrentPage is ContentPage contentPage)
                {
                    AddToPage(contentPage);
                }
                else if (page is ContentPage cp)
                {
                    AddToPage(cp);
                }
            }
            
            return _tcs.Task;
        }

        private void AddToPage(ContentPage page)
        {
            if (page.Content is Grid grid)
            {
                if (grid.RowDefinitions.Count > 0)
                    Grid.SetRowSpan(this, grid.RowDefinitions.Count);
                if (grid.ColumnDefinitions.Count > 0)
                    Grid.SetColumnSpan(this, grid.ColumnDefinitions.Count);
                
                grid.Children.Add(this);
            }
            else if (page.Content is Layout layout)
            {
                var newGrid = new Grid();
                var existingContent = page.Content;
                page.Content = null;
                newGrid.Children.Add(existingContent);
                newGrid.Children.Add(this);
                page.Content = newGrid;
            }
            
            IsVisible = true;
        }

        private void CloseDialog()
        {
            if (Parent is Grid grid)
            {
                grid.Children.Remove(this);
            }
        }

        private void UpdateDisplay()
        {
            SearchTextLabel.Text = _searchText;
        }

        private void StartCursorBlink()
        {
            // Simple cursor blink animation
            Device.StartTimer(TimeSpan.FromMilliseconds(500), () =>
            {
                if (IsVisible)
                {
                    CursorLabel.IsVisible = !CursorLabel.IsVisible;
                    return true;
                }
                return false;
            });
        }

        private void OnKeyPressed(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                _searchText += button.Text.ToLower();
                UpdateDisplay();
            }
        }

        private void OnBackspacePressed(object? sender, EventArgs e)
        {
            if (_searchText.Length > 0)
            {
                _searchText = _searchText.Substring(0, _searchText.Length - 1);
                UpdateDisplay();
            }
        }

        private void OnSpacePressed(object? sender, EventArgs e)
        {
            _searchText += " ";
            UpdateDisplay();
        }

        private void OnClearPressed(object? sender, EventArgs e)
        {
            _searchText = string.Empty;
            UpdateDisplay();
        }

        private void OnSearchPressed(object? sender, EventArgs e)
        {
            CloseDialog();
            _tcs?.TrySetResult(_searchText);
        }

        private void OnCancelClicked(object? sender, EventArgs e)
        {
            CloseDialog();
            _tcs?.TrySetResult(null);
        }
    }
}
