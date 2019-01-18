// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Host;
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
                .GetOrCreateExportProviderFactory(ServiceTestExportProvider.CreateAssemblyCatalog().WithPart(typeof(InProcRemoteHostClientFactory)))
                .CreateExportProvider();
        }

        public static ExportProvider CreateExportProvider()
        {
            return ExportProviderCache
                .GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(typeof(InProcRemoteHostClientFactory)))
                .CreateExportProvider();
        }
    }
}
