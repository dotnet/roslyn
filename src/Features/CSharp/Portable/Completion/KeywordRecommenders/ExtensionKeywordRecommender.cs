// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class ExtensionKeywordRecommender() : AbstractSyntacticSingleKeywordRecommender(SyntaxKind.ExtensionKeyword)
{
    private static readonly ISet<SyntaxKind> s_validModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer);
    private static readonly ISet<SyntaxKind> s_validTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
    {
        SyntaxKind.ClassDeclaration
    };

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            context.ContainingTypeDeclaration?.Modifiers.Any(SyntaxKind.StaticKeyword) is true &&
            context.IsTypeDeclarationContext(
                validModifiers: s_validModifiers,
                validTypeDeclarations: s_validTypeDeclarations,
                canBePartial: false,
                cancellationToken);
    }
}
