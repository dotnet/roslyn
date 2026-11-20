// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators;

#pragma warning disable RS1042 // Test-only implementation used to verify assembly identity checks.
internal sealed class RazorSourceGenerator : ISourceGenerator
#pragma warning restore RS1042
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
    }
}
