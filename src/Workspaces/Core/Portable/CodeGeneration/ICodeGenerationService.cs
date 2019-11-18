// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal interface ICodeGenerationService : ILanguageService
    {
        /// <summary>
        /// Returns a newly created event declaration node from the provided event.
        /// </summary>
        SyntaxNode CreateEventDeclaration(IEventSymbol @event, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified, CodeGenerationOptions options = null);

        /// <summary>
        /// Returns a newly created field declaration node from the provided field.
        /// </summary>
        SyntaxNode CreateFieldDeclaration(IFieldSymbol field, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified, CodeGenerationOptions options = null);

        /// <summary>
        /// Returns a newly created method declaration node from the provided method.
        /// </summary>
        SyntaxNode CreateMethodDeclaration(IMethodSymbol method, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified, CodeGenerationOptions options = null, SyntaxNode destinationNode = null, bool createLocalFunction = false);

        /// <summary>
        /// Returns a newly created property declaration node from the provided property.
        /// </summary>
        SyntaxNode CreatePropertyDeclaration(IPropertySymbol property, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified, CodeGenerationOptions options = null);

        /// <summary>
        /// Returns a newly created named type declaration node from the provided named type.
        /// </summary>
        SyntaxNode CreateNamedTypeDeclaration(INamedTypeSymbol namedType, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified, CodeGenerationOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a newly created namespace declaration node from the provided namespace.
        /// </summary>
        SyntaxNode CreateNamespaceDeclaration(INamespaceSymbol @namespace, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified, CodeGenerationOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds an event into destination.
        /// </summary>
        TDeclarationNode AddEvent<TDeclarationNode>(TDeclarationNode destination, IEventSymbol @event, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a field into destination.
        /// </summary>
        TDeclarationNode AddField<TDeclarationNode>(TDeclarationNode destination, IFieldSymbol field, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a method into destination.
        /// </summary>
        TDeclarationNode AddMethod<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a property into destination. 
        /// </summary>
        TDeclarationNode AddProperty<TDeclarationNode>(TDeclarationNode destination, IPropertySymbol property, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a named type into destination. 
        /// </summary>
        TDeclarationNode AddNamedType<TDeclarationNode>(TDeclarationNode destination, INamedTypeSymbol namedType, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a namespace into destination. 
        /// </summary>
        TDeclarationNode AddNamespace<TDeclarationNode>(TDeclarationNode destination, INamespaceSymbol @namespace, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds members into destination.
        /// </summary>
        TDeclarationNode AddMembers<TDeclarationNode>(TDeclarationNode destination, IEnumerable<ISymbol> members, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds the parameters to destination.
        /// </summary>
        TDeclarationNode AddParameters<TDeclarationNode>(TDeclarationNode destination, IEnumerable<IParameterSymbol> parameters, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds the attributes to destination.
        /// </summary>
        TDeclarationNode AddAttributes<TDeclarationNode>(TDeclarationNode destination, IEnumerable<AttributeData> attributes, SyntaxToken? target = null, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Remove the given attribute from destination.
        /// </summary>
        TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, SyntaxNode attributeToRemove, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Remove the given attribute from destination.
        /// </summary>
        TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, AttributeData attributeToRemove, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Update the modifiers list for the given declaration node.
        /// </summary>
        TDeclarationNode UpdateDeclarationModifiers<TDeclarationNode>(TDeclarationNode declaration, IEnumerable<SyntaxToken> newModifiers, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Update the accessibility modifiers for the given declaration node, retaining the trivia of the existing modifiers.
        /// </summary>
        TDeclarationNode UpdateDeclarationAccessibility<TDeclarationNode>(TDeclarationNode declaration, Accessibility newAccessibility, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Update the type for the given declaration node.
        /// </summary>
        TDeclarationNode UpdateDeclarationType<TDeclarationNode>(TDeclarationNode declaration, ITypeSymbol newType, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Replace the existing members with the given newMembers for the given declaration node.
        /// </summary>
        TDeclarationNode UpdateDeclarationMembers<TDeclarationNode>(TDeclarationNode declaration, IList<ISymbol> newMembers, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds the statements to destination.
        /// </summary>
        TDeclarationNode AddStatements<TDeclarationNode>(TDeclarationNode destination, IEnumerable<SyntaxNode> statements, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a field with the provided signature into destination.
        /// </summary>
        Task<Document> AddEventAsync(Solution solution, INamedTypeSymbol destination, IEventSymbol @event, CodeGenerationOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a field with the provided signature into destination.
        /// </summary>
        Task<Document> AddFieldAsync(Solution solution, INamedTypeSymbol destination, IFieldSymbol field, CodeGenerationOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a method with the provided signature into destination.
        /// </summary>
        Task<Document> AddMethodAsync(Solution solution, INamedTypeSymbol destination, IMethodSymbol method, CodeGenerationOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a property with the provided signature into destination.
        /// </summary>
        Task<Document> AddPropertyAsync(Solution solution, INamedTypeSymbol destination, IPropertySymbol property, CodeGenerationOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a named type into destination.
        /// </summary>
        Task<Document> AddNamedTypeAsync(Solution solution, INamedTypeSymbol destination, INamedTypeSymbol namedType, CodeGenerationOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a named type into destination.
        /// </summary>
        Task<Document> AddNamedTypeAsync(Solution solution, INamespaceSymbol destination, INamedTypeSymbol namedType, CodeGenerationOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a namespace into destination.
        /// </summary>
        Task<Document> AddNamespaceAsync(Solution solution, INamespaceSymbol destination, INamespaceSymbol @namespace, CodeGenerationOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a namespace or type into destination.
        /// </summary>
        Task<Document> AddNamespaceOrTypeAsync(Solution solution, INamespaceSymbol destination, INamespaceOrTypeSymbol namespaceOrType, CodeGenerationOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds all the provided members into destination.
        /// </summary>
        Task<Document> AddMembersAsync(Solution solution, INamedTypeSymbol destination, IEnumerable<ISymbol> members, CodeGenerationOptions options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// <c>true</c> if destination is a location where other symbols can be added to.
        /// </summary>
        bool CanAddTo(ISymbol destination, Solution solution, CancellationToken cancellationToken = default);

        /// <summary>
        /// <c>true</c> if destination is a location where other symbols can be added to.
        /// </summary>
        bool CanAddTo(SyntaxNode destination, Solution solution, CancellationToken cancellationToken = default);

        /// <summary>
        /// Return the most relevant declaration to namespaceOrType,
        /// it will first search the context node contained within,
        /// then the declaration in the same file, then non auto-generated file,
        /// then all the potential location. Return null if no declaration.
        /// </summary>
        Task<SyntaxNode> FindMostRelevantNameSpaceOrTypeDeclarationAsync(Solution solution, INamespaceOrTypeSymbol namespaceOrType, CodeGenerationOptions options, CancellationToken cancellationToken);
    }
}
