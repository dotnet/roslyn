// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#else
using OptionSet = Microsoft.CodeAnalysis.Options.OptionSet;
#endif


namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal interface ICodeGenerationService : ILanguageService
    {
        CodeGenerationPreferences GetPreferences(ParseOptions parseOptions, OptionSet documentOptions);

        /// <summary>
        /// Returns a newly created event declaration node from the provided event.
        /// </summary>
        SyntaxNode CreateEventDeclaration(IEventSymbol @event, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a newly created field declaration node from the provided field.
        /// </summary>
        SyntaxNode CreateFieldDeclaration(IFieldSymbol field, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a newly created method declaration node from the provided method.
        /// TODO: do not return null (https://github.com/dotnet/roslyn/issues/58243)
        /// </summary>
        SyntaxNode? CreateMethodDeclaration(IMethodSymbol method, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a newly created property declaration node from the provided property.
        /// </summary>
        SyntaxNode CreatePropertyDeclaration(IPropertySymbol property, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a newly created named type declaration node from the provided named type.
        /// </summary>
        SyntaxNode CreateNamedTypeDeclaration(INamedTypeSymbol namedType, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a newly created namespace declaration node from the provided namespace.
        /// </summary>
        SyntaxNode CreateNamespaceDeclaration(INamespaceSymbol @namespace, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Adds an event into destination.
        /// </summary>
        TDeclarationNode AddEvent<TDeclarationNode>(TDeclarationNode destination, IEventSymbol @event, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a field into destination.
        /// </summary>
        TDeclarationNode AddField<TDeclarationNode>(TDeclarationNode destination, IFieldSymbol field, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a method into destination.
        /// </summary>
        TDeclarationNode AddMethod<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a property into destination. 
        /// </summary>
        TDeclarationNode AddProperty<TDeclarationNode>(TDeclarationNode destination, IPropertySymbol property, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a named type into destination. 
        /// </summary>
        TDeclarationNode AddNamedType<TDeclarationNode>(TDeclarationNode destination, INamedTypeSymbol namedType, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a namespace into destination. 
        /// </summary>
        TDeclarationNode AddNamespace<TDeclarationNode>(TDeclarationNode destination, INamespaceSymbol @namespace, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds members into destination.
        /// </summary>
        TDeclarationNode AddMembers<TDeclarationNode>(TDeclarationNode destination, IEnumerable<ISymbol> members, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds the parameters to destination.
        /// </summary>
        TDeclarationNode AddParameters<TDeclarationNode>(TDeclarationNode destination, IEnumerable<IParameterSymbol> parameters, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds the attributes to destination.
        /// </summary>
        TDeclarationNode AddAttributes<TDeclarationNode>(TDeclarationNode destination, IEnumerable<AttributeData> attributes, SyntaxToken? target, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Remove the given attribute from destination.
        /// </summary>
        TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, SyntaxNode attributeToRemove, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Remove the given attribute from destination.
        /// </summary>
        TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, AttributeData attributeToRemove, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Update the modifiers list for the given declaration node.
        /// </summary>
        TDeclarationNode UpdateDeclarationModifiers<TDeclarationNode>(TDeclarationNode declaration, IEnumerable<SyntaxToken> newModifiers, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Update the accessibility modifiers for the given declaration node, retaining the trivia of the existing modifiers.
        /// </summary>
        TDeclarationNode UpdateDeclarationAccessibility<TDeclarationNode>(TDeclarationNode declaration, Accessibility newAccessibility, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Update the type for the given declaration node.
        /// </summary>
        TDeclarationNode UpdateDeclarationType<TDeclarationNode>(TDeclarationNode declaration, ITypeSymbol newType, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Replace the existing members with the given newMembers for the given declaration node.
        /// </summary>
        TDeclarationNode UpdateDeclarationMembers<TDeclarationNode>(TDeclarationNode declaration, IList<ISymbol> newMembers, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds the statements to destination.
        /// </summary>
        TDeclarationNode AddStatements<TDeclarationNode>(TDeclarationNode destination, IEnumerable<SyntaxNode> statements, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        /// <summary>
        /// Adds a field with the provided signature into destination.
        /// </summary>
        Task<Document> AddEventAsync(Solution solution, INamedTypeSymbol destination, IEventSymbol @event, CodeGenerationContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a field with the provided signature into destination.
        /// </summary>
        Task<Document> AddFieldAsync(Solution solution, INamedTypeSymbol destination, IFieldSymbol field, CodeGenerationContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a method with the provided signature into destination.
        /// </summary>
        Task<Document> AddMethodAsync(Solution solution, INamedTypeSymbol destination, IMethodSymbol method, CodeGenerationContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a property with the provided signature into destination.
        /// </summary>
        Task<Document> AddPropertyAsync(Solution solution, INamedTypeSymbol destination, IPropertySymbol property, CodeGenerationContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a named type into destination.
        /// </summary>
        Task<Document> AddNamedTypeAsync(Solution solution, INamedTypeSymbol destination, INamedTypeSymbol namedType, CodeGenerationContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a named type into destination.
        /// </summary>
        Task<Document> AddNamedTypeAsync(Solution solution, INamespaceSymbol destination, INamedTypeSymbol namedType, CodeGenerationContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a namespace into destination.
        /// </summary>
        Task<Document> AddNamespaceAsync(Solution solution, INamespaceSymbol destination, INamespaceSymbol @namespace, CodeGenerationContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a namespace or type into destination.
        /// </summary>
        Task<Document> AddNamespaceOrTypeAsync(Solution solution, INamespaceSymbol destination, INamespaceOrTypeSymbol namespaceOrType, CodeGenerationContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Adds all the provided members into destination.
        /// </summary>
        Task<Document> AddMembersAsync(Solution solution, INamedTypeSymbol destination, IEnumerable<ISymbol> members, CodeGenerationContext context, CancellationToken cancellationToken);

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
        Task<SyntaxNode?> FindMostRelevantNameSpaceOrTypeDeclarationAsync(Solution solution, INamespaceOrTypeSymbol namespaceOrType, CodeGenerationContext context, CancellationToken cancellationToken);
    }

    internal static class ICodeGenerationServiceExtensions
    {
        public static CodeGenerationOptions GetOptions(this ICodeGenerationService service, ParseOptions parseOptions, OptionSet documentOptions, CodeGenerationContext context)
            => service.GetPreferences(parseOptions, documentOptions).GetOptions(context);
    }
}
