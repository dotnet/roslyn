// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Globalization;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Globalization
{
    [ExportCodeFixProvider(CA1309DiagnosticAnalyzer.RuleId, LanguageNames.CSharp), Shared]
    public class CA1309CSharpCodeFixProvider : CA1309CodeFixProviderBase
    {
        internal override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, string diagnosticId, CancellationToken cancellationToken)
        {
            // if nothing can be fixed, return the unchanged node
            var newRoot = root;
            var kind = nodeToFix.CSharpKind();
            var syntaxFactoryService = document.GetLanguageService<SyntaxGenerator>();
            switch (kind)
            {
                case SyntaxKind.Argument:
                    // StringComparison.CurrentCulture => StringComparison.Ordinal
                    // StringComparison.CurrentCultureIgnoreCase => StringComparison.OrdinalIgnoreCase
                    var argument = (ArgumentSyntax)nodeToFix;
                    var memberAccess = argument.Expression as MemberAccessExpressionSyntax;
                    if (memberAccess != null)
                    {
                        // preserve the "IgnoreCase" suffix if present
                        bool isIgnoreCase = memberAccess.Name.GetText().ToString().EndsWith(CA1309DiagnosticAnalyzer.IgnoreCaseText);
                        var newOrdinalText = isIgnoreCase ? CA1309DiagnosticAnalyzer.OrdinalIgnoreCaseText : CA1309DiagnosticAnalyzer.OrdinalText;
                        var newIdentifier = syntaxFactoryService.IdentifierName(newOrdinalText);
                        var newMemberAccess = memberAccess.WithName((SimpleNameSyntax)newIdentifier).WithAdditionalAnnotations(Formatter.Annotation);
                        newRoot = root.ReplaceNode(memberAccess, newMemberAccess);
                    }

                    break;
                case SyntaxKind.IdentifierName:
                    // string.Equals(a, b) => string.Equals(a, b, StringComparison.Ordinal)
                    // string.Compare(a, b) => string.Compare(a, b, StringComparison.Ordinal)
                    var identifier = (IdentifierNameSyntax)nodeToFix;
                    var invokeParent = identifier.GetAncestor<InvocationExpressionSyntax>();
                    if (invokeParent != null)
                    {
                        var methodSymbol = model.GetSymbolInfo(identifier).Symbol as IMethodSymbol;
                        if (methodSymbol != null && CanAddStringComparison(methodSymbol))
                        {
                            // append a new StringComparison.Ordinal argument
                            var newArg = syntaxFactoryService.Argument(CreateOrdinalMemberAccess(syntaxFactoryService, model))
                                .WithAdditionalAnnotations(Formatter.Annotation);
                            var newInvoke = invokeParent.AddArgumentListArguments((ArgumentSyntax)newArg).WithAdditionalAnnotations(Formatter.Annotation);
                            newRoot = root.ReplaceNode(invokeParent, newInvoke);
                        }
                    }

                    break;
                case SyntaxKind.EqualsExpression:
                case SyntaxKind.NotEqualsExpression:
                    // "a == b" => "string.Equals(a, b, StringComparison.Ordinal)"
                    // "a != b" => "!string.Equals(a, b, StringComparison.Ordinal)"
                    var binaryExpression = (BinaryExpressionSyntax)nodeToFix;
                    var invocation = CreateEqualsExpression(syntaxFactoryService, model, binaryExpression.Left, binaryExpression.Right, kind == SyntaxKind.EqualsExpression).WithAdditionalAnnotations(Formatter.Annotation);
                    newRoot = root.ReplaceNode(nodeToFix, invocation);
                    break;
            }

            if (newRoot == root)
            {
                return Task.FromResult(document);
            }

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}
