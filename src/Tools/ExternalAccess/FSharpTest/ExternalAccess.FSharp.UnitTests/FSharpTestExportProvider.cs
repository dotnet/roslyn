using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests
{
    public static class FSharpTestExportProvider
    {
        private static Lazy<ComposableCatalog> s_lazyEntireAssemblyCatalogWithFSharp =
            new Lazy<ComposableCatalog>(() => CreateAssemblyCatalogWithFSharp());

        private static Lazy<IExportProviderFactory> s_lazyExportProviderFactoryWithFSharp =
            new Lazy<IExportProviderFactory>(() => ExportProviderCache.GetOrCreateExportProviderFactory(EntireAssemblyCatalogWithFSharp));

        public static ComposableCatalog EntireAssemblyCatalogWithFSharp
            => s_lazyEntireAssemblyCatalogWithFSharp.Value;

        public static IExportProviderFactory ExportProviderFactoryWithFSharp
            => s_lazyExportProviderFactoryWithFSharp.Value;

        public static ExportProvider ExportProviderWithFSharp
            => ExportProviderFactoryWithFSharp.CreateExportProvider();

        private static Lazy<ComposableCatalog> s_lazyMinimumCatalogWithFSharp =
            new Lazy<ComposableCatalog>(() => ExportProviderCache.CreateTypeCatalog(GetNeutralAndFSharpTypes())
                        .WithParts(MinimalTestExportProvider.GetEditorAssemblyCatalog()));

        private static Lazy<IExportProviderFactory> s_lazyMinimumExportProviderFactoryWithFSharp =
            new Lazy<IExportProviderFactory>(() => ExportProviderCache.GetOrCreateExportProviderFactory(MinimumCatalogWithFSharp));

        public static ComposableCatalog MinimumCatalogWithFSharp
            => s_lazyMinimumCatalogWithFSharp.Value;

        public static IExportProviderFactory MinimumExportProviderFactoryWithFSharp
            => s_lazyMinimumExportProviderFactoryWithFSharp.Value;

        private static Type[] GetNeutralAndFSharpTypes()
        {
            var types = new[]
            {
                typeof(FSharpCommandLineStringService),
            };

            return ServiceTestExportProvider.GetLanguageNeutralTypes()
                .Concat(types)
                .Distinct()
                .ToArray();
        }

        private static ComposableCatalog CreateAssemblyCatalogWithFSharp()
            => GetFSharpAssemblyCatalog().WithCompositionService();

        public static ComposableCatalog GetFSharpAssemblyCatalog()
        {
            return ExportProviderCache.GetOrCreateAssemblyCatalog(
                GetNeutralAndFSharpTypes().Select(t => t.Assembly).Distinct(), ExportProviderCache.CreateResolver())
                .WithParts(MinimalTestExportProvider.GetEditorAssemblyCatalog());
        }
    }
}
