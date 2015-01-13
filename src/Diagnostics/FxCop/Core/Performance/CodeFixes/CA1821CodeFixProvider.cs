// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Performance
{
    /// <summary>
    /// CA1821: Remove empty finalizers
    /// </summary>
    [ExportCodeFixProvider(CA1821DiagnosticAnalyzerRule.RuleId, LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    public sealed class CA1821CodeFixProvider : CodeFixProviderBase
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(CA1821DiagnosticAnalyzerRule.RuleId);
        }

        protected sealed override string GetCodeFixDescription(Diagnostic diagnostic)
        {
            return FxCopFixersResources.RemoveEmptyFinalizers;
        }

        internal override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            return Task.FromResult(document.WithSyntaxRoot(root.RemoveNode(nodeToFix, SyntaxRemoveOptions.KeepNoTrivia)));
        }
    }
}