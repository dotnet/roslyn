// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Host
{
    internal sealed partial class CodeStyleHostLanguageServices : HostLanguageServices
    {
        private sealed class MefHostExportProvider : IMefHostExportProvider
        {
            private readonly CompositionHost _compositionContext;

            private MefHostExportProvider(CompositionHost compositionContext)
                => _compositionContext = compositionContext;

            public static MefHostExportProvider Create(string languageName)
            {
                var assemblies = CreateAssemblies(languageName);
                var compositionConfiguration = new ContainerConfiguration().WithAssemblies(assemblies);
                return new MefHostExportProvider(compositionConfiguration.CreateContainer());
            }

            private static ImmutableArray<Assembly> CreateAssemblies(string languageName)
            {
                using var disposer = ArrayBuilder<string>.GetInstance(out var assemblyNames);

                assemblyNames.Add("Microsoft.CodeAnalysis.CodeStyle.Fixes");
                switch (languageName)
                {
                    case LanguageNames.CSharp:
                        assemblyNames.Add("Microsoft.CodeAnalysis.CSharp.CodeStyle.Fixes");
                        break;

                    case LanguageNames.VisualBasic:
                        assemblyNames.Add("Microsoft.CodeAnalysis.VisualBasic.CodeStyle.Fixes");
                        break;
                }

                return MefHostServices.DefaultAssemblies.Concat(
                    MefHostServicesHelpers.LoadNearbyAssemblies(assemblyNames));
            }

            IEnumerable<Lazy<TExtension>> IMefHostExportProvider.GetExports<TExtension>()
                => _compositionContext.GetExports<TExtension>().Select(e => new Lazy<TExtension>(() => e));

            IEnumerable<Lazy<TExtension, TMetadata>> IMefHostExportProvider.GetExports<TExtension, TMetadata>()
            {
                var importer = new WithMetadataImporter<TExtension, TMetadata>();
                _compositionContext.SatisfyImports(importer);
                return importer.Exports;
            }

            private class WithMetadataImporter<TExtension, TMetadata>
            {
                [ImportMany]
                public IEnumerable<Lazy<TExtension, TMetadata>> Exports { get; set; }
            }
        }
    }
}
