// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Debugging;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Razor;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;
#endif

internal static class RazorCSharpProximityExpressionResolverService
{
    public static IList<string> GetProximityExpressions(SyntaxTree syntaxTree, int absoluteIndex, CancellationToken cancellationToken)
        => CSharpProximityExpressionsService.GetProximityExpressions(syntaxTree, absoluteIndex, cancellationToken);
}
