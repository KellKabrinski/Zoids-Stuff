using ZoidsGameMAUI.Models;
using ZoidsGameMAUI.Services;
using ZoidsGameMAUI.ViewModels;

namespace ZoidsGameMAUI.Views;

public partial class TestPage : ContentPage
{
    private readonly ZoidDataService _zoidDataService;
    private readonly GameEngine _gameEngine;
    private readonly BattleService _battleService;

    public TestPage(ZoidDataService zoidDataService, GameEngine gameEngine, BattleService battleService)
    {
        InitializeComponent();
        _zoidDataService = zoidDataService;
        _gameEngine = gameEngine;
        _battleService = battleService;
    }

    private async void OnTestDataLoadingClicked(object sender, EventArgs e)
    {
        try
        {
            DataLoadingResult.Text = "Testing...";
            
            var zoidDataList = await _zoidDataService.LoadZoidDataAsync();
            
            if (zoidDataList != null && zoidDataList.Count > 0)
            {
                var firstZoidData = zoidDataList.First();
                DataLoadingResult.Text = $"✅ SUCCESS: Loaded {zoidDataList.Count} ZoidData entries\n" +
                                       $"Sample: {firstZoidData.Name} (Cost: {firstZoidData.Cost})";
                DataLoadingResult.TextColor = Colors.Green;
            }
            else
            {
                DataLoadingResult.Text = "❌ FAILED: No Zoids loaded";
                DataLoadingResult.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            DataLoadingResult.Text = $"❌ ERROR: {ex.Message}";
            DataLoadingResult.TextColor = Colors.Red;
        }
    }

    private async void OnTestZoidCreationClicked(object sender, EventArgs e)
    {
        try
        {
            ZoidCreationResult.Text = "Testing...";
            
            var zoidDataList = await _zoidDataService.LoadZoidDataAsync();
            if (zoidDataList?.Count > 0)
            {
                var firstZoidData = zoidDataList.First();
                
                // Create a Zoid from ZoidData
                var testZoid = new Zoid(firstZoidData);
                
                // Test basic properties
                bool hasName = !string.IsNullOrEmpty(testZoid.Name);
                bool hasStats = testZoid.Strength > 0 || testZoid.Fighting > 0 || testZoid.Armor > 0;
                bool hasCost = testZoid.Cost > 0;
                
                if (hasName && hasStats && hasCost)
                {
                    ZoidCreationResult.Text = $"✅ SUCCESS: Zoid creation working\n" +
                                            $"Name: {testZoid.Name}\n" +
                                            $"Stats: STR:{testZoid.Strength} FGT:{testZoid.Fighting} ARM:{testZoid.Armor}\n" +
                                            $"Cost: {testZoid.Cost:C0}";
                    ZoidCreationResult.TextColor = Colors.Green;
                }
                else
                {
                    ZoidCreationResult.Text = "❌ FAILED: Incomplete Zoid data";
                    ZoidCreationResult.TextColor = Colors.Red;
                }
            }
            else
            {
                ZoidCreationResult.Text = "❌ FAILED: No ZoidData available";
                ZoidCreationResult.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            ZoidCreationResult.Text = $"❌ ERROR: {ex.Message}";
            ZoidCreationResult.TextColor = Colors.Red;
        }
    }

    private async void OnTestBattleSystemClicked(object sender, EventArgs e)
    {
        try
        {
            BattleSystemResult.Text = "Testing...";
            
            var zoidDataList = await _zoidDataService.LoadZoidDataAsync();
            if (zoidDataList?.Count >= 2)
            {
                var playerZoid = new Zoid(zoidDataList.First());
                var enemyZoid = new Zoid(zoidDataList.Skip(1).First());
                
                // Test battle processing
                var result = _gameEngine.ProcessAttack(playerZoid, enemyZoid, Ranges.Close, 50, 0);
                
                bool attackProcessed = result != null;
                bool hasMessage = !string.IsNullOrEmpty(result?.Message);

                if (attackProcessed && hasMessage && result != null)
                {
                    BattleSystemResult.Text = $"✅ SUCCESS: Battle system working\n" +
                                            $"Player: {playerZoid.Name}\n" +
                                            $"Enemy: {enemyZoid.Name}\n" +
                                            $"Attack Result: {result.Message}\n" +
                                            $"Hit: {result.Success}, Damage: {result.Damage}";
                    BattleSystemResult.TextColor = Colors.Green;
                }
                else
                {
                    BattleSystemResult.Text = "❌ FAILED: Battle system not responding";
                    BattleSystemResult.TextColor = Colors.Red;
                }
            }
            else
            {
                BattleSystemResult.Text = "❌ FAILED: Not enough Zoids for battle test";
                BattleSystemResult.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            BattleSystemResult.Text = $"❌ ERROR: {ex.Message}";
            BattleSystemResult.TextColor = Colors.Red;
        }
    }

    private void OnTestSaveSystemClicked(object sender, EventArgs e)
    {
        try
        {
            SaveSystemResult.Text = "Testing...";
            
            // Test preferences save/load
            string testKey = "test_save";
            string testValue = "test_data_" + DateTime.Now.Ticks;
            
            Preferences.Set(testKey, testValue);
            string retrieved = Preferences.Get(testKey, "");
            
            bool saveWorks = retrieved == testValue;
            
            // Clean up test data
            Preferences.Remove(testKey);
            
            if (saveWorks)
            {
                SaveSystemResult.Text = "✅ SUCCESS: Save system working\n" +
                                      "Preferences can save and load data";
                SaveSystemResult.TextColor = Colors.Green;
            }
            else
            {
                SaveSystemResult.Text = "❌ FAILED: Save system not working";
                SaveSystemResult.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            SaveSystemResult.Text = $"❌ ERROR: {ex.Message}";
            SaveSystemResult.TextColor = Colors.Red;
        }
    }

    private void OnTestNavigationClicked(object sender, EventArgs e)
    {
        try
        {
            NavigationResult.Text = "Testing...";
            
            // Test if navigation service exists
            bool canNavigate = Navigation != null;
            bool shellExists = Shell.Current != null;
            
            if (canNavigate && shellExists)
            {
                NavigationResult.Text = "✅ SUCCESS: Navigation system ready\n" +
                                      "Shell navigation available";
                NavigationResult.TextColor = Colors.Green;
            }
            else
            {
                NavigationResult.Text = "❌ FAILED: Navigation not available";
                NavigationResult.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            NavigationResult.Text = $"❌ ERROR: {ex.Message}";
            NavigationResult.TextColor = Colors.Red;
        }
    }

    private async void OnRunAllTestsClicked(object sender, EventArgs e)
    {
        TestSummary.Text = "Running all tests...";
        
        await Task.Delay(500); // Brief pause for UI update
        
        OnTestDataLoadingClicked(sender, e);
        await Task.Delay(1000);
        
        OnTestZoidCreationClicked(sender, e);
        await Task.Delay(1000);
        
        OnTestBattleSystemClicked(sender, e);
        await Task.Delay(1000);
        
        OnTestSaveSystemClicked(sender, e);
        await Task.Delay(1000);
        
        OnTestNavigationClicked(sender, e);
        await Task.Delay(1000);
        
        // Count passed tests
        var results = new[]
        {
            DataLoadingResult.Text.StartsWith("✅"),
            ZoidCreationResult.Text.StartsWith("✅"),
            BattleSystemResult.Text.StartsWith("✅"),
            SaveSystemResult.Text.StartsWith("✅"),
            NavigationResult.Text.StartsWith("✅")
        };
        
        int passed = results.Count(r => r);
        int total = results.Length;
        
        if (passed == total)
        {
            TestSummary.Text = $"🎉 ALL TESTS PASSED! ({passed}/{total})\n" +
                             "Core functionality is working properly.";
            TestSummary.TextColor = Colors.LightGreen;
        }
        else
        {
            TestSummary.Text = $"⚠️ {passed}/{total} tests passed.\n" +
                             "Some functionality needs attention.";
            TestSummary.TextColor = Colors.Orange;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
