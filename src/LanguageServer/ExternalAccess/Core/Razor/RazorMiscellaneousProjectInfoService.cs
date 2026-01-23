// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

[Shared]
[ExportLanguageService(typeof(IMiscellaneousProjectInfoService), Constants.RazorLanguageName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorMiscellaneousProjectInfoService([Import(AllowDefault = true)] Lazy<IRazorSourceGeneratorLocator>? razorSourceGeneratorLocator) : IMiscellaneousProjectInfoService
{
    public string ProjectLanguageOverride => LanguageNames.CSharp;

    public bool AddAsAdditionalDocument => true;

    public IEnumerable<AnalyzerReference>? GetAnalyzerReferences(Host.SolutionServices services)
    {
        if (razorSourceGeneratorLocator is null)
        {
            return null;
        }

        var filePath = razorSourceGeneratorLocator.Value.GetGeneratorFilePath();
        var loaderProvider = services.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();
        var reference = new AnalyzerFileReference(filePath, loaderProvider.SharedShadowCopyLoader);

        return [reference];
    }
}
