// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FixAll
{
    internal abstract class AbstractFixAllSpanMappingService : IFixAllSpanMappingService
    {
        protected abstract Task<ImmutableDictionary<Document, ImmutableArray<TextSpan>>> GetFixAllSpansIfWithinGlobalStatementAsync(
            Document document, TextSpan span, CancellationToken cancellationToken);

        public Task<ImmutableDictionary<Document, ImmutableArray<TextSpan>>> GetFixAllSpansAsync(
            Document document, TextSpan diagnosticSpan, CodeFixes.FixAllScope fixAllScope, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(fixAllScope is CodeFixes.FixAllScope.ContainingMember or CodeFixes.FixAllScope.ContainingType);

            var fixAllInContainingMember = fixAllScope == CodeFixes.FixAllScope.ContainingMember;
            return GetFixAllSpansAsync(document, diagnosticSpan, fixAllInContainingMember, cancellationToken);
        }

        public Task<ImmutableDictionary<Document, ImmutableArray<TextSpan>>> GetFixAllSpansAsync(
            Document document, TextSpan diagnosticSpan, CodeRefactorings.FixAllScope fixAllScope, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(fixAllScope is CodeRefactorings.FixAllScope.ContainingMember or CodeRefactorings.FixAllScope.ContainingType);

            var fixAllInContainingMember = fixAllScope == CodeRefactorings.FixAllScope.ContainingMember;
            return GetFixAllSpansAsync(document, diagnosticSpan, fixAllInContainingMember, cancellationToken);
        }

        private async Task<ImmutableDictionary<Document, ImmutableArray<TextSpan>>> GetFixAllSpansAsync(
            Document document, TextSpan span, bool fixAllInContainingMember, CancellationToken cancellationToken)
        {
            var decl = await GetContainingMemberOrTypeDeclarationAsync(document, fixAllInContainingMember, span, cancellationToken).ConfigureAwait(false);
            if (decl == null)
                return await GetFixAllSpansIfWithinGlobalStatementAsync(document, span, cancellationToken).ConfigureAwait(false);

            if (fixAllInContainingMember)
            {
                return ImmutableDictionary.CreateRange(SpecializedCollections.SingletonEnumerable(
                    KeyValuePairUtil.Create(document, ImmutableArray.Create(decl.FullSpan))));
            }
            else
            {
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var symbol = semanticModel.GetDeclaredSymbol(decl, cancellationToken);
                if (symbol?.DeclaringSyntaxReferences.Length > 1)
                {
                    var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                    var builder = PooledDictionary<Document, ArrayBuilder<TextSpan>>.GetInstance();
                    foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
                    {
                        var documentForLocation = document.Project.GetDocument(syntaxRef.SyntaxTree);
                        Contract.ThrowIfNull(documentForLocation);
                        var root = await syntaxRef.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                        var partialDeclSpan = syntaxFacts.GetContainingTypeDeclaration(root, syntaxRef.Span.Start)!.FullSpan;
                        builder.MultiAdd(documentForLocation, partialDeclSpan);
                    }

                    return builder.ToImmutableMultiDictionaryAndFree();
                }
                else
                {
                    return ImmutableDictionary.CreateRange(SpecializedCollections.SingletonEnumerable(
                        KeyValuePairUtil.Create(document, ImmutableArray.Create(decl.FullSpan))));
                }
            }
        }

        private static async Task<SyntaxNode?> GetContainingMemberOrTypeDeclarationAsync(
            Document document,
            bool fixAllInContainingMember,
            TextSpan span,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var startContainer = fixAllInContainingMember
                ? syntaxFacts.GetContainingMemberDeclaration(root, span.Start)
                : syntaxFacts.GetContainingTypeDeclaration(root, span.Start);

            if (startContainer == null)
                return null;

            if (fixAllInContainingMember && !syntaxFacts.IsMethodLevelMember(startContainer))
                return null;

            if (span.IsEmpty)
                return startContainer;

            var endContainer = fixAllInContainingMember
                ? syntaxFacts.GetContainingMemberDeclaration(root, span.End)
                : syntaxFacts.GetContainingTypeDeclaration(root, span.End);

            if (startContainer == endContainer)
                return startContainer;

            return null;
        }
    }
}
