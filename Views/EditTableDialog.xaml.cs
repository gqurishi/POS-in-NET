using POS_in_NET.Models;

namespace POS_in_NET.Views;

public partial class EditTableDialog : ContentView
{
    private TaskCompletionSource<(bool success, int floorId, string? tableName, string tableDesignIcon)>? _taskCompletionSource;
    private int _currentTableId;
    private string _selectedDesignIcon = "table_1.png"; // Default design

    public EditTableDialog()
    {
        InitializeComponent();
    }

    public void SetFloors(List<Floor> floors)
    {
        FloorPicker.ItemsSource = floors;
        FloorPicker.ItemDisplayBinding = new Binding("Name");
    }

    public void SetTableData(int tableId, string tableName, int floorId, string tableDesignIcon = "table_1.png")
    {
        _currentTableId = tableId;
        TableNumberEntry.Text = tableName;
        _selectedDesignIcon = tableDesignIcon;
        
        // Select the current design
        int designNumber = tableDesignIcon switch
        {
            "table_2.png" => 2,
            "table_3.png" => 3,
            "table_4.png" => 4,
            _ => 1
        };
        SelectDesign(designNumber);
        
        // Select the floor in the picker
        var floors = FloorPicker.ItemsSource as List<Floor>;
        if (floors != null)
        {
            var floor = floors.FirstOrDefault(f => f.Id == floorId);
            if (floor != null)
            {
                FloorPicker.SelectedItem = floor;
            }
        }
    }

    public Task<(bool success, int floorId, string? tableName, string tableDesignIcon)> ShowAsync()
    {
        _taskCompletionSource = new TaskCompletionSource<(bool, int, string?, string)>();
        IsVisible = true;
        
        // Auto-focus on table number entry
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100), () =>
        {
            TableNumberEntry.Focus();
        });
        
        return _taskCompletionSource.Task;
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        IsVisible = false;
        _taskCompletionSource?.SetResult((false, 0, null, "table_1.png"));
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        var tableName = TableNumberEntry.Text?.Trim();
        var selectedFloor = FloorPicker.SelectedItem as Floor;

        if (string.IsNullOrWhiteSpace(tableName))
        {
            DisplayAlert("Required", "Please enter a table number", "OK");
            return;
        }

        if (selectedFloor == null)
        {
            DisplayAlert("Required", "Please select a floor", "OK");
            return;
        }

        IsVisible = false;
        _taskCompletionSource?.SetResult((true, selectedFloor.Id, tableName, _selectedDesignIcon));
    }

    // Table Design Selection Methods
    private void OnDesign1Clicked(object sender, EventArgs e)
    {
        SelectDesign(1);
    }

    private void OnDesign2Clicked(object sender, EventArgs e)
    {
        SelectDesign(2);
    }

    private void OnDesign3Clicked(object sender, EventArgs e)
    {
        SelectDesign(3);
    }

    private void OnDesign4Clicked(object sender, EventArgs e)
    {
        SelectDesign(4);
    }

    private void SelectDesign(int designNumber)
    {
        // Reset all borders
        Design1Border.BackgroundColor = Colors.White;
        Design1Border.Stroke = Color.FromArgb("#E5E7EB");
        Design1Border.StrokeThickness = 2;

        Design2Border.BackgroundColor = Colors.White;
        Design2Border.Stroke = Color.FromArgb("#E5E7EB");
        Design2Border.StrokeThickness = 2;

        Design3Border.BackgroundColor = Colors.White;
        Design3Border.Stroke = Color.FromArgb("#E5E7EB");
        Design3Border.StrokeThickness = 2;

        Design4Border.BackgroundColor = Colors.White;
        Design4Border.Stroke = Color.FromArgb("#E5E7EB");
        Design4Border.StrokeThickness = 2;

        // Highlight selected design
        switch (designNumber)
        {
            case 1:
                Design1Border.BackgroundColor = Color.FromArgb("#F0F9FF");
                Design1Border.Stroke = Color.FromArgb("#3B82F6");
                Design1Border.StrokeThickness = 3;
                _selectedDesignIcon = "table_1.png";
                break;
            case 2:
                Design2Border.BackgroundColor = Color.FromArgb("#F0F9FF");
                Design2Border.Stroke = Color.FromArgb("#3B82F6");
                Design2Border.StrokeThickness = 3;
                _selectedDesignIcon = "table_2.png";
                break;
            case 3:
                Design3Border.BackgroundColor = Color.FromArgb("#F0F9FF");
                Design3Border.Stroke = Color.FromArgb("#3B82F6");
                Design3Border.StrokeThickness = 3;
                _selectedDesignIcon = "table_3.png";
                break;
            case 4:
                Design4Border.BackgroundColor = Color.FromArgb("#F0F9FF");
                Design4Border.Stroke = Color.FromArgb("#3B82F6");
                Design4Border.StrokeThickness = 3;
                _selectedDesignIcon = "table_4.png";
                break;
        }
    }

    private async void DisplayAlert(string title, string message, string cancel)
    {
        if (Application.Current?.MainPage != null)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }
    }
}
