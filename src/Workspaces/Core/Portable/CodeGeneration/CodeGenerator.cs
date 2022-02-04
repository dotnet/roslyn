// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
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
            => services.GetLanguageServices(language).GetRequiredService<ICodeGenerationService>();

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional event of the same signature as the specified event symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddEventDeclarationAsync(Solution solution, INamedTypeSymbol destination, IEventSymbol @event, CodeGenerationContext context, CancellationToken cancellationToken)
            => GetCodeGenerationService(solution.Workspace.Services, destination.Language).AddEventAsync(solution, destination, @event, context, cancellationToken);

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional field of the same signature as the specified field symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddFieldDeclarationAsync(Solution solution, INamedTypeSymbol destination, IFieldSymbol field, CodeGenerationContext context, CancellationToken cancellationToken)
            => GetCodeGenerationService(solution.Workspace.Services, destination.Language).AddFieldAsync(solution, destination, field, context, cancellationToken);

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional method of the same signature as the specified method symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddMethodDeclarationAsync(Solution solution, INamedTypeSymbol destination, IMethodSymbol method, CodeGenerationContext context, CancellationToken cancellationToken)
            => GetCodeGenerationService(solution.Workspace.Services, destination.Language).AddMethodAsync(solution, destination, method, context, cancellationToken);

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional property of the same signature as the specified property symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddPropertyDeclarationAsync(Solution solution, INamedTypeSymbol destination, IPropertySymbol property, CodeGenerationContext context, CancellationToken cancellationToken)
            => GetCodeGenerationService(solution.Workspace.Services, destination.Language).AddPropertyAsync(solution, destination, property, context, cancellationToken);

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional named type of the same signature as the specified named type symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddNamedTypeDeclarationAsync(Solution solution, INamedTypeSymbol destination, INamedTypeSymbol namedType, CodeGenerationContext context, CancellationToken cancellationToken)
            => GetCodeGenerationService(solution.Workspace.Services, destination.Language).AddNamedTypeAsync(solution, destination, namedType, context, cancellationToken);

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional named type of the same signature as the specified named type symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddNamedTypeDeclarationAsync(Solution solution, INamespaceSymbol destination, INamedTypeSymbol namedType, CodeGenerationContext context, CancellationToken cancellationToken)
            => GetCodeGenerationService(solution.Workspace.Services, destination.Language).AddNamedTypeAsync(solution, destination, namedType, context, cancellationToken);

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional namespace of the same signature as the specified namespace symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddNamespaceDeclarationAsync(Solution solution, INamespaceSymbol destination, INamespaceSymbol @namespace, CodeGenerationContext context, CancellationToken cancellationToken)
            => GetCodeGenerationService(solution.Workspace.Services, destination.Language).AddNamespaceAsync(solution, destination, @namespace, context, cancellationToken);

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional namespace or type of the same signature as the specified namespace or type symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddNamespaceOrTypeDeclarationAsync(Solution solution, INamespaceSymbol destination, INamespaceOrTypeSymbol namespaceOrType, CodeGenerationContext context, CancellationToken cancellationToken)
            => GetCodeGenerationService(solution.Workspace.Services, destination.Language).AddNamespaceOrTypeAsync(solution, destination, namespaceOrType, context, cancellationToken);

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has additional members of the same signature as the specified member symbols.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddMemberDeclarationsAsync(Solution solution, INamedTypeSymbol destination, IEnumerable<ISymbol> members, CodeGenerationContext context, CancellationToken cancellationToken)
            => GetCodeGenerationService(solution.Workspace.Services, destination.Language).AddMembersAsync(solution, destination, members, context, cancellationToken);

        /// <summary>
        /// Returns <c>true</c> if additional declarations can be added to the destination symbol's declaration.
        /// </summary>
        public static bool CanAdd(Solution solution, ISymbol destination, CancellationToken cancellationToken)
            => GetCodeGenerationService(solution.Workspace.Services, destination.Language).CanAddTo(destination, solution, cancellationToken);
    }
}
