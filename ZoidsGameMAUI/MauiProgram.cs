using Microsoft.Extensions.Logging;
using ZoidsGameMAUI.Services;
using ZoidsGameMAUI.Views;
using ZoidsGameMAUI.ViewModels;

namespace ZoidsGameMAUI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Register services
		builder.Services.AddSingleton<ZoidDataService>();
		builder.Services.AddSingleton<SaveSystem>();
		builder.Services.AddTransient<GameEngine>();
		builder.Services.AddTransient<BattleService>();
		builder.Services.AddTransient<UpgradeService>();

		// Register pages and view models
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<ZoidSelectionPage>();
		builder.Services.AddTransient<ZoidSelectionViewModel>();
		builder.Services.AddTransient<BattlePage>();
		builder.Services.AddTransient<BattleViewModel>();
		builder.Services.AddTransient<TestPage>();
		builder.Services.AddTransient<SaveLoadPage>();
		builder.Services.AddTransient<ZoidUpgradePage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
