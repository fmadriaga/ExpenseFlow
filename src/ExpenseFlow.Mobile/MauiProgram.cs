using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using ExpenseFlow.Mobile.Services;

namespace ExpenseFlow.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseLocalNotification()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<InboxUploaderService>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<HistoryPage>();
		builder.Services.AddHttpClient<ExpenseFlowApiClient>((services, client) =>
		{
			var configuration = services.GetRequiredService<IConfiguration>();
			var baseUrl = configuration["ExpenseFlow:ApiBaseUrl"];
			if (!string.IsNullOrWhiteSpace(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
			{
				client.BaseAddress = uri;
			}
		});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
