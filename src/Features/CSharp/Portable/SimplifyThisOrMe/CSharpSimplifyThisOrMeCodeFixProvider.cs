﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SimplifyThisOrMe;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyThisOrMe
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SimplifyThisOrMe), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)]
    internal partial class CSharpSimplifyThisOrMeCodeFixProvider
        : AbstractSimplifyThisOrMeCodeFixProvider<MemberAccessExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpSimplifyThisOrMeCodeFixProvider()
        {
        }

        protected override string GetTitle()
            => CSharpFeaturesResources.Remove_this_qualification;

        protected override SyntaxNode Rewrite(
            SemanticModel semanticModel, SyntaxNode root, ISet<MemberAccessExpressionSyntax> memberAccessNodes)
        {
            var rewriter = new Rewriter(memberAccessNodes);
            return rewriter.Visit(root);
        }

        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly ISet<MemberAccessExpressionSyntax> memberAccessNodes;

            public Rewriter(ISet<MemberAccessExpressionSyntax> memberAccessNodes)
                => this.memberAccessNodes = memberAccessNodes;

            public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
                => memberAccessNodes.Contains(node)
                    ? node.GetNameWithTriviaMoved()
                    : base.VisitMemberAccessExpression(node);
        }
    }
}
