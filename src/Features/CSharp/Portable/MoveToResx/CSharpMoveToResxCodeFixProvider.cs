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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ResxSelection;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MoveToResx;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MoveToResx), Shared]
internal sealed class CSharpMoveToResxCodeFixProvider : CodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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

        var validResxFiles = await GetValidResxFilesAsync(context.Document.Project, context.CancellationToken).ConfigureAwait(false);
        if (validResxFiles.IsEmpty)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CSharpFeaturesResources.No_resx_file_found_Please_create_a_resx_file_in_your_project_or_folder,
                    createChangedDocument: c => Task.FromResult(context.Document), // No-op
                    equivalenceKey: "NoResxFileFound"),
                diagnostic);
            return;
        }

        // Try to get AI recommendation first
        var aiRecommendation = await GetAIResxFileRecommendationAsync(context.Document, node, validResxFiles, context.CancellationToken).ConfigureAwait(false);
        
        var allActions = new List<CodeAction>();
        
        // Add AI recommendation as the first option if available
        if (aiRecommendation != null)
        {
            var aiResxFile = validResxFiles.FirstOrDefault(f => 
                string.Equals(f.FilePath, aiRecommendation.SelectedResxFilePath, StringComparison.OrdinalIgnoreCase));
                
            if (aiResxFile != null)
            {
                var confidencePercent = (aiRecommendation.ConfidenceScore * 100).ToString("F0");
                var aiTitle = $"⭐ {Path.GetFileName(aiRecommendation.SelectedResxFilePath)} (AI Recommended - {confidencePercent}% confidence)";
                
                allActions.Add(CodeAction.Create(
                    title: aiTitle,
                    createChangedSolution: cancellationToken => MoveStringToResxAndReplaceLiteralAsync(
                        context.Document, node, aiResxFile, cancellationToken, aiRecommendation.SuggestedResourceKey),
                    equivalenceKey: $"MoveStringTo_AI_{Path.GetFileName(aiRecommendation.SelectedResxFilePath)}"));
            }
        }

        // Add all other resx files as options
        var regularActions = validResxFiles
            .Where(resx => aiRecommendation == null || 
                          !string.Equals(resx.FilePath, aiRecommendation.SelectedResxFilePath, StringComparison.OrdinalIgnoreCase))
            .SelectAsArray(resx =>
            {
                var resxName = Path.GetFileName(resx.FilePath) ?? CSharpFeaturesResources.Unnamed_Resource;
                return CodeAction.Create(
                    title: resxName,
                    createChangedSolution: cancellationToken => MoveStringToResxAndReplaceLiteralAsync(context.Document, node, resx, cancellationToken),
                    equivalenceKey: $"MoveStringTo_{resxName}");
            });

        allActions.AddRange(regularActions);

        context.RegisterCodeFix(
            CodeAction.Create(
                CSharpFeaturesResources.Move_string_to_resx_resource,
                allActions.ToImmutableArray(),
                isInlinable: false),
            diagnostic);
    }

    /// <summary>
    /// Gets all valid .resx files in the project that can be parsed successfully.
    /// </summary>
    private static async Task<ImmutableArray<TextDocument>> GetValidResxFilesAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<TextDocument>.GetInstance(out var validFiles);

        foreach (var document in project.AdditionalDocuments)
        {
            // First check if it's a .resx file by extension
            if (document.FilePath != null &&
                !string.IsNullOrEmpty(Path.GetFileName(document.FilePath)) &&
                string.Equals(".resx", Path.GetExtension(document.FilePath), StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Then validate the XML content
                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var xdoc = XDocument.Parse(sourceText.ToString());

                    // Check that the document has a root element and basic resx structure
                    if (xdoc.Root != null)
                    {
                        validFiles.Add(document);
                    }
                }
                catch (System.Xml.XmlException)
                {
                    // Skip this file - it has invalid XML
                    continue;
                }
            }
        }

        return validFiles.ToImmutable();
    }

    /// <summary>
    /// Gets AI-powered recommendation for the best .resx file for the given string.
    /// </summary>
    private static async Task<ResxFileSelectionResult?> GetAIResxFileRecommendationAsync(
        Document document,
        LiteralExpressionSyntax stringLiteral,
        ImmutableArray<TextDocument> availableResxFiles,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the AI service
            var copilotService = document.GetLanguageService<ICopilotCodeAnalysisService>();
            if (copilotService == null)
            {
                // Service not available (likely no external Copilot service registered)
                return null;
            }

            if (!await copilotService.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
            {
                // Service is registered but not available (quota, disabled, etc.)
                return null;
            }

            // Gather information about available .resx files
            var resxFileInfos = new List<ResxFileInfo>();
            var currentDocumentPath = document.FilePath ?? "";

            foreach (var resxFile in availableResxFiles)
            {
                var entries = await ParseResxEntriesAsync(resxFile, cancellationToken).ConfigureAwait(false);
                var relativePath = GetRelativePath(
                    Path.GetDirectoryName(currentDocumentPath) ?? "", 
                    resxFile.FilePath ?? "");
                var nameSpace = ExtractNamespaceFromResx(resxFile);

                resxFileInfos.Add(new ResxFileInfo(
                    filePath: resxFile.FilePath ?? "",
                    relativePathFromDocument: relativePath,
                    existingEntries: entries,
                    nameSpace: nameSpace));
            }

            // Extract context about the string
            var stringValue = stringLiteral.Token.ValueText;
            var context = ExtractStringContext(stringLiteral);
            var suggestedKey = ToDeterministicResourceKey(stringValue);

            // Create the AI request
            var request = new ResxFileSelectionRequest(
                stringToMove: stringValue,
                stringContext: context,
                currentDocumentPath: currentDocumentPath,
                availableResxFiles: resxFileInfos.ToImmutableArray(),
                suggestedResourceKey: suggestedKey);

            // Ask AI for recommendation
            return await copilotService.SelectBestResxFileAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // If AI fails, gracefully degrade to no recommendation
            return null;
        }
    }

    /// <summary>
    /// Parses .resx file entries for AI analysis.
    /// </summary>
    private static async Task<ImmutableArray<ResxEntry>> ParseResxEntriesAsync(
        TextDocument resxFile, 
        CancellationToken cancellationToken)
    {
        try
        {
            var sourceText = await resxFile.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var doc = XDocument.Parse(sourceText.ToString());
            
            using var _ = ArrayBuilder<ResxEntry>.GetInstance(out var entries);

            foreach (var dataElement in doc.Root?.Elements("data") ?? Enumerable.Empty<XElement>())
            {
                var name = dataElement.Attribute("name")?.Value;
                var value = dataElement.Element("value")?.Value;
                var comment = dataElement.Element("comment")?.Value;

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                {
                    entries.Add(new ResxEntry(name, value, comment));
                }
            }

            return entries.ToImmutable();
        }
        catch
        {
            return ImmutableArray<ResxEntry>.Empty;
        }
    }

    /// <summary>
    /// Extracts namespace from .resx file for AI analysis.
    /// </summary>
    private static string? ExtractNamespaceFromResx(TextDocument resxFile)
    {
        try
        {
            // Simple heuristic: use the directory structure or file name
            var filePath = resxFile.FilePath;
            if (string.IsNullOrEmpty(filePath))
                return null;

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var directory = Path.GetDirectoryName(filePath);
            
            // Try to infer namespace from path structure
            if (!string.IsNullOrEmpty(directory))
            {
                var pathParts = directory.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                var namespaceParts = pathParts.Where(p =>
                    !string.Equals(p, "Properties", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(p, "Resources", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                
                if (namespaceParts.Length > 0)
                {
                    return string.Join(".", namespaceParts) + "." + fileName;
                }
            }

            return fileName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts context information about the string for AI analysis.
    /// </summary>
    private static string ExtractStringContext(LiteralExpressionSyntax stringLiteral)
    {
        var parent = stringLiteral.Parent;
        var context = new StringBuilder();

        // Add parent node context
        switch (parent)
        {
            case ArgumentSyntax arg when arg.Parent?.Parent is InvocationExpressionSyntax invocation:
                context.Append($"Argument to method: {invocation.Expression}");
                break;
            case AssignmentExpressionSyntax assignment:
                context.Append($"Assignment to: {assignment.Left}");
                break;
            case VariableDeclaratorSyntax declarator:
                context.Append($"Variable initialization: {declarator.Identifier}");
                break;
            case ReturnStatementSyntax:
                context.Append("Return statement");
                break;
            case AttributeSyntax:
                context.Append("Attribute value");
                break;
            case ThrowStatementSyntax:
                context.Append("Exception message");
                break;
            default:
                context.Append("String literal");
                break;
        }

        // Add containing method/class context if available
        var containingMethod = stringLiteral.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod != null)
        {
            context.Append($" in method {containingMethod.Identifier}");
        }

        var containingClass = stringLiteral.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (containingClass != null)
        {
            context.Append($" in class {containingClass.Identifier}");
        }

        return context.ToString();
    }

    /// <summary>
    /// Gets the relative path from one directory to another (compatibility method for .NET Standard 2.0).
    /// </summary>
    private static string GetRelativePath(string fromPath, string toPath)
    {
        if (string.IsNullOrEmpty(fromPath) || string.IsNullOrEmpty(toPath))
            return toPath;

        try
        {
            var fromUri = new Uri(Path.GetFullPath(fromPath) + Path.DirectorySeparatorChar);
            var toUri = new Uri(Path.GetFullPath(toPath));
            
            if (fromUri.Scheme != toUri.Scheme)
                return toPath;

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            
            // Convert forward slashes to platform-specific directory separators
            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }
        catch
        {
            // Fallback to absolute path if relative path calculation fails
            return toPath;
        }
    }

    private static async Task<Solution> MoveStringToResxAndReplaceLiteralAsync(
        Document document,
        LiteralExpressionSyntax stringLiteral,
        TextDocument resxFile,
        CancellationToken cancellationToken,
        string? suggestedResourceKey = null)
    {
        var value = stringLiteral.Token.ValueText;
        
        // Use suggested key from AI or generate one deterministically 
        var resourceKey = suggestedResourceKey ?? ToDeterministicResourceKey(value);

        return await MoveStringToResxAndReplaceLiteralCoreAsync(document, stringLiteral, resxFile, value, resourceKey, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Solution> MoveStringToResxAndReplaceLiteralAsync(
        Document document,
        LiteralExpressionSyntax stringLiteral,
        TextDocument resxFile,
        CancellationToken cancellationToken)
    {
        return await MoveStringToResxAndReplaceLiteralAsync(document, stringLiteral, resxFile, cancellationToken, null).ConfigureAwait(false);
    }

    private static async Task<Solution> MoveStringToResxAndReplaceLiteralCoreAsync(
        Document document,
        LiteralExpressionSyntax stringLiteral,
        TextDocument resxFile,
        string value,
        string resourceKey,
        CancellationToken cancellationToken)
    {
        var resourceOperation = new ResourceOperation(value, resourceKey);
        var replacementOperation = new ReplacementOperation(stringLiteral, resourceKey);

        return await ResxResourceManager.UpdateResxAndDocumentAsync(
            document,
            resxFile,
            [resourceOperation],
            [replacementOperation],
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates a deterministic resource key from a string value.
    /// </summary>
    /// <param name="value">The string value to convert to a resource key.</param>
    /// <param name="maxKeyLength">The maximum length of the final generated key.</param>
    /// <returns>A resource key following the pattern Word_Word_Word with each word capitalized.</returns>
    private static string ToDeterministicResourceKey(string value, int maxKeyLength = 60)
    {
        var words = new List<string>();
        using var _ = PooledStringBuilder.GetInstance(out var stringBuilder);

        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c))
            {
                stringBuilder.Append(c);
            }
            else if (stringBuilder.Length > 0)
            {
                words.Add(stringBuilder.ToString());
                stringBuilder.Clear();
            }
        }

        if (stringBuilder.Length > 0)
            words.Add(stringBuilder.ToString());

        // Reuse the same stringBuilder - clear it and use for building the key
        stringBuilder.Clear();

        for (var i = 0; i < words.Count; i++)
        {
            var word = words[i];
            if (word.Length == 0)
                continue;

            // Calculate what the length would be if we add this word
            var separator = i > 0 ? "_" : "";
            var capitalizedWord = char.ToUpperInvariant(word[0]) + (word.Length > 1 ? word.Substring(1).ToLowerInvariant() : "");
            var potentialAddition = separator + capitalizedWord;

            // Check if adding this word would exceed maxKeyLength
            if (stringBuilder.Length + potentialAddition.Length > maxKeyLength)
            {
                // Don't add this word if it would exceed the limit
                break;
            }

            stringBuilder.Append(potentialAddition);
        }

        var key = stringBuilder.ToString();

        // Ensure it starts with a letter if it starts with a digit
        if (key.Length > 0 && char.IsDigit(key[0]))
        {
            key = "Resource_" + key;
            if (key.Length > maxKeyLength)
                key = key.Substring(0, maxKeyLength);
        }

        return key;
    }

    /// <summary>
    /// Represents a resource operation to be performed on a resx file.
    /// </summary>
    private readonly record struct ResourceOperation(string Value, string Key);

    /// <summary>
    /// Represents a replacement operation to be performed on source code.
    /// </summary>
    private readonly record struct ReplacementOperation(LiteralExpressionSyntax Literal, string ResourceKey);

    /// <summary>
    /// Manages operations on resx files and coordinates solution updates.
    /// </summary>
    private static class ResxResourceManager
    {
        /// <summary>
        /// Updates a resx file with multiple resource operations and performs corresponding replacements in the document.
        /// </summary>
        public static async Task<Solution> UpdateResxAndDocumentAsync(
            Document document,
            TextDocument resxFile,
            IReadOnlyList<ResourceOperation> resourceOperations,
            IReadOnlyList<ReplacementOperation> replacementOperations,
            CancellationToken cancellationToken)
        {
            var resxSourceText = await resxFile.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var resxResult = await UpdateResxFileAsync(resxSourceText, resourceOperations, cancellationToken).ConfigureAwait(false);
            if (!resxResult.IsSuccess)
            {
                return document.Project.Solution;
            }

            var documentResult = await UpdateDocumentAsync(document, resxFile, replacementOperations, cancellationToken).ConfigureAwait(false);

            return await CreateUpdatedSolutionAsync(
                document.Project.Solution,
                document.Id,
                resxFile.Id,
                documentResult.UpdatedRoot,
                resxResult.UpdatedText,
                resxResult.RequiresUpdate,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the resx file content with the specified resource operations.
        /// </summary>
        private static Task<ResxUpdateResult> UpdateResxFileAsync(
            SourceText resxSourceText,
            IReadOnlyList<ResourceOperation> resourceOperations,
            CancellationToken cancellationToken)
        {
            // Check for cancellation before starting XML parsing
            cancellationToken.ThrowIfCancellationRequested();

            // Since we validate .resx files before offering the code fix, we can assume valid XML here
            var xdoc = XDocument.Parse(resxSourceText.ToString());

            if (xdoc.Root == null)
            {
                return Task.FromResult(ResxUpdateResult.Failed());
            }

            var requiresUpdate = false;

            foreach (var operation in resourceOperations)
            {
                // Check for cancellation during the loop
                cancellationToken.ThrowIfCancellationRequested();

                // Check if the value already exists in the .resx file
                var existingDataElement = xdoc.Root.Elements("data")
                    .FirstOrDefault(e => e.Element("value")?.Value == operation.Value);

                if (existingDataElement is null)
                {
                    requiresUpdate = true;
                    var dataElement = xdoc.Root.Elements("data")
                        .FirstOrDefault(e => (string?)e.Attribute("name") == operation.Key);

                    if (dataElement is null)
                    {
                        dataElement = new XElement("data",
                            new XAttribute("name", operation.Key),
                            new XAttribute(XNamespace.Xml + "space", "preserve"),
                            new XElement("value", operation.Value));
                        xdoc.Root.Add(dataElement);
                    }
                    else
                    {
                        // If the key exists but with a different value, update it
                        var valueElement = dataElement.Element("value");
                        if (valueElement?.Value != operation.Value)
                        {
                            if (valueElement != null)
                            {
                                valueElement.Value = operation.Value;
                            }
                        }
                    }
                }
            }

            SourceText? updatedText = null;
            if (requiresUpdate)
            {
                // Check for cancellation before generating the updated text
                cancellationToken.ThrowIfCancellationRequested();

                string updatedResxText;
                using (var sw = new StringWriter())
                {
                    xdoc.Save(sw);
                    updatedResxText = sw.ToString();
                }
                updatedText = SourceText.From(updatedResxText, resxSourceText.Encoding);
            }

            return Task.FromResult(ResxUpdateResult.Success(updatedText, requiresUpdate));
        }

        /// <summary>
        /// Updates the document by replacing string literals with resource access expressions.
        /// </summary>
        private static async Task<DocumentUpdateResult> UpdateDocumentAsync(
            Document document,
            TextDocument resxFile,
            IReadOnlyList<ReplacementOperation> replacementOperations,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var resourceClass = Path.GetFileNameWithoutExtension(resxFile.Name);
            var defaultNamespace = document.Project.DefaultNamespace;

            // Create replacement map
            var replacementMap = new Dictionary<LiteralExpressionSyntax, SyntaxNode>();

            foreach (var operation in replacementOperations)
            {
                var resourceAccessString = !string.IsNullOrEmpty(defaultNamespace)
                    ? $"{defaultNamespace}.{resourceClass}.{operation.ResourceKey}"
                    : $"{resourceClass}.{operation.ResourceKey}";

                var resourceAccess = SyntaxFactory.ParseExpression(resourceAccessString)
                    .WithTriviaFrom(operation.Literal);

                replacementMap[operation.Literal] = resourceAccess;
            }

            var newRoot = root.ReplaceNodes(replacementMap.Keys, (original, rewritten) => replacementMap[original]);

            return new DocumentUpdateResult(newRoot);
        }

        /// <summary>
        /// Creates the updated solution with both document and resx file changes.
        /// </summary>
        private static Task<Solution> CreateUpdatedSolutionAsync(
            Solution solution,
            DocumentId documentId,
            DocumentId resxFileId,
            SyntaxNode updatedRoot,
            SourceText? updatedResxText,
            bool resxRequiresUpdate,
            CancellationToken cancellationToken)
        {
            // Check for cancellation before creating the updated solution
            cancellationToken.ThrowIfCancellationRequested();

            var newSolution = solution.WithDocumentSyntaxRoot(documentId, updatedRoot);

            if (resxRequiresUpdate && updatedResxText != null)
            {
                newSolution = newSolution.WithAdditionalDocumentText(resxFileId, updatedResxText);
            }

            return Task.FromResult(newSolution);
        }

        /// <summary>
        /// Gets the actual resource key to use for a given operation, considering existing resources.
        /// </summary>
        public static string GetActualResourceKey(XDocument xdoc, ResourceOperation operation)
        {
            // Check if the value already exists in the .resx file
            var existingDataElement = xdoc.Root?.Elements("data")
                .FirstOrDefault(e => e.Element("value")?.Value == operation.Value);

            if (existingDataElement is not null)
            {
                // Use the existing name
                var nameAttribute = existingDataElement.Attribute("name");
                return nameAttribute?.Value ?? operation.Key;
            }

            return operation.Key;
        }

        /// <summary>
        /// Result of updating a resx file.
        /// </summary>
        private readonly record struct ResxUpdateResult(bool IsSuccess, SourceText? UpdatedText, bool RequiresUpdate)
        {
            public static ResxUpdateResult Success(SourceText? updatedText, bool requiresUpdate)
                => new(true, updatedText, requiresUpdate);

            public static ResxUpdateResult Failed()
                => default;
        }

        /// <summary>
        /// Result of updating a document.
        /// </summary>
        private readonly record struct DocumentUpdateResult(SyntaxNode UpdatedRoot);
    }

    private sealed class CSharpMoveToResxFixAllProvider : FixAllProvider
    {
        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
            => [FixAllScope.Document];

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
            var validResxFiles = await GetValidResxFilesAsync(project, context.CancellationToken).ConfigureAwait(false);
            if (validResxFiles.IsEmpty)
                return null;

            var resx = validResxFiles[0];

            return CodeAction.Create(
                CSharpFeaturesResources.Move_all_strings_to_resx_resource,
                async cancellationToken =>
                {
                    // Collect all string literals and their operations
                    var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var resourceOperations = new List<ResourceOperation>();
                    var replacementOperations = new List<ReplacementOperation>();

                    // We can now safely parse the resx since we've validated it
                    var resxSourceText = await resx.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var xdoc = XDocument.Parse(resxSourceText.ToString());

                    foreach (var diagnostic in diagnostics)
                    {
                        var node = root?.FindNode(diagnostic.Location.SourceSpan) as LiteralExpressionSyntax;
                        if (node is not null)
                        {
                            var value = node.Token.ValueText;
                            var proposedKey = ToDeterministicResourceKey(value);
                            var resourceOperation = new ResourceOperation(value, proposedKey);

                            // Get the actual key that will be used (may be existing key if value already exists)
                            var actualKey = ResxResourceManager.GetActualResourceKey(xdoc, resourceOperation);

                            resourceOperations.Add(new ResourceOperation(value, actualKey));
                            replacementOperations.Add(new ReplacementOperation(node, actualKey));
                        }
                    }

                    if (resourceOperations.Count == 0)
                        return project.Solution;

                    return await ResxResourceManager.UpdateResxAndDocumentAsync(
                        document,
                        resx,
                        resourceOperations,
                        replacementOperations,
                        cancellationToken).ConfigureAwait(false);
                },
                nameof(CSharpMoveToResxFixAllProvider));
        }
    }
}
