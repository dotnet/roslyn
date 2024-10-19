// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration;

/// <summary>
/// A generator used for creating or modifying member declarations in source.
/// </summary>
internal static class CodeGenerator
{
    /// <summary>
    /// Annotation placed on generated syntax.
    /// </summary>
    public static readonly SyntaxAnnotation Annotation = new(nameof(CodeGenerator));

    private static ICodeGenerationService GetCodeGenerationService(HostWorkspaceServices services, string language)
        => services.GetExtendedLanguageServices(language).GetRequiredService<ICodeGenerationService>();

    /// <summary>
    /// Create a new solution where the declaration of the destination symbol has an additional event of the same signature as the specified event symbol.
    /// Returns the document in the new solution where the destination symbol is declared.
    /// </summary>
    public static Task<Document> AddEventDeclarationAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IEventSymbol @event, CancellationToken cancellationToken)
        => GetCodeGenerationService(context.Solution.Workspace.Services, destination.Language).AddEventAsync(context, destination, @event, cancellationToken);

    /// <summary>
    /// Create a new solution where the declaration of the destination symbol has an additional field of the same signature as the specified field symbol.
    /// Returns the document in the new solution where the destination symbol is declared.
    /// </summary>
    public static Task<Document> AddFieldDeclarationAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IFieldSymbol field, CancellationToken cancellationToken)
        => GetCodeGenerationService(context.Solution.Workspace.Services, destination.Language).AddFieldAsync(context, destination, field, cancellationToken);

    /// <summary>
    /// Create a new solution where the declaration of the destination symbol has an additional method of the same signature as the specified method symbol.
    /// Returns the document in the new solution where the destination symbol is declared.
    /// </summary>
    public static Task<Document> AddMethodDeclarationAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IMethodSymbol method, CancellationToken cancellationToken)
        => GetCodeGenerationService(context.Solution.Workspace.Services, destination.Language).AddMethodAsync(context, destination, method, cancellationToken);

    /// <summary>
    /// Create a new solution where the declaration of the destination symbol has an additional property of the same signature as the specified property symbol.
    /// Returns the document in the new solution where the destination symbol is declared.
    /// </summary>
    public static Task<Document> AddPropertyDeclarationAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IPropertySymbol property, CancellationToken cancellationToken)
        => GetCodeGenerationService(context.Solution.Workspace.Services, destination.Language).AddPropertyAsync(context, destination, property, cancellationToken);

    /// <summary>
    /// Create a new solution where the declaration of the destination symbol has an additional named type of the same signature as the specified named type symbol.
    /// Returns the document in the new solution where the destination symbol is declared.
    /// </summary>
    public static Task<Document> AddNamedTypeDeclarationAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, INamedTypeSymbol namedType, CancellationToken cancellationToken)
        => GetCodeGenerationService(context.Solution.Workspace.Services, destination.Language).AddNamedTypeAsync(context, destination, namedType, cancellationToken);

    /// <summary>
    /// Create a new solution where the declaration of the destination symbol has an additional named type of the same signature as the specified named type symbol.
    /// Returns the document in the new solution where the destination symbol is declared.
    /// </summary>
    public static Task<Document> AddNamedTypeDeclarationAsync(CodeGenerationSolutionContext context, INamespaceSymbol destination, INamedTypeSymbol namedType, CancellationToken cancellationToken)
        => GetCodeGenerationService(context.Solution.Workspace.Services, destination.Language).AddNamedTypeAsync(context, destination, namedType, cancellationToken);

    /// <summary>
    /// Create a new solution where the declaration of the destination symbol has an additional namespace of the same signature as the specified namespace symbol.
    /// Returns the document in the new solution where the destination symbol is declared.
    /// </summary>
    public static Task<Document> AddNamespaceDeclarationAsync(CodeGenerationSolutionContext context, INamespaceSymbol destination, INamespaceSymbol @namespace, CancellationToken cancellationToken)
        => GetCodeGenerationService(context.Solution.Workspace.Services, destination.Language).AddNamespaceAsync(context, destination, @namespace, cancellationToken);

    /// <summary>
    /// Create a new solution where the declaration of the destination symbol has an additional namespace or type of the same signature as the specified namespace or type symbol.
    /// Returns the document in the new solution where the destination symbol is declared.
    /// </summary>
    public static Task<Document> AddNamespaceOrTypeDeclarationAsync(CodeGenerationSolutionContext context, INamespaceSymbol destination, INamespaceOrTypeSymbol namespaceOrType, CancellationToken cancellationToken)
        => GetCodeGenerationService(context.Solution.Workspace.Services, destination.Language).AddNamespaceOrTypeAsync(context, destination, namespaceOrType, cancellationToken);

    /// <summary>
    /// Create a new solution where the declaration of the destination symbol has additional members of the same signature as the specified member symbols.
    /// Returns the document in the new solution where the destination symbol is declared.
    /// </summary>
    public static Task<Document> AddMemberDeclarationsAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IEnumerable<ISymbol> members, CancellationToken cancellationToken)
        => GetCodeGenerationService(context.Solution.Workspace.Services, destination.Language).AddMembersAsync(context, destination, members, cancellationToken);

    /// <summary>
    /// Returns <c>true</c> if additional declarations can be added to the destination symbol's declaration.
    /// </summary>
    public static bool CanAdd(Solution solution, ISymbol destination, CancellationToken cancellationToken)
        => GetCodeGenerationService(solution.Workspace.Services, destination.Language).CanAddTo(destination, solution, cancellationToken);
}
