// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract partial class AbstractCodeGenerationService : ICodeGenerationService
    {
        private readonly ISymbolDeclarationService _symbolDeclarationService;
        protected readonly Workspace Workspace;

        protected AbstractCodeGenerationService(
            ISymbolDeclarationService symbolDeclarationService,
            Workspace workspace)
        {
            _symbolDeclarationService = symbolDeclarationService;
            Workspace = workspace;
        }

        public TDeclarationNode AddEvent<TDeclarationNode>(TDeclarationNode destination, IEventSymbol @event, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return WithAnnotations(AddEvent(destination, @event, options ?? CodeGenerationOptions.Default, GetAvailableInsertionIndices(destination, cancellationToken)), options);
        }

        public TDeclarationNode AddField<TDeclarationNode>(TDeclarationNode destination, IFieldSymbol field, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return WithAnnotations(AddField(destination, field, options ?? CodeGenerationOptions.Default, GetAvailableInsertionIndices(destination, cancellationToken)), options);
        }

        public TDeclarationNode AddMethod<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return WithAnnotations(AddMethod(destination, method, options ?? CodeGenerationOptions.Default, GetAvailableInsertionIndices(destination, cancellationToken)), options);
        }

        public TDeclarationNode AddProperty<TDeclarationNode>(TDeclarationNode destination, IPropertySymbol property, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return WithAnnotations(AddProperty(destination, property, options ?? CodeGenerationOptions.Default, GetAvailableInsertionIndices(destination, cancellationToken)), options);
        }

        public TDeclarationNode AddNamedType<TDeclarationNode>(TDeclarationNode destination, INamedTypeSymbol namedType, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return WithAnnotations(AddNamedType(destination, namedType, options ?? CodeGenerationOptions.Default, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), options);
        }

        public TDeclarationNode AddNamespace<TDeclarationNode>(TDeclarationNode destination, INamespaceSymbol @namespace, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return WithAnnotations(AddNamespace(destination, @namespace, options ?? CodeGenerationOptions.Default, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken), options);
        }

        public TDeclarationNode AddMembers<TDeclarationNode>(TDeclarationNode destination, IEnumerable<ISymbol> members, CodeGenerationOptions options, CancellationToken cancellationToken)
            where TDeclarationNode : SyntaxNode
        {
            return WithAnnotations(AddMembers(destination, members, GetAvailableInsertionIndices(destination, cancellationToken), options ?? CodeGenerationOptions.Default, cancellationToken), options);
        }

        private TNode WithAnnotations<TNode>(TNode node, CodeGenerationOptions options) where TNode : SyntaxNode
        {
            return options?.AddImports ?? true
                ? node.WithAdditionalAnnotations(Simplifier.AddImportsAnnotation)
                : node;
        }

        protected abstract TDeclarationNode AddEvent<TDeclarationNode>(TDeclarationNode destination, IEventSymbol @event, CodeGenerationOptions options, IList<bool> availableIndices) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddField<TDeclarationNode>(TDeclarationNode destination, IFieldSymbol field, CodeGenerationOptions options, IList<bool> availableIndices) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddMethod<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, CodeGenerationOptions options, IList<bool> availableIndices) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddProperty<TDeclarationNode>(TDeclarationNode destination, IPropertySymbol property, CodeGenerationOptions options, IList<bool> availableIndices) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddNamedType<TDeclarationNode>(TDeclarationNode destination, INamedTypeSymbol namedType, CodeGenerationOptions options, IList<bool> availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddNamespace<TDeclarationNode>(TDeclarationNode destination, INamespaceSymbol @namespace, CodeGenerationOptions options, IList<bool> availableIndices, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        protected abstract TDeclarationNode AddMembers<TDeclarationNode>(TDeclarationNode destination, IEnumerable<SyntaxNode> members) where TDeclarationNode : SyntaxNode;

        public abstract TDeclarationNode AddParameters<TDeclarationNode>(TDeclarationNode destinationMember, IEnumerable<IParameterSymbol> parameters, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode AddAttributes<TDeclarationNode>(TDeclarationNode destination, IEnumerable<AttributeData> attributes, SyntaxToken? target, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, SyntaxNode attributeToRemove, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode RemoveAttribute<TDeclarationNode>(TDeclarationNode destination, AttributeData attributeToRemove, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode AddStatements<TDeclarationNode>(TDeclarationNode destinationMember, IEnumerable<SyntaxNode> statements, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;

        public abstract TDeclarationNode UpdateDeclarationModifiers<TDeclarationNode>(TDeclarationNode declaration, IEnumerable<SyntaxToken> newModifiers, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode UpdateDeclarationAccessibility<TDeclarationNode>(TDeclarationNode declaration, Accessibility newAccessibility, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode UpdateDeclarationType<TDeclarationNode>(TDeclarationNode declaration, ITypeSymbol newType, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode;
        public abstract TDeclarationNode UpdateDeclarationMembers<TDeclarationNode>(TDeclarationNode declaration, IList<ISymbol> newMembers, CodeGenerationOptions options = null, CancellationToken cancellationToken = default) where TDeclarationNode : SyntaxNode;

        public abstract CodeGenerationDestination GetDestination(SyntaxNode node);
        public abstract SyntaxNode CreateEventDeclaration(IEventSymbol @event, CodeGenerationDestination destination, CodeGenerationOptions options);
        public abstract SyntaxNode CreateFieldDeclaration(IFieldSymbol field, CodeGenerationDestination destination, CodeGenerationOptions options);
        public abstract SyntaxNode CreateMethodDeclaration(IMethodSymbol method, CodeGenerationDestination destination, CodeGenerationOptions options);
        public abstract SyntaxNode CreatePropertyDeclaration(IPropertySymbol property, CodeGenerationDestination destination, CodeGenerationOptions options);
        public abstract SyntaxNode CreateNamedTypeDeclaration(INamedTypeSymbol namedType, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken);
        public abstract SyntaxNode CreateNamespaceDeclaration(INamespaceSymbol @namespace, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken);

        protected static T Cast<T>(object value)
        {
            return (T)value;
        }

        protected static void CheckDeclarationNode<TDeclarationNode>(SyntaxNode destination) where TDeclarationNode : SyntaxNode
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (!(destination is TDeclarationNode))
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

            if (!(destination is TDeclarationNode1) &&
                !(destination is TDeclarationNode2))
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

            if (!(destination is TDeclarationNode1) &&
                !(destination is TDeclarationNode2) &&
                !(destination is TDeclarationNode3))
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
            if (!(destination is TDeclarationNode1) &&
                !(destination is TDeclarationNode2) &&
                !(destination is TDeclarationNode3) &&
                !(destination is TDeclarationNode4))
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
            Func<SyntaxNode, CodeGenerationOptions, IList<bool>, CancellationToken, SyntaxNode> declarationTransform,
            CodeGenerationOptions options,
            IEnumerable<ISymbol> members,
            CancellationToken cancellationToken)
        {
            options ??= CodeGenerationOptions.Default;

            var (destinationDeclaration, availableIndices) =
                await this.FindMostRelevantDeclarationAsync(solution, destination, options, cancellationToken).ConfigureAwait(false);

            if (destinationDeclaration == null)
            {
                throw new ArgumentException(WorkspacesResources.Could_not_find_location_to_generation_symbol_into);
            }

            var transformedDeclaration = declarationTransform(destinationDeclaration, options, availableIndices, cancellationToken);

            var destinationTree = destinationDeclaration.SyntaxTree;

            var root = await destinationTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var currentRoot = root.ReplaceNode(destinationDeclaration, transformedDeclaration);

            var oldDocument = solution.GetDocument(destinationTree);
            var newDocument = oldDocument.WithSyntaxRoot(currentRoot);

            if (options.AddImports)
            {
                newDocument = await ImportAdder.AddImportsFromSymbolAnnotationAsync(
                    newDocument,
                    safe: true,
                    await newDocument.GetOptionsAsync(cancellationToken).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }

            return newDocument;
        }

        protected TDeclarationNode AddMembers<TDeclarationNode>(
            TDeclarationNode destination,
            IEnumerable<ISymbol> members,
            IList<bool> availableIndices,
            CodeGenerationOptions options,
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

            return options.AutoInsertionLocation
                ? AddMembersToAppropiateLocationInDestination(destination, filteredMembers, availableIndices, options, cancellationToken)
                : AddMembersToEndOfDestination(destination, filteredMembers, availableIndices, options, cancellationToken);
        }

        private TDeclarationSyntax AddMembersToEndOfDestination<TDeclarationSyntax>(
            TDeclarationSyntax destination,
            IEnumerable<ISymbol> members,
            IList<bool> availableIndices,
            CodeGenerationOptions options,
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
            if (!GeneratingEnum(members) && options.SortMembers)
            {
                newMembers.Sort(GetMemberComparer());
            }

            return this.AddMembers(destination, newMembers);
        }

        private TDeclarationSyntax AddMembersToAppropiateLocationInDestination<TDeclarationSyntax>(
            TDeclarationSyntax destination,
            IEnumerable<ISymbol> members,
            IList<bool> availableIndices,
            CodeGenerationOptions options,
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

        private SyntaxNode GetNewMember(
            CodeGenerationOptions options, CodeGenerationDestination codeGenerationDestination,
            ISymbol member, CancellationToken cancellationToken)
        {
            switch (member)
            {
                case IEventSymbol @event: return this.CreateEventDeclaration(@event, codeGenerationDestination, options);
                case IFieldSymbol field: return this.CreateFieldDeclaration(field, codeGenerationDestination, options);
                case IPropertySymbol property: return this.CreatePropertyDeclaration(property, codeGenerationDestination, options);
                case IMethodSymbol method: return this.CreateMethodDeclaration(method, codeGenerationDestination, options);
                case INamedTypeSymbol namedType: return this.CreateNamedTypeDeclaration(namedType, codeGenerationDestination, options, cancellationToken);
                case INamespaceSymbol @namespace: return this.CreateNamespaceDeclaration(@namespace, codeGenerationDestination, options, cancellationToken);
            }

            return null;
        }

        private TDeclarationNode UpdateDestination<TDeclarationNode>(
            IList<bool> availableIndices,
            CodeGenerationOptions options,
            TDeclarationNode currentDestination,
            ISymbol member,
            CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            switch (member)
            {
                case IEventSymbol @event: return this.AddEvent(currentDestination, @event, options, availableIndices);
                case IFieldSymbol field: return this.AddField(currentDestination, field, options, availableIndices);
                case IPropertySymbol property: return this.AddProperty(currentDestination, property, options, availableIndices);
                case IMethodSymbol method: return this.AddMethod(currentDestination, method, options, availableIndices);
                case INamedTypeSymbol namedType: return this.AddNamedType(currentDestination, namedType, options, availableIndices, cancellationToken);
                case INamespaceSymbol @namespace: return this.AddNamespace(currentDestination, @namespace, options, availableIndices, cancellationToken);
            }

            return currentDestination;
        }

        private bool GeneratingEnum(IEnumerable<ISymbol> members)
        {
            var field = members.OfType<IFieldSymbol>().FirstOrDefault();
            return field != null && field.ContainingType.IsEnumType();
        }

        protected abstract IComparer<SyntaxNode> GetMemberComparer();

        protected static CodeGenerationOptions CreateOptionsForMultipleMembers(CodeGenerationOptions options)
        {
            // For now we ignore the afterThisLocation/beforeThisLocation if we're adding
            // multiple members.  In the future it would be nice to appropriately handle this.
            // The difficulty lies with ensuring that we properly understand the position we're
            // inserting into, even as we change the type by adding multiple members.  Not
            // impossible to figure out, but out of scope right now.
            options = options.With(afterThisLocation: null, beforeThisLocation: null);
            return options;
        }

        public virtual Task<Document> AddEventAsync(
            Solution solution, INamedTypeSymbol destination, IEventSymbol @event,
            CodeGenerationOptions options, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution,
                destination,
                (t, opts, ai, ct) => AddEvent(t, @event, opts, ai),
                options,
                new[] { @event },
                cancellationToken);
        }

        public Task<Document> AddFieldAsync(Solution solution, INamedTypeSymbol destination, IFieldSymbol field, CodeGenerationOptions options, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution,
                destination,
                (t, opts, ai, ct) => AddField(t, field, opts, ai),
                options,
                new[] { field },
                cancellationToken);
        }

        public Task<Document> AddPropertyAsync(Solution solution, INamedTypeSymbol destination, IPropertySymbol property, CodeGenerationOptions options, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution, destination,
                (t, opts, ai, ct) => AddProperty(t, property, opts, ai),
                options, new[] { property },
                cancellationToken);
        }

        public Task<Document> AddNamedTypeAsync(Solution solution, INamedTypeSymbol destination, INamedTypeSymbol namedType, CodeGenerationOptions options, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution, destination,
                (t, opts, ai, ct) => AddNamedType(t, namedType, opts, ai, ct),
                options, new[] { namedType },
                cancellationToken);
        }

        public Task<Document> AddNamedTypeAsync(Solution solution, INamespaceSymbol destination, INamedTypeSymbol namedType, CodeGenerationOptions options, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution, destination,
                (t, opts, ai, ct) => AddNamedType(t, namedType, opts, ai, ct),
                options, new[] { namedType }, cancellationToken);
        }

        public Task<Document> AddNamespaceAsync(Solution solution, INamespaceSymbol destination, INamespaceSymbol @namespace, CodeGenerationOptions options, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution, destination,
                (t, opts, ai, ct) => AddNamespace(t, @namespace, opts, ai, ct),
                options, new[] { @namespace }, cancellationToken);
        }

        public Task<Document> AddMethodAsync(Solution solution, INamedTypeSymbol destination, IMethodSymbol method, CodeGenerationOptions options, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution, destination,
                (t, opts, ai, ct) => AddMethod(t, method, opts, ai),
                options, new[] { method }, cancellationToken);
        }

        public Task<Document> AddMembersAsync(Solution solution, INamedTypeSymbol destination, IEnumerable<ISymbol> members, CodeGenerationOptions options, CancellationToken cancellationToken)
        {
            return GetEditAsync(
                solution, destination,
                (t, opts, ai, ct) => AddMembers(t, members, ai, opts, ct),
                options, members, cancellationToken);
        }

        public Task<Document> AddNamespaceOrTypeAsync(Solution solution, INamespaceSymbol destination, INamespaceOrTypeSymbol namespaceOrType, CodeGenerationOptions options, CancellationToken cancellationToken)
        {
            if (namespaceOrType == null)
            {
                throw new ArgumentNullException(nameof(namespaceOrType));
            }

            if (namespaceOrType is INamespaceSymbol namespaceSymbol)
            {
                return AddNamespaceAsync(solution, destination, namespaceSymbol, options, cancellationToken);
            }
            else
            {
                return AddNamedTypeAsync(solution, destination, (INamedTypeSymbol)namespaceOrType, options, cancellationToken);
            }
        }

        protected static void CheckLocation<TDeclarationNode>(
            TDeclarationNode destinationMember, Location location) where TDeclarationNode : SyntaxNode
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

        protected static IList<SyntaxToken> GetUpdatedDeclarationAccessibilityModifiers(IList<SyntaxToken> newModifierTokens, SyntaxTokenList modifiersList, Func<SyntaxToken, bool> isAccessibilityModifier)
        {
            var updatedModifiersList = new List<SyntaxToken>();
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
                updatedModifiersList.InsertRange(0, newModifierTokens);
            }
            else
            {
                updatedModifiersList.AddRange(newModifierTokens);
            }

            return updatedModifiersList;
        }
    }
}
