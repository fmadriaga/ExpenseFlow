using System.Collections.ObjectModel;
using System.Windows.Input;
using ExpenseFlow.Mobile.Services;
using Plugin.LocalNotification;

namespace ExpenseFlow.Mobile;

public partial class HistoryPage : ContentPage
{
    private readonly ExpenseFlowApiClient _apiClient;
    private readonly HashSet<int> _notifiedIds = [];
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;
    private bool _isRefreshing;

    public ObservableCollection<DocumentSummary> Items { get; } = [];
    public ICommand RefreshCommand { get; }
    public bool IsEmpty => Items.Count == 0;

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (_isRefreshing == value)
            {
                return;
            }

            _isRefreshing = value;
            OnPropertyChanged();
        }
    }

    public HistoryPage(ExpenseFlowApiClient apiClient)
    {
        InitializeComponent();
        _apiClient = apiClient;
        BindingContext = this;
        RefreshCommand = new Command(async () => await LoadAsync());
        Items.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
        StartPolling();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopPolling();
    }

    private async Task LoadAsync()
    {
        IsRefreshing = true;
        try
        {
            var recent = await _apiClient.GetRecentAsync(20);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Items.Clear();
                foreach (var document in recent)
                {
                    Items.Add(document);
                }
            });

            NotifyForCompletedDocuments(recent);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private void StartPolling()
    {
        if (_pollingTask is not null && !_pollingTask.IsCompleted)
        {
            return;
        }

        _pollingCts = new CancellationTokenSource();
        var ct = _pollingCts.Token;
        _pollingTask = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    await LoadAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // No action needed.
            }
        }, ct);
    }

    private void StopPolling()
    {
        if (_pollingCts is null)
        {
            return;
        }

        _pollingCts.Cancel();
        _pollingCts.Dispose();
        _pollingCts = null;
        _pollingTask = null;
    }

    private async void OnTicketTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Frame frame || frame.BindingContext is not DocumentSummary summary)
            return;

        var services = Handler?.MauiContext?.Services;
        if (services is null) return;

        var apiClient = services.GetService<ExpenseFlowApiClient>();
        if (apiClient is null) return;

        var detailPage = new DetailPage(summary.Id, apiClient);
        await Navigation.PushAsync(detailPage);
    }

    private void NotifyForCompletedDocuments(IEnumerable<DocumentSummary> documents)
    {
#if ANDROID || IOS
        foreach (var document in documents)
        {
            if ((document.OcrStatus is "Success" or "Failed") && _notifiedIds.Add(document.Id))
            {
                var description = document.OcrStatus == "Success"
                    ? $"Ticket {document.Id} procesado correctamente."
                    : $"Ticket {document.Id} falló en el procesamiento.";

                LocalNotificationCenter.Current.SendThrow(
                    new NotificationRequest
                    {
                        NotificationId = document.Id,
                        Title = "ExpenseFlow",
                        Description = description,
                    });
            }
        }
#endif
    }
}

internal static class LocalNotificationExtensions
{
    public static void SendThrow(this INotificationService notificationService, NotificationRequest request)
    {
        notificationService.Show(request);
    }
}
