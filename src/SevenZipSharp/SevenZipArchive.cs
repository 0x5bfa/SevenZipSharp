using System.Collections.ObjectModel;
using SevenZipSharp.Interop;

namespace SevenZipSharp;

public sealed class SevenZipArchive : IDisposable
{
    private readonly object _sync = new();
    private readonly SevenZipLibrary _library;
    private readonly IInArchive _archive;
    private readonly ManagedInStream _input;
    private readonly string? _password;
    private bool _disposed;

    internal SevenZipArchive(
        SevenZipLibrary library,
        IInArchive archive,
        ManagedInStream input,
        ArchiveFormatInfo format,
        string? password)
    {
        _library = library;
        _archive = archive;
        _input = input;
        _password = password;
        Format = format;
        Entries = ReadEntries();
    }

    public ArchiveFormatInfo Format { get; }

    public IReadOnlyList<SevenZipArchiveEntry> Entries { get; }

    public void ExtractTo(
        SevenZipArchiveEntry entry,
        Stream destination,
        IProgress<SevenZipProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(destination);

        if (entry.IsDirectory)
        {
            throw new ArgumentException("A directory entry cannot be extracted to a stream.", nameof(entry));
        }

        if (!ReferenceEquals(Entries[checked((int)entry.Index)], entry))
        {
            throw new ArgumentException("The entry does not belong to this archive.", nameof(entry));
        }

        ExtractCore(
            entry.Index,
            current => current.Index == entry.Index ? new ExtractionTarget(destination, true) : null,
            cancellationToken,
            progress);
    }

    public void ExtractToDirectory(
        string destinationDirectory,
        bool overwrite = false,
        IProgress<SevenZipProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        var root = Path.GetFullPath(destinationDirectory);
        Directory.CreateDirectory(root);
        var rootPrefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;

        ExtractCore(
            null,
            entry => CreateFileTarget(entry, root, rootPrefix, overwrite),
            cancellationToken,
            progress);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _ = _archive.Close();
                ComInterop.FinalRelease(_archive);
            }
            finally
            {
                try
                {
                    _input.Dispose();
                }
                finally
                {
                    _library.ReleaseArchive();
                }
            }
        }
    }

    private ReadOnlyCollection<SevenZipArchiveEntry> ReadEntries()
    {
        HResults.ThrowIfFailed(_archive.GetNumberOfItems(out var count), "Reading the archive item count failed");
        var entries = new List<SevenZipArchiveEntry>(checked((int)count));

        for (uint index = 0; index < count; index++)
        {
            var path = GetString(index, ItemPropertyId.Path)
                ?? GetString(index, ItemPropertyId.Name)
                ?? index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            entries.Add(new SevenZipArchiveEntry(
                index,
                path,
                GetBoolean(index, ItemPropertyId.IsDirectory) ?? false,
                GetUnsigned(index, ItemPropertyId.Size),
                GetUnsigned(index, ItemPropertyId.PackedSize),
                GetFileTime(index, ItemPropertyId.ModifiedTime),
                GetBoolean(index, ItemPropertyId.Encrypted) ?? false,
                checked((uint?)GetUnsigned(index, ItemPropertyId.Crc)),
                GetString(index, ItemPropertyId.Method)));
        }

        return entries.AsReadOnly();
    }

    private unsafe void ExtractCore(
        uint? index,
        Func<SevenZipArchiveEntry, ExtractionTarget?> targetFactory,
        CancellationToken cancellationToken,
        IProgress<SevenZipProgress>? progress)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            using var callback = new ArchiveExtractCallback(
                Entries,
                targetFactory,
                _password,
                cancellationToken,
                progress);

            int result;
            if (index.HasValue)
            {
                var itemIndex = index.Value;
                result = _archive.Extract((nint)(&itemIndex), 1, 0, callback);
            }
            else
            {
                result = _archive.Extract(0, uint.MaxValue, 0, callback);
            }

            callback.ThrowIfFailed();
            _input.ThrowIfFailed();
            HResults.ThrowIfFailed(result, "Extracting the archive failed");
        }
    }

    private static ExtractionTarget? CreateFileTarget(
        SevenZipArchiveEntry entry,
        string root,
        string rootPrefix,
        bool overwrite)
    {
        var relativePath = entry.Path.Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(relativePath) || relativePath.Contains(':', StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Archive entry has an unsafe path: {entry.Path}");
        }

        var destination = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!destination.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(destination, root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Archive entry escapes the destination directory: {entry.Path}");
        }

        if (entry.IsDirectory)
        {
            Directory.CreateDirectory(destination);
            return null;
        }

        var parent = Path.GetDirectoryName(destination);
        if (parent is not null)
        {
            Directory.CreateDirectory(parent);
        }

        var mode = overwrite ? FileMode.Create : FileMode.CreateNew;
        return new ExtractionTarget(
            new FileStream(destination, mode, FileAccess.Write, FileShare.None),
            false);
    }

    private string? GetString(uint index, ItemPropertyId propertyId)
    {
        var value = default(PropVariant);
        try
        {
            HResults.ThrowIfFailed(_archive.GetProperty(index, propertyId, ref value), $"Reading item property {propertyId} failed");
            return value.GetString();
        }
        finally
        {
            value.Dispose();
        }
    }

    private bool? GetBoolean(uint index, ItemPropertyId propertyId)
    {
        var value = default(PropVariant);
        try
        {
            HResults.ThrowIfFailed(_archive.GetProperty(index, propertyId, ref value), $"Reading item property {propertyId} failed");
            return value.GetBoolean();
        }
        finally
        {
            value.Dispose();
        }
    }

    private ulong? GetUnsigned(uint index, ItemPropertyId propertyId)
    {
        var value = default(PropVariant);
        try
        {
            HResults.ThrowIfFailed(_archive.GetProperty(index, propertyId, ref value), $"Reading item property {propertyId} failed");
            return value.GetUnsignedInteger();
        }
        finally
        {
            value.Dispose();
        }
    }

    private DateTimeOffset? GetFileTime(uint index, ItemPropertyId propertyId)
    {
        var value = default(PropVariant);
        try
        {
            HResults.ThrowIfFailed(_archive.GetProperty(index, propertyId, ref value), $"Reading item property {propertyId} failed");
            return value.GetFileTime();
        }
        finally
        {
            value.Dispose();
        }
    }
}
