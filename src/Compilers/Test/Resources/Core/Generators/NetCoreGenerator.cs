// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Build NetCoreGenerator.dll with: csc.exe /t:library NetCoreGenerator.cs /r:Microsoft.CodeAnalysis.dll /r:netstandard.dll /r:System.Private.Corlib.dll /r:System.Runtime.dll

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Runtime.Versioning;

[assembly: TargetFramework(".NETCoreApp,Version=v5.0", FrameworkDisplayName = "")]

[Generator]
public class NetCoreGenerator : ISourceGenerator
{
    public void Execute(SourceGeneratorContext context)
    {
    }

    public void Initialize(InitializationContext context)
    {
    }
}
