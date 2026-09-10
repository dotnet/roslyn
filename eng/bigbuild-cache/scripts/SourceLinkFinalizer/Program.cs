// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

var options = Parse(args);
var marker = RequiredSha(options, "--marker");
var value = RequiredSha(options, "--value");

if (marker == value)
{
    throw new InvalidOperationException("Marker and value must differ.");
}

if (options.ContainsKey("--root"))
{
    FinalizeRoot(
        RequiredDirectory(options, "--root"),
        RequiredOutputPath(options, "--output-root"),
        marker,
        value);
    return 0;
}

FinalizePair(
    RequiredPath(options, "--pe"),
    RequiredPath(options, "--pdb"),
    RequiredOutputPath(options, "--out-pe"),
    RequiredOutputPath(options, "--out-pdb"),
    marker,
    value);

return 0;

static void FinalizeRoot(
    string root,
    string outputRoot,
    string marker,
    string value)
{
    if (IsWithin(outputRoot, root))
    {
        throw new InvalidOperationException("The output root must be outside the input root.");
    }
    if (Directory.Exists(outputRoot) || File.Exists(outputRoot))
    {
        throw new InvalidOperationException("The output root must not already exist.");
    }

    RejectMarkerBearingEmbeddedPdbs(root, marker);

    var pairs = new List<(string Pe, string Pdb)>();
    foreach (var pdbPath in Directory.EnumerateFiles(root, "*.pdb", SearchOption.AllDirectories))
    {
        if (!PortablePdbContainsMarker(pdbPath, marker))
        {
            continue;
        }

        var candidates = new[]
        {
            Path.ChangeExtension(pdbPath, ".dll"),
            Path.ChangeExtension(pdbPath, ".exe"),
        }.Where(File.Exists).ToArray();
        if (candidates.Length != 1)
        {
            throw new InvalidOperationException(
                $"Marker-bearing PDB '{pdbPath}' has {candidates.Length} sibling PE files.");
        }

        pairs.Add((candidates[0], pdbPath));
    }

    if (pairs.Count == 0)
    {
        throw new InvalidOperationException("The closure contains no marker-bearing external Portable PDB.");
    }

    var stagingRoot = outputRoot + ".staging." + Guid.NewGuid().ToString("N");
    try
    {
        foreach (var (pePath, pdbPath) in pairs.OrderBy(pair => pair.Pdb, StringComparer.Ordinal))
        {
            var relativePdb = Path.GetRelativePath(root, pdbPath);
            var relativePe = Path.GetRelativePath(root, pePath);
            FinalizePair(
                pePath,
                pdbPath,
                Path.Combine(stagingRoot, relativePe),
                Path.Combine(stagingRoot, relativePdb),
                marker,
                value);
        }

        Directory.Move(stagingRoot, outputRoot);
    }
    catch
    {
        if (Directory.Exists(stagingRoot))
        {
            Directory.Delete(stagingRoot, recursive: true);
        }

        throw;
    }

    Console.WriteLine($"Finalized files: {pairs.Count}");
    Console.WriteLine($"Output root: {outputRoot}");
}

static void FinalizePair(
    string pePath,
    string pdbPath,
    string outputPePath,
    string outputPdbPath,
    string marker,
    string value)
{
    if (Path.GetFullPath(pePath).Equals(outputPePath, PathComparison())
        || Path.GetFullPath(pdbPath).Equals(outputPdbPath, PathComparison()))
    {
        throw new InvalidOperationException("Output paths must differ from their inputs.");
    }

    var pdb = FinalizePdb(File.ReadAllBytes(pdbPath), marker, value);
    var pe = FinalizePe(File.ReadAllBytes(pePath), pdb);

    Directory.CreateDirectory(Path.GetDirectoryName(outputPdbPath)!);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPePath)!);
    File.WriteAllBytes(outputPdbPath, pdb.Content);
    File.WriteAllBytes(outputPePath, pe);

    Console.WriteLine($"SourceLink: {pdb.SourceLink}");
    Console.WriteLine($"PDB ID: {pdb.ContentId.Guid:D}/{pdb.ContentId.Stamp:x8}");
    Console.WriteLine($"MVID: {ReadMvid(pe):D}");
    Console.WriteLine($"PDB: {outputPdbPath}");
    Console.WriteLine($"PE: {outputPePath}");
}

static bool PortablePdbContainsMarker(string path, string marker)
{
    try
    {
        using var provider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(path));
        return GetSourceLink(provider.GetMetadataReader())?.Contains(marker, StringComparison.Ordinal)
            == true;
    }
    catch (BadImageFormatException)
    {
        return false;
    }
}

static void RejectMarkerBearingEmbeddedPdbs(string root, string marker)
{
    foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        .Where(path => Path.GetExtension(path) is ".dll" or ".exe"))
    {
        try
        {
            using var peReader = new PEReader(File.OpenRead(path));
            foreach (var entry in peReader.ReadDebugDirectory()
                .Where(entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb))
            {
                using var provider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
                if (GetSourceLink(provider.GetMetadataReader())?.Contains(marker, StringComparison.Ordinal)
                    == true)
                {
                    throw new InvalidOperationException(
                        $"Marker-bearing embedded Portable PDB is not supported: '{path}'.");
                }
            }
        }
        catch (BadImageFormatException)
        {
        }
    }
}

static FinalizedPdb FinalizePdb(byte[] input, string marker, string value)
{
    var content = (byte[])input.Clone();
    using var provider = MetadataReaderProvider.FromPortablePdbStream(
        new MemoryStream(content, writable: false));
    var reader = provider.GetMetadataReader();
    var sourceLinkInformation = GetSourceLinkInformation(reader)
        ?? throw new InvalidOperationException("The Portable PDB has no SourceLink record.");
    var sourceLinkBytes = reader.GetBlobBytes(sourceLinkInformation.Value);
    var sourceLink = Encoding.UTF8.GetString(sourceLinkBytes);
    var updatedSourceLink = sourceLink.Replace(marker, value, StringComparison.Ordinal);

    if (sourceLink == updatedSourceLink)
    {
        throw new InvalidOperationException("The SourceLink record does not contain the marker.");
    }
    if (Encoding.UTF8.GetByteCount(updatedSourceLink) != sourceLinkBytes.Length)
    {
        throw new InvalidOperationException("The updated SourceLink record must have the same UTF-8 width.");
    }
    if (Count(sourceLink, marker) != 1)
    {
        throw new InvalidOperationException("The SourceLink record must contain the marker exactly once.");
    }

    var oldId = reader.DebugMetadataHeader?.Id.ToArray()
        ?? throw new InvalidOperationException("The Portable PDB has no debug metadata ID.");
    var idOffset = FindUnique(content, oldId, "Portable PDB ID");
    var originalHashInput = (byte[])input.Clone();
    originalHashInput.AsSpan(idOffset, oldId.Length).Clear();
    var originalHash = SHA256.HashData(originalHashInput);
    var originalContentId = ReadContentId(oldId);

    var sourceLinkOffset = FindUnique(content, sourceLinkBytes, "SourceLink blob");
    Encoding.UTF8.GetBytes(updatedSourceLink).CopyTo(content, sourceLinkOffset);
    content.AsSpan(idOffset, oldId.Length).Clear();

    var hash = SHA256.HashData(content);
    var contentId = BlobContentId.FromHash(hash);
    WriteContentId(content.AsSpan(idOffset, oldId.Length), contentId);

    return new FinalizedPdb(
        content,
        originalContentId,
        originalHash,
        contentId,
        hash,
        updatedSourceLink);
}

static string? GetSourceLink(MetadataReader reader)
{
    var information = GetSourceLinkInformation(reader);
    return information is { } value
        ? Encoding.UTF8.GetString(reader.GetBlobBytes(value.Value))
        : null;
}

static CustomDebugInformation? GetSourceLinkInformation(MetadataReader reader)
{
    var matches = reader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
        .Select(reader.GetCustomDebugInformation)
        .Where(information =>
            reader.GetGuid(information.Kind) == new Guid("cc110556-a091-4d38-9fec-25ab9a351a6a"))
        .ToArray();
    return matches.Length switch
    {
        0 => null,
        1 => matches[0],
        _ => throw new InvalidOperationException("The Portable PDB has multiple SourceLink records."),
    };
}

static byte[] FinalizePe(byte[] input, FinalizedPdb pdb)
{
    var content = (byte[])input.Clone();
    using var peReader = new PEReader(new MemoryStream(content, writable: false));
    var headers = peReader.PEHeaders;
    var metadataReader = peReader.GetMetadataReader();
    var oldMvid = metadataReader.GetGuid(metadataReader.GetModuleDefinition().Mvid);
    var oldMvidBytes = oldMvid.ToByteArray();
    var mvidOffset = FindUnique(content, oldMvidBytes, "MVID");
    var coffStampOffset = headers.CoffHeaderStartOffset + sizeof(ushort) + sizeof(ushort);
    var debugTableOffset = RvaToFileOffset(
        headers,
        headers.PEHeader?.DebugTableDirectory.RelativeVirtualAddress
            ?? throw new InvalidOperationException("The PE has no debug directory."));
    var debugEntries = peReader.ReadDebugDirectory();
    if (debugEntries.Any(entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb))
    {
        throw new InvalidOperationException(
            "Embedded Portable PDB finalization requires a fixed-capacity compressed envelope and is not supported.");
    }

    var codeViewIndex = FindSingleIndex(debugEntries, DebugDirectoryEntryType.CodeView);
    var checksumIndex = FindSingleIndex(debugEntries, DebugDirectoryEntryType.PdbChecksum);
    var codeViewEntry = debugEntries[codeViewIndex];
    var checksumEntry = debugEntries[checksumIndex];
    var checksum = peReader.ReadPdbChecksumDebugDirectoryData(checksumEntry);
    var codeView = peReader.ReadCodeViewDebugDirectoryData(codeViewEntry);

    if (!string.Equals(checksum.AlgorithmName, "SHA256", StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"Unsupported PDB checksum algorithm '{checksum.AlgorithmName}'.");
    }
    if (codeView.Guid != pdb.OriginalContentId.Guid
        || codeViewEntry.Stamp != pdb.OriginalContentId.Stamp)
    {
        throw new InvalidOperationException("The PE CodeView entry does not identify the input Portable PDB.");
    }
    if (!checksum.Checksum.AsSpan().SequenceEqual(pdb.OriginalHash))
    {
        throw new InvalidOperationException("The PE PDB checksum does not match the input Portable PDB.");
    }

    RejectCompletedSignatures(content, headers);

    const int debugEntrySize = 28;
    var codeViewEntryOffset = debugTableOffset + codeViewIndex * debugEntrySize;
    BinaryPrimitives.WriteUInt32LittleEndian(
        content.AsSpan(codeViewEntryOffset + sizeof(uint), sizeof(uint)),
        pdb.ContentId.Stamp);
    pdb.ContentId.Guid.TryWriteBytes(content.AsSpan(codeViewEntry.DataPointer + sizeof(uint), 16));

    var checksumOffset =
        checksumEntry.DataPointer + Encoding.UTF8.GetByteCount(checksum.AlgorithmName) + 1;
    if (checksum.Checksum.Length != pdb.Hash.Length)
    {
        throw new InvalidOperationException("The PE PDB checksum width is not SHA-256.");
    }
    pdb.Hash.CopyTo(content, checksumOffset);

    content.AsSpan(coffStampOffset, sizeof(uint)).Clear();
    content.AsSpan(mvidOffset, oldMvidBytes.Length).Clear();
    var peId = BlobContentId.FromHash(SHA256.HashData(content));
    BinaryPrimitives.WriteUInt32LittleEndian(
        content.AsSpan(coffStampOffset, sizeof(uint)),
        peId.Stamp);
    peId.Guid.TryWriteBytes(content.AsSpan(mvidOffset, oldMvidBytes.Length));

    return content;
}

static void RejectCompletedSignatures(byte[] content, PEHeaders headers)
{
    if (headers.PEHeader?.CertificateTableDirectory.Size > 0)
    {
        throw new InvalidOperationException("An Authenticode-signed PE cannot be finalized.");
    }

    var strongName = headers.CorHeader?.StrongNameSignatureDirectory;
    if (strongName is not { Size: > 0 })
    {
        return;
    }

    var signatureOffset = RvaToFileOffset(headers, strongName.Value.RelativeVirtualAddress);
    if (content.AsSpan(signatureOffset, strongName.Value.Size).ContainsAnyExcept((byte)0))
    {
        throw new InvalidOperationException("A completed strong-name signature cannot be finalized.");
    }
}

static Guid ReadMvid(byte[] pe)
{
    using var reader = new PEReader(new MemoryStream(pe, writable: false));
    var metadata = reader.GetMetadataReader();
    return metadata.GetGuid(metadata.GetModuleDefinition().Mvid);
}

static int RvaToFileOffset(PEHeaders headers, int rva)
{
    foreach (var section in headers.SectionHeaders)
    {
        var size = Math.Max(section.VirtualSize, section.SizeOfRawData);
        if (rva >= section.VirtualAddress && rva < section.VirtualAddress + size)
        {
            return section.PointerToRawData + rva - section.VirtualAddress;
        }
    }

    throw new InvalidOperationException($"RVA 0x{rva:x8} is outside every PE section.");
}

static int FindSingleIndex(
    IReadOnlyList<DebugDirectoryEntry> entries,
    DebugDirectoryEntryType type)
{
    var indexes = Enumerable.Range(0, entries.Count)
        .Where(index => entries[index].Type == type)
        .ToArray();
    return indexes.Length == 1
        ? indexes[0]
        : throw new InvalidOperationException(
            $"Expected one {type} debug entry, found {indexes.Length}.");
}

static int FindUnique(byte[] content, byte[] value, string description)
{
    var offsets = new List<int>();
    for (var offset = 0; offset + value.Length <= content.Length; offset++)
    {
        if (content.AsSpan(offset, value.Length).SequenceEqual(value))
        {
            offsets.Add(offset);
        }
    }

    return offsets.Count == 1
        ? offsets[0]
        : throw new InvalidOperationException(
            $"Expected one physical {description}, found {offsets.Count}.");
}

static int Count(string content, string value)
{
    var count = 0;
    var offset = 0;
    while ((offset = content.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
    {
        count++;
        offset += value.Length;
    }

    return count;
}

static void WriteContentId(Span<byte> destination, BlobContentId contentId)
{
    if (destination.Length != 20)
    {
        throw new InvalidOperationException("Portable PDB content IDs must be 20 bytes.");
    }

    contentId.Guid.TryWriteBytes(destination[..16]);
    BinaryPrimitives.WriteUInt32LittleEndian(destination[16..], contentId.Stamp);
}

static BlobContentId ReadContentId(ReadOnlySpan<byte> source)
{
    if (source.Length != 20)
    {
        throw new InvalidOperationException("Portable PDB content IDs must be 20 bytes.");
    }

    return new BlobContentId(
        new Guid(source[..16]),
        BinaryPrimitives.ReadUInt32LittleEndian(source[16..]));
}

static Dictionary<string, string> Parse(string[] arguments)
{
    if ((arguments.Length & 1) != 0)
    {
        throw new ArgumentException("Options must be supplied as name/value pairs.");
    }

    var result = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var index = 0; index < arguments.Length; index += 2)
    {
        if (!result.TryAdd(arguments[index], arguments[index + 1]))
        {
            throw new ArgumentException($"Option '{arguments[index]}' was supplied more than once.");
        }
    }

    return result;
}

static string RequiredSha(IReadOnlyDictionary<string, string> options, string name)
{
    var value = Required(options, name);
    return value.Length == 40
        && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f')
        ? value
        : throw new ArgumentException($"{name} must be exactly 40 lowercase hexadecimal characters.");
}

static string RequiredPath(IReadOnlyDictionary<string, string> options, string name)
{
    var path = Path.GetFullPath(Required(options, name));
    return File.Exists(path)
        ? path
        : throw new FileNotFoundException($"{name} does not exist.", path);
}

static string RequiredDirectory(IReadOnlyDictionary<string, string> options, string name)
{
    var path = Path.GetFullPath(Required(options, name));
    return Directory.Exists(path)
        ? path
        : throw new DirectoryNotFoundException($"{name} does not exist: '{path}'.");
}

static string RequiredOutputPath(
    IReadOnlyDictionary<string, string> options,
    string name) =>
    Path.GetFullPath(Required(options, name));

static string Required(IReadOnlyDictionary<string, string> options, string name) =>
    options.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
        ? value
        : throw new ArgumentException($"Required option '{name}' was not supplied.");

static bool IsWithin(string candidate, string root)
{
    var relative = Path.GetRelativePath(root, candidate);
    return relative != ".."
        && !relative.StartsWith($"..{Path.DirectorySeparatorChar}", PathComparison())
        && !Path.IsPathRooted(relative);
}

static StringComparison PathComparison() =>
    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

sealed record FinalizedPdb(
    byte[] Content,
    BlobContentId OriginalContentId,
    byte[] OriginalHash,
    BlobContentId ContentId,
    byte[] Hash,
    string SourceLink);
