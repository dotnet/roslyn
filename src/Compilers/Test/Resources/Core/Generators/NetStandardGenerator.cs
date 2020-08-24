// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Build NetStandardGenerator.dll with: csc.exe /t:library NetStandardGenerator.cs /r:Microsoft.CodeAnalysis.dll /r:netstandard.dll 
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Runtime.Versioning;

[assembly: TargetFramework(".NETStandard,Version=v2.0", FrameworkDisplayName = "")]

[Generator]
public class NetStandardGenerator : ISourceGenerator
{
    public void Execute(SourceGeneratorContext context)
    {
    }

    public void Initialize(InitializationContext context)
    {
    }
}
