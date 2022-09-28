﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class AnalyzerConfigDocumentState : TextDocumentState
    {
        private readonly ValueSource<AnalyzerConfig> _analyzerConfigValueSource;

        private AnalyzerConfigDocumentState(
            HostWorkspaceServices solutionServices,
            IDocumentServiceProvider documentServiceProvider,
            DocumentInfo.DocumentAttributes attributes,
            SourceText sourceTextOpt,
            ITextAndVersionSource textAndVersionSource,
            LoadTextOptions loadTextOptions)
            : base(solutionServices, documentServiceProvider, attributes, sourceTextOpt, textAndVersionSource, loadTextOptions)
        {
            _analyzerConfigValueSource = CreateAnalyzerConfigValueSource();
        }

        public AnalyzerConfigDocumentState(
            DocumentInfo documentInfo,
            LoadTextOptions loadTextOptions,
            HostWorkspaceServices solutionServices)
            : base(documentInfo, loadTextOptions, solutionServices)
        {
            _analyzerConfigValueSource = CreateAnalyzerConfigValueSource();
        }

        private ValueSource<AnalyzerConfig> CreateAnalyzerConfigValueSource()
        {
            return new AsyncLazy<AnalyzerConfig>(
                asynchronousComputeFunction: async cancellationToken => AnalyzerConfig.Parse(await GetTextAsync(cancellationToken).ConfigureAwait(false), FilePath),
                synchronousComputeFunction: cancellationToken => AnalyzerConfig.Parse(GetTextSynchronously(cancellationToken), FilePath),
                cacheResult: true);
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
                this.sourceText,
                newTextSource,
                this.LoadTextOptions);
        }
    }
}
