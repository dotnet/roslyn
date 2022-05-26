// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class SemanticModelExtensions
    {
        /// <summary>
        /// Gets semantic information, such as type, symbols, and diagnostics, about the parent of a token.
        /// </summary>
        /// <param name="semanticModel">The SemanticModel object to get semantic information
        /// from.</param>
        /// <param name="token">The token to get semantic information from. This must be part of the
        /// syntax tree associated with the binding.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public static SymbolInfo GetSymbolInfo(this SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
            => semanticModel.GetSymbolInfo(token.Parent!, cancellationToken);

        public static DataFlowAnalysis AnalyzeRequiredDataFlow(this SemanticModel semanticModel, SyntaxNode statementOrExpression)
            => semanticModel.AnalyzeDataFlow(statementOrExpression) ?? throw new InvalidOperationException();

        public static DataFlowAnalysis AnalyzeRequiredDataFlow(this SemanticModel semanticModel, SyntaxNode firstStatement, SyntaxNode lastStatement)
            => semanticModel.AnalyzeDataFlow(firstStatement, lastStatement) ?? throw new InvalidOperationException();

        public static ControlFlowAnalysis AnalyzeRequiredControlFlow(this SemanticModel semanticModel, SyntaxNode statement)
            => semanticModel.AnalyzeControlFlow(statement) ?? throw new InvalidOperationException();

        public static ControlFlowAnalysis AnalyzeRequiredControlFlow(this SemanticModel semanticModel, SyntaxNode firstStatement, SyntaxNode lastStatement)
            => semanticModel.AnalyzeControlFlow(firstStatement, lastStatement) ?? throw new InvalidOperationException();

        public static ISymbol GetRequiredDeclaredSymbol(this SemanticModel semanticModel, SyntaxNode declaration, CancellationToken cancellationToken)
        {
            return semanticModel.GetDeclaredSymbol(declaration, cancellationToken)
                ?? throw new InvalidOperationException();
        }

        public static IOperation GetRequiredOperation(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            return semanticModel.GetOperation(node, cancellationToken)
                ?? throw new InvalidOperationException();
        }

        public static ISymbol GetRequiredEnclosingSymbol(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.GetEnclosingSymbol(position, cancellationToken)
                ?? throw new InvalidOperationException();
        }

        public static TSymbol? GetEnclosingSymbol<TSymbol>(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            where TSymbol : class, ISymbol
        {
            for (var symbol = semanticModel.GetEnclosingSymbol(position, cancellationToken);
                 symbol != null;
                 symbol = symbol.ContainingSymbol)
            {
                if (symbol is TSymbol tSymbol)
                {
                    return tSymbol;
                }
            }

            return null;
        }

        public static ISymbol GetEnclosingNamedTypeOrAssembly(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(position, cancellationToken) ??
                (ISymbol)semanticModel.Compilation.Assembly;
        }

        public static INamedTypeSymbol? GetEnclosingNamedType(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            => semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(position, cancellationToken);

        public static INamespaceSymbol? GetEnclosingNamespace(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            => semanticModel.GetEnclosingSymbol<INamespaceSymbol>(position, cancellationToken);

        public static IEnumerable<ISymbol> GetExistingSymbols(
                    this SemanticModel semanticModel, SyntaxNode? container, CancellationToken cancellationToken, Func<SyntaxNode, bool>? descendInto = null)
        {
            // Ignore an anonymous type property or tuple field.  It's ok if they have a name that
            // matches the name of the local we're introducing.
            return semanticModel.GetAllDeclaredSymbols(container, cancellationToken, descendInto)
                .Where(s => !s.IsAnonymousTypeProperty() && !s.IsTupleField());
        }

        public static SemanticModel GetOriginalSemanticModel(this SemanticModel semanticModel)
        {
            if (!semanticModel.IsSpeculativeSemanticModel)
            {
                return semanticModel;
            }

            Contract.ThrowIfNull(semanticModel.ParentModel);
            Contract.ThrowIfTrue(semanticModel.ParentModel.IsSpeculativeSemanticModel);
            Contract.ThrowIfTrue(semanticModel.ParentModel.ParentModel != null);
            return semanticModel.ParentModel;
        }

        public static HashSet<ISymbol> GetAllDeclaredSymbols(
           this SemanticModel semanticModel, SyntaxNode? container, CancellationToken cancellationToken, Func<SyntaxNode, bool>? filter = null)
        {
            var symbols = new HashSet<ISymbol>();
            if (container != null)
            {
                GetAllDeclaredSymbols(semanticModel, container, symbols, cancellationToken, filter);
            }

            return symbols;
        }

        private static void GetAllDeclaredSymbols(
            SemanticModel semanticModel, SyntaxNode node,
            HashSet<ISymbol> symbols, CancellationToken cancellationToken, Func<SyntaxNode, bool>? descendInto = null)
        {
            var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

            if (symbol != null)
            {
                symbols.Add(symbol);
            }

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    var childNode = child.AsNode()!;
                    if (ShouldDescendInto(childNode, descendInto))
                    {
                        GetAllDeclaredSymbols(semanticModel, childNode, symbols, cancellationToken, descendInto);
                    }
                }
            }

            static bool ShouldDescendInto(SyntaxNode node, Func<SyntaxNode, bool>? filter)
                => filter != null ? filter(node) : true;
        }
    }
}
