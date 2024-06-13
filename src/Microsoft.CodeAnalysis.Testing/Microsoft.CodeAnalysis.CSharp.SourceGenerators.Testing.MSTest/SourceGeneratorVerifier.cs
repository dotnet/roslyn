// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing.MSTest
{
    [Obsolete(ObsoleteMessages.FrameworkPackages)]
    public static class SourceGeneratorVerifier
    {
        public static SourceGeneratorVerifier<TSourceGenerator> Create<TSourceGenerator>()
            where TSourceGenerator : new()
        {
            return new SourceGeneratorVerifier<TSourceGenerator>();
        }
    }
}
