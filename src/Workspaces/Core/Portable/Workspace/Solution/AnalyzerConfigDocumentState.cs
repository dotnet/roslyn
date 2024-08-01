// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed class AnalyzerConfigDocumentState : TextDocumentState
{
    private readonly AsyncLazy<AnalyzerConfig> _lazyAnalyzerConfig;

    private AnalyzerConfigDocumentState(
        SolutionServices solutionServices,
        IDocumentServiceProvider? documentServiceProvider,
        DocumentInfo.DocumentAttributes attributes,
        ITextAndVersionSource textAndVersionSource,
        LoadTextOptions loadTextOptions,
        AsyncLazy<AnalyzerConfig>? lazyAnalyzerConfig = null)
        : base(solutionServices, documentServiceProvider, attributes, textAndVersionSource, loadTextOptions)
    {
        _lazyAnalyzerConfig = lazyAnalyzerConfig ?? AsyncLazy.Create(
            asynchronousComputeFunction: static async (self, cancellationToken) => AnalyzerConfig.Parse(await self.GetTextAsync(cancellationToken).ConfigureAwait(false), self.FilePath),
            synchronousComputeFunction: static (self, cancellationToken) => AnalyzerConfig.Parse(self.GetTextSynchronously(cancellationToken), self.FilePath),
            arg: this);
    }

    public AnalyzerConfigDocumentState(
        SolutionServices solutionServices,
        DocumentInfo documentInfo,
        LoadTextOptions loadTextOptions)
        : this(solutionServices, documentInfo.DocumentServiceProvider, documentInfo.Attributes, CreateTextAndVersionSource(solutionServices, documentInfo, loadTextOptions), loadTextOptions)
    {
    }

    protected override TextDocumentState UpdateAttributes(DocumentInfo.DocumentAttributes newAttributes)
        => new AnalyzerConfigDocumentState(
            SolutionServices,
            DocumentServiceProvider,
            newAttributes,
            TextAndVersionSource,
            LoadTextOptions,
            // Reuse parsed config unless the path changed:
            Attributes.FilePath == newAttributes.FilePath ? _lazyAnalyzerConfig : null);

    public new AnalyzerConfigDocumentState UpdateText(TextLoader loader, PreservationMode mode)
        => (AnalyzerConfigDocumentState)base.UpdateText(loader, mode);

    public new AnalyzerConfigDocumentState UpdateText(SourceText text, PreservationMode mode)
        => (AnalyzerConfigDocumentState)base.UpdateText(text, mode);

    public new AnalyzerConfigDocumentState UpdateText(TextAndVersion newTextAndVersion, PreservationMode mode)
        => (AnalyzerConfigDocumentState)base.UpdateText(newTextAndVersion, mode);

    protected override TextDocumentState UpdateText(ITextAndVersionSource newTextSource, PreservationMode mode, bool incremental)
    {
        return new AnalyzerConfigDocumentState(
            this.SolutionServices,
            this.DocumentServiceProvider,
            this.Attributes,
            newTextSource,
            this.LoadTextOptions);
    }

    public AnalyzerConfig GetAnalyzerConfig(CancellationToken cancellationToken)
        => _lazyAnalyzerConfig.GetValue(cancellationToken);

    public Task<AnalyzerConfig> GetAnalyzerConfigAsync(CancellationToken cancellationToken)
        => _lazyAnalyzerConfig.GetValueAsync(cancellationToken);
}
