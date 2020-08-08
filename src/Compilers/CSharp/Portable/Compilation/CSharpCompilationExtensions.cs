// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class CSharpCompilationExtensions
    {
        internal static bool IsFeatureEnabled(this CSharpCompilation compilation, MessageID feature)
        {
            return ((CSharpParseOptions)compilation.SyntaxTrees.FirstOrDefault()?.Options)?.IsFeatureEnabled(feature) == true;
        }
    }
}
