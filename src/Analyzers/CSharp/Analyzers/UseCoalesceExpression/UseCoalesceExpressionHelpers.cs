// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseCoalesceExpression;

internal static class UseCoalesceExpressionHelpers
{
    public static bool IsTargetTyped(SemanticModel semanticModel, ConditionalExpressionSyntax conditional, CancellationToken cancellationToken)
    {
        var conversion = semanticModel.GetConversion(conditional, cancellationToken);
        return conversion.IsConditionalExpression;
    }
}
