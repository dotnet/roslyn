// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementAbstractClass
{
    internal abstract class AbstractImplementAbstractClassCodeFixProvider<TClassNode> : CodeFixProvider
        where TClassNode : SyntaxNode
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        protected AbstractImplementAbstractClassCodeFixProvider(string diagnosticId)
            => FixableDiagnosticIds = ImmutableArray.Create(diagnosticId);

        protected abstract SyntaxToken GetClassIdentifier(TClassNode classNode);

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

            var data = await ImplementAbstractClassData.TryGetDataAsync(
                document, classNode, GetClassIdentifier(classNode), context.Options.GetImplementTypeGenerationOptions(document.Project.LanguageServices), cancellationToken).ConfigureAwait(false);
            if (data == null)
                return;

            var abstractClassType = data.AbstractClassType;
            var id = GetCodeActionId(abstractClassType.ContainingAssembly.Name, abstractClassType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            context.RegisterCodeFix(
                CodeAction.Create(
                    FeaturesResources.Implement_abstract_class,
                    c => data.ImplementAbstractClassAsync(throughMember: null, canDelegateAllMembers: null, c),
                    id),
                context.Diagnostics);

            foreach (var (through, canDelegateAllMembers) in data.GetDelegatableMembers())
            {
                id = GetCodeActionId(
                    abstractClassType.ContainingAssembly.Name,
                    abstractClassType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    through.Name);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        string.Format(FeaturesResources.Implement_through_0, through.Name),
                        c => data.ImplementAbstractClassAsync(through, canDelegateAllMembers, c),
                        id),
                    context.Diagnostics);
            }
        }

        private static string GetCodeActionId(string assemblyName, string abstractTypeFullyQualifiedName, string through = "")
            => FeaturesResources.Implement_abstract_class + ";" + assemblyName + ";" + abstractTypeFullyQualifiedName + ";" + through;
    }
}
