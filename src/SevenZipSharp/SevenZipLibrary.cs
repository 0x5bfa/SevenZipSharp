using SevenZipSharp.Interop;

namespace SevenZipSharp;

public sealed class SevenZipLibrary : IDisposable
{
    private readonly object _sync = new();
    private readonly SevenZipNativeLibrary _nativeLibrary;
    private int _activeArchives;
    private bool _disposeRequested;

    public SevenZipLibrary(SevenZipLibraryOptions? options = null)
    {
        options ??= new SevenZipLibraryOptions();
        var path = options.NativeLibraryPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, "7z.dll");
        }

        _nativeLibrary = new SevenZipNativeLibrary(Path.GetFullPath(path));
        Formats = _nativeLibrary.Formats;
    }

    public IReadOnlyList<ArchiveFormatInfo> Formats { get; }

    public SevenZipArchive Open(string archivePath, SevenZipOpenOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        options ??= new SevenZipOpenOptions();

        var stream = new FileStream(
            Path.GetFullPath(archivePath),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        try
        {
            return Open(stream, new SevenZipOpenOptions
            {
                FileExtension = options.FileExtension ?? Path.GetExtension(archivePath),
                Password = options.Password,
                MaximumSignatureSearchBytes = options.MaximumSignatureSearchBytes,
                LeaveOpen = false,
            });
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public unsafe SevenZipArchive Open(Stream archiveStream, SevenZipOpenOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(archiveStream);
        if (!archiveStream.CanRead || !archiveStream.CanSeek)
        {
            throw new ArgumentException("The archive stream must be readable and seekable.", nameof(archiveStream));
        }

        options ??= new SevenZipOpenOptions();
        AcquireArchive();
        var input = new ManagedInStream(archiveStream, options.LeaveOpen);
        var startPosition = archiveStream.Position;

        try
        {
            foreach (var format in OrderCandidates(options.FileExtension))
            {
                archiveStream.Position = startPosition;
                var archive = _nativeLibrary.CreateInputArchive(format);
                var callback = new ArchiveOpenCallback(options.Password);
                var maxCheck = options.MaximumSignatureSearchBytes;
                var result = archive.Open(input, (nint)(&maxCheck), callback);

                input.ThrowIfFailed();
                callback.ThrowIfFailed();

                if (result == HResults.S_OK)
                {
                    try
                    {
                        return new SevenZipArchive(this, archive, input, format, options.Password);
                    }
                    catch
                    {
                        _ = archive.Close();
                        ComInterop.FinalRelease(archive);
                        throw;
                    }
                }

                _ = archive.Close();
                ComInterop.FinalRelease(archive);

                if (result < 0)
                {
                    HResults.ThrowIfFailed(result, $"Opening the archive as {format.Name} failed");
                }
            }

            throw new InvalidDataException("No 7-Zip handler recognized the archive stream.");
        }
        catch
        {
            input.Dispose();
            ReleaseArchive();
            throw;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposeRequested)
            {
                return;
            }

            _disposeRequested = true;
            if (_activeArchives == 0)
            {
                _nativeLibrary.Dispose();
            }
        }
    }

    internal void ReleaseArchive()
    {
        lock (_sync)
        {
            _activeArchives--;
            if (_activeArchives == 0 && _disposeRequested)
            {
                _nativeLibrary.Dispose();
            }
        }
    }

    private void AcquireArchive()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposeRequested, this);
            _activeArchives++;
        }
    }

    private IEnumerable<ArchiveFormatInfo> OrderCandidates(string? extension)
    {
        var normalized = extension?.Trim().TrimStart('.');
        if (string.IsNullOrEmpty(normalized))
        {
            return Formats;
        }

        return Formats
            .OrderByDescending(format => format.Extensions.Contains(normalized, StringComparer.OrdinalIgnoreCase));
    }
}
