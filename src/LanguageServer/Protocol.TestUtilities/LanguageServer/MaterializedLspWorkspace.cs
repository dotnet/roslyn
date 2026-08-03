// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Test.Utilities;

internal sealed class MaterializedLspWorkspace
{
    public LspWorkspaceContent Content { get; }
    public string RootPath { get; }
    public Dictionary<string, IList<LSP.Location>> AnnotatedLocations { get; }

    private MaterializedLspWorkspace(
        LspWorkspaceContent content,
        string rootPath,
        Dictionary<string, IList<LSP.Location>> annotatedLocations)
    {
        Content = content;
        RootPath = rootPath;
        AnnotatedLocations = annotatedLocations;
    }

    public static MaterializedLspWorkspace Create(
        TempRoot tempRoot,
        LspWorkspaceContent content,
        CancellationToken cancellationToken)
    {
        var rootPath = tempRoot.CreateDirectory().Path;
        var annotatedLocations = new Dictionary<string, IList<LSP.Location>>();

        foreach (var (relativePath, file) in content.Files)
        {
            var filePath = GetFullPath(rootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            cancellationToken.ThrowIfCancellationRequested();
            File.WriteAllText(filePath, file.Content);

            if (Path.GetExtension(relativePath).Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(filePath);
                AddAnnotatedLocations(
                    annotatedLocations,
                    GetAnnotatedLocations(documentUri, SourceText.From(file.Content), file.MarkupSpans));
            }
        }

        if (content.ShouldRestore)
        {
            foreach (var projectPath in content.Files.Keys.Where(static path => PathUtilities.GetExtension(path) == ".csproj"))
                ProcessUtilities.Run("dotnet", $"restore --project \"{GetFullPath(rootPath, projectPath)}\"");
        }

        return new MaterializedLspWorkspace(content, rootPath, annotatedLocations);
    }

    public string GetFullPath(string relativePath)
        => GetFullPath(RootPath, relativePath);

    private static string GetFullPath(string workspaceRootPath, string relativePath)
        => PathUtilities.CombinePathsUnchecked(workspaceRootPath, relativePath);

    private static Dictionary<string, IList<LSP.Location>> GetAnnotatedLocations(
        DocumentUri codeUri,
        SourceText text,
        IReadOnlyDictionary<string, ImmutableArray<TextSpan>> spanMap)
    {
        var locations = new Dictionary<string, IList<LSP.Location>>();
        foreach (var (name, spans) in spanMap)
        {
            locations[name] =
            [
                .. spans.Select(span => new LSP.Location
                {
                    DocumentUri = codeUri,
                    Range = ProtocolConversions.TextSpanToRange(span, text),
                })
            ];
        }

        return locations;
    }

    private static void AddAnnotatedLocations(
        Dictionary<string, IList<LSP.Location>> locations,
        Dictionary<string, IList<LSP.Location>> locationsToAdd)
    {
        foreach (var (name, newLocations) in locationsToAdd)
        {
            var locationsForName = locations.GetValueOrDefault(name, []);
            locationsForName.AddRange(newLocations);
            locations[name] = [.. locationsForName.Distinct()];
        }
    }
}
