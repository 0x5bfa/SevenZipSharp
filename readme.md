# SevenZipSharp

SevenZipSharp is a ground-up .NET 10 wrapper around the official 7-Zip archive
engine. It is designed for trimming and NativeAOT from the first commit and
does not preserve the API of the earlier SevenZipSharp project.

The single NuGet package contains the official 7-Zip 26.02 `7z.dll` for
Windows x86, x64, and ARM64. NuGet selects the native asset for the consuming
application's runtime identifier, so applications do not need a separate
native-assets package.

## Read and extract an archive

```csharp
using SevenZipSharp;

using var library = new SevenZipLibrary();
using var archive = library.Open("example.7z");

foreach (var entry in archive.Entries)
{
    Console.WriteLine($"{entry.Path} ({entry.Size} bytes)");
}

archive.ExtractToDirectory("output");
```

For a replaceable or system-provided engine, set an explicit path:

```csharp
using var library = new SevenZipLibrary(new SevenZipLibraryOptions
{
    NativeLibraryPath = @"C:\Program Files\7-Zip\7z.dll",
});
```

The native DLL stays a separate runtime file; it is not statically linked into
the managed assembly or NativeAOT executable.

## Current rewrite scope

- Dynamic discovery of formats exported by `7z.dll`
- Archive opening from paths or seekable streams
- Entry metadata and password callbacks
- Extraction to streams or traversal-safe directories
- Cancellation and progress callbacks
- Source-generated COM interop with runtime marshalling disabled
- Framework-dependent and NativeAOT smoke coverage in CI

Archive creation and multi-volume input are intentionally not part of this
first vertical slice of the rewrite.

## Licensing

SevenZipSharp's managed wrapper remains licensed under LGPL-3.0-only. The
bundled 7-Zip engine has its own upstream terms: mostly LGPL-2.1-or-later,
with BSD-licensed components and an unRAR restriction. The NuGet package
contains the complete license notices and the corresponding 7-Zip 26.02
source archive under `licenses/`.

A consuming application may use its own license, including a proprietary one,
provided it complies with the licenses of SevenZipSharp and 7-Zip. Legal advice
for a particular distribution should come from qualified counsel.
