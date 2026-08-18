using System.Runtime.InteropServices.Marshalling;

namespace SevenZipSharp.Interop;

[GeneratedComClass]
internal sealed partial class ManagedInStream : CallbackBase, IInStream, IDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    internal ManagedInStream(Stream stream, bool leaveOpen)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
    }

    public unsafe int Read(nint data, uint size, nint processedSize)
    {
        try
        {
            if (processedSize != 0)
            {
                *(uint*)processedSize = 0;
            }

            if (size == 0)
            {
                return HResults.S_OK;
            }

            var read = _stream.Read(new Span<byte>((void*)data, checked((int)size)));
            if (processedSize != 0)
            {
                *(uint*)processedSize = checked((uint)read);
            }

            return HResults.S_OK;
        }
        catch (Exception exception)
        {
            return Fail(exception);
        }
    }

    public unsafe int Seek(long offset, uint seekOrigin, nint newPosition)
    {
        try
        {
            if (seekOrigin > 2)
            {
                return HResults.E_INVALIDARG;
            }

            var position = _stream.Seek(offset, (SeekOrigin)seekOrigin);
            if (newPosition != 0)
            {
                *(ulong*)newPosition = checked((ulong)position);
            }

            return HResults.S_OK;
        }
        catch (Exception exception)
        {
            return Fail(exception);
        }
    }

    public void Dispose()
    {
        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }
}
