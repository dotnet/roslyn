// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Roslyn.Diagnostics.Analyzers;
using StyleCop.Analyzers.Helpers;

namespace Roslyn.Diagnostics.CSharp.Analyzers.WrapStatements
{
    internal class CSharpWrapStatementsFixAllProvider : DocumentBasedFixAllProvider
    {
        protected override string CodeActionTitle
            => RoslynDiagnosticsAnalyzersResources.Place_statement_on_following_line;

        protected override Task<SyntaxNode> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            => CSharpWrapStatementsCodeFixProvider.FixAllAsync(document, diagnostics, fixAllContext.CancellationToken);
        }
    }
}
