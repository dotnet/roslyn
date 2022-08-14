// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Test.Utilities.TestGenerators;

internal class TestSourceGenerator : ISourceGenerator
{
    public Action<GeneratorInitializationContext>? InitializeImpl;
    public Action<GeneratorExecutionContext>? ExecuteImpl;

    public void Execute(GeneratorExecutionContext context)
        => (ExecuteImpl ?? throw new NotImplementedException()).Invoke(context);

    public void Initialize(GeneratorInitializationContext context)
        => InitializeImpl?.Invoke(context);
}
