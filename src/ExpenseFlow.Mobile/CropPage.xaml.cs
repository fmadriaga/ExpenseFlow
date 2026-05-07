using ExpenseFlow.Mobile.Services;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace ExpenseFlow.Mobile;

public partial class CropPage : ContentPage
{
    private enum DragHandle
    {
        None,
        Move,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    private readonly SKBitmap _bitmap;
    private readonly ImageProcessorService _processor;
    private readonly TaskCompletionSource<Stream?> _tcs = new();

    private SKRect _imageRect;
    private SKRect _cropRect;
    private DragHandle _dragHandle = DragHandle.None;
    private bool _dragging;
    private SKRect _cropAtGestureStart;
    private SKPoint _lastTouchPoint;
    private float _viewToSurfaceScaleX = 1f;
    private float _viewToSurfaceScaleY = 1f;
    private bool _completed;

    public Task<Stream?> Result => _tcs.Task;

    public CropPage(SKBitmap originalBitmap, ImageProcessorService processor)
    {
        InitializeComponent();
        _bitmap = originalBitmap ?? throw new ArgumentNullException(nameof(originalBitmap));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _imageRect = SKRect.Empty;
        _cropRect = SKRect.Empty;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Canvas.InvalidateSurface();
    }

    protected override void OnDisappearing()
    {
        if (!_completed)
            _tcs.TrySetResult(null);
        _bitmap.Dispose();
        base.OnDisappearing();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var info = e.Info;
        var surface = e.Surface;
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Black);

        if (_bitmap.Width < 1 || _bitmap.Height < 1)
            return;

        if (Canvas.Width > 0 && Canvas.Height > 0)
        {
            _viewToSurfaceScaleX = info.Width / (float)Canvas.Width;
            _viewToSurfaceScaleY = info.Height / (float)Canvas.Height;
        }

        var scale = Math.Min(info.Width / (float)_bitmap.Width, info.Height / (float)_bitmap.Height);
        var drawW = _bitmap.Width * scale;
        var drawH = _bitmap.Height * scale;
        var left = (info.Width - drawW) / 2f;
        var top = (info.Height - drawH) / 2f;
        _imageRect = new SKRect(left, top, left + drawW, top + drawH);

        if (_cropRect.IsEmpty || _cropRect.Width < 1)
        {
            var marginX = _imageRect.Width * 0.1f;
            var marginY = _imageRect.Height * 0.1f;
            _cropRect = new SKRect(
                _imageRect.Left + marginX,
                _imageRect.Top + marginY,
                _imageRect.Right - marginX,
                _imageRect.Bottom - marginY);
        }

        var src = new SKRect(0, 0, _bitmap.Width, _bitmap.Height);
        using var drawPaint = new SKPaint { IsAntialias = true };
        canvas.DrawBitmap(_bitmap, src, _imageRect, drawPaint);

        using var overlayPaint = new SKPaint { Color = SKColors.Black.WithAlpha(153) };
        canvas.DrawRect(0, 0, info.Width, _cropRect.Top, overlayPaint);
        canvas.DrawRect(0, _cropRect.Bottom, info.Width, info.Height - _cropRect.Bottom, overlayPaint);
        canvas.DrawRect(0, _cropRect.Top, _cropRect.Left, _cropRect.Height, overlayPaint);
        canvas.DrawRect(_cropRect.Right, _cropRect.Top, info.Width - _cropRect.Right, _cropRect.Height, overlayPaint);

        using var borderPaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
        canvas.DrawRect(_cropRect, borderPaint);

        const float handleRadius = 12f;
        using var handleFill = new SKPaint { Color = SKColors.White, IsAntialias = true };
        foreach (var c in GetCornerCenters(_cropRect))
            canvas.DrawCircle(c.X, c.Y, handleRadius, handleFill);

        using var gridPaint = new SKPaint { Color = new SKColor(255, 255, 255, 77), Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        var dx = _cropRect.Width / 3f;
        var dy = _cropRect.Height / 3f;
        for (var i = 1; i <= 2; i++)
        {
            var x = _cropRect.Left + dx * i;
            var y = _cropRect.Top + dy * i;
            canvas.DrawLine(x, _cropRect.Top, x, _cropRect.Bottom, gridPaint);
            canvas.DrawLine(_cropRect.Left, y, _cropRect.Right, y, gridPaint);
        }
    }

    private static SKPoint[] GetCornerCenters(SKRect r) =>
    [
        new SKPoint(r.Left, r.Top),
        new SKPoint(r.Right, r.Top),
        new SKPoint(r.Left, r.Bottom),
        new SKPoint(r.Right, r.Bottom),
    ];

    private SKPoint ViewPointToSurface(Point p) =>
        new((float)(p.X * _viewToSurfaceScaleX), (float)(p.Y * _viewToSurfaceScaleY));

    private void OnCanvasTouch(object? sender, SKTouchEventArgs e)
    {
        e.Handled = true;

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _dragHandle = HitTest(e.Location);
                _dragging = _dragHandle != DragHandle.None;
                _cropAtGestureStart = _cropRect;
                _lastTouchPoint = e.Location;
                break;

            case SKTouchAction.Moved:
                if (_dragging && _dragHandle != DragHandle.None)
                {
                    var dx = e.Location.X - _lastTouchPoint.X;
                    var dy = e.Location.Y - _lastTouchPoint.Y;
                    _lastTouchPoint = e.Location;
                    _cropAtGestureStart = _cropRect;
                    ApplyPanDelta(dx, dy);
                    Canvas.InvalidateSurface();
                }
                break;

            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                _dragging = false;
                _dragHandle = DragHandle.None;
                break;
        }
    }

    private void ApplyPanDelta(float dx, float dy)
    {
        var r = _cropAtGestureStart;
        var minSize = 80f;

        switch (_dragHandle)
        {
            case DragHandle.Move:
                var w = r.Width;
                var h = r.Height;
                var nl = r.Left + dx;
                var nt = r.Top + dy;
                nl = Math.Clamp(nl, _imageRect.Left, _imageRect.Right - w);
                nt = Math.Clamp(nt, _imageRect.Top, _imageRect.Bottom - h);
                _cropRect = new SKRect(nl, nt, nl + w, nt + h);
                break;
            case DragHandle.TopLeft:
                _cropRect = new SKRect(r.Left + dx, r.Top + dy, r.Right, r.Bottom);
                NormalizeCropRect(minSize);
                break;
            case DragHandle.TopRight:
                _cropRect = new SKRect(r.Left, r.Top + dy, r.Right + dx, r.Bottom);
                NormalizeCropRect(minSize);
                break;
            case DragHandle.BottomLeft:
                _cropRect = new SKRect(r.Left + dx, r.Top, r.Right, r.Bottom + dy);
                NormalizeCropRect(minSize);
                break;
            case DragHandle.BottomRight:
                _cropRect = new SKRect(r.Left, r.Top, r.Right + dx, r.Bottom + dy);
                NormalizeCropRect(minSize);
                break;
        }
    }

    private void NormalizeCropRect(float minSize)
    {
        var l = Math.Min(_cropRect.Left, _cropRect.Right);
        var r = Math.Max(_cropRect.Left, _cropRect.Right);
        var t = Math.Min(_cropRect.Top, _cropRect.Bottom);
        var b = Math.Max(_cropRect.Top, _cropRect.Bottom);
        l = Math.Clamp(l, _imageRect.Left, _imageRect.Right - minSize);
        r = Math.Clamp(r, _imageRect.Left + minSize, _imageRect.Right);
        t = Math.Clamp(t, _imageRect.Top, _imageRect.Bottom - minSize);
        b = Math.Clamp(b, _imageRect.Top + minSize, _imageRect.Bottom);
        if (r - l < minSize)
        {
            if (l <= _imageRect.Left + 0.5f)
                r = l + minSize;
            else
                l = r - minSize;
        }
        if (b - t < minSize)
        {
            if (t <= _imageRect.Top + 0.5f)
                b = t + minSize;
            else
                t = b - minSize;
        }
        _cropRect = new SKRect(l, t, r, b);
    }

    private DragHandle HitTest(SKPoint surfacePoint)
    {
        const float hitRadius = 24f;
        var corners = new (DragHandle h, SKPoint p)[]
        {
            (DragHandle.TopLeft, new SKPoint(_cropRect.Left, _cropRect.Top)),
            (DragHandle.TopRight, new SKPoint(_cropRect.Right, _cropRect.Top)),
            (DragHandle.BottomLeft, new SKPoint(_cropRect.Left, _cropRect.Bottom)),
            (DragHandle.BottomRight, new SKPoint(_cropRect.Right, _cropRect.Bottom)),
        };
        foreach (var (h, p) in corners)
        {
            if (SKPoint.Distance(surfacePoint, p) <= hitRadius)
                return h;
        }
        if (_cropRect.Contains(surfacePoint.X, surfacePoint.Y))
            return DragHandle.Move;
        return DragHandle.None;
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        _completed = true;
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync();
    }

    private async void OnConfirmClicked(object? sender, EventArgs e)
    {
        try
        {
            if (_imageRect.Width < 1 || _bitmap.Width < 1)
            {
                _completed = true;
                _tcs.TrySetResult(null);
                await Navigation.PopModalAsync();
                return;
            }

            var l = (_cropRect.Left - _imageRect.Left) / _imageRect.Width * _bitmap.Width;
            var t = (_cropRect.Top - _imageRect.Top) / _imageRect.Height * _bitmap.Height;
            var r = (_cropRect.Right - _imageRect.Left) / _imageRect.Width * _bitmap.Width;
            var b = (_cropRect.Bottom - _imageRect.Top) / _imageRect.Height * _bitmap.Height;

            var cropI = new SKRectI(
                (int)Math.Floor(l),
                (int)Math.Floor(t),
                (int)Math.Ceiling(r),
                (int)Math.Ceiling(b));

            await using var encoded = new MemoryStream();
            using (var img = SKImage.FromBitmap(_bitmap))
            {
                using var data = img.Encode(SKEncodedImageFormat.Jpeg, 92);
                if (data is null)
                    throw new InvalidOperationException("No se pudo codificar la imagen original.");
                data.SaveTo(encoded);
            }
            encoded.Position = 0;

            var processed = await _processor.ProcessAsync(encoded, cropI);
            _completed = true;
            _tcs.TrySetResult(processed);
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Aceptar");
            _completed = true;
            _tcs.TrySetResult(null);
            await Navigation.PopModalAsync();
        }
    }
}
