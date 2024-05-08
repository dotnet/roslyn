﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class UShortKeywordRecommender() : AbstractSpecialTypePreselectingKeywordRecommender(SyntaxKind.UShortKeyword)
{
    /// <summary>
    /// We set the <see cref="MatchPriority"/> of this item less than the default value so that
    /// completion selects the <see langword="using"/> keyword over it as the user starts typing.
    /// </summary>
    protected override int DefaultMatchPriority => MatchPriority.Default - 1;

    protected override SpecialType SpecialType => SpecialType.System_UInt16;

    protected override bool IsValidContextWorker(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            context.IsEnumBaseListContext ||
            context.IsFixedVariableDeclarationContext ||
            context.IsPrimaryFunctionExpressionContext ||
            context.SyntaxTree.IsAfterKeyword(position, SyntaxKind.StackAllocKeyword, cancellationToken);
    }
}
