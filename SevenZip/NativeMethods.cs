[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]

namespace SevenZip
{
    using System;
    using System.Runtime.InteropServices;

#if UNMANAGED
    internal static partial class NativeMethods
    {
        [LibraryImport("kernel32.dll", EntryPoint = "LoadLibraryW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial IntPtr LoadLibrary(string fileName);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool FreeLibrary(IntPtr hModule);

        [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr GetProcAddress(IntPtr hModule, string procName);

        [LibraryImport("ole32.dll")]
        public static partial int PropVariantClear(ref PropVariant propVariant);

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
