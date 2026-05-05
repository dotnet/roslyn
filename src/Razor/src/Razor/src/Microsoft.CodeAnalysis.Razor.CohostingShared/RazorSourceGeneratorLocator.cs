// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.CodeAnalysis.Razor.CohostingShared;

[Export(typeof(IRazorSourceGeneratorLocator))]
[method: ImportingConstructor]
internal sealed class RazorSourceGeneratorLocator() : IRazorSourceGeneratorLocator
{
    public string GetGeneratorFilePath()
    {
        return typeof(RazorSourceGenerator).Assembly.Location;
    }
}
