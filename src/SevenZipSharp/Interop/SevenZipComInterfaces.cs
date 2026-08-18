using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SevenZipSharp.Interop;

[GeneratedComInterface]
[Guid("23170F69-40C1-278A-0000-000300010000")]
internal partial interface ISequentialInStream
{
    [PreserveSig]
    int Read(nint data, uint size, nint processedSize);
}

[GeneratedComInterface]
[Guid("23170F69-40C1-278A-0000-000300020000")]
internal partial interface ISequentialOutStream
{
    [PreserveSig]
    int Write(nint data, uint size, nint processedSize);
}

[GeneratedComInterface]
[Guid("23170F69-40C1-278A-0000-000300030000")]
internal partial interface IInStream : ISequentialInStream
{
    [PreserveSig]
    int Seek(long offset, uint seekOrigin, nint newPosition);
}

[GeneratedComInterface]
[Guid("23170F69-40C1-278A-0000-000000050000")]
internal partial interface IProgress
{
    [PreserveSig]
    int SetTotal(ulong total);

    [PreserveSig]
    int SetCompleted(nint completeValue);
}

[GeneratedComInterface]
[Guid("23170F69-40C1-278A-0000-000600100000")]
internal partial interface IArchiveOpenCallback
{
    [PreserveSig]
    int SetTotal(nint files, nint bytes);

    [PreserveSig]
    int SetCompleted(nint files, nint bytes);
}

[GeneratedComInterface]
[Guid("23170F69-40C1-278A-0000-000600200000")]
internal partial interface IArchiveExtractCallback : IProgress
{
    [PreserveSig]
    int GetStream(uint index, out ISequentialOutStream? outStream, int askExtractMode);

    [PreserveSig]
    int PrepareOperation(int askExtractMode);

    [PreserveSig]
    int SetOperationResult(int operationResult);
}

[GeneratedComInterface]
[Guid("23170F69-40C1-278A-0000-000500100000")]
internal partial interface ICryptoGetTextPassword
{
    [PreserveSig]
    int CryptoGetTextPassword([MarshalAs(UnmanagedType.BStr)] out string password);
}

[GeneratedComInterface]
[Guid("23170F69-40C1-278A-0000-000600600000")]
internal partial interface IInArchive
{
    [PreserveSig]
    int Open(IInStream stream, nint maxCheckStartPosition, IArchiveOpenCallback openCallback);

    [PreserveSig]
    int Close();

    [PreserveSig]
    int GetNumberOfItems(out uint numberOfItems);

    [PreserveSig]
    int GetProperty(uint index, ItemPropertyId propertyId, ref PropVariant value);

    [PreserveSig]
    int Extract(nint indices, uint numberOfItems, int testMode, IArchiveExtractCallback extractCallback);

    [PreserveSig]
    int GetArchiveProperty(ItemPropertyId propertyId, ref PropVariant value);

    [PreserveSig]
    int GetNumberOfProperties(out uint numberOfProperties);

    [PreserveSig]
    int GetPropertyInfo(uint index, nint name, nint propertyId, nint variantType);

    [PreserveSig]
    int GetNumberOfArchiveProperties(out uint numberOfProperties);

    [PreserveSig]
    int GetArchivePropertyInfo(uint index, nint name, nint propertyId, nint variantType);
}
