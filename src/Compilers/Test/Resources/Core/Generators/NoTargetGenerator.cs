// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Build NetFrameworkGenerator.dll with: csc.exe /t:library NetFrameworkGenerator.cs /r:Microsoft.CodeAnalysis.dll /r:netstandard.dll /r:mscorlib.dll 

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[Generator]
public class NoTargetGenerator : ISourceGenerator
{
    public void Execute(SourceGeneratorContext context)
    {
    }

    public void Initialize(InitializationContext context)
    {
    }
}
