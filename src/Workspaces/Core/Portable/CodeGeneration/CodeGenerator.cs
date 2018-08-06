// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        public static readonly SyntaxAnnotation Annotation = new SyntaxAnnotation(nameof(CodeGenerator));

        private static ICodeGenerationService GetCodeGenerationService(Workspace workspace, string language)
        {
            return workspace.Services.GetLanguageServices(language).GetService<ICodeGenerationService>();
        }

        /// <summary>
        /// Returns a newly created event declaration node from the provided event.
        /// </summary>
        public static SyntaxNode CreateEventDeclaration(IEventSymbol @event, Workspace workspace, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified, CodeGenerationOptions options = null)
        {
            return GetCodeGenerationService(workspace, @event.Language).CreateEventDeclaration(@event, destination, options);
        }

        /// <summary>
        /// Returns a newly created field declaration node from the provided field.
        /// </summary>
        public static SyntaxNode CreateFieldDeclaration(IFieldSymbol field, Workspace workspace, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified, CodeGenerationOptions options = null)
        {
            return GetCodeGenerationService(workspace, field.Language).CreateFieldDeclaration(field, destination, options);
        }

        /// <summary>
        /// Returns a newly created method declaration node from the provided method.
        /// </summary>
        public static SyntaxNode CreateMethodDeclaration(IMethodSymbol method, Workspace workspace, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified, CodeGenerationOptions options = null)
        {
            return GetCodeGenerationService(workspace, method.Language).CreateMethodDeclaration(method, destination, options);
        }

        /// <summary>
        /// Returns a newly created property declaration node from the provided property.
        /// </summary>
        public static SyntaxNode CreatePropertyDeclaration(IPropertySymbol property, Workspace workspace, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified, CodeGenerationOptions options = null)
        {
            return GetCodeGenerationService(workspace, property.Language).CreatePropertyDeclaration(property, destination, options);
        }

        /// <summary>
        /// Returns a newly created named type declaration node from the provided named type.
        /// </summary>
        public static SyntaxNode CreateNamedTypeDeclaration(INamedTypeSymbol namedType, Workspace workspace, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified, CodeGenerationOptions options = null)
        {
            return GetCodeGenerationService(workspace, namedType.Language).CreateNamedTypeDeclaration(namedType, destination, options);
        }

        /// <summary>
        /// Returns a newly created namespace declaration node from the provided namespace.
        /// </summary>
        public static SyntaxNode CreateNamespaceDeclaration(INamespaceSymbol @namespace, Workspace workspace, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified, CodeGenerationOptions options = null)
        {
            return GetCodeGenerationService(workspace, @namespace.Language).CreateNamespaceDeclaration(@namespace, destination, options);
        }

        /// <summary>
        /// Create a new declaration node with an event declaration of the same signature as the specified symbol added to it.
        /// </summary>
        public static TDeclarationNode AddEventDeclaration<TDeclarationNode>(TDeclarationNode destination, IEventSymbol @event, Workspace workspace, CodeGenerationOptions options = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destination.Language).AddEvent(destination, @event, options);
        }

        /// <summary>
        /// Create a new declaration node with a field declaration of the same signature as the specified symbol added to it.
        /// </summary>
        public static TDeclarationNode AddFieldDeclaration<TDeclarationNode>(TDeclarationNode destination, IFieldSymbol field, Workspace workspace, CodeGenerationOptions options = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destination.Language).AddField(destination, field, options);
        }

        /// <summary>
        /// Create a new declaration node with a method declaration of the same signature as the specified symbol added to it.
        /// </summary>
        public static TDeclarationNode AddMethodDeclaration<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, Workspace workspace, CodeGenerationOptions options = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destination.Language).AddMethod(destination, method, options);
        }

        /// <summary>
        /// Create a new declaration node with a property declaration of the same signature as the specified symbol added to it.
        /// </summary>
        public static TDeclarationNode AddPropertyDeclaration<TDeclarationNode>(TDeclarationNode destination, IPropertySymbol property, Workspace workspace, CodeGenerationOptions options = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destination.Language).AddProperty(destination, property, options);
        }

        /// <summary>
        /// Create a new declaration node with a named type declaration of the same signature as the specified symbol added to it.
        /// </summary>
        public static TDeclarationNode AddNamedTypeDeclaration<TDeclarationNode>(TDeclarationNode destination, INamedTypeSymbol namedType, Workspace workspace, CodeGenerationOptions options = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destination.Language).AddNamedType(destination, namedType, options);
        }

        /// <summary>
        /// Create a new declaration node with multiple member declarations of the same signatures as the specified symbols added to it.
        /// </summary>
        public static TDeclarationNode AddMemberDeclarations<TDeclarationNode>(TDeclarationNode destination, IEnumerable<ISymbol> members, Workspace workspace, CodeGenerationOptions options = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destination.Language).AddMembers(destination, members, options);
        }

        /// <summary>
        /// Create a new declaration node with one or more parameter declarations of the same signature as the specified symbols added to it.
        /// </summary>
        public static TDeclarationNode AddParameterDeclarations<TDeclarationNode>(TDeclarationNode destinationMember, IEnumerable<IParameterSymbol> parameters, Workspace workspace, CodeGenerationOptions options = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destinationMember.Language).AddParameters(destinationMember, parameters, options);
        }

        /// <summary>
        /// Create a new declaration node with the specified attributes added to it.
        /// </summary>
        public static TDeclarationNode AddAttributes<TDeclarationNode>(TDeclarationNode destination, Workspace workspace, IEnumerable<AttributeData> attributes, SyntaxToken? target = null, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destination.Language).AddAttributes(destination, attributes, target, options ?? CodeGenerationOptions.Default, cancellationToken);
        }

        /// <summary>
        /// Removes the specified attribute node from the given declaration node.
        /// </summary>
        public static TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, Workspace workspace, AttributeData attributeToRemove, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destination.Language).RemoveAttribute(destination, attributeToRemove, options ?? CodeGenerationOptions.Default, cancellationToken);
        }

        /// <summary>
        /// Removes the specified attribute node from the given declaration node.
        /// </summary>
        public static TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, Workspace workspace, SyntaxNode attributeToRemove, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destination.Language).RemoveAttribute(destination, attributeToRemove, options ?? CodeGenerationOptions.Default, cancellationToken);
        }

        public static TDeclarationNode UpdateDeclarationModifiers<TDeclarationNode>(TDeclarationNode destination, Workspace workspace, IEnumerable<SyntaxToken> newModifiers, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destination.Language).UpdateDeclarationModifiers(destination, newModifiers, options ?? new CodeGenerationOptions(reuseSyntax: true), cancellationToken);
        }

        public static TDeclarationNode UpdateDeclarationAccessibility<TDeclarationNode>(TDeclarationNode destination, Workspace workspace, Accessibility newAccessibility, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destination.Language).UpdateDeclarationAccessibility(destination, newAccessibility, options ?? new CodeGenerationOptions(reuseSyntax: true), cancellationToken);
        }

        public static TDeclarationNode UpdateDeclarationType<TDeclarationNode>(TDeclarationNode destination, Workspace workspace, ITypeSymbol newType, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destination.Language).UpdateDeclarationType(destination, newType, options ?? new CodeGenerationOptions(reuseSyntax: true), cancellationToken);
        }

        public static TDeclarationNode UpdateDeclarationMembers<TDeclarationNode>(TDeclarationNode destination, Workspace workspace, IList<ISymbol> newMembers, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destination.Language).UpdateDeclarationMembers(destination, newMembers, options ?? new CodeGenerationOptions(reuseSyntax: true), cancellationToken);
        }

        /// <summary>
        /// Create a new declaration node with one or more statements added to its body.
        /// </summary>
        public static TDeclarationNode AddStatements<TDeclarationNode>(TDeclarationNode destinationMember, IEnumerable<SyntaxNode> statements, Workspace workspace, CodeGenerationOptions options = default) where TDeclarationNode : SyntaxNode
        {
            return GetCodeGenerationService(workspace, destinationMember.Language).AddStatements(destinationMember, statements, options);
        }

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional event of the same signature as the specified event symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddEventDeclarationAsync(Solution solution, INamedTypeSymbol destination, IEventSymbol @event, CodeGenerationOptions options = default, CancellationToken cancellationToken = default)
        {
            return GetCodeGenerationService(solution.Workspace, destination.Language).AddEventAsync(solution, destination, @event, options, cancellationToken);
        }

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional field of the same signature as the specified field symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddFieldDeclarationAsync(Solution solution, INamedTypeSymbol destination, IFieldSymbol field, CodeGenerationOptions options = default, CancellationToken cancellationToken = default)
        {
            return GetCodeGenerationService(solution.Workspace, destination.Language).AddFieldAsync(solution, destination, field, options, cancellationToken);
        }

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional method of the same signature as the specified method symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddMethodDeclarationAsync(Solution solution, INamedTypeSymbol destination, IMethodSymbol method, CodeGenerationOptions options = default, CancellationToken cancellationToken = default)
        {
            return GetCodeGenerationService(solution.Workspace, destination.Language).AddMethodAsync(solution, destination, method, options, cancellationToken);
        }

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional property of the same signature as the specified property symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddPropertyDeclarationAsync(Solution solution, INamedTypeSymbol destination, IPropertySymbol property, CodeGenerationOptions options = default, CancellationToken cancellationToken = default)
        {
            return GetCodeGenerationService(solution.Workspace, destination.Language).AddPropertyAsync(solution, destination, property, options, cancellationToken);
        }

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional named type of the same signature as the specified named type symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddNamedTypeDeclarationAsync(Solution solution, INamedTypeSymbol destination, INamedTypeSymbol namedType, CodeGenerationOptions options = default, CancellationToken cancellationToken = default)
        {
            return GetCodeGenerationService(solution.Workspace, destination.Language).AddNamedTypeAsync(solution, destination, namedType, options, cancellationToken);
        }

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional named type of the same signature as the specified named type symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddNamedTypeDeclarationAsync(Solution solution, INamespaceSymbol destination, INamedTypeSymbol namedType, CodeGenerationOptions options = default, CancellationToken cancellationToken = default)
        {
            return GetCodeGenerationService(solution.Workspace, destination.Language).AddNamedTypeAsync(solution, destination, namedType, options, cancellationToken);
        }

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional namespace of the same signature as the specified namespace symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddNamespaceDeclarationAsync(Solution solution, INamespaceSymbol destination, INamespaceSymbol @namespace, CodeGenerationOptions options = default, CancellationToken cancellationToken = default)
        {
            return GetCodeGenerationService(solution.Workspace, destination.Language).AddNamespaceAsync(solution, destination, @namespace, options, cancellationToken);
        }

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has an additional namespace or type of the same signature as the specified namespace or type symbol.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddNamespaceOrTypeDeclarationAsync(Solution solution, INamespaceSymbol destination, INamespaceOrTypeSymbol namespaceOrType, CodeGenerationOptions options = default, CancellationToken cancellationToken = default)
        {
            return GetCodeGenerationService(solution.Workspace, destination.Language).AddNamespaceOrTypeAsync(solution, destination, namespaceOrType, options, cancellationToken);
        }

        /// <summary>
        /// Create a new solution where the declaration of the destination symbol has additional members of the same signature as the specified member symbols.
        /// Returns the document in the new solution where the destination symbol is declared.
        /// </summary>
        public static Task<Document> AddMemberDeclarationsAsync(Solution solution, INamedTypeSymbol destination, IEnumerable<ISymbol> members, CodeGenerationOptions options = default, CancellationToken cancellationToken = default)
            => GetCodeGenerationService(solution.Workspace, destination.Language).AddMembersAsync(solution, destination, members, options, cancellationToken);

        /// <summary>
        /// Returns <c>true</c> if additional declarations can be added to the destination symbol's declaration.
        /// </summary>
        public static bool CanAdd(Solution solution, ISymbol destination, CancellationToken cancellationToken = default)
        {
            return GetCodeGenerationService(solution.Workspace, destination.Language).CanAddTo(destination, solution, cancellationToken);
        }
    }
}
