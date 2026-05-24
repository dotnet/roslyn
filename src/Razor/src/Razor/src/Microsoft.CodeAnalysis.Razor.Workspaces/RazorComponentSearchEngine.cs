// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal class RazorComponentSearchEngine(ILoggerFactory loggerFactory) : IRazorComponentSearchEngine
{
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<RazorComponentSearchEngine>();

    /// <summary>
    ///  Search for a component in a project based on its tag name and fully qualified name.
    /// </summary>
    /// <param name="tagHelper">
    ///  A <see cref="TagHelperDescriptor"/> to find the corresponding Razor component for.
    /// </param>
    /// <param name="solutionQueryOperations">
    ///  An <see cref="ISolutionQueryOperations"/> to enumerate project snapshots.
    /// </param>
    /// <param name="cancellationToken">
    ///  A token that is checked to cancel work.
    /// </param>
    /// <returns>
    ///  The corresponding <see cref="IDocumentSnapshot"/> if found, <see langword="null"/> otherwise.
    /// </returns>
    /// <remarks>
    ///  This method makes several assumptions about the nature of components. First,
    ///  it assumes that a component a given name "Name" will be located in a file
    ///  "Name.razor". Second, it assumes that the namespace the component is present in
    ///  has the same name as the assembly its corresponding tag helper is loaded from.
    ///  Implicitly, this method inherits any assumptions made by TrySplitNamespaceAndType.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///  Thrown if <paramref name="tagHelper"/> is <see langword="null"/>.
    /// </exception>
    public async Task<IDocumentSnapshot?> TryLocateComponentAsync(
        TagHelperDescriptor tagHelper,
        ISolutionQueryOperations solutionQueryOperations,
        CancellationToken cancellationToken)
    {
        if (tagHelper.Kind != TagHelperKind.Component)
        {
            return null;
        }

        var typeName = tagHelper.TypeNameIdentifier;
        var namespaceName = tagHelper.TypeNamespace;
        if (typeName == null || namespaceName == null)
        {
            _logger.LogWarning($"Could not split namespace and type for name {tagHelper.Name}.");
            return null;
        }

        var lookupSymbolName = RemoveGenericContent(typeName.AsMemory());

        foreach (var project in solutionQueryOperations.GetProjects())
        {
            foreach (var path in project.DocumentFilePaths)
            {
                // Get document and code document
                if (!project.TryGetDocument(path, out var document))
                {
                    continue;
                }

                // Rule out if not Razor component with correct name
                if (!document.IsPathCandidateForComponent(lookupSymbolName))
                {
                    continue;
                }

                var razorCodeDocument = await document.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
                if (razorCodeDocument is null)
                {
                    continue;
                }

                // Make sure we have the right namespace of the fully qualified name
                if (!razorCodeDocument.ComponentNamespaceMatches(namespaceName))
                {
                    continue;
                }

                return document;
            }
        }

        return null;
    }

    internal static ReadOnlyMemory<char> RemoveGenericContent(ReadOnlyMemory<char> typeName)
    {
        var genericSeparatorStart = typeName.Span.IndexOf('<');

        return genericSeparatorStart > 0
            ? typeName[..genericSeparatorStart]
            : typeName;
    }
}
