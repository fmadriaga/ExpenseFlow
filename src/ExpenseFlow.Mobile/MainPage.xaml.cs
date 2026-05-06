using ExpenseFlow.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseFlow.Mobile;

public partial class MainPage : ContentPage
{
    private readonly InboxUploaderService _uploader;

    public MainPage(InboxUploaderService uploader)
    {
        InitializeComponent();
        _uploader = uploader;
    }

    private async void OnCaptureClicked(object? sender, EventArgs e)
    {
        var permissionStatus = await CheckAndRequestCameraPermissionAsync();
        if (permissionStatus != PermissionStatus.Granted)
        {
            StatusLabel.Text = "Permiso de cámara denegado";
            StatusLabel.TextColor = Colors.Red;
            return;
        }

        SetBusy(true);
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is null)
            {
                return;
            }

            await using var stream = await photo.OpenReadAsync();
            var path = await _uploader.UploadAsync(stream);

            StatusLabel.Text = $"✓ Ticket guardado: {Path.GetFileName(path)}";
            StatusLabel.TextColor = Colors.Green;
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error al guardar ticket: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static async Task<PermissionStatus> CheckAndRequestCameraPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status == PermissionStatus.Granted)
        {
            return status;
        }

        return await Permissions.RequestAsync<Permissions.Camera>();
    }

    private void SetBusy(bool busy)
    {
        BusyIndicator.IsVisible = busy;
        BusyIndicator.IsRunning = busy;
        CaptureButton.IsEnabled = !busy;
    }

    private async void OnHistoryClicked(object? sender, EventArgs e)
    {
        var services = Handler?.MauiContext?.Services;
        var historyPage = services?.GetService<HistoryPage>();
        if (historyPage is not null)
        {
            await Navigation.PushAsync(historyPage);
        }
    }
}
