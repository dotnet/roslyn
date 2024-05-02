// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class ExtensionKeywordRecommender() : AbstractSyntacticSingleKeywordRecommender(SyntaxKind.ExtensionKeyword)
{
    private static readonly ISet<SyntaxKind> s_validModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.PublicKeyword,
            SyntaxKind.ProtectedKeyword,
            SyntaxKind.InternalKeyword,
            SyntaxKind.PrivateKeyword,
            SyntaxKind.UnsafeKeyword,
            SyntaxKind.FileKeyword,
            SyntaxKind.NewKeyword,
            SyntaxKind.StaticKeyword,
        };

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        var targetToken = context.TargetToken;
        if (targetToken.Kind() is not SyntaxKind.ImplicitKeyword and not SyntaxKind.ExplicitKeyword)
            return false;

        return context.SyntaxTree.IsTypeDeclarationContext(
            targetToken.SpanStart,
            context: null,
            validModifiers: s_validModifiers,
            // Extensions can't appear in any other types.
            validTypeDeclarations: null,
            canBePartial: true,
            cancellationToken);
    }
}
