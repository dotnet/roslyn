// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class FileKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    private static readonly ISet<SyntaxKind> s_validModifiers = SyntaxKindSet.AllMemberModifiers
        .Where(s => s != SyntaxKind.FileKeyword && !SyntaxFacts.IsAccessibilityModifier(s))
        .ToSet();

    public FileKeywordRecommender()
        : base(SyntaxKind.FileKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.ContainingTypeDeclaration == null
            && context.IsTypeDeclarationContext(s_validModifiers, SyntaxKindSet.AllTypeDeclarations, canBePartial: true, cancellationToken);
    }
}
