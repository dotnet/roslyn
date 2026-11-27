// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.CodeAnalysis.Razor.CohostingShared;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[ExportLanguageService(typeof(IMiscellaneousProjectInfoService), LanguageInfoProvider.RazorLanguageName)]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class RazorMiscellaneousProjectInfoService() : IMiscellaneousProjectInfoService
{
    public string ProjectLanguageOverride => LanguageNames.CSharp;

    public bool AddAsAdditionalDocument => true;

    public IEnumerable<AnalyzerReference>? GetAnalyzerReferences(Host.SolutionServices services)
    {
        var filePath = typeof(RazorSourceGenerator).Assembly.Location;
        var loaderProvider = services.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();
        var reference = new AnalyzerFileReference(filePath, loaderProvider.SharedShadowCopyLoader);

        return [reference];
    }
}
