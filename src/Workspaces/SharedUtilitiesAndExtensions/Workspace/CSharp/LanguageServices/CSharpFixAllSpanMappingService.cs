// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes
{
    [ExportLanguageService(typeof(IFixAllSpanMappingService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpFixAllSpanMappingService : AbstractFixAllSpanMappingService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpFixAllSpanMappingService()
        {
        }

        protected override async Task<ImmutableDictionary<Document, ImmutableArray<TextSpan>>> GetFixAllSpansIfWithinGlobalStatementAsync(
            Document document, TextSpan diagnosticSpan, FixAllScope fixAllScope, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(fixAllScope is FixAllScope.ContainingMember or FixAllScope.ContainingType);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnosticSpan);
            if (node.GetAncestorOrThis<GlobalStatementSyntax>() is null)
                return ImmutableDictionary<Document, ImmutableArray<TextSpan>>.Empty;

            // Compute the fix all span for the global statements to be fixed.
            // If the file has type or namespace declaration towards the end, they need to be excluded
            // from the fix all span.
            var fixAllSpan = root.FullSpan;
            var firstTypeOrNamespaceDecl = root.ChildNodes().FirstOrDefault(n => SyntaxFacts.IsNamespaceMemberDeclaration(n.Kind()));
            if (firstTypeOrNamespaceDecl is not null)
            {
                // Bail out for compiler error case where a type or namespace declaration precedes a global statement.
                // C# compiler requires all global statements to preceed type and namespace declarations.
                var globalStatements = root.ChildNodes().OfType<GlobalStatementSyntax>();
                if (globalStatements.Any(g => firstTypeOrNamespaceDecl.SpanStart < g.SpanStart))
                    return ImmutableDictionary<Document, ImmutableArray<TextSpan>>.Empty;

                fixAllSpan = new TextSpan(root.FullSpan.Start, firstTypeOrNamespaceDecl.FullSpan.Start - 1);
            }

            return ImmutableDictionary<Document, ImmutableArray<TextSpan>>.Empty
                .Add(document, ImmutableArray.Create(fixAllSpan));
        }
    }
}
