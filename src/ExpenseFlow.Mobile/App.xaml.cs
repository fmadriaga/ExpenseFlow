using Microsoft.Extensions.DependencyInjection;

namespace ExpenseFlow.Mobile;

public partial class App : Application
{
    private readonly TabbedPage _tabbedPage;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        var tabbedPage = new TabbedPage();
        tabbedPage.Children.Add(
            new NavigationPage(serviceProvider.GetRequiredService<MainPage>())
            {
                Title = "Capturar",
                IconImageSource = string.Empty,
            });
        tabbedPage.Children.Add(
            new NavigationPage(serviceProvider.GetRequiredService<HistoryPage>())
            {
                Title = "Historial",
                IconImageSource = string.Empty,
            });

        MainPage = tabbedPage;
        _tabbedPage = tabbedPage;
    }

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(_tabbedPage);
	}
}