// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DiagnosticComments.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveDuplicateParamTag), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class CSharpRemoveDocCommentNodeCodeFixProvider : AbstractRemoveDocCommentNodeCodeFixProvider
    {
        /// <summary>
        /// Duplicate param tag
        /// </summary>
        internal const string CS1571 = nameof(CS1571);

        /// <summary>
        /// Duplicate typeparam tag
        /// </summary>
        internal const string CS1710 = nameof(CS1710);

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CS1571, CS1710);

        protected override string DocCommentSignifierToken { get; } = "///";

        protected override SyntaxTriviaList GetRevisedDocCommentTrivia(string docCommentText)
            => SyntaxFactory.ParseLeadingTrivia(docCommentText);

        protected override SyntaxNode GetDocCommentElementNode(SyntaxNode fullDocComentNode, TextSpan span)
            => fullDocComentNode.FindNode(span).Parent.Parent;
    }
}