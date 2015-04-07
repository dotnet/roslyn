// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace System.Runtime.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class CSharpUseOrdinalStringComparisonFixer : UseOrdinalStringComparisonFixerBase
    {
        protected override bool IsInArgumentContext(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.Argument) &&
                   ((ArgumentSyntax)node).Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression);
        }

        protected override Task<Document> FixArgument(Document document, SyntaxGenerator generator, SyntaxNode root, SyntaxNode argument)
        {
            var memberAccess = ((ArgumentSyntax)argument)?.Expression as MemberAccessExpressionSyntax;
            if (memberAccess != null)
            {
                // preserve the "IgnoreCase" suffix if present
                bool isIgnoreCase = memberAccess.Name.GetText().ToString().EndsWith(UseOrdinalStringComparisonAnalyzer.IgnoreCaseText, StringComparison.Ordinal);
                var newOrdinalText = isIgnoreCase ? UseOrdinalStringComparisonAnalyzer.OrdinalIgnoreCaseText : UseOrdinalStringComparisonAnalyzer.OrdinalText;
                var newIdentifier = generator.IdentifierName(newOrdinalText);
                var newMemberAccess = memberAccess.WithName((SimpleNameSyntax)newIdentifier).WithAdditionalAnnotations(Formatter.Annotation);
                var newRoot = root.ReplaceNode(memberAccess, newMemberAccess);
                return Task.FromResult(document.WithSyntaxRoot(newRoot));
            }

            return Task.FromResult(document);
        }

        protected override bool IsInIdentifierNameContext(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.IdentifierName) &&
                   node?.Parent?.FirstAncestorOrSelf<InvocationExpressionSyntax>() != null;
        }

        protected override async Task<Document> FixIdentifierName(Document document, SyntaxGenerator generator, SyntaxNode root, SyntaxNode identifier, CancellationToken cancellationToken)
        {
            var invokeParent = identifier?.Parent?.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invokeParent != null)
            {
                var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var methodSymbol = model.GetSymbolInfo((IdentifierNameSyntax)identifier, cancellationToken).Symbol as IMethodSymbol;
                if (methodSymbol != null && CanAddStringComparison(methodSymbol, model))
                {
                    // append a new StringComparison.Ordinal argument
                    var newArg = generator.Argument(CreateOrdinalMemberAccess(generator, model))
                        .WithAdditionalAnnotations(Formatter.Annotation);
                    var newInvoke = invokeParent.AddArgumentListArguments((ArgumentSyntax)newArg).WithAdditionalAnnotations(Formatter.Annotation);
                    var newRoot = root.ReplaceNode(invokeParent, newInvoke);
                    return document.WithSyntaxRoot(newRoot);
                }
            }

            return document;
        }

        protected override bool IsInEqualsContext(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.EqualsExpression) || node.IsKind(SyntaxKind.NotEqualsExpression);
        }

        protected override async Task<Document> FixEquals(Document document, SyntaxGenerator generator, SyntaxNode root, SyntaxNode node, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var binaryExpression = (BinaryExpressionSyntax)node;
            var invocation = CreateEqualsExpression(generator, model, binaryExpression.Left, binaryExpression.Right, node.Kind() == SyntaxKind.EqualsExpression).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(node, invocation);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
