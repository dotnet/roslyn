// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class IDocumentExtensions
    {
        public static async Task<CompilationUnitSyntax> GetCSharpSyntaxRootAsync(this Document document, CancellationToken cancellationToken = default(CancellationToken))
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return (CompilationUnitSyntax)root;
        }

        public static Task<SyntaxTree> GetCSharpSyntaxTreeAsync(this Document document, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.GetSyntaxTreeAsync(cancellationToken);
        }

        public static Task<SemanticModel> GetCSharpSemanticModelAsync(this Document document, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.GetSemanticModelAsync(cancellationToken);
        }

        public static Task<SemanticModel> GetCSharpSemanticModelForNodeAsync(this Document document, SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.GetSemanticModelForNodeAsync(node, cancellationToken);
        }

        public static Task<SemanticModel> GetCSharpSemanticModelForSpanAsync(this Document document, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.GetSemanticModelForSpanAsync(span, cancellationToken);
        }

        public static Task<Compilation> GetCSharpCompilationAsync(this Document document, CancellationToken cancellationToken = default(CancellationToken))
        {
            return document.Project.GetCompilationAsync(cancellationToken);
        }
    }
}