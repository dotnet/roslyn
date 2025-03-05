// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.ResxSourceGenerator.CSharp
{
    [Generator(LanguageNames.CSharp)]
    internal sealed class CSharpResxGenerator : AbstractResxGenerator
    {
        protected override bool SupportsNullable(Compilation compilation)
        {
            return ((CSharpCompilation)compilation).LanguageVersion >= LanguageVersion.CSharp8;
        }
    }
}
