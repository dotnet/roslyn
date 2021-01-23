// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Debugging;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal class RazorCSharpProximityExpressionResolver
    {
        public IList<string> GetProximityExpressions(SyntaxTree syntaxTree, int absoluteIndex)
            => CSharpProximityExpressionsService.Do(syntaxTree, absoluteIndex);
    }
}
