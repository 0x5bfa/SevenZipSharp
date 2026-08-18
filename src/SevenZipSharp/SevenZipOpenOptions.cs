namespace SevenZipSharp;

public sealed class SevenZipOpenOptions
{
    public string? FileExtension { get; init; }

    public string? Password { get; init; }

    public ulong MaximumSignatureSearchBytes { get; init; } = 8 * 1024 * 1024;

    public bool LeaveOpen { get; init; }
}
