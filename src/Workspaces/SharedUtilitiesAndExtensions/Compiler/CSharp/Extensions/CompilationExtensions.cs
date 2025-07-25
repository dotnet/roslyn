// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class CompilationExtensions
{
    extension(Compilation compilation)
    {
        public LanguageVersion LanguageVersion()
        => ((CSharpCompilation)compilation).LanguageVersion;
    }
}
