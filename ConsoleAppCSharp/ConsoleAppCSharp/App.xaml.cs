using System.Windows;
using System.IO;
using System.Text.Json;

namespace ZoidsBattle
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Check if we should run console or WPF
            if (e.Args.Contains("--console"))
            {
                RunConsoleGame();
                Shutdown();
            }
            else
            {
                // WPF mode - create and show main window
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }

        private void RunConsoleGame()
        {
            var zoids = LoadZoids("ConvertedZoidStats.json");
            var gameEngine = new ConsoleGameEngine();
            CharacterData playerData = new CharacterData();

            // Main game loop
            do
            {
                var result = gameEngine.RunBattle(zoids, playerData);
                playerData = result.PlayerData;
                
            } while (gameEngine.AskPlayAgain());

            Console.WriteLine("Updating Save...");
            playerData.SaveToFile("save1.json");
        }

        private List<ZoidData> LoadZoids(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<ZoidData>>(json)!;
        }
    }
}
