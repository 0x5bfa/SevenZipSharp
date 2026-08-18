namespace SevenZipSharp;

public sealed record ArchiveFormatInfo
{
    internal ArchiveFormatInfo(
        uint index,
        Guid classId,
        string name,
        IReadOnlyList<string> extensions,
        bool supportsWriting)
    {
        Index = index;
        ClassId = classId;
        Name = name;
        Extensions = extensions;
        SupportsWriting = supportsWriting;
    }

    internal uint Index { get; }

    internal Guid ClassId { get; }

    public string Name { get; }

    public IReadOnlyList<string> Extensions { get; }

    public bool SupportsWriting { get; }

    public override string ToString() => Name;
}
