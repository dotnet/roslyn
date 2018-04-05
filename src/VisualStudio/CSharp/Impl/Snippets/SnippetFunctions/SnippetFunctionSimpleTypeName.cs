// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets.SnippetFunctions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets.SnippetFunctions
{
    internal sealed class SnippetFunctionSimpleTypeName : AbstractSnippetFunctionSimpleTypeName
    {
        public SnippetFunctionSimpleTypeName(SnippetExpansionClient snippetExpansionClient, ITextView textView, ITextBuffer subjectBuffer, string fieldName, string fullyQualifiedName)
            : base(snippetExpansionClient, textView, subjectBuffer, fieldName, fullyQualifiedName)
        {
        }

        protected override bool TryGetSimplifiedTypeName(Document documentWithFullyQualifiedTypeName, TextSpan updatedTextSpan, CancellationToken cancellationToken, out string simplifiedTypeName)
        {
            simplifiedTypeName = string.Empty;

            var typeAnnotation = new SyntaxAnnotation();
            var syntaxRoot = documentWithFullyQualifiedTypeName.GetSyntaxRootSynchronously(cancellationToken);
            var nodeToReplace = syntaxRoot.DescendantNodes().FirstOrDefault(n => n.Span == updatedTextSpan);

            if (nodeToReplace == null)
            {
                return false;
            }

            var updatedRoot = syntaxRoot.ReplaceNode(nodeToReplace, nodeToReplace.WithAdditionalAnnotations(typeAnnotation, Simplifier.Annotation));
            var documentWithAnnotations = documentWithFullyQualifiedTypeName.WithSyntaxRoot(updatedRoot);

            var simplifiedDocument = Simplifier.ReduceAsync(documentWithAnnotations, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
            simplifiedTypeName = simplifiedDocument.GetSyntaxRootSynchronously(cancellationToken).GetAnnotatedNodesAndTokens(typeAnnotation).Single().ToString();
            return true;
        }
    }
}
