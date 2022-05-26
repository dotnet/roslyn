// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal interface ICodeGenerationService : ILanguageService
    {
        CodeGenerationOptions DefaultOptions { get; }
        CodeGenerationOptions GetCodeGenerationOptions(AnalyzerConfigOptions options, CodeGenerationOptions? fallbackOptions);

        /// <summary>
        /// Returns a newly created event declaration node from the provided event.
        /// </summary>
        SyntaxNode CreateEventDeclaration(IEventSymbol @event, CodeGenerationDestination destination, CodeGenerationContextInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a newly created field declaration node from the provided field.
        /// </summary>
        SyntaxNode CreateFieldDeclaration(IFieldSymbol field, CodeGenerationDestination destination, CodeGenerationContextInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a newly created method declaration node from the provided method.
        /// TODO: do not return null (https://github.com/dotnet/roslyn/issues/58243)
        /// </summary>
        SyntaxNode? CreateMethodDeclaration(IMethodSymbol method, CodeGenerationDestination destination, CodeGenerationContextInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a newly created property declaration node from the provided property.
        /// </summary>
        SyntaxNode CreatePropertyDeclaration(IPropertySymbol property, CodeGenerationDestination destination, CodeGenerationContextInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a newly created named type declaration node from the provided named type.
        /// </summary>
        SyntaxNode CreateNamedTypeDeclaration(INamedTypeSymbol namedType, CodeGenerationDestination destination, CodeGenerationContextInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a newly created namespace declaration node from the provided namespace.
        /// </summary>
        SyntaxNode CreateNamespaceDeclaration(INamespaceSymbol @namespace, CodeGenerationDestination destination, CodeGenerationContextInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Adds an event into destination.
        /// </summary>
        TDeclarationNode AddEvent<TDeclarationNode>(TDeclarationNode destination, IEventSymbol @event, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a field into destination.
        /// </summary>
        TDeclarationNode AddField<TDeclarationNode>(TDeclarationNode destination, IFieldSymbol field, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a method into destination.
        /// </summary>
        TDeclarationNode AddMethod<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a property into destination. 
        /// </summary>
        TDeclarationNode AddProperty<TDeclarationNode>(TDeclarationNode destination, IPropertySymbol property, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a named type into destination. 
        /// </summary>
        TDeclarationNode AddNamedType<TDeclarationNode>(TDeclarationNode destination, INamedTypeSymbol namedType, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a namespace into destination. 
        /// </summary>
        TDeclarationNode AddNamespace<TDeclarationNode>(TDeclarationNode destination, INamespaceSymbol @namespace, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds members into destination.
        /// </summary>
        TDeclarationNode AddMembers<TDeclarationNode>(TDeclarationNode destination, IEnumerable<ISymbol> members, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds the parameters to destination.
        /// </summary>
        TDeclarationNode AddParameters<TDeclarationNode>(TDeclarationNode destination, IEnumerable<IParameterSymbol> parameters, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds the attributes to destination.
        /// </summary>
        TDeclarationNode AddAttributes<TDeclarationNode>(TDeclarationNode destination, IEnumerable<AttributeData> attributes, SyntaxToken? target, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Remove the given attribute from destination.
        /// </summary>
        TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, SyntaxNode attributeToRemove, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Remove the given attribute from destination.
        /// </summary>
        TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, AttributeData attributeToRemove, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Update the modifiers list for the given declaration node.
        /// </summary>
        TDeclarationNode UpdateDeclarationModifiers<TDeclarationNode>(TDeclarationNode declaration, IEnumerable<SyntaxToken> newModifiers, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Update the accessibility modifiers for the given declaration node, retaining the trivia of the existing modifiers.
        /// </summary>
        TDeclarationNode UpdateDeclarationAccessibility<TDeclarationNode>(TDeclarationNode declaration, Accessibility newAccessibility, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Update the type for the given declaration node.
        /// </summary>
        TDeclarationNode UpdateDeclarationType<TDeclarationNode>(TDeclarationNode declaration, ITypeSymbol newType, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Replace the existing members with the given newMembers for the given declaration node.
        /// </summary>
        TDeclarationNode UpdateDeclarationMembers<TDeclarationNode>(TDeclarationNode declaration, IList<ISymbol> newMembers, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds the statements to destination.
        /// </summary>
        TDeclarationNode AddStatements<TDeclarationNode>(TDeclarationNode destination, IEnumerable<SyntaxNode> statements, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a field with the provided signature into destination.
        /// </summary>
        Task<Document> AddEventAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IEventSymbol @event, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a field with the provided signature into destination.
        /// </summary>
        Task<Document> AddFieldAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IFieldSymbol field, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a method with the provided signature into destination.
        /// </summary>
        Task<Document> AddMethodAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IMethodSymbol method, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a property with the provided signature into destination.
        /// </summary>
        Task<Document> AddPropertyAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IPropertySymbol property, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a named type into destination.
        /// </summary>
        Task<Document> AddNamedTypeAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, INamedTypeSymbol namedType, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a named type into destination.
        /// </summary>
        Task<Document> AddNamedTypeAsync(CodeGenerationSolutionContext context, INamespaceSymbol destination, INamedTypeSymbol namedType, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a namespace into destination.
        /// </summary>
        Task<Document> AddNamespaceAsync(CodeGenerationSolutionContext context, INamespaceSymbol destination, INamespaceSymbol @namespace, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a namespace or type into destination.
        /// </summary>
        Task<Document> AddNamespaceOrTypeAsync(CodeGenerationSolutionContext context, INamespaceSymbol destination, INamespaceOrTypeSymbol namespaceOrType, CancellationToken cancellationToken);

        /// <summary>
        /// Adds all the provided members into destination.
        /// </summary>
        Task<Document> AddMembersAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IEnumerable<ISymbol> members, CancellationToken cancellationToken);

        /// <summary>
        /// <c>true</c> if destination is a location where other symbols can be added to.
        /// </summary>
        bool CanAddTo(ISymbol destination, Solution solution, CancellationToken cancellationToken);

        /// <summary>
        /// <c>true</c> if destination is a location where other symbols can be added to.
        /// </summary>
        bool CanAddTo(SyntaxNode destination, Solution solution, CancellationToken cancellationToken);

        /// <summary>
        /// Return the most relevant declaration to namespaceOrType,
        /// it will first search the context node contained within,
        /// then the declaration in the same file, then non auto-generated file,
        /// then all the potential location. Return null if no declaration.
        /// </summary>
        Task<SyntaxNode?> FindMostRelevantNameSpaceOrTypeDeclarationAsync(Solution solution, INamespaceOrTypeSymbol namespaceOrType, Location? location, CancellationToken cancellationToken);
    }
}
