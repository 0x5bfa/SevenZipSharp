using System.Runtime.InteropServices.Marshalling;

namespace SevenZipSharp.Interop;

[GeneratedComClass]
internal sealed partial class ArchiveOpenCallback : CallbackBase, IArchiveOpenCallback, ICryptoGetTextPassword
{
    private readonly string? _password;

    internal ArchiveOpenCallback(string? password)
    {
        _password = password;
    }

    public int SetTotal(nint files, nint bytes) => HResults.S_OK;

    public int SetCompleted(nint files, nint bytes) => HResults.S_OK;

    public int CryptoGetTextPassword(out string password)
    {
        password = _password ?? string.Empty;
        return _password is null ? HResults.E_ABORT : HResults.S_OK;
    }
}
