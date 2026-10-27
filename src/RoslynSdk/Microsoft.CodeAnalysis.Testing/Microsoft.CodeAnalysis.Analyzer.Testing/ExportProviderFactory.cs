// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Testing
{
    internal static class ExportProviderFactory
    {
        private static readonly object s_lock = new();
        private static Task<IExportProviderFactory>? s_exportProviderFactory;

        public static Task<IExportProviderFactory> GetOrCreateExportProviderFactoryAsync()
        {
            if (s_exportProviderFactory is { } exportProviderFactory)
            {
                return exportProviderFactory;
            }

            lock (s_lock)
            {
                s_exportProviderFactory ??= Task.Run(CreateExportProviderFactorySlowAsync);
                return s_exportProviderFactory;
            }

            static async Task<IExportProviderFactory> CreateExportProviderFactorySlowAsync()
            {
                var discovery = new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true);
                var parts = await discovery.CreatePartsAsync(MefHostServices.DefaultAssemblies).ConfigureAwait(false);
                var catalog = ComposableCatalog.Create(Resolver.DefaultInstance).AddParts(parts).WithDocumentTextDifferencingService();

                var configuration = CompositionConfiguration.Create(catalog);
                var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
                return runtimeComposition.CreateExportProviderFactory();
            }
        }
    }
}
