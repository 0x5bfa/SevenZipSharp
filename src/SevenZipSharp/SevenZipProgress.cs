namespace SevenZipSharp;

public readonly record struct SevenZipProgress(ulong? CompletedBytes, ulong? TotalBytes);
