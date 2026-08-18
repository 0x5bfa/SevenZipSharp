namespace SevenZipSharp.Interop;

internal static class HResults
{
    internal const int S_OK = 0;
    internal const int S_FALSE = 1;
    internal const int E_ABORT = unchecked((int)0x80004004);
    internal const int E_FAIL = unchecked((int)0x80004005);
    internal const int E_INVALIDARG = unchecked((int)0x80070057);

    internal static void ThrowIfFailed(int result, string operation)
    {
        if (result >= 0)
        {
            return;
        }

        throw new SevenZipException(operation, result);
    }
}
