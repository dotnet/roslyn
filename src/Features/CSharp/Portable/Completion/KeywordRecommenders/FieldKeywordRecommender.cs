// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class FieldKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    // interfaces don't have members that you can put a [field:] attribute on
    private static readonly ISet<SyntaxKind> s_validTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
    {
        SyntaxKind.StructDeclaration,
        SyntaxKind.ClassDeclaration,
        SyntaxKind.RecordDeclaration,
        SyntaxKind.RecordStructDeclaration,
        SyntaxKind.EnumDeclaration,
    };

    public FieldKeywordRecommender()
        : base(SyntaxKind.FieldKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        => context.IsMemberAttributeContext(s_validTypeDeclarations, cancellationToken);
}
