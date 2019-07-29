// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.ImplementAbstractClass;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.ImplementAbstractClass
{
    internal abstract class AbstractImplementAbstractClassCodeFixProvider<TClassNode> : CodeFixProvider
        where TClassNode : SyntaxNode
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }

        public sealed override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        protected AbstractImplementAbstractClassCodeFixProvider(string diagnosticId)
        {
            FixableDiagnosticIds = ImmutableArray.Create(diagnosticId);
        }

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

            var classNode = token.Parent.GetAncestorOrThis<TClassNode>();
            if (classNode == null)
            {
                return;
            }

            var service = document.GetLanguageService<IImplementAbstractClassService>();

            var canImplement = await service.CanImplementAbstractClassAsync(
                document,
                classNode,
                cancellationToken).ConfigureAwait(false);
            if (!canImplement)
            {
                return;
            }
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (!(semanticModel.GetDeclaredSymbol(classNode) is INamedTypeSymbol classSymbol))
            {
                return;
            }

            var abstractType = classSymbol.BaseType;
            var id = GetCodeActionId(abstractType.ContainingAssembly.Name, abstractType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            context.RegisterCodeFix(
                new MyCodeAction(
                    c => ImplementAbstractClassAsync(document, classNode, c),
                    id),
                context.Diagnostics);
        }

        private static string GetCodeActionId(string assemblyName, string abstractTypeFullyQualifiedName)
        {
            return FeaturesResources.Implement_Abstract_Class + ";" +
                assemblyName + ";" +
                abstractTypeFullyQualifiedName;
        }

        private Task<Document> ImplementAbstractClassAsync(
            Document document, TClassNode classNode, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IImplementAbstractClassService>();
            return service.ImplementAbstractClassAsync(document, classNode, cancellationToken);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string id)
                : base(FeaturesResources.Implement_Abstract_Class, createChangedDocument, id)
            {
            }
        }
    }
}
