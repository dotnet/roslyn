// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal abstract partial class AbstractCodeGenerationService<TCodeGenerationContextInfo>
{
    protected abstract IList<bool>? GetAvailableInsertionIndices(SyntaxNode destination, CancellationToken cancellationToken);

    private IList<bool>? GetAvailableInsertionIndices<TDeclarationNode>(TDeclarationNode destination, CancellationToken cancellationToken) where TDeclarationNode : SyntaxNode
        => GetAvailableInsertionIndices((SyntaxNode)destination, cancellationToken);

    public bool CanAddTo(ISymbol destination, Solution solution, CancellationToken cancellationToken)
    {
        var declarations = _symbolDeclarationService.GetDeclarations(destination);
        return declarations.Any(static (r, arg) => arg.self.CanAddTo(r.GetSyntax(arg.cancellationToken), arg.solution, arg.cancellationToken), (self: this, solution, cancellationToken));
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
        => CanAddTo(destination, solution, cancellationToken, out _);

    private bool CanAddTo(
        SyntaxNode? destination,
        Solution solution,
        CancellationToken cancellationToken,
        out IList<bool>? availableIndices,
        bool checkGeneratedCode = false)
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

        // We can never generate into a document from a source generator, because those are immutable
        if (document is SourceGeneratedDocument)
        {
            return false;
        }

#if WORKSPACE
        // If we are avoiding generating into files marked as generated (but are still regular files)
        // then check accordingly. This is distinct from the prior check in that we as a fallback
        // will generate into these files is we have no alternative.
        if (checkGeneratedCode && document.IsGeneratedCode(cancellationToken))
        {
            return false;
        }
#endif

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
    public SyntaxNode? FindMostRelevantNameSpaceOrTypeDeclaration(
        Solution solution,
        INamespaceOrTypeSymbol namespaceOrType,
        Location? location,
        CancellationToken cancellationToken)
    {
        var (declaration, _) = FindMostRelevantDeclaration(solution, namespaceOrType, location, cancellationToken);
        return declaration;
    }

    private (SyntaxNode? declaration, IList<bool>? availableIndices) FindMostRelevantDeclaration(
        Solution solution,
        INamespaceOrTypeSymbol namespaceOrType,
        Location? location,
        CancellationToken cancellationToken)
    {
        var symbol = namespaceOrType;

        var declarationReferences = _symbolDeclarationService.GetDeclarations(symbol);
        var declarations = declarationReferences.SelectAsArray(r => r.GetSyntax(cancellationToken));

        using var _ = PooledHashSet<SyntaxNode>.GetInstance(out var ancestors);

        var fallbackDeclaration = (SyntaxNode?)null;
        if (location != null && location.IsInSource)
        {
            var token = location.FindToken(cancellationToken);
            ancestors.AddRange(token.GetAncestors<SyntaxNode>());

            // Prefer non-compilation-unit results over compilation-unit results. If we're adding to a type, we'd prefer
            // to actually add to a real type-decl vs the compilation-unit if this is a top level type.

            if (TryAddToRelatedDeclaration(declarations.Where(d => d is not ICompilationUnitSyntax), checkGeneratedCode: false, out var declaration1, out var availableIndices1) ||
                TryAddToRelatedDeclaration(declarations.Where(d => d is ICompilationUnitSyntax), checkGeneratedCode: false, out declaration1, out availableIndices1))
            {
                return (declaration1, availableIndices1);
            }
        }

        // Check all declarations, preferring declarations not in generated files.
        if (TryAddToWorker(declarations, checkGeneratedCode: true, out var declaration2, out var availableIndices2, predicate: node => true))
            return (declaration2, availableIndices2);

        // Generate into any declaration we can find.
        return (fallbackDeclaration, availableIndices: null);
        bool TryAddToRelatedDeclaration(
            IEnumerable<SyntaxNode> declarations,
            bool checkGeneratedCode,
            [NotNullWhen(true)] out SyntaxNode? declaration,
            out IList<bool>? availableIndices)
        {
            // Prefer a declaration that the context node is contained within. 
            //
            // Note: This behavior is slightly suboptimal in some cases.  For example, when the user has the pattern:
            //
            // C.cs
            //
            //   partial class C
            //   {
            //       // Stuff.
            //   }
            // 
            // C.NestedType.cs
            //
            //   partial class C
            //   {
            //       class NestedType 
            //       {
            //           // Context location.  
            //       }
            //   }
            //
            // If we're at the specified context location, but we're trying to find the most relevant part for C, then
            // we want to pick the part in C.cs not the one in C.NestedType.cs that contains the context location.  This
            // is because this container isn't really used by the user to place code, but is instead just used to
            // separate out the nested type.  It would be nice to detect this and do the right thing.

            // If we didn't find a declaration that the context node is contained within, then just pick the first
            // declaration in the same file.

            return
                TryAddToWorker(declarations, checkGeneratedCode, out declaration, out availableIndices, d => ancestors.Contains(d)) ||
                TryAddToWorker(declarations, checkGeneratedCode, out declaration, out availableIndices, d => d.SyntaxTree == location?.SourceTree);
        }

        bool TryAddToWorker(
            IEnumerable<SyntaxNode> declarations,
            bool checkGeneratedCode,
            [NotNullWhen(true)] out SyntaxNode? declaration,
            out IList<bool>? availableIndices,
            Func<SyntaxNode, bool> predicate)
        {
            foreach (var decl in declarations)
            {
                if (predicate(decl))
                {
                    fallbackDeclaration ??= decl;
                    if (CanAddTo(decl, solution, cancellationToken, out availableIndices, checkGeneratedCode))
                    {
                        declaration = decl;
                        return true;
                    }
                }
            }

            declaration = null;
            availableIndices = null;
            return false;
        }
    }
}
