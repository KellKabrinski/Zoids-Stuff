using ZoidsGameMAUI.Services;

namespace ZoidsGameMAUI.Views;

public partial class MainPage : ContentPage
{
    private readonly ZoidDataService _zoidDataService;
    private readonly SaveSystem _saveSystem;

    public MainPage(ZoidDataService zoidDataService, SaveSystem saveSystem)
    {
        InitializeComponent();
        _zoidDataService = zoidDataService;
        _saveSystem = saveSystem;
    }

    private async void OnNewGameClicked(object sender, EventArgs e)
    {
        // Navigate to Zoid Selection for starting a new game
        await Shell.Current.GoToAsync("zoidselection");
    }

    private async void OnLoadGameClicked(object sender, EventArgs e)
    {
        // Navigate to the save/load page in load mode
        await Shell.Current.GoToAsync("saveload?mode=load");
    }

    private async void OnZoidShopClicked(object sender, EventArgs e)
    {
        // Navigate to Zoid Selection for shopping
        await Shell.Current.GoToAsync("zoidselection");
    }

    private async void OnSaveManagerClicked(object sender, EventArgs e)
    {
        // Navigate to the save/load page in load mode (can switch to save mode from there)
        await Shell.Current.GoToAsync("saveload?mode=load");
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        var action = await DisplayActionSheet("Settings", "Cancel", null, 
            "View Save Files", "Delete All Saves", "Game Information");
            
        switch (action)
        {
            case "View Save Files":
                await Shell.Current.GoToAsync("saveload?mode=load");
                break;
                
            case "Delete All Saves":
                var confirm = await DisplayAlert("Delete All Saves", 
                    "Are you sure you want to delete ALL save files?\n\nThis action cannot be undone!", 
                    "Delete All", "Cancel");
                if (confirm)
                {
                    await DeleteAllSavesAsync();
                }
                break;
                
            case "Game Information":
                await DisplayAlert("Zoids Battle Game", 
                    "Version: 1.0.0\n\n" +
                    "Features:\n" +
                    "• Turn-based tactical combat\n" +
                    "• Position-based attacks\n" +
                    "• Variable speed movement\n" +
                    "• Zoid purchase system\n" +
                    "• Save/Load system\n" +
                    "• Victory rewards (5,000 credits)\n\n" +
                    "Save files are stored locally on your device.", 
                    "OK");
                break;
        }
    }

    private async Task DeleteAllSavesAsync()
    {
        try
        {
            var saveFiles = await _saveSystem.GetSaveFilesAsync();
            int deletedCount = 0;
            
            foreach (var saveFile in saveFiles)
            {
                if (await _saveSystem.DeleteSaveAsync(saveFile))
                {
                    deletedCount++;
                }
            }
            
            await DisplayAlert("Success", $"Deleted {deletedCount} save files.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to delete saves: {ex.Message}", "OK");
        }
    }

    private async void OnSystemTestClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("test");
    }
}
