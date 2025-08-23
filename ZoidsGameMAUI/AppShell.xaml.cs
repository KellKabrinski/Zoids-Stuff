namespace ZoidsGameMAUI;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		// Register routes for navigation
		Routing.RegisterRoute("zoidselection", typeof(Views.ZoidSelectionPage));
		Routing.RegisterRoute("battle", typeof(Views.BattlePage));
		Routing.RegisterRoute("test", typeof(Views.TestPage));
		Routing.RegisterRoute("saveload", typeof(Views.SaveLoadPage));
	}
}
