// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class SourceGeneratorItem(
    ProjectId projectId,
    string generatorName,
    SourceGeneratorIdentity generatorIdentity,
    AnalyzerReference analyzerReference) : BaseItem(generatorName)
{
    public ProjectId ProjectId { get; } = projectId;
    public SourceGeneratorIdentity Identity { get; } = generatorIdentity;
    public AnalyzerReference AnalyzerReference { get; } = analyzerReference;

    // TODO: do we need an icon for our use?
    public override ImageMoniker IconMoniker => KnownMonikers.Process;

    public override object GetBrowseObject()
    {
        return new BrowseObject(this);
    }
}
