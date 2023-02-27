// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator
{
    internal static class Composition
    {
        public static readonly ImmutableArray<Assembly> MefCompositionAssemblies =
            MSBuildMefHostServices.DefaultAssemblies.Add(typeof(RoslynLanguageServer).Assembly);

        public static async Task<HostServices> CreateHostServicesAsync()
        {
            var discovery = new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true);

            var catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
                .AddParts(await discovery.CreatePartsAsync(MefCompositionAssemblies))
                .WithCompositionService(); // Makes an ICompositionService export available to MEF parts to import

            var config = CompositionConfiguration.Create(catalog);

            var exportProvider = config.CreateExportProviderFactory().CreateExportProvider();
            return VisualStudioMefHostServices.Create(exportProvider);
        }
    }
}
