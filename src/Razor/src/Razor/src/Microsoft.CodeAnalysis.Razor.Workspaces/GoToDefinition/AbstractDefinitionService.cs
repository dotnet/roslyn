// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Microsoft.CodeAnalysis.Razor.GoToDefinition;

internal abstract class AbstractDefinitionService(
    IRazorComponentSearchEngine componentSearchEngine,
    ITagHelperSearchEngine? tagHelperSearchEngine,
    IDocumentMappingService documentMappingService,
    ILogger logger) : IDefinitionService
{
    private readonly IRazorComponentSearchEngine _componentSearchEngine = componentSearchEngine;
    private readonly ITagHelperSearchEngine? _tagHelperSearchEngine = tagHelperSearchEngine;
    private readonly IDocumentMappingService _documentMappingService = documentMappingService;
    private readonly ILogger _logger = logger;

    public async Task<LspLocation[]?> GetDefinitionAsync(
        IDocumentSnapshot documentSnapshot,
        DocumentPositionInfo positionInfo,
        ISolutionQueryOperations solutionQueryOperations,
        bool includeMvcTagHelpers,
        CancellationToken cancellationToken)
    {
        if (!includeMvcTagHelpers && !documentSnapshot.FileKind.IsComponent())
        {
            _logger.LogInformation($"'{documentSnapshot.FileKind}' is not a component type.");
            return null;
        }

        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        if (!RazorComponentDefinitionHelpers.TryGetBoundTagHelpers(codeDocument, positionInfo.HostDocumentIndex, _logger, out var boundTagHelperResults))
        {
            _logger.LogInformation($"Could not retrieve bound tag helper information.");
            return null;
        }

        if (includeMvcTagHelpers)
        {
            Debug.Assert(_tagHelperSearchEngine is not null, "If includeMvcTagHelpers is true, _tagHelperSearchEngine must not be null.");

            var tagHelperLocations = await _tagHelperSearchEngine.TryLocateTagHelperDefinitionsAsync(boundTagHelperResults, documentSnapshot, solutionQueryOperations, cancellationToken).ConfigureAwait(false);
            if (tagHelperLocations is { Length: > 0 })
            {
                return tagHelperLocations;
            }
        }

        // For Razor components, there can only ever be one tag helper result
        var (boundTagHelper, boundAttribute) = boundTagHelperResults[0];

        var componentDocument = await _componentSearchEngine
            .TryLocateComponentAsync(boundTagHelper, solutionQueryOperations, cancellationToken)
            .ConfigureAwait(false);

        if (componentDocument is null)
        {
            _logger.LogInformation($"Could not locate component document.");
            return null;
        }

        var componentFilePath = componentDocument.FilePath;

        _logger.LogInformation($"Definition found at file path: {componentFilePath}");

        var range = await GetNavigateRangeAsync(componentDocument, boundAttribute, cancellationToken).ConfigureAwait(false);

        return [LspFactory.CreateLocation(componentFilePath, range)];
    }

    private async Task<LspRange> GetNavigateRangeAsync(IDocumentSnapshot documentSnapshot, BoundAttributeDescriptor? attributeDescriptor, CancellationToken cancellationToken)
    {
        if (attributeDescriptor is not null)
        {
            _logger.LogInformation($"Attempting to get definition from an attribute directly.");

            var range = await RazorComponentDefinitionHelpers
                .TryGetPropertyRangeAsync(documentSnapshot, attributeDescriptor.PropertyName, _documentMappingService, _logger, cancellationToken)
                .ConfigureAwait(false);

            if (range is not null)
            {
                return range;
            }
        }

        // When navigating from a start or end tag, we just take the user to the top of the file.
        // If we were trying to navigate to a property, and we couldn't find it, we can at least take
        // them to the file for the component. If the property was defined in a partial class they can
        // at least then press F7 to go there.
        return LspFactory.DefaultRange;
    }

    public async Task<LspLocation[]?> TryGetDefinitionFromStringLiteralAsync(
        IDocumentSnapshot documentSnapshot,
        Position position,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Attempting to get definition from string literal at position {position}.");

        // Get the C# syntax tree to analyze the string literal
        var syntaxTree = await documentSnapshot.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var sourceText = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);

        // Convert position to absolute index
        var absoluteIndex = sourceText.GetRequiredAbsoluteIndex(position);

        // Find the token at the current position
        var token = root.FindToken(absoluteIndex);

        // Check if we're in a string literal
        if (token.IsKind(CSharpSyntaxKind.StringLiteralToken))
        {
            var literalText = token.ValueText;
            _logger.LogDebug($"Found string literal: {literalText}");

            // Try to resolve the file path
            if (TryResolveFilePath(documentSnapshot, literalText, out var resolvedPath))
            {
                _logger.LogDebug($"Resolved file path: {resolvedPath}");
                return [LspFactory.CreateLocation(resolvedPath, LspFactory.DefaultRange)];
            }
        }

        return null;
    }

    private bool TryResolveFilePath(IDocumentSnapshot documentSnapshot, string filePath, out string resolvedPath)
    {
        resolvedPath = string.Empty;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        // Only process if it looks like a Razor file path
        if (!filePath.IsRazorFilePath())
        {
            return false;
        }

        var project = documentSnapshot.Project;

        // Handle tilde paths (~/ or ~\) - these are relative to the project root
        if (filePath is ['~', '/' or '\\', ..])
        {
            var projectDirectory = Path.GetDirectoryName(project.FilePath);
            if (projectDirectory is null)
            {
                return false;
            }

            // Remove the tilde and normalize path separators
            var relativePath = filePath.Substring(2).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var candidatePath = Path.GetFullPath(Path.Combine(projectDirectory, relativePath));

            if (project.ContainsDocument(candidatePath))
            {
                resolvedPath = candidatePath;
                return true;
            }
        }

        // Handle relative paths - relative to the current document
        var currentDocumentDirectory = Path.GetDirectoryName(documentSnapshot.FilePath);
        if (currentDocumentDirectory is not null)
        {
            var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var candidatePath = Path.GetFullPath(Path.Combine(currentDocumentDirectory, normalizedPath));

            if (project.ContainsDocument(candidatePath))
            {
                resolvedPath = candidatePath;
                return true;
            }
        }

        return false;
    }
}
