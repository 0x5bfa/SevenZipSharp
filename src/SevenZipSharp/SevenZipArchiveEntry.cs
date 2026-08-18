namespace SevenZipSharp;

public sealed record SevenZipArchiveEntry
{
    internal SevenZipArchiveEntry(
        uint index,
        string path,
        bool isDirectory,
        ulong? size,
        ulong? packedSize,
        DateTimeOffset? modifiedTime,
        bool isEncrypted,
        uint? crc,
        string? method)
    {
        Index = index;
        Path = path;
        IsDirectory = isDirectory;
        Size = size;
        PackedSize = packedSize;
        ModifiedTime = modifiedTime;
        IsEncrypted = isEncrypted;
        Crc = crc;
        Method = method;
    }

    public uint Index { get; }

    public string Path { get; }

    public bool IsDirectory { get; }

    public ulong? Size { get; }

    public ulong? PackedSize { get; }

    public DateTimeOffset? ModifiedTime { get; }

    public bool IsEncrypted { get; }

    public uint? Crc { get; }

    public string? Method { get; }

    public override string ToString() => Path;
}
