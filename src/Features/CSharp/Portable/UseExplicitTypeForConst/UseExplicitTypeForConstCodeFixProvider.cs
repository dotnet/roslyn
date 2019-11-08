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

namespace Microsoft.CodeAnalysis.CSharp.UseExplicitTypeForConst
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExplicitTypeForConst), Shared]
    internal sealed class UseExplicitTypeForConstCodeFixProvider : CodeFixProvider
    {
        private const string CS0822 = nameof(CS0822); // Implicitly-typed variables cannot be constant

        [ImportingConstructor]
        public UseExplicitTypeForConstCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(CS0822);

        public override FixAllProvider GetFixAllProvider()
        {
            // This code fix addresses a very specific compiler error. It's unlikely there will be more than 1 of them at a time.
            return null;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root.FindNode(context.Span) is VariableDeclarationSyntax { Variables: { Count: 1 } } variableDeclaration)
            {
                var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

                var type = semanticModel.GetTypeInfo(variableDeclaration.Type, context.CancellationToken).ConvertedType;
                if (type == null || type.TypeKind == TypeKind.Error || type.IsAnonymousType)
                {
                    return;
                }

                context.RegisterCodeFix(
                    new MyCodeAction(c => FixAsync(context.Document, context.Span, type, c)),
                    context.Diagnostics);
            }
        }

        private static async Task<Document> FixAsync(
            Document document, TextSpan span, ITypeSymbol type, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var variableDeclaration = (VariableDeclarationSyntax)root.FindNode(span);

            var newRoot = root.ReplaceNode(variableDeclaration.Type, type.GenerateTypeSyntax(allowVar: false));
            return document.WithSyntaxRoot(newRoot);
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Use_explicit_type_instead_of_var,
                       createChangedDocument)
            {
            }
        }
    }
}
