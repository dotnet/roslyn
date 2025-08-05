// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.MoveToResx;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpMoveToResxCodeFixProvider)), Shared]
internal sealed class CSharpMoveToResxCodeFixProvider : CodeFixProvider
{
    [ImportingConstructor]
    [Obsolete("Do not call directly. Use dependency injection or MEF.", error: true)]
    public CSharpMoveToResxCodeFixProvider() { }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.MoveToResxDiagnosticId];

    public override FixAllProvider GetFixAllProvider()
        => new CSharpMoveToResxFixAllProvider();

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) as LiteralExpressionSyntax;
        if (node is null)
            return;

        var allResx = GetAllResxFiles(context.Document.Project).ToList();
        if (allResx.Count == 0)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CSharpFeaturesResources.No_resx_file_found_Please_create_a_resx_file_in_your_project_or_folder,
                    createChangedDocument: c => Task.FromResult(context.Document), // No-op
                    equivalenceKey: "NoResxFileFound"),
                diagnostic);
            return;
        }

        var nestedActions = allResx
            .Select(resx =>
            {
                var resxName = Path.GetFileName(resx.FilePath) ?? CSharpFeaturesResources.Unnamed_Resource;
                return CodeAction.Create(
                    title: resxName,
                    createChangedSolution: c => MoveStringToResxAndReplaceLiteralAsync(context.Document, node, resx, c),
                    equivalenceKey: $"MoveStringTo_{resxName}");
            })
            .ToImmutableArray();

        context.RegisterCodeFix(
            CodeAction.Create(
                CSharpFeaturesResources.Move_string_to_resx_resource,
                nestedActions,
                isInlinable: false),
            diagnostic);
    }

    private static IEnumerable<TextDocument> GetAllResxFiles(Project project)
    {
        return project.AdditionalDocuments
            .Where(f => f.FilePath != null && string.Equals(".resx", Path.GetExtension(f.FilePath), StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<Solution> MoveStringToResxAndReplaceLiteralAsync(
    Document document,
    LiteralExpressionSyntax stringLiteral,
    TextDocument resxFile,
    CancellationToken cancellationToken)
    {
        var resxSourceText = await resxFile.GetTextAsync(cancellationToken).ConfigureAwait(false);
        
        XDocument xdoc;
        try
        {
            xdoc = XDocument.Parse(resxSourceText.ToString());
        }
        catch (System.Xml.XmlException)
        {
            // If the resx file is malformed, we can't safely modify it
            // Return the original solution unchanged
            return document.Project.Solution;
        }

        // Ensure we have a valid root element
        if (xdoc.Root == null)
        {
            return document.Project.Solution;
        }

        var value = stringLiteral.Token.ValueText;
        var name = ToDeterministicResourceKey(value);

        // Check if the value already exists in the .resx file (regardless of name)
        var existingDataElement = xdoc.Root?.Elements("data")
            .FirstOrDefault(e => e.Element("value")?.Value == value);

        bool resxNeedsUpdate = false;

        if (existingDataElement is not null)
        {
            // Use the existing name and do NOT update the .resx
            var nameAttribute = existingDataElement.Attribute("name");
            name = nameAttribute?.Value ?? name;
        }
        else
        {
            resxNeedsUpdate = true;
            var dataElement = xdoc.Root?.Elements("data")
                .FirstOrDefault(e => (string?)e.Attribute("name") == name);

            if (dataElement is null)
            {
                dataElement = new XElement("data",
                    new XAttribute("name", name),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", value));
                xdoc.Root?.Add(dataElement);
            }
            else
            {
                // If the key exists but with a different value, update it
                if (dataElement.Element("value")?.Value != value)
                {
                    var valueElement = dataElement.Element("value");
                    if (valueElement != null)
                    {
                        valueElement.Value = value;
                    }
                }
            }
        }

        var resourceClass = Path.GetFileNameWithoutExtension(resxFile.Name);
        var defaultNamespace = document.Project.DefaultNamespace;
        string resourceAccessString = !string.IsNullOrEmpty(defaultNamespace)
            ? $"{defaultNamespace}.{resourceClass}.{name}"
            : $"{resourceClass}.{name}";

        var resourceAccess = SyntaxFactory.ParseExpression(resourceAccessString).WithTriviaFrom(stringLiteral);

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root.ReplaceNode(stringLiteral, resourceAccess);

        Solution newSolution;
        if (resxNeedsUpdate)
        {
            string updatedResxText;
            using (var sw = new StringWriter())
            {
                xdoc.Save(sw);
                updatedResxText = sw.ToString();
            }

            var newResxSourceText = SourceText.From(updatedResxText, resxSourceText.Encoding);
            newSolution = document.Project.Solution
                .WithAdditionalDocumentText(resxFile.Id, newResxSourceText)
                .WithDocumentSyntaxRoot(document.Id, newRoot);
        }
        else
        {
            newSolution = document.Project.Solution
                .WithDocumentSyntaxRoot(document.Id, newRoot);
        }

        return newSolution;
    }

    private static string ToDeterministicResourceKey(string value, int maxLength = 60)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "emptyString";

        var words = new List<string>();
        var stringBuilder = new StringBuilder();
        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c))
            {
                stringBuilder.Append(c);
            }
            else if (stringBuilder.Length > 0)
            {
                words.Add(stringBuilder.ToString().ToLowerInvariant());
                stringBuilder.Clear();
            }
        }
        if (stringBuilder.Length > 0)
            words.Add(stringBuilder.ToString().ToLowerInvariant());

        using var _ = PooledStringBuilder.GetInstance(out var keyBuilder);
        for (int i = 0; i < words.Count; i++)
        {
            var word = words[i];
            if (i == 0)
            {
                // Check if adding the first word would exceed maxLength
                if (word.Length > maxLength)
                {
                    keyBuilder.Append(word.Substring(0, maxLength));
                    break;
                }
                keyBuilder.Append(word);
            }
            else if (word.Length > 0)
            {
                var capitalizedWord = char.ToUpperInvariant(word[0]) + word.Substring(1);

                // Check if adding this word would exceed maxLength
                if (keyBuilder.Length + capitalizedWord.Length > maxLength)
                {
                    // Add as much of the word as possible without exceeding maxLength
                    var remainingLength = maxLength - keyBuilder.Length;
                    if (remainingLength > 0)
                    {
                        keyBuilder.Append(capitalizedWord.Substring(0, remainingLength));
                    }
                    break;
                }

                keyBuilder.Append(capitalizedWord);
            }
        }

        var key = keyBuilder.ToString();
        if (key.Length == 0)
            key = "emptyString";
        else if (char.IsDigit(key[0]))
            key = "_" + key;

        return key;
    }

    private class CSharpMoveToResxFixAllProvider : FixAllProvider
    {
        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
            => new[] { FixAllScope.Document };

        public override async Task<CodeAction?> GetFixAsync(FixAllContext context)
        {
            var document = context.Document;
            if (document is null)
                return null;

            // Get the diagnostics map for this document specifically
            var documentsAndDiagnosticsToFixMap = await context.GetDocumentDiagnosticsToFixAsync().ConfigureAwait(false);
            if (!documentsAndDiagnosticsToFixMap.TryGetValue(document, out var diagnostics) || diagnostics.IsEmpty)
                return null;

            var project = document.Project;
            var allResx = GetAllResxFiles(project).ToList();
            if (allResx.Count == 0)
                return null;

            var resx = allResx.First();

            return CodeAction.Create(
                CSharpFeaturesResources.Move_all_strings_to_resx_resource,
                async ct =>
                {
                    // Collect all string literals and their values first
                    var root = await document.GetRequiredSyntaxRootAsync(ct).ConfigureAwait(false);
                    var literalsToFix = new List<(LiteralExpressionSyntax literal, string value, string resourceKey)>();

                    foreach (var diagnostic in diagnostics)
                    {
                        var node = root?.FindNode(diagnostic.Location.SourceSpan) as LiteralExpressionSyntax;
                        if (node is not null)
                        {
                            var value = node.Token.ValueText;
                            var resourceKey = ToDeterministicResourceKey(value);
                            literalsToFix.Add((node, value, resourceKey));
                        }
                    }

                    if (literalsToFix.Count == 0)
                        return project.Solution;

                    // Process resx file once to add all needed entries
                    var currentSolution = project.Solution;
                    var currentResx = resx;
                    var resxSourceText = await currentResx.GetTextAsync(ct).ConfigureAwait(false);

                    XDocument xdoc;
                    try
                    {
                        xdoc = XDocument.Parse(resxSourceText.ToString());
                    }
                    catch (System.Xml.XmlException)
                    {
                        // If the resx file is malformed, we can't safely modify it
                        // Return the original solution unchanged
                        return project.Solution;
                    }

                    // Ensure we have a valid root element
                    if (xdoc.Root == null)
                    {
                        return project.Solution;
                    }

                    bool resxNeedsUpdate = false;

                    // Collect all the resource keys we need to add
                    var resourcesToAdd = new Dictionary<string, string>();
                    var resourceKeysToUse = new Dictionary<LiteralExpressionSyntax, string>();

                    foreach (var literalInfo in literalsToFix)
                    {
                        var literal = literalInfo.literal;
                        var value = literalInfo.value;
                        var resourceKey = literalInfo.resourceKey;

                        // Check if the value already exists in the .resx file
                        var existingDataElement = xdoc.Root.Elements("data")
                            .FirstOrDefault(e => e.Element("value")?.Value == value);

                        if (existingDataElement is not null)
                        {
                            // Use the existing name
                            var nameAttribute = existingDataElement.Attribute("name");
                            var existingName = nameAttribute?.Value ?? resourceKey;
                            resourceKeysToUse[literal] = existingName;
                        }
                        else
                        {
                            // Check if this resource key already exists with a different value
                            var existingKeyElement = xdoc.Root.Elements("data")
                                .FirstOrDefault(e => (string?)e.Attribute("name") == resourceKey);

                            if (existingKeyElement is null)
                            {
                                // New resource entry needed
                                resourcesToAdd[resourceKey] = value;
                                resourceKeysToUse[literal] = resourceKey;
                                resxNeedsUpdate = true;
                            }
                            else
                            {
                                // Key exists but with different value, update it
                                var valueElement = existingKeyElement.Element("value");
                                if (valueElement != null)
                                {
                                    valueElement.Value = value;
                                }
                                resourceKeysToUse[literal] = resourceKey;
                                resxNeedsUpdate = true;
                            }
                        }
                    }

                    // Add new resource entries to resx
                    foreach (var kvp in resourcesToAdd)
                    {
                        var key = kvp.Key;
                        var value = kvp.Value;
                        var dataElement = new XElement("data",
                            new XAttribute("name", key),
                            new XAttribute(XNamespace.Xml + "space", "preserve"),
                            new XElement("value", value));
                        xdoc.Root.Add(dataElement);
                    }

                    // Update resx file if needed
                    if (resxNeedsUpdate)
                    {
                        string updatedResxText;
                        using (var sw = new StringWriter())
                        {
                            xdoc.Save(sw);
                            updatedResxText = sw.ToString();
                        }

                        var newResxSourceText = SourceText.From(updatedResxText, resxSourceText.Encoding);
                        currentSolution = currentSolution.WithAdditionalDocumentText(currentResx.Id, newResxSourceText);
                    }

                    // Replace all string literals with resource access
                    var currentDocument = currentSolution.GetDocument(document.Id)!;
                    var currentRoot = await currentDocument.GetSyntaxRootAsync(ct).ConfigureAwait(false);

                    var resourceClass = Path.GetFileNameWithoutExtension(currentResx.Name);
                    var ns = currentDocument.Project.DefaultNamespace;

                    // Use ReplaceNodes (plural) to replace all nodes at once
                    var newRoot = currentRoot.ReplaceNodes(
                        resourceKeysToUse.Keys,
                        (originalNode, rewrittenNode) =>
                        {
                            var resourceKey = resourceKeysToUse[originalNode];
                            string resourceAccessString = !string.IsNullOrEmpty(ns)
                                ? $"{ns}.{resourceClass}.{resourceKey}"
                                : $"{resourceClass}.{resourceKey}";

                            return SyntaxFactory.ParseExpression(resourceAccessString).WithTriviaFrom(originalNode);
                        });

                    return currentSolution.WithDocumentSyntaxRoot(currentDocument.Id, newRoot);
                },
                nameof(CSharpMoveToResxFixAllProvider));
        }
    }
}
