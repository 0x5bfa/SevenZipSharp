using System.Runtime.InteropServices.Marshalling;

namespace SevenZipSharp.Interop;

internal readonly record struct ExtractionTarget(Stream Stream, bool LeaveOpen);

[GeneratedComClass]
internal sealed partial class ArchiveExtractCallback : CallbackBase, IArchiveExtractCallback, ICryptoGetTextPassword, IDisposable
{
    private readonly IReadOnlyList<SevenZipArchiveEntry> _entries;
    private readonly Func<SevenZipArchiveEntry, ExtractionTarget?> _targetFactory;
    private readonly string? _password;
    private readonly CancellationToken _cancellationToken;
    private readonly IProgress<SevenZipProgress>? _progress;
    private ManagedOutStream? _currentStream;
    private ulong? _totalBytes;

    internal ArchiveExtractCallback(
        IReadOnlyList<SevenZipArchiveEntry> entries,
        Func<SevenZipArchiveEntry, ExtractionTarget?> targetFactory,
        string? password,
        CancellationToken cancellationToken,
        IProgress<SevenZipProgress>? progress)
    {
        _entries = entries;
        _targetFactory = targetFactory;
        _password = password;
        _cancellationToken = cancellationToken;
        _progress = progress;
    }

    public int SetTotal(ulong total)
    {
        _totalBytes = total;
        _progress?.Report(new SevenZipProgress(null, total));
        return CheckCancellation();
    }

    public unsafe int SetCompleted(nint completeValue)
    {
        ulong? completed = completeValue == 0 ? null : *(ulong*)completeValue;
        _progress?.Report(new SevenZipProgress(completed, _totalBytes));
        return CheckCancellation();
    }

    public int GetStream(uint index, out ISequentialOutStream? outStream, int askExtractMode)
    {
        outStream = null;

        try
        {
            _cancellationToken.ThrowIfCancellationRequested();
            DisposeCurrentStream();

            if (askExtractMode != (int)ExtractAskMode.Extract)
            {
                return HResults.S_OK;
            }

            if (index >= _entries.Count)
            {
                return HResults.E_INVALIDARG;
            }

            var target = _targetFactory(_entries[checked((int)index)]);
            if (target is null)
            {
                return HResults.S_OK;
            }

            _currentStream = new ManagedOutStream(target.Value.Stream, target.Value.LeaveOpen);
            outStream = _currentStream;
            return HResults.S_OK;
        }
        catch (Exception exception)
        {
            return Fail(exception);
        }
    }

    public int PrepareOperation(int askExtractMode) => CheckCancellation();

    public int SetOperationResult(int operationResult)
    {
        try
        {
            _currentStream?.ThrowIfFailed();
            DisposeCurrentStream();

            if (operationResult != (int)ExtractOperationResult.Ok)
            {
                return Fail(new InvalidDataException($"7-Zip extraction failed: {(ExtractOperationResult)operationResult}."));
            }

            return CheckCancellation();
        }
        catch (Exception exception)
        {
            return Fail(exception);
        }
    }

    public int CryptoGetTextPassword(out string password)
    {
        password = _password ?? string.Empty;
        return _password is null ? HResults.E_ABORT : HResults.S_OK;
    }

    public void Dispose() => DisposeCurrentStream();

    private int CheckCancellation() => _cancellationToken.IsCancellationRequested
        ? Fail(new OperationCanceledException(_cancellationToken))
        : HResults.S_OK;

    private void DisposeCurrentStream()
    {
        _currentStream?.Dispose();
        _currentStream = null;
    }
}
