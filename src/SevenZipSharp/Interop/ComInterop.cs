using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SevenZipSharp.Interop;

internal static class ComInterop
{
    private static readonly StrategyBasedComWrappers Wrappers = new();

    internal static T WrapOwned<T>(nint instance)
        where T : class
    {
        if (instance == 0)
        {
            throw new InvalidOperationException("7-Zip returned a null COM interface.");
        }

        try
        {
            return (T)Wrappers.GetOrCreateObjectForComInstance(instance, CreateObjectFlags.UniqueInstance);
        }
        catch
        {
            Marshal.Release(instance);
            throw;
        }
    }

    internal static void FinalRelease(object instance)
    {
        if (instance is ComObject comObject)
        {
            comObject.FinalRelease();
        }
    }
}
