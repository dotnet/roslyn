// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using System.Text;

namespace Microsoft.CodeAnalysis.SemanticSearch.Extensions;

/// <summary>
/// Models project information not tracked by Compilation.
/// </summary>
public sealed class ProjectModel
{
    private readonly Lazy<ImmutableDictionary<string, ResxFile>> _lazyResxFiles;

    public string FilePath { get; }

    public ProjectModel(string filePath)
    {
        FilePath = filePath;
        _lazyResxFiles = new(LoadResxFiles, isThreadSafe: true);
    }

    public ProjectModel(string filePath, ImmutableDictionary<string, ResxFile> resxFiles)
    {
        FilePath = filePath;
        _lazyResxFiles = new(() => resxFiles, isThreadSafe: true);
    }

    public ImmutableDictionary<string, ResxFile> ResxFiles
        => _lazyResxFiles.Value;

    public ProjectModel ReplaceResxFile(ResxFile file)
        => new(FilePath, ResxFiles.SetItem(file.FilePath, file));

    internal ImmutableDictionary<string, ResxFile> LoadResxFiles()
    {
        var resxFiles = ImmutableDictionary.CreateBuilder<string, ResxFile>();
        var projectDirectory = Path.GetDirectoryName(FilePath)!;

        // TODO: get EmbeddedResources items from msbuild instead
        foreach (var filePath in Directory.EnumerateFileSystemEntries(projectDirectory, "*.resx", SearchOption.AllDirectories))
        {
            resxFiles.Add(filePath, ResxFile.ReadFromFile(filePath));
        }

        return resxFiles.ToImmutable();
    }

    public static IEnumerable<(string filePath, string? newContent)> GetChanges(ProjectModel oldModel, ProjectModel newModel)
    {
        if (!oldModel._lazyResxFiles.IsValueCreated && !newModel._lazyResxFiles.IsValueCreated)
        {
            yield break;
        }

        foreach (var (filePath, newResx) in newModel.ResxFiles)
        {
            var newContent = newResx.GetContent();

            if (oldModel.ResxFiles.TryGetValue(filePath, out var oldResx) && newContent == oldResx.GetContent())
            {
                continue;
            }

            // new or updated resx file:
            yield return (filePath, newContent);
        }

        foreach (var (filePath, _) in oldModel.ResxFiles)
        {
            if (!newModel.ResxFiles.ContainsKey(filePath))
            {
                // deleted resx file:
                yield return (filePath, null);
            }
        }
    }
}

public sealed class ResxFile
{
    public string FilePath { get; }

    private readonly ImmutableDictionary<string, string> _changes;

    internal ResxFile(string filePath, ImmutableDictionary<string, string> changes)
    {
        FilePath = filePath;
        _changes = changes;
    }

    internal static ResxFile ReadFromFile(string filePath)
    {
        return new ResxFile(filePath, changes: ImmutableDictionary<string, string>.Empty);
    }

    public ResxFile AddString(string name, string value)
        => new(FilePath, _changes.SetItem(name, value));

    public string GetContent()
    {
        if (_changes.Count == 0)
        {
            return File.ReadAllText(FilePath, Encoding.UTF8);
        }

        var newDocument = XDocument.Load(FilePath, LoadOptions.None);

        foreach (var (name, value) in _changes)
        {
            newDocument.Root!.Add(new XElement("data",
                new XAttribute("name", name),
                new XAttribute(XNamespace.Xml + "space", "preserve"),
                new XElement("value", value)
            ));
        }

        using var stream = new MemoryStream();

        using var xmlWriter = XmlWriter.Create(stream, new()
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            IndentChars = "\t",
            NewLineChars = "\r\n",
            NewLineOnAttributes = false,
        });

        newDocument.Save(xmlWriter);
        xmlWriter.Close();

        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
#endif
