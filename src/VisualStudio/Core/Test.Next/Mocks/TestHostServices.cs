// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices;

namespace Roslyn.VisualStudio.Next.UnitTests.Mocks
{
    public static class TestHostServices
    {
        public static HostServices CreateHostServices(ExportProvider exportProvider = null)
        {
            return VisualStudioMefHostServices.Create(exportProvider ?? CreateMinimalExportProvider());
        }

        public static ExportProvider CreateMinimalExportProvider()
        {
            return ExportProviderCache
                .GetOrCreateExportProviderFactory(ServiceTestExportProvider.CreateAssemblyCatalog().WithPart(typeof(InProcRemoteHostClientProvider.Factory)))
                .CreateExportProvider();
        }

        public static ExportProvider CreateExportProvider()
        {
            return ExportProviderCache
                .GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(typeof(InProcRemoteHostClientProvider.Factory)))
                .CreateExportProvider();
        }
    }
}
