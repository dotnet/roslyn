// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Performance;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Performance
{
    /// <summary>
    /// CA1813: Seal attribute types for improved performance. Sealing attribute types speeds up performance during reflection on custom attributes.
    /// </summary>
    [ExportCodeFixProvider(CA1813DiagnosticAnalyzer.RuleId, LanguageNames.CSharp), Shared]
    public class CA1813CSharpCodeFixProvider : CA1813CodeFixProviderBase
    {
        internal override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var attributeSyntax = nodeToFix as ClassDeclarationSyntax;
            if (attributeSyntax != null)
            {
                // TODO : Organize the modifiers list after adding sealed modifier.
                var sealedModifier = SyntaxFactory.Token(SyntaxKind.SealedKeyword);
                var newAttributeSyntax = attributeSyntax
                    .WithModifiers(attributeSyntax.Modifiers.Add(sealedModifier))
                    .WithAdditionalAnnotations(Formatter.Annotation);
                document = document.WithSyntaxRoot(root.ReplaceNode(attributeSyntax, newAttributeSyntax));
            }

            return Task.FromResult(document);
        }
    }
}