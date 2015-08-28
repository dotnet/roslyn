// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ImplementAbstractClass;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.ImplementAbstractClass
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ImplementAbstractClass), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateType)]
    internal class ImplementAbstractClassCodeFixProvider : CodeFixProvider
    {
        private const string CS0534 = "CS0534"; // 'Program' does not implement inherited abstract member 'Foo.bar()'

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS0534); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var token = root.FindToken(context.Span.Start);
            if (!token.Span.IntersectsWith(context.Span))
            {
                return;
            }

            var classNode = token.Parent as ClassDeclarationSyntax;
            if (classNode == null)
            {
                return;
            }

            var service = context.Document.GetLanguageService<IImplementAbstractClassService>();
            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            foreach (var baseTypeSyntax in classNode.BaseList.Types)
            {
                var node = baseTypeSyntax.Type;

                if (service.CanImplementAbstractClass(
                    context.Document,
                    model,
                    node,
                    context.CancellationToken))
                {
                    var title = CSharpFeaturesResources.ImplementAbstractClass;
                    var abstractType = model.GetTypeInfo(node, context.CancellationToken).Type;
                    var id = GetCodeActionId(abstractType.ContainingAssembly.Name, abstractType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                    context.RegisterCodeFix(
                        new MyCodeAction(title,
                            (c) => ImplementAbstractClassAsync(context.Document, node, c),
                            id),
                        context.Diagnostics);
                    return;
                }
            }
        }

        // internal for testing purposes.
        internal static string GetCodeActionId(string assemblyName, string abstractTypeFullyQualifiedName)
        {
            return CSharpFeaturesResources.ImplementAbstractClass + ";" +
                assemblyName + ";" +
                abstractTypeFullyQualifiedName;
        }

        private async Task<Document> ImplementAbstractClassAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IImplementAbstractClassService>();
            return await service.ImplementAbstractClassAsync(
                document,
                await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false),
                node,
                cancellationToken).ConfigureAwait(false);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string id) :
                base(title, createChangedDocument, id)
            {
            }
        }
    }
}
