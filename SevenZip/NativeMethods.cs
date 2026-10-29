[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]

namespace SevenZip
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Windows.Win32;
    using Windows.Win32.Foundation;
    using Windows.Win32.System.Com.StructuredStorage;

#if UNMANAGED
    internal static partial class NativeMethods
    {
        public static unsafe IntPtr LoadLibrary(string fileName)
        {
            fixed (char* fileNamePointer = fileName)
            {
                return PInvoke.LoadLibrary((PCWSTR)fileNamePointer);
            }
        }

        public static bool FreeLibrary(IntPtr hModule)
        {
            return PInvoke.FreeLibrary((HMODULE)hModule);
        }

        public static unsafe IntPtr GetProcAddress(IntPtr hModule, string procName)
        {
            var procNameBytes = Encoding.ASCII.GetBytes(procName + '\0');
            fixed (byte* procNamePointer = procNameBytes)
            {
                return PInvoke.GetProcAddress((HMODULE)hModule, (PCSTR)procNamePointer);
            }
        }

        public static int PropVariantClear(ref PropVariant propVariant)
        {
            return PInvoke.PropVariantClear(ref Unsafe.As<PropVariant, PROPVARIANT>(ref propVariant)).Value;
        }

        public static T SafeCast<T>(PropVariant var, T def)
        {
            object obj;

            try
            {
                obj = var.Object;
            }
            catch (Exception)
            {
                return def;
            }

            if (obj is T expected)
            {
                return expected;
            }

            return def;
        }

        public static T GetProperty<T>(IInArchive archive, uint index, ItemPropId property, T defaultValue)
        {
            var value = new PropVariant();

            try
            {
                archive.GetProperty(index, property, ref value);
                return SafeCast(value, defaultValue);
            }
            finally
            {
                value.Clear();
            }
        }
    }
#endif
}
