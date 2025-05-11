// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RelatedDocuments;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RelatedDocuments;

[ExportLanguageService(typeof(IRelatedDocumentsService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpRelatedDocumentsService() : AbstractRelatedDocumentsService<
    ExpressionSyntax,
    NameSyntax>
{
    protected override IEnumerable<(ExpressionSyntax expression, SyntaxToken nameToken)> IteratePotentialTypeNodes(SyntaxNode root)
    {
        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var stack);
        stack.Push(root);

        while (stack.TryPop(out var current))
        {
            if (current is MemberAccessExpressionSyntax memberAccess)
            {
                // Could be a static member access off of a type name.  Check the left side, and if it's just a
                // dotted name, return that.

                if (IsPossibleTypeName(memberAccess.Expression, out var nameToken))
                {
                    // Something like `X.Y.Z` where `X.Y` is a type name.  Bind X.Y
                    yield return (memberAccess.Expression, nameToken);
                }
                else
                {
                    // Something like `(...).Y`.  Recurse down the left side of the member access to see if there
                    // are types in there. We don't want to recurse down the name portion as it will never be a
                    // type.
                    stack.Push(memberAccess.Expression);
                }

                continue;
            }
            else if (current is NameSyntax name)
            {
                yield return (name, name.GetNameToken());

                // Intentionally continue to recurse down the name so that if we have things like `X<Y>` we'll bind
                // the inner `Y` as well.
            }

            // Don't need to recurse in order as our caller is ordering results anyways.
            foreach (var child in current.ChildNodesAndTokens())
            {
                if (child.AsNode(out var childNode))
                    stack.Push(childNode);
            }
        }

        static bool IsPossibleTypeName(ExpressionSyntax expression, out SyntaxToken nameToken)
        {
            while (expression is MemberAccessExpressionSyntax memberAccessExpression)
                expression = memberAccessExpression.Expression;

            if (expression is not NameSyntax name)
            {
                nameToken = default;
                return false;
            }

            nameToken = name.GetNameToken();
            return true;
        }
    }
}
