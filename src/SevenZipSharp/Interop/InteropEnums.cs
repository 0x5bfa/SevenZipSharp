namespace SevenZipSharp.Interop;

internal enum HandlerPropertyId : uint
{
    Name = 0,
    ClassId = 1,
    Extension = 2,
    AddExtension = 3,
    Update = 4,
    KeepName = 5,
    Signature = 6,
    MultiSignature = 7,
    SignatureOffset = 8,
    AlternateStreams = 9,
    NtSecurity = 10,
    Flags = 11,
    TimeFlags = 12,
}

internal enum ItemPropertyId : uint
{
    Path = 3,
    Name = 4,
    Extension = 5,
    IsDirectory = 6,
    Size = 7,
    PackedSize = 8,
    CreationTime = 10,
    AccessTime = 11,
    ModifiedTime = 12,
    Encrypted = 15,
    Crc = 19,
    Method = 22,
}

internal enum ExtractAskMode
{
    Extract = 0,
    Test = 1,
    Skip = 2,
    ReadExternal = 3,
}

internal enum ExtractOperationResult
{
    Ok = 0,
    UnsupportedMethod = 1,
    DataError = 2,
    CrcError = 3,
    Unavailable = 4,
    UnexpectedEnd = 5,
    DataAfterEnd = 6,
    IsNotArchive = 7,
    HeadersError = 8,
    WrongPassword = 9,
}
