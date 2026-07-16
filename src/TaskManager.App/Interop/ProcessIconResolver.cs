using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using TaskManager.App.ViewModels;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace TaskManager.App.Interop;

/// <summary>
/// Resolves process icons via the WinRT thumbnail cache: the executable path becomes a
/// <see cref="StorageFile"/> whose <see cref="StorageItemThumbnail"/> is its shell icon.
/// This is the WinUI-native route (WinUI 3 has no HICON→ImageSource bridge), so it avoids
/// hand-marshalling icon bitmaps. Results are cached per path — icons are per-executable and
/// don't change tick to tick (spec §5) — and failures resolve to <see langword="null"/> so
/// the row shows its generic placeholder (spec §4 degradation), never an error.
/// </summary>
/// <remarks>
/// UI-thread affine: <see cref="BitmapImage"/> and its <c>SetSourceAsync</c> must run on the
/// UI thread. Every call originates from <c>ProcessRowViewModel.Update</c> on the tick's UI
/// marshal, so the caches need no locking.
/// </remarks>
internal sealed class ProcessIconResolver : IProcessIconResolver
{
    // 32px covers the 16px row cell on high-DPI displays without re-fetching.
    private const uint RequestedIconSize = 32;

    private readonly Dictionary<string, ImageSource?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Task<ImageSource?>> _inFlight = new(StringComparer.OrdinalIgnoreCase);

    public Task<ImageSource?> ResolveAsync(string imagePath)
    {
        if (_cache.TryGetValue(imagePath, out ImageSource? cached))
        {
            return Task.FromResult(cached);
        }

        // Coalesce concurrent requests for the same executable (many rows, one chrome.exe).
        if (_inFlight.TryGetValue(imagePath, out Task<ImageSource?>? pending))
        {
            return pending;
        }

        Task<ImageSource?> load = LoadAsync(imagePath);
        _inFlight[imagePath] = load;
        return load;
    }

    private async Task<ImageSource?> LoadAsync(string imagePath)
    {
        ImageSource? result = null;
        try
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(imagePath);
            using StorageItemThumbnail thumbnail =
                await file.GetThumbnailAsync(ThumbnailMode.SingleItem, RequestedIconSize);

            if (thumbnail is { Size: > 0 })
            {
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(thumbnail);
                result = bitmap;
            }
        }
        catch
        {
            // Protected / unreadable path (or no associated icon): fall back to the generic
            // placeholder. Caching the null means we don't retry it every tick.
            result = null;
        }

        _cache[imagePath] = result;
        _inFlight.Remove(imagePath);
        return result;
    }
}
