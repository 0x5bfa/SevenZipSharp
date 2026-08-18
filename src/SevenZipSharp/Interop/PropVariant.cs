using System.Runtime.InteropServices;

namespace SevenZipSharp.Interop;

[StructLayout(LayoutKind.Explicit)]
internal struct PropVariant : IDisposable
{
    private const ushort VT_EMPTY = 0;
    private const ushort VT_NULL = 1;
    private const ushort VT_I2 = 2;
    private const ushort VT_I4 = 3;
    private const ushort VT_R4 = 4;
    private const ushort VT_R8 = 5;
    private const ushort VT_BSTR = 8;
    private const ushort VT_BOOL = 11;
    private const ushort VT_I1 = 16;
    private const ushort VT_UI1 = 17;
    private const ushort VT_UI2 = 18;
    private const ushort VT_UI4 = 19;
    private const ushort VT_I8 = 20;
    private const ushort VT_UI8 = 21;
    private const ushort VT_INT = 22;
    private const ushort VT_UINT = 23;
    private const ushort VT_FILETIME = 64;

    [FieldOffset(0)]
    private ushort _type;

    [FieldOffset(8)]
    private long _signed;

    [FieldOffset(8)]
    private ulong _unsigned;

    [FieldOffset(8)]
    private nint _pointer;

    [FieldOffset(8)]
    private PropVariantArray _arraySize;

    internal readonly bool IsEmpty => _type is VT_EMPTY or VT_NULL;

    internal readonly string? GetString()
    {
        if (IsEmpty)
        {
            return null;
        }

        EnsureType(VT_BSTR);
        return _pointer == 0 ? null : Marshal.PtrToStringBSTR(_pointer);
    }

    internal readonly unsafe Guid GetGuid()
    {
        EnsureType(VT_BSTR);
        if (_pointer == 0 || Marshal.ReadInt32(_pointer - sizeof(int)) != 16)
        {
            throw new InvalidDataException("The 7-Zip handler returned an invalid binary GUID.");
        }

        return *(Guid*)_pointer;
    }

    internal readonly bool? GetBoolean()
    {
        if (IsEmpty)
        {
            return null;
        }

        EnsureType(VT_BOOL);
        return unchecked((short)_signed) != 0;
    }

    internal readonly ulong? GetUnsignedInteger()
    {
        if (IsEmpty)
        {
            return null;
        }

        return _type switch
        {
            VT_UI1 => (byte)_unsigned,
            VT_UI2 => (ushort)_unsigned,
            VT_UI4 or VT_UINT => (uint)_unsigned,
            VT_UI8 => _unsigned,
            VT_I1 => checked((ulong)(sbyte)_signed),
            VT_I2 => checked((ulong)(short)_signed),
            VT_I4 or VT_INT => checked((ulong)(int)_signed),
            VT_I8 => checked((ulong)_signed),
            _ => throw UnexpectedType(),
        };
    }

    internal readonly DateTimeOffset? GetFileTime()
    {
        if (IsEmpty)
        {
            return null;
        }

        EnsureType(VT_FILETIME);
        return DateTimeOffset.FromFileTime(checked((long)_unsigned));
    }

    public void Dispose()
    {
        if (_type != VT_EMPTY)
        {
            _ = NativeMethods.PropVariantClear(ref this);
        }
    }

    private readonly void EnsureType(ushort expected)
    {
        if (_type != expected)
        {
            throw UnexpectedType();
        }
    }

    private readonly InvalidDataException UnexpectedType() =>
        new($"Unexpected PROPVARIANT type: {_type}.");
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct PropVariantArray
{
    private readonly uint _count;
    private readonly nint _elements;
}

internal static partial class NativeMethods
{
    [LibraryImport("ole32.dll")]
    internal static partial int PropVariantClear(ref PropVariant variant);
}
