// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SimplifyThisOrMe;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyThisOrMe;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SimplifyThisOrMe), Shared]
[ExtensionOrder(After = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class CSharpSimplifyThisOrMeCodeFixProvider()
    : AbstractSimplifyThisOrMeCodeFixProvider<MemberAccessExpressionSyntax>
{
    protected override string GetTitle()
        => CSharpFeaturesResources.Remove_this_qualification;

    protected override SyntaxNode Rewrite(SyntaxNode root, ISet<MemberAccessExpressionSyntax> memberAccessNodes)
        => new Rewriter(memberAccessNodes).Visit(root);

    private sealed class Rewriter(ISet<MemberAccessExpressionSyntax> memberAccessNodes) : CSharpSyntaxRewriter
    {
        private readonly ISet<MemberAccessExpressionSyntax> _memberAccessNodes = memberAccessNodes;

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            => _memberAccessNodes.Contains(node)
                ? node.GetNameWithTriviaMoved()
                : base.VisitMemberAccessExpression(node);
    }
}
