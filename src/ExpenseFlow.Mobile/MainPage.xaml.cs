using ExpenseFlow.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace ExpenseFlow.Mobile;

public partial class MainPage : ContentPage
{
    private readonly InboxUploaderService _uploader;
    private readonly ImageProcessorService _imageProcessor;

    public MainPage(InboxUploaderService uploader, ImageProcessorService imageProcessor)
    {
        InitializeComponent();
        _uploader = uploader;
        _imageProcessor = imageProcessor;
    }

    // Botón principal: cámara (Android/iOS físico)
    private async void OnCaptureClicked(object? sender, EventArgs e)
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            StatusLabel.Text = "Cámara no disponible en este dispositivo. Usá 'Seleccionar imagen'.";
            StatusLabel.TextColor = Colors.Orange;
            return;
        }

        var permissionStatus = await CheckAndRequestCameraPermissionAsync();
        if (permissionStatus != PermissionStatus.Granted)
        {
            StatusLabel.Text = "Permiso de cámara denegado. Habilitalo en Configuración.";
            StatusLabel.TextColor = Colors.Red;
            return;
        }

        await ExecuteUploadAsync(async () =>
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is null) return null;
            return await photo.OpenReadAsync();
        });
    }

    // Botón secundario: file picker (Windows, emulador, demo)
    private async void OnPickClicked(object? sender, EventArgs e)
    {
        await ExecuteUploadAsync(async () =>
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleccioná la imagen del ticket",
                FileTypes = FilePickerFileType.Images,
            });
            if (result is null) return null;
            return await result.OpenReadAsync();
        });
    }

    private async Task ExecuteUploadAsync(Func<Task<Stream?>> getStream)
    {
        SetBusy(true);
        try
        {
            var stream = await getStream();
            if (stream is null)
            {
                StatusLabel.Text = "Operación cancelada.";
                StatusLabel.TextColor = Colors.Gray;
                return;
            }

            await using (stream)
            {
                await using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);
                buffer.Position = 0;

                var bitmap = SKBitmap.Decode(buffer);
                if (bitmap is null)
                {
                    StatusLabel.Text = "No se pudo abrir la imagen.";
                    StatusLabel.TextColor = Colors.Red;
                    return;
                }

                var cropPage = new CropPage(bitmap, _imageProcessor);
                await Navigation.PushModalAsync(new NavigationPage(cropPage));
                var processedStream = await cropPage.Result;
                if (processedStream is null)
                {
                    StatusLabel.Text = "Recorte cancelado.";
                    StatusLabel.TextColor = Colors.Gray;
                    return;
                }

                await using (processedStream)
                {
                    var path = await _uploader.UploadAsync(processedStream);
                    StatusLabel.Text = $"✓ Ticket enviado: {Path.GetFileName(path)}";
                    StatusLabel.TextColor = Colors.Green;
                }
            }
        }
        catch (PermissionException)
        {
            StatusLabel.Text = "Permiso denegado por el sistema.";
            StatusLabel.TextColor = Colors.Red;
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.GetType().Name} — {ex.Message}";
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
        if (status == PermissionStatus.Granted) return status;
        return await Permissions.RequestAsync<Permissions.Camera>();
    }

    private void SetBusy(bool busy)
    {
        BusyIndicator.IsVisible = busy;
        BusyIndicator.IsRunning = busy;
        CaptureButton.IsEnabled = !busy;
        PickButton.IsEnabled = !busy;
    }

    private async void OnHistoryClicked(object? sender, EventArgs e)
    {
        var services = Handler?.MauiContext?.Services;
        var historyPage = services?.GetService<HistoryPage>();
        if (historyPage is not null)
            await Navigation.PushAsync(historyPage);
    }
}
