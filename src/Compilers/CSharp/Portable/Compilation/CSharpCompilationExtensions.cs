// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
