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
        // Confirm if user wants to start a new game
        bool confirm = await DisplayAlert(
            "New Game", 
            "Are you sure you want to start a new game? This will create a new character with 40,000 credits.", 
            "Yes, Create New Game", 
            "Cancel"
        );
        
        if (!confirm)
            return;
            
        // Get character name from user
        string characterName = await DisplayPromptAsync(
            "Character Name", 
            "Enter a name for your character:", 
            "OK", 
            "Cancel", 
            "Player", 
            maxLength: 20,
            keyboard: Keyboard.Text
        );
        
        // If user cancels the name prompt, abort the new game creation
        if (characterName == null)
        {
            return;
        }
        
        if (string.IsNullOrWhiteSpace(characterName))
        {
            characterName = "Player"; // Default if user enters empty name
        }
        
        // Navigate to Zoid Selection with the character name
        await Shell.Current.GoToAsync($"zoidselection?newgame=true&charactername={Uri.EscapeDataString(characterName)}");
    }

    private async void OnContinueGameClicked(object sender, EventArgs e)
    {
        // Navigate to Zoid Selection to continue with existing save or create default character
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
