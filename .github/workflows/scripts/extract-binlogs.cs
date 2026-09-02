#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Extract *.binlog entries from one Azure DevOps build-log artifact.
//
// The archive is produced by a PR-triggered build, so its entry paths, metadata
// and contents are untrusted. Two properties keep extraction safe:
//
//   * destination names are generated here, never taken from the archive, so a
//     traversal or absolute path cannot choose where bytes land;
//   * writing stops as soon as the caller's remaining byte budget is exceeded,
//     so a zip bomb cannot fill the runner disk.
//
// Entry paths and types are still validated up front - an archive containing a
// traversal path or a link/device entry is hostile rather than merely odd, so
// the whole artifact is rejected instead of partially extracted.
//
// Usage: dotnet run ./extract-binlogs.cs -- <archive> <destination> <prefix> <budget-bytes> [label]
// Prints "<extracted-count> <written-bytes>" on stdout; diagnostics go to stderr.

using System.IO.Compression;
using System.Text;

const int ChunkSize = 1024 * 1024;

if (args.Length is < 4 or > 5)
{
    Console.Error.WriteLine("usage: extract-binlogs.cs <archive> <destination> <prefix> <budget-bytes> [label]");
    return 1;
}

var archivePath = args[0];
var destination = args[1];
var prefix = args[2];
if (!long.TryParse(args[3], out var budgetBytes))
{
    Console.Error.WriteLine("budget-bytes must be an integer");
    return 1;
}

// Artifact names are untrusted build metadata, so re-sanitize here rather than
// trusting the caller: only the destination name generated in this process may
// decide where bytes land.
var label = args.Length == 5 ? args[4] : string.Empty;
var safeLabel = label.Length == 0 ? string.Empty : GetSafeLabel(label);

try
{
    using var zip = ZipFile.OpenRead(archivePath);

    // Validate every entry before reading any payload.
    for (var i = 0; i < zip.Entries.Count; i++)
    {
        var entry = zip.Entries[i];
        if (IsUnsafePath(entry.FullName))
        {
            Console.Error.WriteLine($"archive entry {i} has an unsafe path");
            return 1;
        }

        if (IsUnsupportedType(entry))
        {
            Console.Error.WriteLine($"archive entry {i} has an unsupported type");
            return 1;
        }
    }

    var selected = zip.Entries
        .Where(entry => !IsDirectoryEntry(entry) && entry.FullName.EndsWith(".binlog", StringComparison.OrdinalIgnoreCase))
        .ToArray();

    Directory.CreateDirectory(destination);

    var written = 0L;
    var buffer = new byte[ChunkSize];

    for (var index = 0; index < selected.Length; index++)
    {
        var stem = safeLabel.Length == 0 ? $"{prefix}_{index}" : $"{prefix}_{index}_{safeLabel}";
        var target = Path.Combine(destination, $"{stem}.binlog");

        using var source = selected[index].Open();

        // CreateNew, so a name that somehow already exists is an error rather
        // than a silent overwrite of a previous artifact's binlog.
        using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write);

        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            written += read;
            if (written > budgetBytes)
            {
                Console.Error.WriteLine("extracted binlogs exceed the remaining budget");
                return 1;
            }

            output.Write(buffer, 0, read);
        }
    }

    Console.Out.WriteLine($"{selected.Length} {written}");
    return 0;
}
catch (InvalidDataException ex)
{
    Console.Error.WriteLine($"archive could not be read: {ex.Message}");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"archive could not be extracted: {ex.Message}");
    return 1;
}

static string GetSafeLabel(string value)
{
    var builder = new StringBuilder(value.Length);
    foreach (var c in value)
    {
        builder.Append(char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' ? c : '_');
    }

    var result = builder.ToString().Trim('.', '_', '-');
    return result.Length > 80 ? result[..80] : result;
}

static bool IsUnsafePath(string name)
{
    if (name.Contains('\0'))
    {
        return true;
    }

    var normalized = name.Replace('\\', '/');
    if (normalized.StartsWith('/'))
    {
        return true;
    }

    var parts = normalized.Split('/');
    if (Array.IndexOf(parts, "..") >= 0)
    {
        return true;
    }

    // A Windows drive spec such as "c:/foo" or "c:foo" is not rooted by POSIX
    // rules but is still an attempt to escape the destination.
    var first = parts[0];
    return first.Length >= 2 && char.IsAsciiLetter(first[0]) && first[1] == ':';
}

static bool IsUnsupportedType(ZipArchiveEntry entry)
{
    // S_IFMT mask and the only entry types a log artifact may legitimately contain.
    const int FileTypeMask = 0xF000;
    const int RegularFile = 0x8000;
    const int DirectoryType = 0x4000;

    // Entries written on Windows carry no Unix mode; 0 means "unspecified",
    // which is not evidence of a hostile type.
    var mode = (entry.ExternalAttributes >> 16) & 0xFFFF;
    var fileType = mode & FileTypeMask;
    return fileType != 0 && fileType != RegularFile && fileType != DirectoryType;
}

static bool IsDirectoryEntry(ZipArchiveEntry entry)
    => entry.FullName.EndsWith('/') || entry.Name.Length == 0;
