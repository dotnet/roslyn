// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Microsoft.CodeAnalysis.Remote.Razor.GoToDefinition;

[Export(typeof(IDefinitionService)), Shared]
[method: ImportingConstructor]
internal sealed class DefinitionService(
    IRazorComponentSearchEngine componentSearchEngine,
    ITagHelperSearchEngine? tagHelperSearchEngine,
    IDocumentMappingService documentMappingService,
    IFileSystem fileSystem,
    ILoggerFactory loggerFactory) : IDefinitionService
{
    private readonly IRazorComponentSearchEngine _componentSearchEngine = componentSearchEngine;
    private readonly ITagHelperSearchEngine? _tagHelperSearchEngine = tagHelperSearchEngine;
    private readonly IDocumentMappingService _documentMappingService = documentMappingService;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<DefinitionService>();

    public async Task<LspLocation[]?> GetDefinitionAsync(
        RemoteDocumentSnapshot documentSnapshot,
        DocumentPositionInfo positionInfo,
        RemoteSolutionSnapshot solutionSnapshot,
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

            var tagHelperLocations = await _tagHelperSearchEngine.TryLocateTagHelperDefinitionsAsync(boundTagHelperResults, documentSnapshot, solutionSnapshot, cancellationToken).ConfigureAwait(false);
            if (tagHelperLocations is { Length: > 0 })
            {
                return tagHelperLocations;
            }
        }

        // For Razor components, there can only ever be one tag helper result
        var (boundTagHelper, boundAttribute) = boundTagHelperResults[0];

        var componentDocument = await _componentSearchEngine
            .TryLocateComponentAsync(boundTagHelper, solutionSnapshot, cancellationToken)
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

    private async Task<LspRange> GetNavigateRangeAsync(RemoteDocumentSnapshot documentSnapshot, BoundAttributeDescriptor? attributeDescriptor, CancellationToken cancellationToken)
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
        RemoteDocumentSnapshot documentSnapshot,
        DocumentPositionInfo positionInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Attempting to get definition from string literal at position {positionInfo.Position}.");

        // Get the C# syntax tree to analyze the string literal
        var syntaxTree = await documentSnapshot.GetCSharpSyntaxTreeAsync(positionInfo.InDeclDocument, cancellationToken).ConfigureAwait(false);
        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var sourceText = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);

        // Convert position to absolute index
        var absoluteIndex = sourceText.GetRequiredAbsoluteIndex(positionInfo.Position);

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

    private bool TryResolveFilePath(RemoteDocumentSnapshot documentSnapshot, string filePath, out string resolvedPath)
    {
        resolvedPath = string.Empty;

        // Leave empty values and external URLs to the normal definition handlers.
        if (string.IsNullOrWhiteSpace(filePath) || filePath.IndexOf("://", StringComparison.Ordinal) >= 0)
        {
            return false;
        }

        var project = documentSnapshot.ProjectSnapshot;
        var normalizedPath = FilePathNormalizer.Normalize(filePath);

        // Tilde paths (~/ or ~\) may point to web content or project files.
        if (filePath is ['~', '/' or '\\', ..])
        {
            var projectDirectory = Path.GetDirectoryName(project.FilePath);
            if (projectDirectory is null)
            {
                _logger.LogDebug("Could not resolve a tilde path without a project directory.");
                return false;
            }

            var relativePath = normalizedPath[2..];

            // Prefer wwwroot for web content such as stylesheets, scripts, and images.
            if (TryResolveFilePath(project, Path.Combine(projectDirectory, "wwwroot"), relativePath, out resolvedPath))
            {
                return true;
            }

            // Fall back to the project root if wwwroot is absent or doesn't contain the file.
            return TryResolveFilePath(project, projectDirectory, relativePath, out resolvedPath);
        }

        // Unlike tilde paths, ordinary relative paths are based on the current Razor document.
        return TryResolveFilePath(project, Path.GetDirectoryName(documentSnapshot.FilePath), normalizedPath, out resolvedPath);
    }

    private bool TryResolveFilePath(RemoteProjectSnapshot project, string? directory, string filePath, out string resolvedPath)
    {
        resolvedPath = string.Empty;

        if (directory is null)
        {
            _logger.LogDebug("Could not resolve a file path without a base directory.");
            return false;
        }

        string candidatePath;
        try
        {
            // Resolve . and .. so workspace and file-system lookups use the same absolute path.
            candidatePath = Path.GetFullPath(Path.Combine(directory, filePath));
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Could not resolve file path '{filePath}': {ex.Message}");
            return false;
        }

        // Razor documents are tracked by the project snapshot, so check there before accessing disk.
        if (candidatePath.IsRazorFilePath() && project.ContainsDocument(candidatePath))
        {
            resolvedPath = candidatePath;
            return true;
        }

        // Check that the candidate file exists on disk before returning its path.
        if (_fileSystem.FileExists(candidatePath))
        {
            resolvedPath = candidatePath;
            return true;
        }

        return false;
    }
}
