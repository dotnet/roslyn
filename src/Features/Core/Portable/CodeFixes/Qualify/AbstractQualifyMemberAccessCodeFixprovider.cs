// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CodeFixes.Qualify
{
    internal abstract class AbstractQualifyMemberAccessCodeFixprovider<TSyntaxNode> : CodeFixProvider where TSyntaxNode : SyntaxNode
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.AddQualificationDiagnosticId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindToken(span.Start);
            if (!token.Span.IntersectsWith(span))
            {
                return;
            }

            var node = token.GetAncestor<TSyntaxNode>();
            if (node == null)
            {
                return;
            }

            var generator = document.GetLanguageService<SyntaxGenerator>();
            var codeAction = new QualifyMemberAccessCodeAction(
                FeaturesResources.AddQualification,
                c => document.ReplaceNodeAsync(node, GetReplacementSyntax(node, generator), c),
                FeaturesResources.AddQualification);
            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static SyntaxNode GetReplacementSyntax(SyntaxNode node, SyntaxGenerator generator)
        {
            var qualifiedAccess =
                generator.MemberAccessExpression(
                    generator.ThisExpression(),
                    node.WithLeadingTrivia())
                .WithLeadingTrivia(node.GetLeadingTrivia());
            return qualifiedAccess;
        }
    }
}
