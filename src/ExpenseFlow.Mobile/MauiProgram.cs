using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using ExpenseFlow.Mobile.Services;
using Microsoft.Extensions.Configuration;
using Plugin.LocalNotification.Maui;

namespace ExpenseFlow.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		var assembly = typeof(MauiProgram).Assembly;
		using var stream = assembly.GetManifestResourceStream("ExpenseFlow.Mobile.appsettings.json");
		if (stream is not null)
		{
			var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
			builder.Configuration.AddConfiguration(config);
		}

		builder
			.UseMauiApp<App>()
			.UseLocalNotification()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddLocalNotification();

		var apiBaseUrl = builder.Configuration["ExpenseFlow:ApiBaseUrl"] ?? "http://localhost:5000/";
		builder.Services.AddHttpClient<ExpenseFlowApiClient>(client =>
		{
			client.BaseAddress = new Uri(apiBaseUrl);
			client.Timeout = TimeSpan.FromSeconds(10);
		});

		builder.Services.AddSingleton<InboxUploaderService>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<HistoryPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

internal static class LocalNotificationServiceCollectionExtensions
{
	public static IServiceCollection AddLocalNotification(this IServiceCollection services)
	{
		services.AddSingleton(_ => LocalNotificationCenter.Current);
		return services;
	}
}
