using System.Collections.ObjectModel;
using ZoidsGameMAUI.Services;

namespace ZoidsGameMAUI.Views;

[QueryProperty(nameof(Mode), "mode")]
public partial class SaveLoadPage : ContentPage
{
    private readonly SaveSystem _saveSystem;
    private ObservableCollection<string> _saveFiles = new();
    private string _selectedSaveFile = "";
    
    public string Mode { get; set; } = "load"; // "load" or "save"

    public SaveLoadPage(SaveSystem saveSystem)
    {
        InitializeComponent();
        _saveSystem = saveSystem;
        
        SaveFilesCollectionView.ItemsSource = _saveFiles;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        
        // Configure UI based on mode
        if (Mode == "save")
        {
            HeaderLabel.Text = "Save Game";
            NewSaveButton.IsVisible = true;
            QuickSaveButton.IsVisible = true;
        }
        else
        {
            HeaderLabel.Text = "Load Game";
            QuickLoadButton.IsVisible = true;
        }
        
        await LoadSaveFilesAsync();
    }

    private async Task LoadSaveFilesAsync()
    {
        try
        {
            var saveFiles = await _saveSystem.GetSaveFilesAsync();
            _saveFiles.Clear();
            
            if (saveFiles.Any())
            {
                foreach (var file in saveFiles.OrderByDescending(f => f))
                {
                    _saveFiles.Add(file);
                }
                
                NoSavesMessage.IsVisible = false;
                SaveFilesCollectionView.IsVisible = true;
                
                // Load save info for each file (simplified for now)
                await LoadSaveInfoAsync();
            }
            else
            {
                NoSavesMessage.IsVisible = true;
                SaveFilesCollectionView.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load save files: {ex.Message}", "OK");
        }
    }

    private async Task LoadSaveInfoAsync()
    {
        // Load detailed save info to show in the UI
        foreach (var saveFile in _saveFiles)
        {
            try
            {
                var characterData = await _saveSystem.LoadCharacterAsync(saveFile);
                if (characterData != null)
                {
                    // We could enhance the UI to show this info per save file
                    // For now, we'll use it in the load confirmation dialog
                    System.Diagnostics.Debug.WriteLine($"Save '{saveFile}': {characterData.Name}, Credits: {characterData.Credits:N0}, Zoids: {characterData.Zoids.Count}");
                }
            }
            catch
            {
                // Save file might be corrupted
                System.Diagnostics.Debug.WriteLine($"Save '{saveFile}': Corrupted or invalid");
            }
        }
    }

    private void OnSaveFileSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string selectedFile)
        {
            _selectedSaveFile = selectedFile;
        }
    }

    private async void OnLoadSaveClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string saveFileName)
        {
            await LoadGameAsync(saveFileName);
        }
    }

    private async void OnDeleteSaveClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string saveFileName)
        {
            var result = await DisplayAlert("Delete Save", 
                $"Are you sure you want to delete '{saveFileName}'?\n\nThis action cannot be undone.", 
                "Delete", "Cancel");
                
            if (result)
            {
                try
                {
                    await _saveSystem.DeleteSaveAsync(saveFileName);
                    await DisplayAlert("Success", $"Save file '{saveFileName}' deleted.", "OK");
                    await LoadSaveFilesAsync(); // Refresh the list
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to delete save: {ex.Message}", "OK");
                }
            }
        }
    }

    private async void OnNewSaveClicked(object sender, EventArgs e)
    {
        var saveFileName = await DisplayPromptAsync("New Save", 
            "Enter a name for your save file:", 
            "Save", "Cancel",
            $"Save_{DateTime.Now:yyyyMMdd_HHmmss}",
            maxLength: 50);
            
        if (!string.IsNullOrWhiteSpace(saveFileName))
        {
            await SaveGameAsync(saveFileName);
        }
    }

    private async void OnQuickSaveClicked(object sender, EventArgs e)
    {
        await SaveGameAsync("QuickSave");
    }

    private async void OnQuickLoadClicked(object sender, EventArgs e)
    {
        await LoadGameAsync("QuickSave");
    }

    private async Task SaveGameAsync(string fileName)
    {
        try
        {
            // Try to load current character data from the running game
            // For now, we'll create a simple save or load from current_save
            var currentCharacter = await _saveSystem.LoadCharacterAsync("current_save");
            if (currentCharacter != null)
            {
                await _saveSystem.SaveCharacterAsync(currentCharacter, fileName);
                await DisplayAlert("Success", $"Game saved as '{fileName}'!", "OK");
                await LoadSaveFilesAsync(); // Refresh the list
            }
            else
            {
                await DisplayAlert("Error", "No current game data found to save.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save game: {ex.Message}", "OK");
        }
    }

    private async Task LoadGameAsync(string fileName)
    {
        try
        {
            var characterData = await _saveSystem.LoadCharacterAsync(fileName);
            
            // Copy the loaded data to "current_save" to make it the active save
            await _saveSystem.SaveCharacterAsync(characterData, "current_save");
            
            // Show detailed load information
            var zoidInfo = characterData.Zoids.Any() 
                ? $"🤖 Zoids Owned: {characterData.Zoids.Count}\n   " +
                  string.Join(", ", characterData.Zoids.Take(3).Select(z => z.Name)) + 
                  (characterData.Zoids.Count > 3 ? $" (and {characterData.Zoids.Count - 3} more)" : "")
                : "🤖 No Zoids owned yet\n   💡 Use your credits to purchase your first Zoid!";
            
            await DisplayAlert("Game Loaded Successfully!", 
                $"🎮 Character: {characterData.Name}\n" +
                $"💰 Credits: {characterData.Credits:N0}\n" +
                $"{zoidInfo}\n" +
                $"\n🚀 Ready to continue your adventure!", 
                "Continue");
            
            // Navigate to the Zoid Selection page
            await Shell.Current.GoToAsync("zoidselection");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load game: {ex.Message}", "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
