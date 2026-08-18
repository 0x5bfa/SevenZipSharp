using System.Runtime.InteropServices.Marshalling;

namespace SevenZipSharp.Interop;

[GeneratedComClass]
internal sealed partial class ManagedOutStream : CallbackBase, ISequentialOutStream, IDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    internal ManagedOutStream(Stream stream, bool leaveOpen)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
    }

    public unsafe int Write(nint data, uint size, nint processedSize)
    {
        try
        {
            if (processedSize != 0)
            {
                *(uint*)processedSize = 0;
            }

            if (size != 0)
            {
                _stream.Write(new ReadOnlySpan<byte>((void*)data, checked((int)size)));
            }

            if (processedSize != 0)
            {
                *(uint*)processedSize = size;
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
