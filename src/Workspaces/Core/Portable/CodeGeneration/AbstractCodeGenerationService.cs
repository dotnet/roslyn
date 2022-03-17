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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract partial class AbstractCodeGenerationService<TCodeGenerationOptions> : ICodeGenerationService
        where TCodeGenerationOptions : CodeGenerationOptions
    {
        private readonly ISymbolDeclarationService _symbolDeclarationService;

        protected AbstractCodeGenerationService(
            ISymbolDeclarationService symbolDeclarationService)
        {
            _symbolDeclarationService = symbolDeclarationService;
        }

        public abstract CodeGenerationPreferences GetPreferences(ParseOptions parseOptions, OptionSet documentOptions);

        #region ICodeGenerationService

        public TDeclarationNode AddEvent<TDeclarationNode>(TDeclarationNode destination, IEventSymbol @event, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddEvent(destination, @event, (TCodeGenerationOptions)options, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), options);

        public TDeclarationNode AddField<TDeclarationNode>(TDeclarationNode destination, IFieldSymbol field, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddField(destination, field, (TCodeGenerationOptions)options, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), options);

        public TDeclarationNode AddMethod<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddMethod(destination, method, (TCodeGenerationOptions)options, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), options);

        public TDeclarationNode AddProperty<TDeclarationNode>(TDeclarationNode destination, IPropertySymbol property, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddProperty(destination, property, (TCodeGenerationOptions)options, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), options);

        public TDeclarationNode AddNamedType<TDeclarationNode>(TDeclarationNode destination, INamedTypeSymbol namedType, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddNamedType(destination, namedType, (TCodeGenerationOptions)options, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), options);

        public TDeclarationNode AddNamespace<TDeclarationNode>(TDeclarationNode destination, INamespaceSymbol @namespace, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddNamespace(destination, @namespace, (TCodeGenerationOptions)options, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), options);

        public TDeclarationNode AddMembers<TDeclarationNode>(TDeclarationNode destination, IEnumerable<ISymbol> members, CodeGenerationOptions options, CancellationToken cancellationToken)
            where TDeclarationNode : SyntaxNode
            => WithAnnotations(AddMembers(destination, members, GetAvailableInsertionIndices(destination, cancellationToken), (TCodeGenerationOptions)options, cancellationToken), options);

        private static TNode WithAnnotations<TNode>(TNode node, CodeGenerationOptions options) where TNode : SyntaxNode
        {
            return options.Context.AddImports
                ? node.WithAdditionalAnnotations(Simplifier.AddImportsAnnotation)
                : node;
        }

        public SyntaxNode CreateEventDeclaration(IEventSymbol @event, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken)
            => CreateEventDeclaration(@event, destination, (TCodeGenerationOptions)options, cancellationToken);

        public SyntaxNode CreateFieldDeclaration(IFieldSymbol field, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken)
            => CreateFieldDeclaration(field, destination, (TCodeGenerationOptions)options, cancellationToken);

        public SyntaxNode? CreateMethodDeclaration(IMethodSymbol method, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken)
            => CreateMethodDeclaration(method, destination, (TCodeGenerationOptions)options, cancellationToken);

        public SyntaxNode CreatePropertyDeclaration(IPropertySymbol property, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken)
            => CreatePropertyDeclaration(property, destination, (TCodeGenerationOptions)options, cancellationToken);

        public SyntaxNode CreateNamedTypeDeclaration(INamedTypeSymbol namedType, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken)
            => CreateNamedTypeDeclaration(namedType, destination, (TCodeGenerationOptions)options, cancellationToken);

        public SyntaxNode CreateNamespaceDeclaration(INamespaceSymbol @namespace, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken)
            => CreateNamespaceDeclaration(@namespace, destination, (TCodeGenerationOptions)options, cancellationToken);

        public TDeclarationNode AddParameters<TDeclarationNode>(TDeclarationNode destination, IEnumerable<IParameterSymbol> parameters, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => AddParameters(destination, parameters, (TCodeGenerationOptions)options, cancellationToken);

        public TDeclarationNode AddAttributes<TDeclarationNode>(TDeclarationNode destination, IEnumerable<AttributeData> attributes, SyntaxToken? target, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => AddAttributes(destination, attributes, target, (TCodeGenerationOptions)options, cancellationToken);

        public TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, SyntaxNode attributeToRemove, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => RemoveAttribute(destination, attributeToRemove, (TCodeGenerationOptions)options, cancellationToken);

        public TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, AttributeData attributeToRemove, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => RemoveAttribute(destination, attributeToRemove, (TCodeGenerationOptions)options, cancellationToken);

        public TDeclarationNode UpdateDeclarationModifiers<TDeclarationNode>(TDeclarationNode declaration, IEnumerable<SyntaxToken> newModifiers, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => UpdateDeclarationModifiers(declaration, newModifiers, (TCodeGenerationOptions)options, cancellationToken);

        public TDeclarationNode UpdateDeclarationAccessibility<TDeclarationNode>(TDeclarationNode declaration, Accessibility newAccessibility, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => UpdateDeclarationAccessibility(declaration, newAccessibility, (TCodeGenerationOptions)options, cancellationToken);

        public TDeclarationNode UpdateDeclarationType<TDeclarationNode>(TDeclarationNode declaration, ITypeSymbol newType, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => UpdateDeclarationType(declaration, newType, (TCodeGenerationOptions)options, cancellationToken);

        public TDeclarationNode UpdateDeclarationMembers<TDeclarationNode>(TDeclarationNode declaration, IList<ISymbol> newMembers, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => UpdateDeclarationMembers(declaration, newMembers, (TCodeGenerationOptions)options, cancellationToken);

        public TDeclarationNode AddStatements<TDeclarationNode>(TDeclarationNode destination, IEnumerable<SyntaxNode> statements, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
            => AddStatements(destination, statements, (TCodeGenerationOptions)options, cancellationToken);

        #endregion

        protected abstract TDeclarationNode AddEvent<TDeclarationNode>(TDeclarationNode destination, IEventSymbol @event, TCodeGenerationOptions options, IList<bool>? availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddField<TDeclarationNode>(TDeclarationNode destination, IFieldSymbol field, TCodeGenerationOptions options, IList<bool>? availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddMethod<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, TCodeGenerationOptions options, IList<bool>? availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddProperty<TDeclarationNode>(TDeclarationNode destination, IPropertySymbol property, TCodeGenerationOptions options, IList<bool>? availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddNamedType<TDeclarationNode>(TDeclarationNode destination, INamedTypeSymbol namedType, TCodeGenerationOptions options, IList<bool>? availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddNamespace<TDeclarationNode>(TDeclarationNode destination, INamespaceSymbol @namespace, TCodeGenerationOptions options, IList<bool>? availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddMembers<TDeclarationNode>(TDeclarationNode destination, IEnumerable<SyntaxNode> members) where TDeclarationNode : SyntaxNode;

        public abstract TDeclarationNode AddParameters<TDeclarationNode>(TDeclarationNode destinationMember, IEnumerable<IParameterSymbol> parameters, TCodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode AddAttributes<TDeclarationNode>(TDeclarationNode destination, IEnumerable<AttributeData> attributes, SyntaxToken? target, TCodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, SyntaxNode attributeToRemove, TCodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, AttributeData attributeToRemove, TCodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode AddStatements<TDeclarationNode>(TDeclarationNode destinationMember, IEnumerable<SyntaxNode> statements, TCodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        public abstract TDeclarationNode UpdateDeclarationModifiers<TDeclarationNode>(TDeclarationNode declaration, IEnumerable<SyntaxToken> newModifiers, TCodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode UpdateDeclarationAccessibility<TDeclarationNode>(TDeclarationNode declaration, Accessibility newAccessibility, TCodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode UpdateDeclarationType<TDeclarationNode>(TDeclarationNode declaration, ITypeSymbol newType, TCodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode UpdateDeclarationMembers<TDeclarationNode>(TDeclarationNode declaration, IList<ISymbol> newMembers, TCodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        public abstract CodeGenerationDestination GetDestination(SyntaxNode node);
        public abstract SyntaxNode CreateEventDeclaration(IEventSymbol @event, CodeGenerationDestination destination, TCodeGenerationOptions options, CancellationToken cancellationToken);
        public abstract SyntaxNode CreateFieldDeclaration(IFieldSymbol field, CodeGenerationDestination destination, TCodeGenerationOptions options, CancellationToken cancellationToken);

        // TODO: Change to not return null (https://github.com/dotnet/roslyn/issues/58243)
        public abstract SyntaxNode? CreateMethodDeclaration(IMethodSymbol method, CodeGenerationDestination destination, TCodeGenerationOptions options, CancellationToken cancellationToken);

        public abstract SyntaxNode CreatePropertyDeclaration(IPropertySymbol property, CodeGenerationDestination destination, TCodeGenerationOptions options, CancellationToken cancellationToken);
        public abstract SyntaxNode CreateNamedTypeDeclaration(INamedTypeSymbol namedType, CodeGenerationDestination destination, TCodeGenerationOptions options, CancellationToken cancellationToken);
        public abstract SyntaxNode CreateNamespaceDeclaration(INamespaceSymbol @namespace, CodeGenerationDestination destination, TCodeGenerationOptions options, CancellationToken cancellationToken);

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
                    string.Format(WorkspacesResources.Destination_type_must_be_a_0_but_given_one_is_1, typeof(TDeclarationNode).Name, destination.GetType().Name),
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
                    string.Format(WorkspacesResources.Destination_type_must_be_a_0_or_a_1_but_given_one_is_2,
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
                    string.Format(WorkspacesResources.Destination_type_must_be_a_0_1_or_2_but_given_one_is_3,
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
                    string.Format(WorkspacesResources.Destination_type_must_be_a_0_1_2_or_3_but_given_one_is_4,
                        typeof(TDeclarationNode1).Name, typeof(TDeclarationNode2).Name, typeof(TDeclarationNode3).Name, typeof(TDeclarationNode4).Name, destination.GetType().Name),
                    nameof(destination));
            }
        }

        private async Task<Document> GetEditAsync(
            Solution solution,
            INamespaceOrTypeSymbol destination,
            Func<SyntaxNode, TCodeGenerationOptions, IList<bool>?, CancellationToken, SyntaxNode> declarationTransform,
            CodeGenerationContext context,
            CancellationToken cancellationToken)
        {
            var (destinationDeclaration, availableIndices) =
                await this.FindMostRelevantDeclarationAsync(solution, destination, context, cancellationToken).ConfigureAwait(false);

            if (destinationDeclaration == null)
            {
                throw new ArgumentException(WorkspacesResources.Could_not_find_location_to_generation_symbol_into);
            }

            var destinationTree = destinationDeclaration.SyntaxTree;
            var oldDocument = solution.GetRequiredDocument(destinationTree);
            var options = (TCodeGenerationOptions)await CodeGenerationOptions.FromDocumentAsync(context, oldDocument, cancellationToken).ConfigureAwait(false);
            var transformedDeclaration = declarationTransform(destinationDeclaration, options, availableIndices, cancellationToken);

            var root = await destinationTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var currentRoot = root.ReplaceNode(destinationDeclaration, transformedDeclaration);

            var newDocument = oldDocument.WithSyntaxRoot(currentRoot);

            if (context.AddImports)
            {
                var addImportsOptions = await AddImportPlacementOptions.FromDocumentAsync(newDocument, cancellationToken).ConfigureAwait(false);
                newDocument = await ImportAdder.AddImportsFromSymbolAnnotationAsync(newDocument, addImportsOptions, cancellationToken).ConfigureAwait(false);
            }

            return newDocument;
        }

        protected TDeclarationNode AddMembers<TDeclarationNode>(
            TDeclarationNode destination,
            IEnumerable<ISymbol> members,
            IList<bool>? availableIndices,
            TCodeGenerationOptions options,
            CancellationToken cancellationToken)
            where TDeclarationNode : SyntaxNode
        {
            var membersList = members.ToList();
            if (membersList.Count > 1)
            {
                options = CreateOptionsForMultipleMembers(options);
            }

            // Filter out the members that are implicitly declared.  They're implicit, hence we do
            // not want an explicit declaration. The only exception are fields generated from implicit tuple fields.
            var filteredMembers = membersList.Where(m => !m.IsImplicitlyDeclared || m.IsTupleField());

            return options.Context.AutoInsertionLocation
                ? AddMembersToAppropriateLocationInDestination(destination, filteredMembers, availableIndices, options, cancellationToken)
                : AddMembersToEndOfDestination(destination, filteredMembers, options, cancellationToken);
        }

        private TDeclarationSyntax AddMembersToEndOfDestination<TDeclarationSyntax>(
            TDeclarationSyntax destination,
            IEnumerable<ISymbol> members,
            TCodeGenerationOptions options,
            CancellationToken cancellationToken)
            where TDeclarationSyntax : SyntaxNode
        {
            var newMembers = new List<SyntaxNode>();
            var codeGenerationDestination = GetDestination(destination);
            foreach (var member in members)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var newMember = GetNewMember(options, codeGenerationDestination, member, cancellationToken);

                if (newMember != null)
                {
                    newMembers.Add(newMember);
                }
            }

            // Metadata as source generates complete declarations and doesn't modify
            // existing ones. We can take the members to generate, sort them once,
            // and then add them in that order to the end of the destination.
            if (!GeneratingEnum(members) && options.Context.SortMembers)
            {
                newMembers.Sort(GetMemberComparer());
            }

            return this.AddMembers(destination, newMembers);
        }

        private TDeclarationSyntax AddMembersToAppropriateLocationInDestination<TDeclarationSyntax>(
            TDeclarationSyntax destination,
            IEnumerable<ISymbol> members,
            IList<bool>? availableIndices,
            TCodeGenerationOptions options,
            CancellationToken cancellationToken)
            where TDeclarationSyntax : SyntaxNode
        {
            var currentDestination = destination;

            foreach (var member in members)
            {
                cancellationToken.ThrowIfCancellationRequested();
                currentDestination = UpdateDestination(availableIndices, options, currentDestination, member, cancellationToken);
            }

            return currentDestination;
        }

        private SyntaxNode? GetNewMember(TCodeGenerationOptions options, CodeGenerationDestination codeGenerationDestination, ISymbol member, CancellationToken cancellationToken)
            => member switch
            {
                IEventSymbol @event => CreateEventDeclaration(@event, codeGenerationDestination, options, cancellationToken),
                IFieldSymbol field => CreateFieldDeclaration(field, codeGenerationDestination, options, cancellationToken),
                IPropertySymbol property => CreatePropertyDeclaration(property, codeGenerationDestination, options, cancellationToken),
                IMethodSymbol method => CreateMethodDeclaration(method, codeGenerationDestination, options, cancellationToken),
                INamedTypeSymbol namedType => CreateNamedTypeDeclaration(namedType, codeGenerationDestination, options, cancellationToken),
                INamespaceSymbol @namespace => CreateNamespaceDeclaration(@namespace, codeGenerationDestination, options, cancellationToken),
                _ => null,
            };

        private TDeclarationNode UpdateDestination<TDeclarationNode>(
            IList<bool>? availableIndices,
            TCodeGenerationOptions options,
            TDeclarationNode currentDestination,
            ISymbol member,
            CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return member switch
            {
                IEventSymbol @event => AddEvent(currentDestination, @event, options, availableIndices, cancellationToken),
                IFieldSymbol field => AddField(currentDestination, field, options, availableIndices, cancellationToken),
                IPropertySymbol property => AddProperty(currentDestination, property, options, availableIndices, cancellationToken),
                IMethodSymbol method => AddMethod(currentDestination, method, options, availableIndices, cancellationToken),
                INamedTypeSymbol namedType => AddNamedType(currentDestination, namedType, options, availableIndices, cancellationToken),
                INamespaceSymbol @namespace => AddNamespace(currentDestination, @namespace, options, availableIndices, cancellationToken),
                _ => currentDestination,
            };
        }

        private static bool GeneratingEnum(IEnumerable<ISymbol> members)
        {
            var field = members.OfType<IFieldSymbol>().FirstOrDefault();
            return field != null && field.ContainingType.IsEnumType();
        }

        protected abstract IComparer<SyntaxNode> GetMemberComparer();

        protected static TCodeGenerationOptions CreateOptionsForMultipleMembers(TCodeGenerationOptions options)
        {
            // For now we ignore the afterThisLocation/beforeThisLocation if we're adding
            // multiple members.  In the future it would be nice to appropriately handle this.
            // The difficulty lies with ensuring that we properly understand the position we're
            // inserting into, even as we change the type by adding multiple members.  Not
            // impossible to figure out, but out of scope right now.
            return (TCodeGenerationOptions)options.WithContext(options.Context.With(afterThisLocation: null, beforeThisLocation: null));
        }

        public virtual Task<Document> AddEventAsync(
            Solution solution, INamedTypeSymbol destination, IEventSymbol @event,
            CodeGenerationContext context, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution,
                destination,
                (t, opts, ai, ct) => AddEvent(t, @event, opts, ai, ct),
                context,
                cancellationToken);
        }

        public Task<Document> AddFieldAsync(Solution solution, INamedTypeSymbol destination, IFieldSymbol field, CodeGenerationContext context, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution,
                destination,
                (t, opts, ai, ct) => AddField(t, field, opts, ai, ct),
                context,
                cancellationToken);
        }

        public Task<Document> AddPropertyAsync(Solution solution, INamedTypeSymbol destination, IPropertySymbol property, CodeGenerationContext context, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution, destination,
                (t, opts, ai, ct) => AddProperty(t, property, opts, ai, ct),
                context,
                cancellationToken);
        }

        public Task<Document> AddNamedTypeAsync(Solution solution, INamedTypeSymbol destination, INamedTypeSymbol namedType, CodeGenerationContext context, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution, destination,
                (t, opts, ai, ct) => AddNamedType(t, namedType, opts, ai, ct),
                context,
                cancellationToken);
        }

        public Task<Document> AddNamedTypeAsync(Solution solution, INamespaceSymbol destination, INamedTypeSymbol namedType, CodeGenerationContext options, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution, destination,
                (t, opts, ai, ct) => AddNamedType(t, namedType, opts, ai, ct),
                options,
                cancellationToken);
        }

        public Task<Document> AddNamespaceAsync(Solution solution, INamespaceSymbol destination, INamespaceSymbol @namespace, CodeGenerationContext context, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution, destination,
                (t, opts, ai, ct) => AddNamespace(t, @namespace, opts, ai, ct),
                context,
                cancellationToken);
        }

        public Task<Document> AddMethodAsync(Solution solution, INamedTypeSymbol destination, IMethodSymbol method, CodeGenerationContext context, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution, destination,
                (t, opts, ai, ct) => AddMethod(t, method, opts, ai, ct),
                context,
                cancellationToken);
        }

        public Task<Document> AddMembersAsync(Solution solution, INamedTypeSymbol destination, IEnumerable<ISymbol> members, CodeGenerationContext context, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution, destination,
                (t, opts, ai, ct) => AddMembers(t, members, ai, opts, ct),
                context,
                cancellationToken);
        }

        public Task<Document> AddNamespaceOrTypeAsync(Solution solution, INamespaceSymbol destination, INamespaceOrTypeSymbol namespaceOrType, CodeGenerationContext context, CancellationToken cancellationToken)
        {
            if (namespaceOrType == null)
            {
                throw new ArgumentNullException(nameof(namespaceOrType));
            }

            if (namespaceOrType is INamespaceSymbol namespaceSymbol)
            {
                return AddNamespaceAsync(solution, destination, namespaceSymbol, context, cancellationToken);
            }
            else
            {
                return AddNamedTypeAsync(solution, destination, (INamedTypeSymbol)namespaceOrType, context, cancellationToken);
            }
        }

        protected static void CheckLocation(SyntaxNode destinationMember, [NotNull] Location? location)
        {
            if (location == null)
            {
                throw new ArgumentException(WorkspacesResources.No_location_provided_to_add_statements_to);
            }

            if (!location.IsInSource)
            {
                throw new ArgumentException(WorkspacesResources.Destination_location_was_not_in_source);
            }

            if (location.SourceTree != destinationMember.SyntaxTree)
            {
                throw new ArgumentException(WorkspacesResources.Destination_location_was_from_a_different_tree);
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
