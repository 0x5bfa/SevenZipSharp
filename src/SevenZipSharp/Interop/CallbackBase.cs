using System.Runtime.ExceptionServices;

namespace SevenZipSharp.Interop;

internal abstract class CallbackBase
{
    private ExceptionDispatchInfo? _exception;

    internal int Fail(Exception exception)
    {
        Interlocked.CompareExchange(ref _exception, ExceptionDispatchInfo.Capture(exception), null);
        return exception is OperationCanceledException
            ? HResults.E_ABORT
            : exception.HResult < 0 ? exception.HResult : HResults.E_FAIL;
    }

    internal void ThrowIfFailed() => _exception?.Throw();
}
