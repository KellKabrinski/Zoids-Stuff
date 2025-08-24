using ZoidsGameMAUI.Services;
using ZoidsGameMAUI.Models;
using System.Collections.ObjectModel;

namespace ZoidsGameMAUI.Views;

[QueryProperty(nameof(IsNewGame), "newgame")]
[QueryProperty(nameof(CharacterName), "charactername")]
public partial class ZoidSelectionPage : ContentPage
{
    private readonly ZoidDataService _zoidDataService;
    private readonly SaveSystem _saveSystem;
    private readonly BattleService _battleService;
    private List<ZoidData> _allZoids = new();
    private ObservableCollection<ZoidData> _filteredZoids = new();
    private ZoidData? _selectedZoid;
    private CharacterData? _currentCharacter;
    private bool _sortByName = true;
    
    // Query parameters for new game flow
    public string IsNewGame { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;

    public ZoidSelectionPage(ZoidDataService zoidDataService, SaveSystem saveSystem, BattleService battleService)
    {
        InitializeComponent();
        _zoidDataService = zoidDataService;
        _saveSystem = saveSystem;
        _battleService = battleService;
        
        ZoidCollectionView.ItemsSource = _filteredZoids;
        FilterPicker.SelectedIndex = 0; // Default to "All Zoids"
        
        LoadZoidData();
        LoadCharacterData();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Refresh character data when returning to this page (e.g., from battle)
        LoadCharacterData();
        
        // If the player has owned Zoids, default to showing them
        // Otherwise show all Zoids so they can purchase their first one
        if (_currentCharacter?.Zoids?.Any() == true)
        {
            FilterPicker.SelectedIndex = 1; // Switch to "Owned Zoids" filter
        }
        else
        {
            FilterPicker.SelectedIndex = 0; // Show "All Zoids" so they can purchase
        }
    }

    private async void LoadZoidData()
    {
        try
        {
            _allZoids = await _zoidDataService.LoadZoidDataAsync();
            FilterZoids();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load Zoid data: {ex.Message}", "OK");
        }
    }

    private async void LoadCharacterData()
    {
        try
        {
            // Check if this is a new game request
            bool isNewGameRequest = !string.IsNullOrEmpty(IsNewGame) && IsNewGame.ToLower() == "true";
            
            if (isNewGameRequest)
            {
                // Create new character with provided name
                string newCharacterName = Uri.UnescapeDataString(CharacterName ?? "Player");
                if (string.IsNullOrWhiteSpace(newCharacterName))
                {
                    newCharacterName = "Player";
                }
                
                // Delete any existing save file first to ensure a completely fresh start
                try
                {
                    await _saveSystem.DeleteSaveAsync("current_save");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Note: Could not delete existing save (this is normal for first time): {ex.Message}");
                }
                
                _currentCharacter = new CharacterData
                {
                    Name = newCharacterName,
                    Credits = 40000,
                    Zoids = new List<Zoid>()
                };
                
                // Save the new character immediately
                await _saveSystem.SaveCharacterAsync(_currentCharacter, "current_save");
                
                // Clear the query parameters so they don't trigger again
                IsNewGame = string.Empty;
                CharacterName = string.Empty;
                
                await DisplayAlert("Welcome!", 
                    $"Welcome to Zoids Battle, {_currentCharacter.Name}! You start with {_currentCharacter.Credits:N0} credits. Use them wisely to purchase your first Zoid!", 
                    "OK");
            }
            else
            {
                // Try to load existing character data
                _currentCharacter = await _saveSystem.LoadCharacterAsync("current_save");
                
                // If no save exists, create a default character
                if (_currentCharacter == null)
                {
                    _currentCharacter = new CharacterData
                    {
                        Name = "Player",
                        Credits = 40000,
                        Zoids = new List<Zoid>()
                    };
                    
                    // Save the default character
                    await _saveSystem.SaveCharacterAsync(_currentCharacter, "current_save");
                }
            }
            
            UpdateCreditsDisplay();
            
            // Debug: Show owned Zoids count
            System.Diagnostics.Debug.WriteLine($"Loaded character: {_currentCharacter.Name}, Credits: {_currentCharacter.Credits}, Owned Zoids: {_currentCharacter.Zoids.Count}");
            if (_currentCharacter.Zoids.Any())
            {
                foreach (var zoid in _currentCharacter.Zoids)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Owned Zoid: {zoid.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback to default character if loading fails
            _currentCharacter = new CharacterData
            {
                Name = "Player",
                Credits = 40000,
                Zoids = new List<Zoid>()
            };
            UpdateCreditsDisplay();
            await DisplayAlert("Warning", $"Could not load save data, using defaults: {ex.Message}", "OK");
        }
    }

    private void FilterZoids()
    {
        var filtered = FilterPicker.SelectedIndex switch
        {
            1 => FilterOwnedZoids(),
            2 => _allZoids.Where(z => z.PowerLevel >= 1 && z.PowerLevel <= 5).ToList(),
            3 => _allZoids.Where(z => z.PowerLevel >= 6 && z.PowerLevel <= 10).ToList(),
            4 => _allZoids.Where(z => z.PowerLevel >= 11 && z.PowerLevel <= 15).ToList(),
            5 => _allZoids.Where(z => z.PowerLevel >= 16).ToList(),
            6 => _allZoids.Where(z => _currentCharacter != null && z.Cost <= _currentCharacter.Credits).ToList(),
            _ => _allZoids.ToList()
        };

        // Sort the filtered list
        if (_sortByName)
        {
            filtered = filtered.OrderBy(z => z.Name).ToList();
        }
        else
        {
            filtered = filtered.OrderBy(z => z.Cost).ToList();
        }

        _filteredZoids.Clear();
        foreach (var zoid in filtered)
        {
            _filteredZoids.Add(zoid);
        }
        
        System.Diagnostics.Debug.WriteLine($"Filter applied. Selected Index: {FilterPicker.SelectedIndex}, Filtered count: {filtered.Count}");
    }

    private List<ZoidData> FilterOwnedZoids()
    {
        if (_currentCharacter?.Zoids == null || !_currentCharacter.Zoids.Any())
        {
            System.Diagnostics.Debug.WriteLine("No owned Zoids found - player needs to purchase their first Zoid");
            
            // Show a helpful message for new players
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_currentCharacter?.Credits >= 1000) // Only show if they have credits to spend
                {
                    await DisplayAlert("Welcome to Zoids Battle!", 
                        "You don't own any Zoids yet!\n\n" +
                        $"💰 You have {_currentCharacter.Credits:N0} credits to spend.\n" +
                        "🛒 Purchase your first Zoid to begin battling!\n\n" +
                        "💡 Tip: Use the 'Affordable' filter to see what you can buy.", 
                        "Got it!");
                }
            });
            
            return new List<ZoidData>();
        }

        System.Diagnostics.Debug.WriteLine($"Filtering owned Zoids. Character has {_currentCharacter.Zoids.Count} Zoids:");
        foreach (var ownedZoid in _currentCharacter.Zoids)
        {
            System.Diagnostics.Debug.WriteLine($"  - Owned: {ownedZoid.Name}");
        }

        var result = _currentCharacter.Zoids
            .Where(ownedZoid => _allZoids.Any(z => z.Name == ownedZoid.Name))
            .Select(ownedZoid => _allZoids.First(z => z.Name == ownedZoid.Name))
            .ToList();

        System.Diagnostics.Debug.WriteLine($"Owned Zoids filter result: {result.Count} Zoids");
        foreach (var zoid in result)
        {
            System.Diagnostics.Debug.WriteLine($"  - Available for selection: {zoid.Name}");
        }

        return result;
    }

    private void UpdateCreditsDisplay()
    {
        if (_currentCharacter != null)
        {
            CreditsLabel.Text = $"Credits: {_currentCharacter.Credits:N0}";
        }
    }

    private void OnFilterChanged(object sender, EventArgs e)
    {
        FilterZoids();
    }

    private void OnSortClicked(object sender, EventArgs e)
    {
        _sortByName = !_sortByName;
        SortButton.Text = _sortByName ? "Sort by Cost" : "Sort by Name";
        FilterZoids();
    }

    private void OnZoidSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ZoidData selectedZoid)
        {
            _selectedZoid = selectedZoid;
            ShowZoidDetails(selectedZoid);
            EnableActionButtons(true);
        }
        else
        {
            _selectedZoid = null;
            HideZoidDetails();
            EnableActionButtons(false);
        }
    }

    private void ShowZoidDetails(ZoidData zoid)
    {
        SelectedZoidFrame.IsVisible = true;
        
        // Check if this Zoid is owned
        bool isOwned = _currentCharacter?.Zoids?.Any(z => z.Name == zoid.Name) ?? false;
        string ownershipStatus = isOwned ? " ✓ OWNED" : " - Not Owned";
        
        SelectedZoidName.Text = $"{zoid.Name} (Power Level {zoid.PowerLevel}){ownershipStatus}";
        
        SelectedZoidDefenses.Text = $"Toughness: {zoid.Defenses.Toughness} | " +
                                   $"Parry: {zoid.Defenses.Parry} | " +
                                   $"Dodge: {zoid.Defenses.Dodge}";

        // Show key powers
        var keyPowers = zoid.Powers.Where(p => 
            p.Type.Contains("Range") || 
            p.Type == "Melee" || 
            p.Type == "E-Shield" || 
            p.Type == "Concealment" ||
            p.Type == "Protection").ToList();

        if (keyPowers.Any())
        {
            var powerTexts = keyPowers.Select(p => $"{p.Type}: {p.Rank}").ToList();
            SelectedZoidPowers.Text = string.Join(" | ", powerTexts);
        }
        else
        {
            SelectedZoidPowers.Text = "No special powers";
        }
    }

    private void HideZoidDetails()
    {
        SelectedZoidFrame.IsVisible = false;
    }

    private void EnableActionButtons(bool enabled)
    {
        // Check if the selected Zoid is owned for battle selection
        bool isZoidOwned = _currentCharacter?.Zoids?.Any(z => z.Name == _selectedZoid?.Name) ?? false;
        
        bool canSelectForBattle = enabled && isZoidOwned;
        bool canPurchase = enabled && _selectedZoid != null && 
                          _currentCharacter != null && 
                          _selectedZoid.Cost <= _currentCharacter.Credits;
        
        SelectForBattleButton.IsEnabled = canSelectForBattle;
        PurchaseButton.IsEnabled = canPurchase;
        
        // Update button colors based on enabled state
        SelectForBattleButton.BackgroundColor = canSelectForBattle 
            ? Color.FromArgb("#708090") // Steel color when enabled
            : Color.FromArgb("#A9A9A9");  // Dark gray when disabled
            
        PurchaseButton.BackgroundColor = canPurchase 
            ? Color.FromArgb("#708090") // Steel color when enabled
            : Color.FromArgb("#A9A9A9");  // Dark gray when disabled
    }

    private async void OnSelectForBattleClicked(object sender, EventArgs e)
    {
        if (_selectedZoid == null)
        {
            await DisplayAlert("Error", "Please select a Zoid first.", "OK");
            return;
        }

        // Check if the Zoid is owned
        bool isZoidOwned = _currentCharacter?.Zoids?.Any(z => z.Name == _selectedZoid.Name) ?? false;
        if (!isZoidOwned)
        {
            await DisplayAlert("Error", "You must own this Zoid to use it in battle. Purchase it first!", "OK");
            return;
        }

        try
        {
            // Create player Zoid instance
            var playerZoid = new Zoid(_selectedZoid);
            
            // Show battle confirmation
            var result = await DisplayAlert("Start Battle", 
                $"Begin battle with {playerZoid.Name}?\n\n" +
                $"Power Level: {_selectedZoid.PowerLevel}\n" +
                $"Stats: Fighting {playerZoid.Fighting}, Strength {playerZoid.Strength}, " +
                $"Dexterity {playerZoid.Dexterity}, Agility {playerZoid.Agility}, Awareness {playerZoid.Awareness}", 
                "Start Battle", "Cancel");

            if (result)
            {
                // Navigate to battle page with selected Zoid data
                await Shell.Current.GoToAsync($"battle?zoidName={Uri.EscapeDataString(_selectedZoid.Name)}&powerLevel={_selectedZoid.PowerLevel}");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to start battle: {ex.Message}", "OK");
        }
    }

    private async void OnPurchaseClicked(object sender, EventArgs e)
    {
        if (_selectedZoid == null || _currentCharacter == null)
        {
            await DisplayAlert("Error", "Please select a Zoid first.", "OK");
            return;
        }

        if (_selectedZoid.Cost > _currentCharacter.Credits)
        {
            await DisplayAlert("Insufficient Credits", 
                $"You need {_selectedZoid.Cost:N0} credits but only have {_currentCharacter.Credits:N0}.", 
                "OK");
            return;
        }

        var result = await DisplayAlert("Purchase Confirmation", 
            $"Purchase {_selectedZoid.Name} for {_selectedZoid.Cost:N0} credits?", 
            "Yes", "No");

        if (result)
        {
            try
            {
                // Deduct credits and add Zoid to character
                _currentCharacter.Credits -= (int)_selectedZoid.Cost;
                var newZoid = new Zoid(_selectedZoid);
                _currentCharacter.Zoids.Add(newZoid);

                UpdateCreditsDisplay();
                EnableActionButtons(_selectedZoid != null); // Refresh button states
                
                // Update the selected Zoid details to show it's now owned
                if (_selectedZoid != null)
                {
                    ShowZoidDetails(_selectedZoid);
                    
                    var showOwned = await DisplayAlert("Purchase Successful",
                        $"You have purchased {_selectedZoid.Name}!\n" +
                        $"Remaining credits: {_currentCharacter.Credits:N0}\n\n" +
                        $"Would you like to view your owned Zoids?",
                        "Yes", "No");
                    
                    if (showOwned)
                    {
                        FilterPicker.SelectedIndex = 1; // Switch to "Owned Zoids" filter
                        FilterZoids();
                    }
                }

                // Save the character data
                await _saveSystem.SaveCharacterAsync(_currentCharacter, "current_save");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to purchase Zoid: {ex.Message}", "OK");
            }
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnSaveGameClicked(object sender, EventArgs e)
    {
        // Navigate to the save/load page in save mode
        await Shell.Current.GoToAsync("saveload?mode=save");
    }

    private async void OnUpgradeZoidsClicked(object sender, EventArgs e)
    {
        if (_currentCharacter?.Zoids == null || !_currentCharacter.Zoids.Any())
        {
            await DisplayAlert("No Zoids to Upgrade", 
                "You need to own at least one Zoid to access the upgrade system.", 
                "OK");
            return;
        }

        await Shell.Current.GoToAsync("zoidupgrade");
    }
}
