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
        private const string CS0534 = nameof(CS0534); // 'Program' does not implement inherited abstract member 'Foo.bar()'

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(CS0534);

        public sealed override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

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

            var service = document.GetLanguageService<IImplementAbstractClassService>();

            if (await service.CanImplementAbstractClassAsync(
                document,
                classNode,
                cancellationToken).ConfigureAwait(false))
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var classSymbol = semanticModel.GetDeclaredSymbol(classNode);
                var abstractType = classSymbol.BaseType;
                var id = GetCodeActionId(abstractType.ContainingAssembly.Name, abstractType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                context.RegisterCodeFix(
                    new MyCodeAction(
                        c => ImplementAbstractClassAsync(document, classNode, c),
                        id),
                    context.Diagnostics);
            }
        }

        // internal for testing purposes.
        internal static string GetCodeActionId(string assemblyName, string abstractTypeFullyQualifiedName)
        {
            return CSharpFeaturesResources.Implement_Abstract_Class + ";" +
                assemblyName + ";" +
                abstractTypeFullyQualifiedName;
        }

        private Task<Document> ImplementAbstractClassAsync(
            Document document, ClassDeclarationSyntax classNode, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IImplementAbstractClassService>();
            return service.ImplementAbstractClassAsync(document, classNode, cancellationToken);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string id) :
                base(CSharpFeaturesResources.Implement_Abstract_Class, createChangedDocument, id)
            {
            }
        }
    }
}