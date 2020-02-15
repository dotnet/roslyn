// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementAbstractClass
{
    internal abstract partial class AbstractImplementAbstractClassCodeFixProvider<TClassNode> : CodeFixProvider
        where TClassNode : SyntaxNode
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }

        public sealed override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        protected AbstractImplementAbstractClassCodeFixProvider(string diagnosticId)
            => FixableDiagnosticIds = ImmutableArray.Create(diagnosticId);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindToken(context.Span.Start);
            if (!token.Span.IntersectsWith(context.Span))
                return;

            var classNode = token.Parent.GetAncestorOrThis<TClassNode>();
            if (classNode == null)
                return;

            var data = await ImplementAbstractClassData.TryGetDataAsync(document, classNode, cancellationToken).ConfigureAwait(false);
            if (data == null)
                return;

            var abstractClassType = data.ClassType.BaseType!;
            var id = GetCodeActionId(abstractClassType.ContainingAssembly.Name, abstractClassType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            context.RegisterCodeFix(
                new MyCodeAction(
                    FeaturesResources.Implement_Abstract_Class,
                    c => data.ImplementAbstractClassAsync(throughMember: null, c), id),
                context.Diagnostics);

            foreach (var through in data.GetDelegatableMembers())
            {
                id = GetCodeActionId(
                    abstractClassType.ContainingAssembly.Name,
                    abstractClassType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    through.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                context.RegisterCodeFix(
                    new MyCodeAction(
                        string.Format(FeaturesResources.Implement_through_0, GetName(through)),
                        c => data.ImplementAbstractClassAsync(through, c), id),
                    context.Diagnostics);
            }
        }

        private static string GetName(ISymbol throughMember)
            => throughMember switch
            {
                IFieldSymbol field => field.Name,
                IPropertySymbol property => property.Name,
                _ => throw new InvalidOperationException(),
            };

        private static string GetCodeActionId(string assemblyName, string abstractTypeFullyQualifiedName, string through = "")
            => FeaturesResources.Implement_Abstract_Class + ";" + assemblyName + ";" + abstractTypeFullyQualifiedName + ";" + through;

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string id)
                : base(title, createChangedDocument, id)
            {
            }
        }
    }
}
