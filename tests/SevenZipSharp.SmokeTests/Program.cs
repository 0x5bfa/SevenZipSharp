using System.Formats.Tar;
using System.Text;
using SevenZipSharp;

var archivePath = Path.Combine(AppContext.BaseDirectory, "TestData", "multiple_files.7z");
var nativeLibraryPath = Path.Combine(AppContext.BaseDirectory, "7z.dll");
var extractionRoot = Path.Combine(Path.GetTempPath(), "SevenZipSharp-SmokeTests", Guid.NewGuid().ToString("N"));

try
{
    using var library = new SevenZipLibrary(new SevenZipLibraryOptions
    {
        NativeLibraryPath = nativeLibraryPath,
    });

    Assert(library.Formats.Count > 20, "Expected the bundled 7-Zip library to expose archive formats.");
    Assert(library.Formats.Any(format => format.Extensions.Contains("7z", StringComparer.OrdinalIgnoreCase)),
        "Expected a 7z handler.");

    using var archive = library.Open(archivePath);
    Assert(archive.Format.Extensions.Contains("7z", StringComparer.OrdinalIgnoreCase), "Expected the 7z format.");
    Assert(archive.Entries.Count == 3, "Expected three archive entries.");
    Assert(archive.Entries.All(static entry => entry.Size == 5), "Expected five-byte entries.");

    using var output = new MemoryStream();
    archive.ExtractTo(archive.Entries[0], output);
    Assert(Encoding.UTF8.GetString(output.ToArray()) == "file1", "Stream extraction returned unexpected data.");

    using (var cancellation = new CancellationTokenSource())
    {
        cancellation.Cancel();
        AssertThrows<OperationCanceledException>(() =>
            archive.ExtractTo(archive.Entries[0], Stream.Null, cancellationToken: cancellation.Token));
    }

    archive.ExtractToDirectory(extractionRoot);
    for (var index = 1; index <= 3; index++)
    {
        var contents = File.ReadAllText(Path.Combine(extractionRoot, $"file{index}.txt"));
        Assert(contents == $"file{index}", $"Directory extraction failed for file{index}.txt.");
    }

    using (var archiveStream = File.OpenRead(archivePath))
    {
        using (library.Open(archiveStream, new SevenZipOpenOptions
        {
            FileExtension = "7z",
            LeaveOpen = true,
        }))
        {
        }

        Assert(archiveStream.CanRead, "LeaveOpen should preserve the caller-owned stream.");
    }

    using var maliciousTar = CreateTraversalArchive();
    using var maliciousArchive = library.Open(maliciousTar, new SevenZipOpenOptions
    {
        FileExtension = "tar",
        LeaveOpen = true,
    });
    var maliciousDestination = Path.Combine(extractionRoot, "malicious");
    AssertThrows<InvalidDataException>(() => maliciousArchive.ExtractToDirectory(maliciousDestination));
    Assert(!File.Exists(Path.Combine(extractionRoot, "escape.txt")),
        "A traversal entry escaped the extraction directory.");

    Console.WriteLine($"SevenZipSharp smoke tests passed with {library.Formats.Count} formats.");
}
finally
{
    if (Directory.Exists(extractionRoot))
    {
        Directory.Delete(extractionRoot, recursive: true);
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

static MemoryStream CreateTraversalArchive()
{
    var stream = new MemoryStream();
    using (var writer = new TarWriter(stream, leaveOpen: true))
    using (var contents = new MemoryStream("escape"u8.ToArray()))
    {
        var entry = new PaxTarEntry(TarEntryType.RegularFile, "../escape.txt")
        {
            DataStream = contents,
        };
        writer.WriteEntry(entry);
    }

    stream.Position = 0;
    return stream;
}
