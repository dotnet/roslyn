// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;

namespace Roslyn.VisualStudio.Next.UnitTests.Mocks
{
    public static class TestHostServices
    {
        public static HostServices CreateHostServices(ExportProvider exportProvider = null)
        {
            exportProvider = exportProvider ?? CreateMinimalExportProvider();
            return MefV1HostServices.Create(exportProvider.AsExportProvider());
        }

        public static ExportProvider CreateMinimalExportProvider()
        {
            return ExportProviderCache.CreateExportProvider(
                ServiceTestExportProvider.CreateAssemblyCatalog()
                                         .WithPart(typeof(InProcRemoteHostClientFactory)));
        }

        public static ExportProvider CreateExportProvider()
        {
            return ExportProviderCache.CreateExportProvider(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(typeof(InProcRemoteHostClientFactory)));
        }
    }
}
