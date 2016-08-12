﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Async
{
    internal abstract partial class AbstractAddAwaitCodeFixProvider : AbstractAsyncCodeFix
    {
        protected abstract Task<DescriptionAndNode> GetDescriptionAndNodeAsync(
            SyntaxNode root, SyntaxNode oldNode, SemanticModel semanticModel, Diagnostic diagnostic, Document document, CancellationToken cancellationToken);

        protected override async Task<CodeAction> GetCodeActionAsync(
            SyntaxNode root, SyntaxNode node, Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var data = await this.GetDescriptionAndNodeAsync(root, node, semanticModel, diagnostic, document, cancellationToken).ConfigureAwait(false);
            if (data.Node == null)
            {
                return null;
            }

            return new MyCodeAction(
                data.Description,
                c => Task.FromResult(document.WithSyntaxRoot(data.Node)));
        }

        protected static bool TryGetExpressionType(
            SyntaxNode expression,
            SemanticModel semanticModel,
            out INamedTypeSymbol returnType)
        {
            var typeInfo = semanticModel.GetTypeInfo(expression);
            returnType = typeInfo.Type as INamedTypeSymbol;
            return returnType != null;
        }

        protected static bool TryGetTaskType(SemanticModel semanticModel, out INamedTypeSymbol taskType)
        {
            var compilation = semanticModel.Compilation;
            taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            return taskType != null;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }

        protected struct DescriptionAndNode
        {
            public readonly string Description;
            public readonly SyntaxNode Node;

            public DescriptionAndNode(string description, SyntaxNode node)
            {
                Description = description;
                Node = node;
            }
        }
    }
}