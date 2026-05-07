using ExpenseFlow.Mobile.Services;

namespace ExpenseFlow.Mobile;

public partial class DetailPage : ContentPage
{
    private readonly ExpenseFlowApiClient _apiClient;
    private readonly int _documentId;
    private DocumentDetail? _detail;

    // Exposed for the OcrStatusColorConverter binding on the page root
    public string? OcrStatus { get; private set; }

    public DetailPage(int documentId, ExpenseFlowApiClient apiClient)
    {
        InitializeComponent();
        _documentId = documentId;
        _apiClient = apiClient;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        ContentPanel.IsVisible = false;
        ActionBar.IsVisible = false;

        _detail = await _apiClient.GetDetailAsync(_documentId);
        if (_detail is null)
        {
            LoadingIndicator.IsVisible = false;
            await DisplayAlert("Error", "No se pudo cargar el ticket.", "Aceptar");
            await Navigation.PopAsync();
            return;
        }

        Populate(_detail);

        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;
        ContentPanel.IsVisible = true;
        ActionBar.IsVisible = true;
    }

    private void Populate(DocumentDetail d)
    {
        // Update bindable property so the status badge color converter fires
        OcrStatus = d.OcrStatus;
        OnPropertyChanged(nameof(OcrStatus));

        ConfidenceLabel.Text = $"Confianza: {d.Confidence:0.#}%";

        // Error message
        if (!string.IsNullOrWhiteSpace(d.ErrorMessage))
        {
            ErrorLabel.Text = d.ErrorMessage;
            ErrorFrame.IsVisible = true;
        }
        else
        {
            ErrorFrame.IsVisible = false;
        }

        MerchantEntry.Text = d.MerchantName ?? string.Empty;

        DateEntry.Text = d.TransactionDate.HasValue
            ? d.TransactionDate.Value.ToString("dd/MM/yyyy")
            : string.Empty;

        AmountEntry.Text = d.TotalAmount.HasValue
            ? d.TotalAmount.Value.ToString("0.##")
            : string.Empty;

        CurrencyLabel.Text = d.Currency ?? string.Empty;
        CategoryLabel.Text = string.IsNullOrWhiteSpace(d.Category) ? "—" : d.Category;

        CreatedAtLabel.Text = $"Registrado {d.CreatedAt.LocalDateTime:dd/MM/yyyy HH:mm}";

        // Lines
        var lines = d.Lines ?? [];
        if (lines.Count > 0)
        {
            LinesCollection.ItemsSource = lines;
            LinesCollection.IsVisible = true;
            NoLinesLabel.IsVisible = false;
        }
        else
        {
            LinesCollection.IsVisible = false;
            NoLinesLabel.IsVisible = true;
        }

        // Reprocess button: only for non-Success documents
        var canReprocess = d.OcrStatus is not "Success";
        ReprocessButton.IsVisible = canReprocess;

        // If only one action, make the Save button span both columns
        if (!canReprocess)
        {
            Grid.SetColumn(SaveButton, 0);
            Grid.SetColumnSpan(SaveButton, 2);
        }
        else
        {
            Grid.SetColumn(SaveButton, 1);
            Grid.SetColumnSpan(SaveButton, 1);
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        SaveButton.IsEnabled = false;
        try
        {
            var merchantName = string.IsNullOrWhiteSpace(MerchantEntry.Text)
                ? null
                : MerchantEntry.Text.Trim();

            DateOnly? transactionDate = null;
            if (!string.IsNullOrWhiteSpace(DateEntry.Text))
            {
                if (DateOnly.TryParseExact(
                        DateEntry.Text.Trim(),
                        ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"],
                        null,
                        System.Globalization.DateTimeStyles.None,
                        out var parsed))
                {
                    transactionDate = parsed;
                }
                else
                {
                    await DisplayAlert("Fecha inválida",
                        "Ingresá la fecha en formato dd/mm/aaaa.", "Aceptar");
                    return;
                }
            }

            decimal? totalAmount = null;
            if (!string.IsNullOrWhiteSpace(AmountEntry.Text))
            {
                if (decimal.TryParse(
                        AmountEntry.Text.Trim(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var amt))
                {
                    totalAmount = amt;
                }
                else
                {
                    await DisplayAlert("Monto inválido",
                        "Ingresá un número válido para el monto.", "Aceptar");
                    return;
                }
            }

            var ok = await _apiClient.PatchDocumentAsync(
                _documentId, merchantName, transactionDate, totalAmount);

            if (ok)
            {
                await DisplayAlert("✓", "Cambios guardados.", "OK");
                await LoadAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudieron guardar los cambios.", "Aceptar");
            }
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private async void OnReprocessClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlert(
            "Reprocesar",
            "¿Reenviar este ticket al OCR?",
            "Sí", "No");
        if (!confirm) return;

        ReprocessButton.IsEnabled = false;
        try
        {
            var ok = await _apiClient.ReprocessAsync(_documentId);
            if (ok)
            {
                await DisplayAlert("✓",
                    "Ticket enviado a reproceso. El resultado aparecerá en el historial en unos segundos.",
                    "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo enviar a reproceso.", "Aceptar");
            }
        }
        finally
        {
            ReprocessButton.IsEnabled = true;
        }
    }
}
