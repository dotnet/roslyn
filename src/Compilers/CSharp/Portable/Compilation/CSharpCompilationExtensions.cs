// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class CSharpCompilationExtensions
    {
        internal static bool IsFeatureEnabled(this CSharpCompilation compilation, MessageID feature)
        {
            return compilation.LanguageVersion >= feature.RequiredVersion();
        }

        internal static bool IsFeatureEnabled(this SyntaxNode? syntax, MessageID feature)
        {
            return ((CSharpParseOptions?)syntax?.SyntaxTree.Options)?.IsFeatureEnabled(feature) == true;
        }

        internal static bool ShouldEmitNativeIntegerAttributes(this CSharpCompilation compilation, TypeSymbol type)
        {
            return compilation.ShouldEmitNativeIntegerAttributes() && type.ContainsNativeIntegerWrapperType();
        }
    }
}
