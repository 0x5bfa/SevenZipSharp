using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace SevenZipSharp.Interop;

internal sealed unsafe class SevenZipNativeLibrary : IDisposable
{
    private readonly delegate* unmanaged[Stdcall]<uint*, int> _getNumberOfFormats;
    private readonly delegate* unmanaged[Stdcall]<uint, HandlerPropertyId, PropVariant*, int> _getHandlerProperty;
    private readonly delegate* unmanaged[Stdcall]<Guid*, Guid*, nint*, int> _createObject;
    private nint _module;

    internal SevenZipNativeLibrary(string path)
    {
        _module = NativeLibrary.Load(path);

        try
        {
            _getNumberOfFormats = (delegate* unmanaged[Stdcall]<uint*, int>)NativeLibrary.GetExport(_module, "GetNumberOfFormats");
            _getHandlerProperty = (delegate* unmanaged[Stdcall]<uint, HandlerPropertyId, PropVariant*, int>)NativeLibrary.GetExport(_module, "GetHandlerProperty2");
            _createObject = (delegate* unmanaged[Stdcall]<Guid*, Guid*, nint*, int>)NativeLibrary.GetExport(_module, "CreateObject");
            Formats = LoadFormats();
        }
        catch
        {
            NativeLibrary.Free(_module);
            _module = 0;
            throw;
        }
    }

    internal IReadOnlyList<ArchiveFormatInfo> Formats { get; }

    internal IInArchive CreateInputArchive(ArchiveFormatInfo format)
    {
        ObjectDisposedException.ThrowIf(_module == 0, this);

        var classId = format.ClassId;
        var interfaceId = new Guid("23170F69-40C1-278A-0000-000600600000");
        nint instance = 0;
        var result = _createObject(&classId, &interfaceId, &instance);

        if (result < 0)
        {
            if (instance != 0)
            {
                Marshal.Release(instance);
            }

            HResults.ThrowIfFailed(result, $"Creating the {format.Name} input handler failed");
        }

        return ComInterop.WrapOwned<IInArchive>(instance);
    }

    public void Dispose()
    {
        if (_module != 0)
        {
            NativeLibrary.Free(_module);
            _module = 0;
        }
    }

    private ReadOnlyCollection<ArchiveFormatInfo> LoadFormats()
    {
        uint count = 0;
        HResults.ThrowIfFailed(_getNumberOfFormats(&count), "Enumerating 7-Zip formats failed");

        var formats = new List<ArchiveFormatInfo>(checked((int)count));
        for (uint index = 0; index < count; index++)
        {
            var name = GetStringProperty(index, HandlerPropertyId.Name);
            var classId = GetGuidProperty(index, HandlerPropertyId.ClassId);
            var extensionList = GetStringProperty(index, HandlerPropertyId.Extension);
            var extensions = extensionList
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static extension => extension.TrimStart('.'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var supportsWriting = GetBooleanProperty(index, HandlerPropertyId.Update) ?? false;

            formats.Add(new ArchiveFormatInfo(
                index,
                classId,
                name,
                Array.AsReadOnly(extensions),
                supportsWriting));
        }

        return formats.AsReadOnly();
    }

    private string GetStringProperty(uint index, HandlerPropertyId propertyId)
    {
        var value = default(PropVariant);
        try
        {
            HResults.ThrowIfFailed(_getHandlerProperty(index, propertyId, &value), $"Reading handler property {propertyId} failed");
            return value.GetString() ?? string.Empty;
        }
        finally
        {
            value.Dispose();
        }
    }

    private Guid GetGuidProperty(uint index, HandlerPropertyId propertyId)
    {
        var value = default(PropVariant);
        try
        {
            HResults.ThrowIfFailed(_getHandlerProperty(index, propertyId, &value), $"Reading handler property {propertyId} failed");
            return value.GetGuid();
        }
        finally
        {
            value.Dispose();
        }
    }

    private bool? GetBooleanProperty(uint index, HandlerPropertyId propertyId)
    {
        var value = default(PropVariant);
        try
        {
            HResults.ThrowIfFailed(_getHandlerProperty(index, propertyId, &value), $"Reading handler property {propertyId} failed");
            return value.GetBoolean();
        }
        finally
        {
            value.Dispose();
        }
    }
}
