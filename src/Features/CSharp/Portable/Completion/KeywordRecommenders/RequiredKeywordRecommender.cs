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

internal class RequiredKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    private static readonly ISet<SyntaxKind> s_validModifiers = SyntaxKindSet.AllMemberModifiers.Where(s => s is not (SyntaxKind.RequiredKeyword or SyntaxKind.StaticKeyword or SyntaxKind.ReadOnlyKeyword or SyntaxKind.ConstKeyword)).ToSet();

    private static readonly ISet<SyntaxKind> s_validTypeDeclarations = SyntaxKindSet.ClassStructRecordTypeDeclarations;

    public RequiredKeywordRecommender()
        : base(SyntaxKind.RequiredKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.IsMemberDeclarationContext(s_validModifiers, s_validTypeDeclarations, canBePartial: true, cancellationToken);
    }
}
