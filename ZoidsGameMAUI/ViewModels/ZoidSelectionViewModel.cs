using System.Collections.ObjectModel;
using System.ComponentModel;
using ZoidsGameMAUI.Models;
using ZoidsGameMAUI.Services;

namespace ZoidsGameMAUI.ViewModels
{
    public class ZoidSelectionViewModel : INotifyPropertyChanged
    {
        private readonly ZoidDataService _zoidDataService;
        private readonly SaveSystem _saveSystem;
        
        public ObservableCollection<ZoidData> FilteredZoids { get; } = new();
        public ObservableCollection<string> FilterOptions { get; } = new()
        {
            "All Zoids",
            "Power 1-5", 
            "Power 6-10",
            "Power 11-15",
            "Power 16+",
            "Affordable"
        };

        private List<ZoidData> _allZoids = new();
        private ZoidData? _selectedZoid;
        private CharacterData? _currentCharacter;
        private bool _sortByName = true;
        private int _selectedFilterIndex = 0;
        private string _creditsText = "Credits: 0";
        private bool _isSelectedZoidDetailsVisible = false;

        public static async Task<ZoidSelectionViewModel> CreateAsync(ZoidDataService zoidDataService, SaveSystem saveSystem)
        {
            var viewModel = new ZoidSelectionViewModel(zoidDataService, saveSystem);
            await viewModel.LoadZoidDataAsync();
            return viewModel;
        }

        private ZoidSelectionViewModel(ZoidDataService zoidDataService, SaveSystem saveSystem)
        {
            _zoidDataService = zoidDataService;
            _saveSystem = saveSystem;
            
            // Initialize with default character
            _currentCharacter = new CharacterData
            {
                Name = "Player",
                Credits = 40000,
                Zoids = new List<Zoid>()
            };
            
            UpdateCreditsText();
        }

        public ZoidData? SelectedZoid
        {
            get => _selectedZoid;
            set
            {
                if (_selectedZoid != value)
                {
                    _selectedZoid = value;
                    IsSelectedZoidDetailsVisible = value != null;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanSelectForBattle));
                    OnPropertyChanged(nameof(CanPurchase));
                }
            }
        }

        public bool CanSelectForBattle => SelectedZoid != null;
        
        public bool CanPurchase => SelectedZoid != null && 
                                   _currentCharacter != null && 
                                   SelectedZoid.Cost <= _currentCharacter.Credits;

        public int SelectedFilterIndex
        {
            get => _selectedFilterIndex;
            set
            {
                if (_selectedFilterIndex != value)
                {
                    _selectedFilterIndex = value;
                    FilterZoids();
                    OnPropertyChanged();
                }
            }
        }

        public string CreditsText
        {
            get => _creditsText;
            private set
            {
                if (_creditsText != value)
                {
                    _creditsText = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelectedZoidDetailsVisible
        {
            get => _isSelectedZoidDetailsVisible;
            private set
            {
                if (_isSelectedZoidDetailsVisible != value)
                {
                    _isSelectedZoidDetailsVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SortButtonText => _sortByName ? "Sort by Cost" : "Sort by Name";

        public async Task LoadZoidDataAsync()
        {
            try
            {
                _allZoids = await _zoidDataService.LoadZoidDataAsync();
                FilterZoids();
            }
            catch (Exception ex)
            {
                // Handle error - in a real app you might use a messaging service
                System.Diagnostics.Debug.WriteLine($"Error loading Zoid data: {ex.Message}");
            }
        }

        public void ToggleSort()
        {
            _sortByName = !_sortByName;
            FilterZoids();
            OnPropertyChanged(nameof(SortButtonText));
        }

        public Task<bool> PurchaseSelectedZoidAsync()
        {
            if (SelectedZoid == null || _currentCharacter == null || !CanPurchase)
                return Task.FromResult(false);

            try
            {
                // Deduct credits and add Zoid to character
                _currentCharacter.Credits -= (int)SelectedZoid.Cost;
                var newZoid = new Zoid(SelectedZoid);
                _currentCharacter.Zoids.Add(newZoid);

                UpdateCreditsText();
                OnPropertyChanged(nameof(CanPurchase));

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error purchasing Zoid: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        public string GetSelectedZoidDetails()
        {
            if (SelectedZoid == null) return "";

            var details = $"{SelectedZoid.Name} (Power Level {SelectedZoid.PowerLevel})\n\n";
            details += $"Toughness: {SelectedZoid.Defenses.Toughness} | ";
            details += $"Parry: {SelectedZoid.Defenses.Parry} | ";
            details += $"Dodge: {SelectedZoid.Defenses.Dodge}\n\n";

            // Show key powers
            var keyPowers = SelectedZoid.Powers.Where(p => 
                p.Type.Contains("Range") || 
                p.Type == "Melee" || 
                p.Type == "E-Shield" || 
                p.Type == "Concealment" ||
                p.Type == "Protection").ToList();

            if (keyPowers.Any())
            {
                var powerTexts = keyPowers.Select(p => $"{p.Type}: {p.Rank}");
                details += string.Join(" | ", powerTexts);
            }
            else
            {
                details += "No special powers";
            }

            return details;
        }

        private void FilterZoids()
        {
            var filtered = SelectedFilterIndex switch
            {
                1 => _allZoids.Where(z => z.PowerLevel >= 1 && z.PowerLevel <= 5).ToList(),
                2 => _allZoids.Where(z => z.PowerLevel >= 6 && z.PowerLevel <= 10).ToList(),
                3 => _allZoids.Where(z => z.PowerLevel >= 11 && z.PowerLevel <= 15).ToList(),
                4 => _allZoids.Where(z => z.PowerLevel >= 16).ToList(),
                5 => _allZoids.Where(z => _currentCharacter != null && z.Cost <= _currentCharacter.Credits).ToList(),
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

            FilteredZoids.Clear();
            foreach (var zoid in filtered)
            {
                FilteredZoids.Add(zoid);
            }
        }

        private void UpdateCreditsText()
        {
            if (_currentCharacter != null)
            {
                CreditsText = $"Credits: {_currentCharacter.Credits:N0}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
