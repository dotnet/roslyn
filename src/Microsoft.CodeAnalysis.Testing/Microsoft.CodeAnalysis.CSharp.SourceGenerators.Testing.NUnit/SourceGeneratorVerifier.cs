// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Testing.NUnit
{
    public static class SourceGeneratorVerifier
    {
        public static SourceGeneratorVerifier<TSourceGenerator> Create<TSourceGenerator>()
            where TSourceGenerator : ISourceGenerator, new()
        {
            return new SourceGeneratorVerifier<TSourceGenerator>();
        }
    }
}
