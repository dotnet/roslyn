// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract partial class AbstractCodeGenerationService
    {
        protected abstract IList<bool> GetAvailableInsertionIndices(SyntaxNode destination, CancellationToken cancellationToken);

        private IList<bool> GetAvailableInsertionIndices<TDeclarationNode>(TDeclarationNode destination, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        {
            return GetAvailableInsertionIndices((SyntaxNode)destination, cancellationToken);
        }

        public bool CanAddTo(ISymbol destination, Solution solution, CancellationToken cancellationToken)
        {
            var declarations = _symbolDeclarationService.GetDeclarations(destination);
            return declarations.Any(r => CanAddTo(r.GetSyntax(cancellationToken), solution, cancellationToken));
        }

        protected static SyntaxToken GetEndToken(SyntaxNode node)
        {
            var lastToken = node.GetLastToken(includeZeroWidth: true, includeSkipped: true);

            if (lastToken.IsMissing)
            {
                var nextToken = lastToken.GetNextToken(includeZeroWidth: true, includeSkipped: true);
                if (nextToken.RawKind != 0)
                {
                    return nextToken;
                }
            }

            return lastToken;
        }

        protected static TextSpan GetSpan(SyntaxNode node)
        {
            var start = node.GetFirstToken();
            var end = GetEndToken(node);

            return TextSpan.FromBounds(start.SpanStart, end.Span.End);
        }

        public bool CanAddTo(SyntaxNode destination, Solution solution, CancellationToken cancellationToken)
            => CanAddTo(destination, solution, cancellationToken, out var availableIndices);

        private bool CanAddTo(SyntaxNode destination, Solution solution, CancellationToken cancellationToken,
            out IList<bool> availableIndices, bool checkGeneratedCode = false)
        {
            availableIndices = null;
            if (destination == null)
            {
                return false;
            }

            var syntaxTree = destination.SyntaxTree;
            var document = solution.GetDocument(syntaxTree);

            if (document == null)
            {
                return false;
            }

            // check for generated files if needed.
            if (checkGeneratedCode && document.IsGeneratedCode(cancellationToken))
            {
                return false;
            }

            // Anything completely hidden is something you can't add to. Anything completely visible
            // is something you can add to.  Anything that is partially hidden will have to defer to
            // the underlying language to make a determination.
            var span = GetSpan(destination);
            if (syntaxTree.IsEntirelyHidden(span, cancellationToken))
            {
                // It's entirely hidden, there's no place to generate inside of this.
                return false;
            }

            var overlapsHiddenRegion = syntaxTree.OverlapsHiddenPosition(span, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (!overlapsHiddenRegion)
            {
                // Totally safe to add to this node.
                return true;
            }

            // Part of this node overlaps a hidden region.  We have to defer to the specific language
            // to see if there's anywhere we can generate into here.

            availableIndices = GetAvailableInsertionIndices(destination, cancellationToken);
            return availableIndices != null && availableIndices.Any(b => b);
        }

        /// <summary>
        /// Return the most relevant declaration to namespaceOrType,
        /// it will first search the context node contained within,
        /// then the declaration in the same file, then non auto-generated file,
        /// then all the potential location. Return null if no declaration.
        /// </summary>
        public async Task<SyntaxNode> FindMostRelevantNameSpaceOrTypeDeclarationAsync(
            Solution solution,
            INamespaceOrTypeSymbol namespaceOrType,
            CodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var option = options ?? CodeGenerationOptions.Default;
            var (declaration, _) = await FindMostRelevantDeclarationAsync(solution, namespaceOrType, option, cancellationToken).ConfigureAwait(false);
            return declaration;
        }

        private async Task<(SyntaxNode declaration, IList<bool> availableIndices)> FindMostRelevantDeclarationAsync(
            Solution solution,
            INamespaceOrTypeSymbol namespaceOrType,
            CodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var declaration = default(SyntaxNode);
            IList<bool> availableIndices = null;

            var symbol = namespaceOrType;
            var locationOpt = options.BestLocation;

            var declarations = _symbolDeclarationService.GetDeclarations(symbol);

            var fallbackDeclaration = default(SyntaxNode);
            if (locationOpt is { IsInSource: true })
            {
                var token = locationOpt.FindToken(cancellationToken);

                // Prefer a declaration that the context node is contained within. 
                //
                // Note: This behavior is slightly suboptimal in some cases.  For example, when the
                // user has the pattern:
#if false
                C.cs
                partial class C
                {
                    // Stuff.
                }

                C.NestedType.cs
                partial class C
                {
                    class NestedType 
                    {
                        // Context location.  
                    }
                }
#endif
                // If we're at the specified context location, but we're trying to find the most
                // relevant part for C, then we want to pick the part in C.cs not the one in
                // C.NestedType.cs that contains the context location.  This is because this
                // container isn't really used by the user to place code, but is instead just
                // used to separate out the nested type.  It would be nice to detect this and do the
                // right thing.
                declaration = await SelectFirstOrDefaultAsync(declarations, token.Parent.AncestorsAndSelf().Contains, cancellationToken).ConfigureAwait(false);
                fallbackDeclaration = declaration;
                if (CanAddTo(declaration, solution, cancellationToken, out availableIndices))
                {
                    return (declaration, availableIndices);
                }

                // Then, prefer a declaration from the same file.
                declaration = await SelectFirstOrDefaultAsync(declarations.Where(r => r.SyntaxTree == locationOpt.SourceTree), node => true, cancellationToken).ConfigureAwait(false);
                fallbackDeclaration ??= declaration;
                if (CanAddTo(declaration, solution, cancellationToken, out availableIndices))
                {
                    return (declaration, availableIndices);
                }
            }

            // If there is a declaration in a non auto-generated file, prefer it.
            foreach (var decl in declarations)
            {
                declaration = await decl.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                if (CanAddTo(declaration, solution, cancellationToken, out availableIndices, checkGeneratedCode: true))
                {
                    return (declaration, availableIndices);
                }
            }

            // Generate into any declaration we can find.
            availableIndices = null;
            declaration = fallbackDeclaration ?? await SelectFirstOrDefaultAsync(declarations, node => true, cancellationToken).ConfigureAwait(false);

            return (declaration, availableIndices);
        }

        private static async Task<SyntaxNode> SelectFirstOrDefaultAsync(IEnumerable<SyntaxReference> references, Func<SyntaxNode, bool> predicate, CancellationToken cancellationToken)
        {
            foreach (var r in references)
            {
                var node = await r.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                if (predicate(node))
                {
                    return node;
                }
            }

            return null;
        }
    }
}
