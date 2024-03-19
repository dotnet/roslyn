// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed class AnalyzerConfigDocumentState : TextDocumentState
{
    private readonly AsyncLazy<AnalyzerConfig> _analyzerConfigValueSource;

    private AnalyzerConfigDocumentState(
        SolutionServices solutionServices,
        IDocumentServiceProvider documentServiceProvider,
        DocumentInfo.DocumentAttributes attributes,
        ITextAndVersionSource textAndVersionSource,
        LoadTextOptions loadTextOptions)
        : base(solutionServices, documentServiceProvider, attributes, textAndVersionSource, loadTextOptions)
    {
        _analyzerConfigValueSource = CreateAnalyzerConfigValueSource();
    }

    public AnalyzerConfigDocumentState(
        SolutionServices solutionServices,
        DocumentInfo documentInfo,
        LoadTextOptions loadTextOptions)
        : base(solutionServices, documentInfo, loadTextOptions)
    {
        _analyzerConfigValueSource = CreateAnalyzerConfigValueSource();
    }

    private AsyncLazy<AnalyzerConfig> CreateAnalyzerConfigValueSource()
    {
        return AsyncLazy.Create(
            asynchronousComputeFunction: static async (self, cancellationToken) => AnalyzerConfig.Parse(await self.GetTextAsync(cancellationToken).ConfigureAwait(false), self.FilePath),
            synchronousComputeFunction: static (self, cancellationToken) => AnalyzerConfig.Parse(self.GetTextSynchronously(cancellationToken), self.FilePath),
            arg: this);
    }

    public AnalyzerConfig GetAnalyzerConfig(CancellationToken cancellationToken) => _analyzerConfigValueSource.GetValue(cancellationToken);
    public Task<AnalyzerConfig> GetAnalyzerConfigAsync(CancellationToken cancellationToken) => _analyzerConfigValueSource.GetValueAsync(cancellationToken);

    public new AnalyzerConfigDocumentState UpdateText(TextLoader loader, PreservationMode mode)
        => (AnalyzerConfigDocumentState)base.UpdateText(loader, mode);

    public new AnalyzerConfigDocumentState UpdateText(SourceText text, PreservationMode mode)
        => (AnalyzerConfigDocumentState)base.UpdateText(text, mode);

    public new AnalyzerConfigDocumentState UpdateText(TextAndVersion newTextAndVersion, PreservationMode mode)
        => (AnalyzerConfigDocumentState)base.UpdateText(newTextAndVersion, mode);

    protected override TextDocumentState UpdateText(ITextAndVersionSource newTextSource, PreservationMode mode, bool incremental)
    {
        return new AnalyzerConfigDocumentState(
            this.solutionServices,
            this.Services,
            this.Attributes,
            newTextSource,
            this.LoadTextOptions);
    }
}
