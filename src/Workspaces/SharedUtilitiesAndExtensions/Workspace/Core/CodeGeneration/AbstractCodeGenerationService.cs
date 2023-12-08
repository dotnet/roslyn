// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract partial class AbstractCodeGenerationService<TCodeGenerationContextInfo> : ICodeGenerationService
        where TCodeGenerationContextInfo : CodeGenerationContextInfo
    {
        private readonly ISymbolDeclarationService _symbolDeclarationService;

        protected AbstractCodeGenerationService(
            LanguageServices languageServices)
        {
            LanguageServices = languageServices;
            _symbolDeclarationService = languageServices.GetRequiredService<ISymbolDeclarationService>();
        }

        public LanguageServices LanguageServices { get; }

        public abstract CodeGenerationOptions DefaultOptions { get; }
        public abstract CodeGenerationOptions GetCodeGenerationOptions(IOptionsReader options, CodeGenerationOptions? fallbackOptions);
        public abstract TCodeGenerationContextInfo GetInfo(CodeGenerationContext context, CodeGenerationOptions options, ParseOptions parseOptions);

        CodeGenerationContextInfo ICodeGenerationService.GetInfo(CodeGenerationContext context, CodeGenerationOptions options, ParseOptions parseOptions)
            => GetInfo(context, options, parseOptions);

        #region ICodeGenerationService

        public TDeclarationNode AddEvent<TDeclarationNode>(TDeclarationNode destination, IEventSymbol @event, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddEvent(destination, @event, (TCodeGenerationContextInfo)info, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), info);

        public TDeclarationNode AddField<TDeclarationNode>(TDeclarationNode destination, IFieldSymbol field, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddField(destination, field, (TCodeGenerationContextInfo)info, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), info);

        public TDeclarationNode AddMethod<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddMethod(destination, method, (TCodeGenerationContextInfo)info, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), info);

        public TDeclarationNode AddProperty<TDeclarationNode>(TDeclarationNode destination, IPropertySymbol property, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddProperty(destination, property, (TCodeGenerationContextInfo)info, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), info);

        public TDeclarationNode AddNamedType<TDeclarationNode>(TDeclarationNode destination, INamedTypeSymbol namedType, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddNamedType(destination, namedType, (TCodeGenerationContextInfo)info, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), info);

        public TDeclarationNode AddNamespace<TDeclarationNode>(TDeclarationNode destination, INamespaceSymbol @namespace, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddNamespace(destination, @namespace, (TCodeGenerationContextInfo)info, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), info);

        public TDeclarationNode AddMembers<TDeclarationNode>(TDeclarationNode destination, IEnumerable<ISymbol> members, CodeGenerationContextInfo info, CancellationToken cancellationToken)
            where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddMembers(destination, members, GetAvailableInsertionIndices(destination, cancellationToken), (TCodeGenerationContextInfo)info, cancellationToken), info);

        private static TNode WithAnnotations<TNode>(TNode node, CodeGenerationContextInfo info) where TNode : SyntaxNode
        {
            return info.Context.AddImports
                ? node.WithAdditionalAnnotations(Simplifier.AddImportsAnnotation)
                : node;
        }

        public SyntaxNode CreateEventDeclaration(IEventSymbol @event, CodeGenerationDestination destination, CodeGenerationContextInfo info, CancellationToken cancellationToken)
            => CreateEventDeclaration(@event, destination, (TCodeGenerationContextInfo)info, cancellationToken);

        public SyntaxNode CreateFieldDeclaration(IFieldSymbol field, CodeGenerationDestination destination, CodeGenerationContextInfo info, CancellationToken cancellationToken)
            => CreateFieldDeclaration(field, destination, (TCodeGenerationContextInfo)info, cancellationToken);

        public SyntaxNode? CreateMethodDeclaration(IMethodSymbol method, CodeGenerationDestination destination, CodeGenerationContextInfo info, CancellationToken cancellationToken)
            => CreateMethodDeclaration(method, destination, (TCodeGenerationContextInfo)info, cancellationToken);

        public SyntaxNode CreatePropertyDeclaration(IPropertySymbol property, CodeGenerationDestination destination, CodeGenerationContextInfo info, CancellationToken cancellationToken)
            => CreatePropertyDeclaration(property, destination, (TCodeGenerationContextInfo)info, cancellationToken);

        public SyntaxNode CreateNamedTypeDeclaration(INamedTypeSymbol namedType, CodeGenerationDestination destination, CodeGenerationContextInfo info, CancellationToken cancellationToken)
            => CreateNamedTypeDeclaration(namedType, destination, (TCodeGenerationContextInfo)info, cancellationToken);

        public SyntaxNode CreateNamespaceDeclaration(INamespaceSymbol @namespace, CodeGenerationDestination destination, CodeGenerationContextInfo info, CancellationToken cancellationToken)
            => CreateNamespaceDeclaration(@namespace, destination, (TCodeGenerationContextInfo)info, cancellationToken);

        public TDeclarationNode AddParameters<TDeclarationNode>(TDeclarationNode destination, IEnumerable<IParameterSymbol> parameters, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => AddParameters(destination, parameters, (TCodeGenerationContextInfo)info, cancellationToken);

        public TDeclarationNode AddAttributes<TDeclarationNode>(TDeclarationNode destination, IEnumerable<AttributeData> attributes, SyntaxToken? target, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => AddAttributes(destination, attributes, target, (TCodeGenerationContextInfo)info, cancellationToken);

        public TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, SyntaxNode attributeToRemove, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => RemoveAttribute(destination, attributeToRemove, (TCodeGenerationContextInfo)info, cancellationToken);

        public TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, AttributeData attributeToRemove, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => RemoveAttribute(destination, attributeToRemove, (TCodeGenerationContextInfo)info, cancellationToken);

        public TDeclarationNode UpdateDeclarationModifiers<TDeclarationNode>(TDeclarationNode declaration, IEnumerable<SyntaxToken> newModifiers, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => UpdateDeclarationModifiers(declaration, newModifiers, (TCodeGenerationContextInfo)info, cancellationToken);

        public TDeclarationNode UpdateDeclarationAccessibility<TDeclarationNode>(TDeclarationNode declaration, Accessibility newAccessibility, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => UpdateDeclarationAccessibility(declaration, newAccessibility, (TCodeGenerationContextInfo)info, cancellationToken);

        public TDeclarationNode UpdateDeclarationType<TDeclarationNode>(TDeclarationNode declaration, ITypeSymbol newType, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => UpdateDeclarationType(declaration, newType, (TCodeGenerationContextInfo)info, cancellationToken);

        public TDeclarationNode UpdateDeclarationMembers<TDeclarationNode>(TDeclarationNode declaration, IList<ISymbol> newMembers, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => UpdateDeclarationMembers(declaration, newMembers, (TCodeGenerationContextInfo)info, cancellationToken);

        public TDeclarationNode AddStatements<TDeclarationNode>(TDeclarationNode destination, IEnumerable<SyntaxNode> statements, CodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => AddStatements(destination, statements, (TCodeGenerationContextInfo)info, cancellationToken);

        #endregion

        protected abstract TDeclarationNode AddEvent<TDeclarationNode>(TDeclarationNode destination, IEventSymbol @event, TCodeGenerationContextInfo info, IList<bool>? availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddField<TDeclarationNode>(TDeclarationNode destination, IFieldSymbol field, TCodeGenerationContextInfo info, IList<bool>? availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddMethod<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, TCodeGenerationContextInfo info, IList<bool>? availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddProperty<TDeclarationNode>(TDeclarationNode destination, IPropertySymbol property, TCodeGenerationContextInfo info, IList<bool>? availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddNamedType<TDeclarationNode>(TDeclarationNode destination, INamedTypeSymbol namedType, TCodeGenerationContextInfo info, IList<bool>? availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddNamespace<TDeclarationNode>(TDeclarationNode destination, INamespaceSymbol @namespace, TCodeGenerationContextInfo info, IList<bool>? availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddMembers<TDeclarationNode>(TDeclarationNode destination, IEnumerable<SyntaxNode> members) where TDeclarationNode : SyntaxNode;

        public abstract TDeclarationNode AddParameters<TDeclarationNode>(TDeclarationNode destinationMember, IEnumerable<IParameterSymbol> parameters, TCodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode AddAttributes<TDeclarationNode>(TDeclarationNode destination, IEnumerable<AttributeData> attributes, SyntaxToken? target, TCodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, SyntaxNode attributeToRemove, TCodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, AttributeData attributeToRemove, TCodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode AddStatements<TDeclarationNode>(TDeclarationNode destinationMember, IEnumerable<SyntaxNode> statements, TCodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        public abstract TDeclarationNode UpdateDeclarationModifiers<TDeclarationNode>(TDeclarationNode declaration, IEnumerable<SyntaxToken> newModifiers, TCodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode UpdateDeclarationAccessibility<TDeclarationNode>(TDeclarationNode declaration, Accessibility newAccessibility, TCodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode UpdateDeclarationType<TDeclarationNode>(TDeclarationNode declaration, ITypeSymbol newType, TCodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode UpdateDeclarationMembers<TDeclarationNode>(TDeclarationNode declaration, IList<ISymbol> newMembers, TCodeGenerationContextInfo info, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        public abstract CodeGenerationDestination GetDestination(SyntaxNode node);
        public abstract SyntaxNode CreateEventDeclaration(IEventSymbol @event, CodeGenerationDestination destination, TCodeGenerationContextInfo info, CancellationToken cancellationToken);
        public abstract SyntaxNode CreateFieldDeclaration(IFieldSymbol field, CodeGenerationDestination destination, TCodeGenerationContextInfo info, CancellationToken cancellationToken);

        // TODO: Change to not return null (https://github.com/dotnet/roslyn/issues/58243)
        public abstract SyntaxNode? CreateMethodDeclaration(IMethodSymbol method, CodeGenerationDestination destination, TCodeGenerationContextInfo info, CancellationToken cancellationToken);

        public abstract SyntaxNode CreatePropertyDeclaration(IPropertySymbol property, CodeGenerationDestination destination, TCodeGenerationContextInfo info, CancellationToken cancellationToken);
        public abstract SyntaxNode CreateNamedTypeDeclaration(INamedTypeSymbol namedType, CodeGenerationDestination destination, TCodeGenerationContextInfo info, CancellationToken cancellationToken);
        public abstract SyntaxNode CreateNamespaceDeclaration(INamespaceSymbol @namespace, CodeGenerationDestination destination, TCodeGenerationContextInfo info, CancellationToken cancellationToken);

        protected static T Cast<T>(object value)
            => (T)value;

        protected static void CheckDeclarationNode<TDeclarationNode>(SyntaxNode destination) where TDeclarationNode : SyntaxNode
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination is not TDeclarationNode)
            {
                throw new ArgumentException(
                    string.Format(WorkspaceExtensionsResources.Destination_type_must_be_a_0_but_given_one_is_1, typeof(TDeclarationNode).Name, destination.GetType().Name),
                    nameof(destination));
            }
        }

        protected static void CheckDeclarationNode<TDeclarationNode1, TDeclarationNode2>(SyntaxNode destination)
            where TDeclarationNode1 : SyntaxNode
            where TDeclarationNode2 : SyntaxNode
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination is not TDeclarationNode1 and
                not TDeclarationNode2)
            {
                throw new ArgumentException(
                    string.Format(WorkspaceExtensionsResources.Destination_type_must_be_a_0_or_a_1_but_given_one_is_2,
                        typeof(TDeclarationNode1).Name, typeof(TDeclarationNode2).Name, destination.GetType().Name),
                    nameof(destination));
            }
        }

        protected static void CheckDeclarationNode<TDeclarationNode1, TDeclarationNode2, TDeclarationNode3>(SyntaxNode destination)
            where TDeclarationNode1 : SyntaxNode
            where TDeclarationNode2 : SyntaxNode
            where TDeclarationNode3 : SyntaxNode
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination is not TDeclarationNode1 and
                not TDeclarationNode2 and
                not TDeclarationNode3)
            {
                throw new ArgumentException(
                    string.Format(WorkspaceExtensionsResources.Destination_type_must_be_a_0_1_or_2_but_given_one_is_3,
                        typeof(TDeclarationNode1).Name, typeof(TDeclarationNode2).Name, typeof(TDeclarationNode3).Name, destination.GetType().Name),
                    nameof(destination));
            }
        }

        protected static void CheckDeclarationNode<TDeclarationNode1, TDeclarationNode2, TDeclarationNode3, TDeclarationNode4>(SyntaxNode destination)
            where TDeclarationNode1 : SyntaxNode
            where TDeclarationNode2 : SyntaxNode
            where TDeclarationNode3 : SyntaxNode
            where TDeclarationNode4 : SyntaxNode
        {
            if (destination is not TDeclarationNode1 and
                not TDeclarationNode2 and
                not TDeclarationNode3 and
                not TDeclarationNode4)
            {
                throw new ArgumentException(
                    string.Format(WorkspaceExtensionsResources.Destination_type_must_be_a_0_1_2_or_3_but_given_one_is_4,
                        typeof(TDeclarationNode1).Name, typeof(TDeclarationNode2).Name, typeof(TDeclarationNode3).Name, typeof(TDeclarationNode4).Name, destination.GetType().Name),
                    nameof(destination));
            }
        }

        private async Task<Document> GetEditAsync(
            CodeGenerationSolutionContext context,
            INamespaceOrTypeSymbol destination,
            Func<SyntaxNode, TCodeGenerationContextInfo, IList<bool>?, CancellationToken, SyntaxNode> declarationTransform,
            CancellationToken cancellationToken)
        {
            var (destinationDeclaration, availableIndices) =
                await FindMostRelevantDeclarationAsync(context.Solution, destination, context.Context.BestLocation, cancellationToken).ConfigureAwait(false);

            if (destinationDeclaration == null)
            {
                throw new ArgumentException(WorkspaceExtensionsResources.Could_not_find_location_to_generation_symbol_into);
            }

            var destinationTree = destinationDeclaration.SyntaxTree;
            var oldDocument = context.Solution.GetRequiredDocument(destinationTree);
#if CODE_STYLE
            var codeGenOptions = CodeGenerationOptions.CommonDefaults;
#else
            var codeGenOptions = await oldDocument.GetCodeGenerationOptionsAsync(context.FallbackOptions, cancellationToken).ConfigureAwait(false);
#endif
            var info = GetInfo(context.Context, codeGenOptions, destinationDeclaration.SyntaxTree.Options);
            var transformedDeclaration = declarationTransform(destinationDeclaration, info, availableIndices, cancellationToken);

            var root = await destinationTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var currentRoot = root.ReplaceNode(destinationDeclaration, transformedDeclaration);

            var newDocument = oldDocument.WithSyntaxRoot(currentRoot);

#if !CODE_STYLE
            if (context.Context.AddImports)
            {
                var addImportsOptions = await newDocument.GetAddImportPlacementOptionsAsync(context.FallbackOptions, cancellationToken).ConfigureAwait(false);
                newDocument = await ImportAdder.AddImportsFromSymbolAnnotationAsync(newDocument, addImportsOptions, cancellationToken).ConfigureAwait(false);
            }
#endif

            return newDocument;
        }

        protected TDeclarationNode AddMembers<TDeclarationNode>(
            TDeclarationNode destination,
            IEnumerable<ISymbol> members,
            IList<bool>? availableIndices,
            TCodeGenerationContextInfo info,
            CancellationToken cancellationToken)
            where TDeclarationNode : SyntaxNode
        {
            var membersList = members.ToList();
            if (membersList.Count > 1)
            {
                info = CreateContextInfoForMultipleMembers(info);
            }

            // Filter out the members that are implicitly declared.  They're implicit, hence we do
            // not want an explicit declaration. The only exception are fields generated from implicit tuple fields.
            var filteredMembers = membersList.Where(m => !m.IsImplicitlyDeclared || m.IsTupleField());

            return info.Context.AutoInsertionLocation
                ? AddMembersToAppropriateLocationInDestination(destination, filteredMembers, availableIndices, info, cancellationToken)
                : AddMembersToEndOfDestination(destination, filteredMembers, info, cancellationToken);
        }

        private TDeclarationSyntax AddMembersToEndOfDestination<TDeclarationSyntax>(
            TDeclarationSyntax destination,
            IEnumerable<ISymbol> members,
            TCodeGenerationContextInfo info,
            CancellationToken cancellationToken)
            where TDeclarationSyntax : SyntaxNode
        {
            var newMembers = new List<SyntaxNode>();
            var codeGenerationDestination = GetDestination(destination);
            foreach (var member in members)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var newMember = GetNewMember(info, codeGenerationDestination, member, cancellationToken);

                if (newMember != null)
                {
                    newMembers.Add(newMember);
                }
            }

            // Metadata as source generates complete declarations and doesn't modify
            // existing ones. We can take the members to generate, sort them once,
            // and then add them in that order to the end of the destination.
            if (!GeneratingEnum(members) && info.Context.SortMembers)
            {
                newMembers.Sort(GetMemberComparer());
            }

            return this.AddMembers(destination, newMembers);
        }

        private TDeclarationSyntax AddMembersToAppropriateLocationInDestination<TDeclarationSyntax>(
            TDeclarationSyntax destination,
            IEnumerable<ISymbol> members,
            IList<bool>? availableIndices,
            TCodeGenerationContextInfo info,
            CancellationToken cancellationToken)
            where TDeclarationSyntax : SyntaxNode
        {
            var currentDestination = destination;

            foreach (var member in members)
            {
                cancellationToken.ThrowIfCancellationRequested();
                currentDestination = UpdateDestination(availableIndices, info, currentDestination, member, cancellationToken);
            }

            return currentDestination;
        }

        private SyntaxNode? GetNewMember(TCodeGenerationContextInfo info, CodeGenerationDestination codeGenerationDestination, ISymbol member, CancellationToken cancellationToken)
            => member switch
            {
                IEventSymbol @event => CreateEventDeclaration(@event, codeGenerationDestination, info, cancellationToken),
                IFieldSymbol field => CreateFieldDeclaration(field, codeGenerationDestination, info, cancellationToken),
                IPropertySymbol property => CreatePropertyDeclaration(property, codeGenerationDestination, info, cancellationToken),
                IMethodSymbol method => CreateMethodDeclaration(method, codeGenerationDestination, info, cancellationToken),
                INamedTypeSymbol namedType => CreateNamedTypeDeclaration(namedType, codeGenerationDestination, info, cancellationToken),
                INamespaceSymbol @namespace => CreateNamespaceDeclaration(@namespace, codeGenerationDestination, info, cancellationToken),
                _ => null,
            };

        private TDeclarationNode UpdateDestination<TDeclarationNode>(
            IList<bool>? availableIndices,
            TCodeGenerationContextInfo info,
            TDeclarationNode currentDestination,
            ISymbol member,
            CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return member switch
            {
                IEventSymbol @event => AddEvent(currentDestination, @event, info, availableIndices, cancellationToken),
                IFieldSymbol field => AddField(currentDestination, field, info, availableIndices, cancellationToken),
                IPropertySymbol property => AddProperty(currentDestination, property, info, availableIndices, cancellationToken),
                IMethodSymbol method => AddMethod(currentDestination, method, info, availableIndices, cancellationToken),
                INamedTypeSymbol namedType => AddNamedType(currentDestination, namedType, info, availableIndices, cancellationToken),
                INamespaceSymbol @namespace => AddNamespace(currentDestination, @namespace, info, availableIndices, cancellationToken),
                _ => currentDestination,
            };
        }

        private static bool GeneratingEnum(IEnumerable<ISymbol> members)
        {
            var field = members.OfType<IFieldSymbol>().FirstOrDefault();
            return field != null && field.ContainingType.IsEnumType();
        }

        protected abstract IComparer<SyntaxNode> GetMemberComparer();

        protected static TCodeGenerationContextInfo CreateContextInfoForMultipleMembers(TCodeGenerationContextInfo info)
        {
            // For now we ignore the afterThisLocation/beforeThisLocation if we're adding
            // multiple members.  In the future it would be nice to appropriately handle this.
            // The difficulty lies with ensuring that we properly understand the position we're
            // inserting into, even as we change the type by adding multiple members.  Not
            // impossible to figure out, but out of scope right now.
            return (TCodeGenerationContextInfo)info.WithContext(info.Context.With(afterThisLocation: null, beforeThisLocation: null));
        }

        public virtual Task<Document> AddEventAsync(
            CodeGenerationSolutionContext context, INamedTypeSymbol destination, IEventSymbol @event, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                context,
                destination,
                (t, opts, ai, ct) => AddEvent(t, @event, opts, ai, ct),
                cancellationToken);
        }

        public Task<Document> AddFieldAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IFieldSymbol field, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                context,
                destination,
                (t, opts, ai, ct) => AddField(t, field, opts, ai, ct),
                cancellationToken);
        }

        public Task<Document> AddPropertyAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IPropertySymbol property, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                context,
                destination,
                (t, opts, ai, ct) => AddProperty(t, property, opts, ai, ct),
                cancellationToken);
        }

        public Task<Document> AddNamedTypeAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, INamedTypeSymbol namedType, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                context,
                destination,
                (t, opts, ai, ct) => AddNamedType(t, namedType, opts, ai, ct),
                cancellationToken);
        }

        public Task<Document> AddNamedTypeAsync(CodeGenerationSolutionContext context, INamespaceSymbol destination, INamedTypeSymbol namedType, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                context,
                destination,
                (t, opts, ai, ct) => AddNamedType(t, namedType, opts, ai, ct),
                cancellationToken);
        }

        public Task<Document> AddNamespaceAsync(CodeGenerationSolutionContext context, INamespaceSymbol destination, INamespaceSymbol @namespace, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                context,
                destination,
                (t, opts, ai, ct) => AddNamespace(t, @namespace, opts, ai, ct),
                cancellationToken);
        }

        public Task<Document> AddMethodAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IMethodSymbol method, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                context,
                destination,
                (t, opts, ai, ct) => AddMethod(t, method, opts, ai, ct),
                cancellationToken);
        }

        public Task<Document> AddMembersAsync(CodeGenerationSolutionContext context, INamedTypeSymbol destination, IEnumerable<ISymbol> members, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                context,
                destination,
                (t, opts, ai, ct) => AddMembers(t, members, ai, opts, ct),
                cancellationToken);
        }

        public Task<Document> AddNamespaceOrTypeAsync(CodeGenerationSolutionContext context, INamespaceSymbol destination, INamespaceOrTypeSymbol namespaceOrType, CancellationToken cancellationToken)
        {
            if (namespaceOrType == null)
            {
                throw new ArgumentNullException(nameof(namespaceOrType));
            }

            if (namespaceOrType is INamespaceSymbol namespaceSymbol)
            {
                return AddNamespaceAsync(context, destination, namespaceSymbol, cancellationToken);
            }
            else
            {
                return AddNamedTypeAsync(context, destination, (INamedTypeSymbol)namespaceOrType, cancellationToken);
            }
        }

        protected static void CheckLocation(SyntaxNode destinationMember, [NotNull] Location? location)
        {
            if (location == null)
            {
                throw new ArgumentException(WorkspaceExtensionsResources.No_location_provided_to_add_statements_to);
            }

            if (!location.IsInSource)
            {
                throw new ArgumentException(WorkspaceExtensionsResources.Destination_location_was_not_in_source);
            }

            if (location.SourceTree != destinationMember.SyntaxTree)
            {
                throw new ArgumentException(WorkspaceExtensionsResources.Destination_location_was_from_a_different_tree);
            }
        }

        protected static void ComputePositionAndTriviaForRemoveAttributeList(
            SyntaxNode attributeList,
            Func<SyntaxTrivia, bool> isEndOfLineTrivia,
            out int positionOfRemovedNode,
            out IEnumerable<SyntaxTrivia> triviaOfRemovedNode)
        {
            positionOfRemovedNode = attributeList.FullSpan.Start;
            var leading = attributeList.GetLeadingTrivia();
            var trailing = attributeList.GetTrailingTrivia();
            if (trailing.Count >= 1 && isEndOfLineTrivia(trailing.Last()))
            {
                // Remove redundant trailing trivia as we are removing the entire attribute list.
                triviaOfRemovedNode = leading;
            }
            else
            {
                triviaOfRemovedNode = leading.Concat(trailing);
            }
        }

        protected static void ComputePositionAndTriviaForRemoveAttributeFromAttributeList(
            SyntaxNode attributeToRemove,
            Func<SyntaxToken, bool> isComma,
            out int positionOfRemovedNode,
            out IEnumerable<SyntaxTrivia> triviaOfRemovedNode)
        {
            positionOfRemovedNode = attributeToRemove.FullSpan.Start;
            var root = attributeToRemove.SyntaxTree.GetRoot();
            var previousToken = root.FindToken(attributeToRemove.FullSpan.Start - 1);
            var leading = isComma(previousToken) ? previousToken.LeadingTrivia : attributeToRemove.GetLeadingTrivia();
            var nextToken = root.FindToken(attributeToRemove.FullSpan.End + 1);
            var trailing = isComma(nextToken) ? nextToken.TrailingTrivia : attributeToRemove.GetTrailingTrivia();
            triviaOfRemovedNode = leading.Concat(trailing);
        }

        protected static T AppendTriviaAtPosition<T>(T node, int position, SyntaxTriviaList trivia)
            where T : SyntaxNode
        {
            if (trivia.Any())
            {
                var tokenToInsertTrivia = node.FindToken(position);
                var tokenWithInsertedTrivia = tokenToInsertTrivia.WithLeadingTrivia(trivia.Concat(tokenToInsertTrivia.LeadingTrivia));
                return node.ReplaceToken(tokenToInsertTrivia, tokenWithInsertedTrivia);
            }

            return node;
        }

        protected static SyntaxTokenList GetUpdatedDeclarationAccessibilityModifiers(
            ArrayBuilder<SyntaxToken> newModifierTokens, SyntaxTokenList modifiersList,
            Func<SyntaxToken, bool> isAccessibilityModifier)
        {
            using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var updatedModifiersList);
            var anyAccessModifierSeen = false;
            foreach (var modifier in modifiersList)
            {
                SyntaxToken newModifier;
                if (isAccessibilityModifier(modifier))
                {
                    if (newModifierTokens.Count == 0)
                    {
                        continue;
                    }

                    newModifier = newModifierTokens[0]
                        .WithLeadingTrivia(modifier.LeadingTrivia)
                        .WithTrailingTrivia(modifier.TrailingTrivia);
                    newModifierTokens.RemoveAt(0);
                    anyAccessModifierSeen = true;
                }
                else
                {
                    if (anyAccessModifierSeen && newModifierTokens.Any())
                    {
                        updatedModifiersList.AddRange(newModifierTokens);
                        newModifierTokens.Clear();
                    }

                    newModifier = modifier;
                }

                updatedModifiersList.Add(newModifier);
            }

            if (!anyAccessModifierSeen)
            {
                for (var i = newModifierTokens.Count - 1; i >= 0; i--)
                {
                    updatedModifiersList.Insert(0, newModifierTokens[i]);
                }
            }
            else
            {
                updatedModifiersList.AddRange(newModifierTokens);
            }

            return updatedModifiersList.ToSyntaxTokenList();
        }
    }
}
