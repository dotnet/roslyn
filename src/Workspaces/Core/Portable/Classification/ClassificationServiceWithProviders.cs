// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// A <see cref="ClassificationService"/> that defers to one or more <see cref="ClassificationProvider"/>.
    /// </summary>
    internal abstract class ClassificationServiceWithProviders : ClassificationService
    {
        private readonly Workspace _workspace;

        protected ClassificationServiceWithProviders(Workspace workspace)
        {
            _workspace = workspace;
        }

        protected abstract string Language { get; }

        private ImmutableArray<ClassificationProvider> _importedProviders;

        protected ImmutableArray<ClassificationProvider> GetProviders()
        {
            if (_importedProviders == null)
            {
                var language = this.Language;
                var mefExporter = (IMefHostExportProvider)_workspace.Services.HostServices;

                var providers = ExtensionOrderer.Order(
                        mefExporter.GetExports<ClassificationProvider, ClassificationProviderMetadata>()
                        .Where(lz => lz.Metadata.Language == language)
                        ).Select(lz => lz.Value).ToImmutableArray();

                ImmutableInterlocked.InterlockedCompareExchange(ref _importedProviders, providers, default(ImmutableArray<ClassificationProvider>));
            }

            return _importedProviders;
        }

        public override ClassifiedSpan AdjustClassification(SourceText text, ClassifiedSpan classifiedSpan)
        {
            var extensionManager = _workspace.Services.GetService<IExtensionManager>();
            foreach (var provider in this.GetProviders())
            {
                try
                {
                    if (!extensionManager.IsDisabled(provider))
                    {
                        var reclassified = provider.AdjustClassification(text, classifiedSpan);
                        if (!reclassified.Equals(classifiedSpan))
                        {
                            return reclassified;
                        }
                    }
                }
                catch (Exception e) when (extensionManager.CanHandleException(provider, e))
                {
                    extensionManager.HandleException(provider, e);
                }
            }

            return classifiedSpan;
        }

        public override ImmutableArray<ClassifiedSpan> GetLexicalClassifications(SourceText text, TextSpan span, CancellationToken cancellationToken)
        {
            var extensionManager = _workspace.Services.GetService<IExtensionManager>();
            var spans = SharedPools.Default<List<ClassifiedSpan>>().Allocate();
            var spanSet = SharedPools.Default<HashSet<ClassifiedSpan>>().Allocate();
            try
            {
                var context = new ClassificationContext(spans, spanSet);
                foreach (var provider in this.GetProviders())
                {
                    try
                    {
                        if (!extensionManager.IsDisabled(provider))
                        {
                            provider.AddLexicalClassifications(text, span, context, cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e) when (extensionManager.CanHandleException(provider, e))
                    {
                        extensionManager.HandleException(provider, e);
                    }
                }

                return spans.ToImmutableArray();
            }
            finally
            {
                SharedPools.Default<List<ClassifiedSpan>>().ClearAndFree(spans);
                SharedPools.Default<HashSet<ClassifiedSpan>>().ClearAndFree(spanSet);
            }
        }

        public override async Task<ImmutableArray<ClassifiedSpan>> GetSyntacticClassificationsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var extensionManager = _workspace.Services.GetService<IExtensionManager>();
            var spans = SharedPools.Default<List<ClassifiedSpan>>().Allocate();
            var spanSet = SharedPools.Default<HashSet<ClassifiedSpan>>().Allocate();
            try
            {
                var context = new ClassificationContext(spans, spanSet);
                foreach (var provider in this.GetProviders())
                {
                    try
                    {
                        if (!extensionManager.IsDisabled(provider))
                        {
                            await provider.AddSyntacticClassificationsAsync(document, span, context, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e) when (extensionManager.CanHandleException(provider, e))
                    {
                        extensionManager.HandleException(provider, e);
                    }
                }

                return spans.ToImmutableArray();
            }
            finally
            {
                SharedPools.Default<List<ClassifiedSpan>>().ClearAndFree(spans);
                SharedPools.Default<HashSet<ClassifiedSpan>>().ClearAndFree(spanSet);
            }
        }

        public override async Task<ImmutableArray<ClassifiedSpan>> GetSemanticClassificationsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var extensionManager = _workspace.Services.GetService<IExtensionManager>();
            var spans = SharedPools.Default<List<ClassifiedSpan>>().Allocate();
            var spanSet = SharedPools.Default<HashSet<ClassifiedSpan>>().Allocate();
            try
            {
                var context = new ClassificationContext(spans, spanSet);
                foreach (var provider in this.GetProviders())
                {
                    try
                    {
                        if (!extensionManager.IsDisabled(provider))
                        {
                            await provider.AddSemanticClassificationsAsync(document, span, context, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e) when (extensionManager.CanHandleException(provider, e))
                    {
                        extensionManager.HandleException(provider, e);
                    }
                }

                return spans.ToImmutableArray();
            }
            finally
            {
                SharedPools.Default<List<ClassifiedSpan>>().ClearAndFree(spans);
                SharedPools.Default<HashSet<ClassifiedSpan>>().ClearAndFree(spanSet);
            }
        }
    }
}