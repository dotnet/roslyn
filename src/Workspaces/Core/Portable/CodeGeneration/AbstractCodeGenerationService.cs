// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract partial class AbstractCodeGenerationService : ICodeGenerationService
    {
        private readonly ISymbolDeclarationService _symbolDeclarationService;

        protected AbstractCodeGenerationService(
            ISymbolDeclarationService symbolDeclarationService)
        {
            _symbolDeclarationService = symbolDeclarationService;
        }

        public TDeclarationNode AddEvent<TDeclarationNode>(TDeclarationNode destination, IEventSymbol @event, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return AddEvent(destination, @event, options ?? CodeGenerationOptions.Default, GetAvailableInsertionIndices(destination, cancellationToken));
        }

        public TDeclarationNode AddField<TDeclarationNode>(TDeclarationNode destination, IFieldSymbol field, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return AddField(destination, field, options ?? CodeGenerationOptions.Default, GetAvailableInsertionIndices(destination, cancellationToken));
        }

        public TDeclarationNode AddMethod<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return AddMethod(destination, method, options ?? CodeGenerationOptions.Default, GetAvailableInsertionIndices(destination, cancellationToken));
        }

        public TDeclarationNode AddProperty<TDeclarationNode>(TDeclarationNode destination, IPropertySymbol property, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return AddProperty(destination, property, options ?? CodeGenerationOptions.Default, GetAvailableInsertionIndices(destination, cancellationToken));
        }

        public TDeclarationNode AddNamedType<TDeclarationNode>(TDeclarationNode destination, INamedTypeSymbol namedType, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return AddNamedType(destination, namedType, options ?? CodeGenerationOptions.Default, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken);
        }

        public TDeclarationNode AddNamespace<TDeclarationNode>(TDeclarationNode destination, INamespaceSymbol @namespace, CodeGenerationOptions options, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return AddNamespace(destination, @namespace, options ?? CodeGenerationOptions.Default, GetAvailableInsertionIndices(destination, cancellationToken), cancellationToken);
        }

        public TDeclarationNode AddMembers<TDeclarationNode>(TDeclarationNode destination, IEnumerable<ISymbol> members, CodeGenerationOptions options, CancellationToken cancellationToken)
            where TDeclarationNode : SyntaxNode
        {
            return AddMembers(destination, members, GetAvailableInsertionIndices(destination, cancellationToken), options ?? CodeGenerationOptions.Default, cancellationToken);
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
        public abstract TDeclarationNode UpdateDeclarationMembers<TDeclarationNode>(TDeclarationNode declaration, IList<ISymbol> newMembers, CodeGenerationOptions options = null, CancellationToken cancellationToken = default(CancellationToken)) where TDeclarationNode : SyntaxNode;

        public abstract CodeGenerationDestination GetDestination(SyntaxNode node);
        public abstract SyntaxNode CreateEventDeclaration(IEventSymbol @event, CodeGenerationDestination destination, CodeGenerationOptions options);
        public abstract SyntaxNode CreateFieldDeclaration(IFieldSymbol field, CodeGenerationDestination destination, CodeGenerationOptions options);
        public abstract SyntaxNode CreateMethodDeclaration(IMethodSymbol method, CodeGenerationDestination destination, CodeGenerationOptions options);
        public abstract SyntaxNode CreatePropertyDeclaration(IPropertySymbol property, CodeGenerationDestination destination, CodeGenerationOptions options);
        public abstract SyntaxNode CreateNamedTypeDeclaration(INamedTypeSymbol namedType, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken);
        public abstract SyntaxNode CreateNamespaceDeclaration(INamespaceSymbol @namespace, CodeGenerationDestination destination, CodeGenerationOptions options, CancellationToken cancellationToken);

        protected abstract AbstractImportsAdder CreateImportsAdder(Document document);

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
                    string.Format(WorkspacesResources.InvalidDestinationNode, typeof(TDeclarationNode).Name, destination.GetType().Name),
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
                    string.Format(WorkspacesResources.InvalidDestinationNode2,
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
                    string.Format(WorkspacesResources.InvalidDestinationNode3,
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
                    string.Format(WorkspacesResources.InvalidDestinationNode3,
                        typeof(TDeclarationNode1).Name, typeof(TDeclarationNode2).Name, typeof(TDeclarationNode3).Name, typeof(TDeclarationNode4).Name),
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
            options = options ?? CodeGenerationOptions.Default;

            var result = await this.FindMostRelevantDeclarationAsync(solution, destination, options, cancellationToken).ConfigureAwait(false);
            SyntaxNode destinationDeclaration = result.Item1;
            IList<bool> availableIndices = result.Item2;

            if (destinationDeclaration == null)
            {
                throw new ArgumentException(WorkspacesResources.CouldNotFindLocationToGen);
            }

            var transformedDeclaration = declarationTransform(destinationDeclaration, options, availableIndices, cancellationToken);

            var destinationTree = destinationDeclaration.SyntaxTree;

            var root = await destinationTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var currentRoot = root.ReplaceNode(destinationDeclaration, transformedDeclaration);

            var oldDocument = solution.GetDocument(destinationTree);
            var newDocument = oldDocument.WithSyntaxRoot(currentRoot);

            if (options.AddImports)
            {
                var adder = this.CreateImportsAdder(newDocument);
                newDocument = await adder.AddAsync(members, options.PlaceSystemNamespaceFirst, options, cancellationToken).ConfigureAwait(false);
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

            var currentDestination = destination;

            // Filter out the members that are implicitly declared.  They're implicit, hence we do
            // not want an explicit declaration.
            var filteredMembers = membersList.Where(m => !m.IsImplicitlyDeclared);

            if (options.AutoInsertionLocation)
            {
                foreach (var member in filteredMembers)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentDestination = member.TypeSwitch(
                        (IEventSymbol @event) => this.AddEvent(currentDestination, @event, options, availableIndices),
                        (IFieldSymbol field) => this.AddField(currentDestination, field, options, availableIndices),
                        (IPropertySymbol property) => this.AddProperty(currentDestination, property, options, availableIndices),
                        (IMethodSymbol method) => this.AddMethod(currentDestination, method, options, availableIndices),
                        (INamedTypeSymbol namedType) => this.AddNamedType(currentDestination, namedType, options, availableIndices, cancellationToken),
                        (INamespaceSymbol @namespace) => this.AddNamespace(currentDestination, @namespace, options, availableIndices, cancellationToken),
                        _ => currentDestination);
                }
            }
            else
            {
                var newMembers = new List<SyntaxNode>();
                var codeGenerationDestination = GetDestination(destination);
                foreach (var member in filteredMembers)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var newMember = member.TypeSwitch(
                        (IEventSymbol @event) => this.CreateEventDeclaration(@event, codeGenerationDestination, options),
                        (IFieldSymbol field) => this.CreateFieldDeclaration(field, codeGenerationDestination, options),
                        (IPropertySymbol property) => this.CreatePropertyDeclaration(property, codeGenerationDestination, options),
                        (IMethodSymbol method) => this.CreateMethodDeclaration(method, codeGenerationDestination, options),
                        (INamedTypeSymbol namedType) => this.CreateNamedTypeDeclaration(namedType, codeGenerationDestination, options, cancellationToken),
                        (INamespaceSymbol @namespace) => this.CreateNamespaceDeclaration(@namespace, codeGenerationDestination, options, cancellationToken),
                        _ => null);

                    if (newMember != null)
                    {
                        newMembers.Add(newMember);
                    }
                }

                // Metadata as source generates complete declarations and doesn't modify
                // existing ones. We can take the members to generate, sort them once,
                // and then add them in that order to the end of the destination.
                if (!GeneratingEnum(members))
                {
                    newMembers.Sort(GetMemberComparer());
                }

                currentDestination = this.AddMembers(currentDestination, newMembers);
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
            options = new CodeGenerationOptions(
                options.ContextLocation,
                addImports: options.AddImports,
                placeSystemNamespaceFirst: options.PlaceSystemNamespaceFirst,
                additionalImports: options.AdditionalImports,
                generateMembers: options.GenerateMembers,
                mergeNestedNamespaces: options.MergeNestedNamespaces,
                mergeAttributes: options.MergeAttributes,
                generateDefaultAccessibility: options.GenerateDefaultAccessibility,
                generateMethodBodies: options.GenerateMethodBodies,
                generateDocumentationComments: options.GenerateDocumentationComments,
                autoInsertionLocation: options.AutoInsertionLocation,
                reuseSyntax: options.ReuseSyntax);
            return options;
        }

        public Task<Document> AddEventAsync(Solution solution, INamedTypeSymbol destination, IEventSymbol @event, CodeGenerationOptions options, CancellationToken cancellationToken)
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

            if (namespaceOrType is INamespaceSymbol)
            {
                return AddNamespaceAsync(solution, destination, (INamespaceSymbol)namespaceOrType, options, cancellationToken);
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
                throw new ArgumentException(WorkspacesResources.InvalidLocationForCodeGen);
            }

            if (!location.IsInSource)
            {
                throw new ArgumentException(WorkspacesResources.InvalidNonSourceLocationForCodeGen);
            }

            if (location.SourceTree != destinationMember.SyntaxTree)
            {
                throw new ArgumentException(WorkspacesResources.DestinationLocationFromDifferentTree);
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
