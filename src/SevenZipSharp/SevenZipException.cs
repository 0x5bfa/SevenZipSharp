using System.Runtime.InteropServices;

namespace SevenZipSharp;

public sealed class SevenZipException : ExternalException
{
    public SevenZipException(string message, int errorCode)
        : base($"{message} (HRESULT: 0x{errorCode:X8})", errorCode)
    {
    }
}
