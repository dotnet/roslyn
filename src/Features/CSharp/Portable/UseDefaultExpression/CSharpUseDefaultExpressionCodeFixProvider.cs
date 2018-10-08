// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseDefaultExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseDefaultExpression), Shared]
    internal sealed class CSharpUseDefaultExpressionCodeFixProvider : CodeFixProvider
    {
        private const string CS8313 = nameof(CS8313); // A default literal 'default' is not valid as a case constant. Use another literal (e.g. '0' or 'null') as appropriate. If you intended to write the default label, use 'default:' without 'case'.
        private const string CS8363 = nameof(CS8363); // A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern 'var _'.
        private const string CS8107 = nameof(CS8107); // Feature is not available in C# 7.0. Please use language version X or greater.
        private const string CS8059 = nameof(CS8059); // Feature is not available in C# 6. Please use language version X or greater.
        private const string CS8026 = nameof(CS8026); // Feature is not available in C# 5. Please use language version X or greater.
        private const string CS8025 = nameof(CS8025); // Feature is not available in C# 4. Please use language version X or greater.
        private const string CS8024 = nameof(CS8024); // Feature is not available in C# 3. Please use language version X or greater.
        private const string CS8023 = nameof(CS8023); // Feature is not available in C# 2. Please use language version X or greater.

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS8313, CS8363, CS8107, CS8059, CS8026, CS8025, CS8024, CS8023);

        public override FixAllProvider GetFixAllProvider()
        {
            // This code fix addresses very specific compiler errors. It's unlikely there will be more than 1 of them at a time.
            return null;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var token = syntaxRoot.FindToken(context.Span.Start);

            if (token.Span == context.Span &&
                token.IsKind(SyntaxKind.DefaultKeyword) &&
                token.Parent.IsKind(SyntaxKind.DefaultLiteralExpression))
            {
                // If there happens to be more than 1 diagnostic (for example a default literal in a case label in C# 7.0),
                // we will fix all of them, so pass in context.Diagnostics, not just the first one.
                context.RegisterCodeFix(new MyCodeAction(
                    c => FixAsync(context.Document, context.Span, c)),
                    context.Diagnostics);
            }
        }

        private static async Task<Document> FixAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var defaultToken = syntaxRoot.FindToken(span.Start);
            var defaultLiteral = (LiteralExpressionSyntax)defaultToken.Parent;

            var type = semanticModel.GetTypeInfo(defaultLiteral, cancellationToken).ConvertedType;
            if (type == null || type.IsAnonymousType)
            {
                type = semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
            }

            var typeSyntax = type.GenerateTypeSyntax(allowVar: false);

            var defaultExpression =
                SyntaxFactory.DefaultExpression(
                    defaultToken.WithoutTrivia(),
                    SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                    typeSyntax,
                    SyntaxFactory.Token(SyntaxKind.CloseParenToken)).WithTriviaFrom(defaultLiteral);

            var newRoot = syntaxRoot.ReplaceNode(defaultLiteral, defaultExpression);
            return document.WithSyntaxRoot(newRoot);
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Use_default_expression, createChangedDocument, CSharpFeaturesResources.Use_default_expression)
            {
            }
        }
    }
}
