using ZoidsGameMAUI.Services;
using ZoidsGameMAUI.Models;

namespace ZoidsGameMAUI.Views;

public partial class ZoidUpgradePage : ContentPage
{
    private readonly UpgradeService _upgradeService;
    private readonly SaveSystem _saveSystem;
    private CharacterData? _currentCharacter;
    private Zoid? _selectedZoid;

    public ZoidUpgradePage(UpgradeService upgradeService, SaveSystem saveSystem)
    {
        InitializeComponent();
        _upgradeService = upgradeService;
        _saveSystem = saveSystem;
        
        LoadCharacterData();
        SetupPowerTypePicker();
        
        // Setup event handlers
        PowerTypePicker.SelectedIndexChanged += OnPowerTypeChanged;
        RankEntry.TextChanged += OnRankTextChanged;
    }

    private int GetRankFromEntry()
    {
        try
        {
            if (RankEntry != null && !string.IsNullOrWhiteSpace(RankEntry.Text))
            {
                if (int.TryParse(RankEntry.Text, out int rank) && rank >= 1 && rank <= 20)
                {
                    return rank;
                }
            }
        }
        catch
        {
            // Handle any parsing errors gracefully
        }
        return 0; // Default to rank 0
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadCharacterData();
    }

    private async void LoadCharacterData()
    {
        try
        {
            _currentCharacter = await _saveSystem.LoadCharacterAsync("current_save");
            
            if (_currentCharacter == null)
            {
                await DisplayAlert("Error", "No character data found. Please start a new game first.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            UpdateCreditsDisplay();
            PopulateZoidPicker();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load character data: {ex.Message}", "OK");
        }
    }

    private void UpdateCreditsDisplay()
    {
        if (_currentCharacter != null)
        {
            CreditsLabel.Text = $"Credits: {_currentCharacter.Credits:N0}";
        }
    }

    private void PopulateZoidPicker()
    {
        ZoidPicker.Items.Clear();
        
        if (_currentCharacter?.Zoids != null)
        {
            foreach (var zoid in _currentCharacter.Zoids)
            {
                ZoidPicker.Items.Add(zoid.Name);
            }
        }
    }

    private void SetupPowerTypePicker()
    {
        PowerTypePicker.Items.Clear();
        var powerTypes = _upgradeService.GetAvailablePowerTypes();
        foreach (var powerType in powerTypes)
        {
            PowerTypePicker.Items.Add(powerType);
        }
        
        PowerTypePicker.SelectedIndexChanged += OnPowerTypeChanged;
        RankEntry.TextChanged += OnRankTextChanged;
    }


    private void OnZoidSelected(object sender, EventArgs e)
    {
        if (_currentCharacter?.Zoids == null) return;
        
        var picker = sender as Picker;
        if (picker?.SelectedIndex == -1 || picker?.SelectedIndex == null) return;

        _selectedZoid = _currentCharacter.Zoids[picker.SelectedIndex];
        
        if (_selectedZoid != null)
        {
            UpdateZoidInfo();
            ShowUpgradeOptions();
        }
    }

    private void UpdateZoidInfo()
    {
        if (_selectedZoid == null) return;

        ZoidInfoFrame.IsVisible = true;
        ZoidNameLabel.Text = _selectedZoid.Name;
        
        var currentPowerLevel = _upgradeService.CalculateCurrentPowerLevel(_selectedZoid);
        var powerLevelCap = _upgradeService.GetPowerLevelCap(_selectedZoid.PowerLevel);
        
        PowerLevelLabel.Text = $"Power Level: {currentPowerLevel}";
        PowerLevelCapLabel.Text = $"Cap: {powerLevelCap}";
    }

    private void ShowUpgradeOptions()
    {
        if (_selectedZoid == null) return;

        StatsUpgradeFrame.IsVisible = true;
        PowersFrame.IsVisible = true;
        CurrentPowersFrame.IsVisible = true;
        
        CreateStatsUpgradeControls();
        UpdateCurrentPowersDisplay();
    }

    private void CreateStatsUpgradeControls()
    {
        if (_selectedZoid == null) return;

        // Clear existing dynamic content (keep headers)
        var childrenToRemove = StatsGrid.Children.Where(child => StatsGrid.GetRow(child) > 0).ToList();
        foreach (var child in childrenToRemove)
        {
            StatsGrid.Children.Remove(child);
        }

        var stats = new[]
        {
            ("Fighting", _selectedZoid.Fighting),
            ("Strength", _selectedZoid.Strength),
            ("Dexterity", _selectedZoid.Dexterity),
            ("Agility", _selectedZoid.Agility),
            ("Awareness", _selectedZoid.Awareness),
            ("Toughness", _selectedZoid.Toughness),
            ("Parry", _selectedZoid.Parry),
            ("Dodge", _selectedZoid.Dodge)
        };

        for (int i = 0; i < stats.Length; i++)
        {
            var (statName, statValue) = stats[i];
            var rowIndex = i + 1; // +1 because row 0 has headers
            
            // Add row definition
            StatsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Stat name
            var nameLabel = new Label
            {
                Text = statName,
                TextColor = Colors.White,
                VerticalOptions = LayoutOptions.Center
            };
            StatsGrid.SetColumn(nameLabel, 0);
            StatsGrid.SetRow(nameLabel, rowIndex);
            StatsGrid.Children.Add(nameLabel);

            // Current value
            var valueLabel = new Label
            {
                Text = statValue.ToString(),
                TextColor = Colors.White,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            };
            StatsGrid.SetColumn(valueLabel, 1);
            StatsGrid.SetRow(valueLabel, rowIndex);
            StatsGrid.Children.Add(valueLabel);

            // Upgrade cost
            var upgradeCost = _upgradeService.CalculateStatUpgradeCost(statValue, statValue + 1);
            var costLabel = new Label
            {
                Text = upgradeCost.ToString("N0"),
                TextColor = Colors.LightBlue,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            };
            StatsGrid.SetColumn(costLabel, 2);
            StatsGrid.SetRow(costLabel, rowIndex);
            StatsGrid.Children.Add(costLabel);

            // Upgrade button
            var upgradeButton = new Button
            {
                Text = "Upgrade",
                BackgroundColor = Color.FromArgb("#4CAF50"),
                TextColor = Colors.White,
                FontSize = 12,
                IsEnabled = _currentCharacter?.Credits >= upgradeCost
            };
            
            upgradeButton.Clicked += (s, e) => OnStatUpgradeClicked(statName, statValue + 1);
            
            StatsGrid.SetColumn(upgradeButton, 3);
            StatsGrid.SetRow(upgradeButton, rowIndex);
            StatsGrid.Children.Add(upgradeButton);
        }
    }

    private async void OnStatUpgradeClicked(string statName, int newValue)
    {
        if (_selectedZoid == null || _currentCharacter == null) return;

        try
        {
            var success = await _upgradeService.UpgradeZoidStatAsync(_currentCharacter, _selectedZoid.Name, statName, newValue);
            
            if (success)
            {
                UpdateCreditsDisplay();
                UpdateZoidInfo();
                CreateStatsUpgradeControls(); // Refresh the stats display
                
                await DisplayAlert("Success", $"{statName} upgraded successfully!", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Cannot upgrade: would exceed power level cap or insufficient funds.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to upgrade stat: {ex.Message}", "OK");
        }
    }

    private void OnPowerTypeChanged(object? sender, EventArgs e)
    {
        UpdatePowerCostDisplay();
    }

    private void OnRankTextChanged(object? sender, EventArgs e)
    {
        UpdatePowerCostDisplay();
    }


    private void UpdatePowerCostDisplay()
    {
        if (_selectedZoid == null || PowerTypePicker.SelectedIndex == -1)
        {
            PowerCostLabel.Text = $"0 credits";
            UpgradePowerButton.IsEnabled = false;
            return;
        }

        var powerType = PowerTypePicker.SelectedItem?.ToString();
        int targetRank = GetRankFromEntry();
        
        if (string.IsNullOrEmpty(powerType) || targetRank <= 0) return;

        var existingPower = _selectedZoid.Powers.FirstOrDefault(p => p.Type == powerType);
        var cost = _upgradeService.CalculatePowerUpgradeCost(existingPower?.Rank, targetRank);
        
        PowerCostLabel.Text = $"{cost:N0} credits";
        UpgradePowerButton.IsEnabled = _currentCharacter?.Credits >= cost;
    }

    private void OnPowerSelected(object sender, EventArgs e)
    {
        // This method is kept for XAML binding compatibility
        UpdatePowerCostDisplay();
    }

    private async void OnUpgradePowerClicked(object sender, EventArgs e)
    {
        if (_selectedZoid == null || _currentCharacter == null || 
            PowerTypePicker.SelectedIndex == -1) // || PowerRankPicker.SelectedIndex == -1) 
            return;

        var powerType = PowerTypePicker.SelectedItem?.ToString();
        int targetRank = GetRankFromEntry(); // PowerRankPicker.SelectedIndex + 1; // Index is 0-based, ranks are 1-based
        
        if (string.IsNullOrEmpty(powerType) || targetRank <= 0) return;
        
        try
        {
            var success = await _upgradeService.UpgradeZoidPowerAsync(_currentCharacter, _selectedZoid.Name, powerType, targetRank);
            
            if (success)
            {
                UpdateCreditsDisplay();
                UpdateZoidInfo();
                UpdateCurrentPowersDisplay();
                UpdatePowerCostDisplay(); // Update cost display
                
                await DisplayAlert("Success", $"{powerType} upgraded to rank {targetRank}!", "OK");
            }
            else
            {
                await DisplayAlert("Error", "Cannot upgrade power: would exceed power level cap or insufficient funds.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to upgrade power: {ex.Message}", "OK");
        }
    }

    private void UpdateCurrentPowersDisplay()
    {
        if (_selectedZoid?.Powers == null) return;

        var powerDisplayList = _selectedZoid.Powers.Select(p => new
        {
            Name = $"{p.Type} (Rank {p.Rank ?? 0})",
            Cost = p.Rank ?? 0
        }).ToList();

        CurrentPowersCollection.ItemsSource = powerDisplayList;
    }
}
