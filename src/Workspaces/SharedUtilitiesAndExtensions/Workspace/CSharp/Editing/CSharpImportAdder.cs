// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Editing;

[ExportLanguageService(typeof(ImportAdderService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpImportAdder() : ImportAdderService
{
    protected override INamespaceSymbol? GetExplicitNamespaceSymbol(SyntaxNode node, SemanticModel model)
    {
        switch (node)
        {
            case QualifiedNameSyntax name:
                return GetExplicitNamespaceSymbol(name, name.Left, model);
            case MemberAccessExpressionSyntax memberAccess:
                return GetExplicitNamespaceSymbol(memberAccess, memberAccess.Expression, model);
        }

        return null;
    }

    protected override Task AddPotentiallyConflictingImportsAsync(
        SemanticModel model,
        SyntaxNode container,
        ImmutableArray<INamespaceSymbol> namespaceSymbols,
        HashSet<INamespaceSymbol> conflicts,
        CancellationToken cancellationToken)
    {
        var conflictFinder = new ConflictFinder(model, namespaceSymbols);
        return conflictFinder.AddPotentiallyConflictingImportsAsync(container, conflicts, cancellationToken);
    }

    private static INamespaceSymbol? GetExplicitNamespaceSymbol(ExpressionSyntax fullName, ExpressionSyntax namespacePart, SemanticModel model)
    {
        // name must refer to something that is not a namespace, but be qualified with a namespace.
        var symbol = model.GetSymbolInfo(fullName).Symbol;
        if (symbol != null && symbol.Kind != SymbolKind.Namespace && model.GetSymbolInfo(namespacePart).Symbol is INamespaceSymbol)
        {
            // use the symbols containing namespace, and not the potentially less than fully qualified namespace in the full name expression.
            var ns = symbol.ContainingNamespace;
            if (ns != null)
            {
                return model.Compilation.GetCompilationNamespace(ns);
            }
        }

        return null;
    }

    /// <summary>
    /// Walks the portion of the tree we're adding imports to looking to see if those imports could likely cause
    /// conflicts with existing code.  Note: this is a best-effort basis, and the goal is to catch reasonable
    /// conflicts effectively.  There may be cases that do slip through that we can adjust for in the future.  Those
    /// cases should be assessed to see how reasonable/likely they are.  I.e. if it's just a hypothetical case with
    /// no users being hit, then that's far less important than if we have a reasonable coding pattern that would be
    /// impacted by adding an import to a normal namespace.
    /// </summary>
    private sealed class ConflictFinder
    {
        private readonly SemanticModel _model;

        /// <summary>
        /// A mapping containing the simple names and arity of all imported types, mapped to the import that they're
        /// brought in by.
        /// </summary>
        private readonly MultiDictionary<(string name, int arity), INamespaceSymbol> _importedTypes = [];

        /// <summary>
        /// A mapping containing the simple names of all imported extension methods, mapped to the import that
        /// they're brought in by.  This doesn't keep track of arity because methods can be called with type
        /// arguments.
        /// </summary>
        /// <remarks>
        /// We could consider adding more information here (for example the min/max number of args that this can be
        /// called with).  That could then be used to check if there could be a conflict. However, that's likely
        /// more complexity than we need currently.  But it is always something we can do in the future.
        /// </remarks>
        private readonly MultiDictionary<string, INamespaceSymbol> _importedExtensionMethods = [];

        public ConflictFinder(
            SemanticModel model,
            ImmutableArray<INamespaceSymbol> namespaceSymbols)
        {
            _model = model;

            AddImportedMembers(namespaceSymbols);
        }

        private void AddImportedMembers(ImmutableArray<INamespaceSymbol> namespaceSymbols)
        {
            foreach (var ns in namespaceSymbols)
            {
                foreach (var type in ns.GetTypeMembers())
                {
                    _importedTypes.Add((type.Name, type.Arity), ns);

                    if (type.MightContainExtensionMethods)
                    {
                        foreach (var member in type.GetMembers())
                        {
                            if (member is IMethodSymbol method && method.IsExtensionMethod)
                                _importedExtensionMethods.Add(method.Name, ns);
                        }
                    }
                }
            }
        }

        public async Task AddPotentiallyConflictingImportsAsync(SyntaxNode container, HashSet<INamespaceSymbol> conflicts, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var nodes);

            CollectInfoFromContainer(container, nodes, out var containsAnonymousMethods);

            await ProducerConsumer<INamespaceSymbol>.RunParallelAsync(
                source: nodes,
                produceItems: static (node, onItemsFound, args, cancellationToken) =>
                {
                    var (self, containsAnonymousMethods, _) = args;
                    if (node is SimpleNameSyntax nameSyntaxNode)
                        self.ProduceConflicts(nameSyntaxNode, onItemsFound, cancellationToken);
                    else if (node is MemberAccessExpressionSyntax memberAccessExpressionNode)
                        self.ProduceConflicts(memberAccessExpressionNode, containsAnonymousMethods, onItemsFound, cancellationToken);
                    else
                        throw ExceptionUtilities.Unreachable();

                    return Task.CompletedTask;
                },
                consumeItems: static async (items, args, cancellationToken) =>
                {
                    var (_, _, conflicts) = args;
                    await foreach (var conflict in items.ConfigureAwait(false))
                        conflicts.Add(conflict);
                },
                args: (self: this, containsAnonymousMethods, conflicts),
                cancellationToken).ConfigureAwait(false);
        }

        private void CollectInfoFromContainer(SyntaxNode container, ArrayBuilder<SyntaxNode> nodes, out bool containsAnonymousMethods)
        {
            containsAnonymousMethods = false;

            foreach (var node in container.DescendantNodesAndSelf())
            {
                switch (node.Kind())
                {
                    case SyntaxKind.IdentifierName:
                    case SyntaxKind.GenericName:
                        if (IsPotentialConflictWithImportedType((SimpleNameSyntax)node))
                            nodes.Add(node);
                        break;
                    case SyntaxKind.SimpleMemberAccessExpression:
                    case SyntaxKind.PointerMemberAccessExpression:
                        if (IsPotentialConflictWithImportedExtensionMethod((MemberAccessExpressionSyntax)node))
                            nodes.Add(node);
                        break;
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                    case SyntaxKind.AnonymousMethodExpression:
                        // Track if we've seen an anonymous method or not.  If so, because of how the language binds lambdas and
                        // overloads, we'll assume any method access we see inside (instance or otherwise) could end up conflicting
                        // with an extension method we might pull in.
                        containsAnonymousMethods = true;
                        break;
                }
            }
        }

        private bool IsPotentialConflictWithImportedType(SimpleNameSyntax node)
        {
            // Check to see if we have an standalone identifier (or identifier on the left of a dot). If so, if that
            // identifier binds to a type, then we don't want to bring in any imports that would bring in the same
            // name and could then potentially conflict here.

            if (node.IsRightSideOfDotOrArrowOrColonColon())
                return false;

            // Check to see if we have a var. If so, then nothing assigned to a var
            // would bring any imports that could cause a potential conflict.
            if (node.IsVar)
                return false;

            // Drastically reduce the number of nodes that need to be inspected by filtering
            // out nodes whose identifier isn't a potential conflict.
            if (!_importedTypes.ContainsKey((node.Identifier.Text, node.Arity)))
                return false;

            return true;
        }

        private bool IsPotentialConflictWithImportedExtensionMethod(MemberAccessExpressionSyntax node)
            => _importedExtensionMethods.ContainsKey(node.Name.Identifier.Text);

        private void ProduceConflicts(SimpleNameSyntax node, Action<INamespaceSymbol> addConflict, CancellationToken cancellationToken)
        {
            var symbol = _model.GetSymbolInfo(node, cancellationToken).GetAnySymbol();
            if (symbol?.Kind == SymbolKind.NamedType)
            {
                foreach (var conflictingSymbol in _importedTypes[(symbol.Name, node.Arity)])
                    addConflict(conflictingSymbol);
            }
        }

        private void ProduceConflicts(MemberAccessExpressionSyntax node, bool containsAnonymousMethods, Action<INamespaceSymbol> addConflict, CancellationToken cancellationToken)
        {
            // Check to see if we have a reference to an extension method.  If so, then pulling in an import could
            // bring in an extension that conflicts with that.

            var symbol = _model.GetSymbolInfo(node.Name, cancellationToken).GetAnySymbol();
            if (symbol is IMethodSymbol method)
            {
                var isConflicting = method.IsReducedExtension();

                if (!isConflicting && containsAnonymousMethods)
                {
                    // lambdas are interesting.  Say you have:
                    //
                    //      Goo(x => x.M());
                    //
                    //      void Goo(Action<C> act) { }
                    //      void Goo(Action<int> act) { }
                    //
                    //      class C { public void M() { } }
                    //
                    // This is legal code where the lambda body is calling the instance method.  However, if we introduce a
                    // using that brings in an extension method 'M' on 'int', then the above will become ambiguous.  This is
                    // because lambda binding will try each interpretation separately and eliminate the ones that fail.
                    // Adding the import will make the int form succeed, causing ambiguity.
                    //
                    // To deal with that, we keep track of if we're in a lambda, and we conservatively assume that a method
                    // access (even to a non-extension method) could conflict with an extension method brought in.
                    isConflicting = node.HasAncestor<AnonymousFunctionExpressionSyntax>();
                }

                if (isConflicting)
                {
                    foreach (var conflictingSymbol in _importedExtensionMethods[method.Name])
                        addConflict(conflictingSymbol);
                }
            }
        }
    }
}
