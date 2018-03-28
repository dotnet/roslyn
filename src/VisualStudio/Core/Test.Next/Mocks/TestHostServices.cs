// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Composition;

namespace Roslyn.VisualStudio.Next.UnitTests.Mocks
{
    public static class TestHostServices
    {
        public static readonly ExportProvider SharedExportProvider = CreateExportProvider();

        public static HostServices CreateHostServices(ExportProvider exportProvider = null)
        {
            exportProvider = exportProvider ?? CreateMinimalExportProvider();
            return MefV1HostServices.Create(exportProvider.AsExportProvider());
        }

        public static ExportProvider CreateMinimalExportProvider()
        {
            return MinimalTestExportProvider.CreateExportProvider(
                ServiceTestExportProvider.CreateAssemblyCatalog()
                                         .WithPart(typeof(InProcRemoteHostClientFactory)));
        }

        public static ExportProvider CreateExportProvider()
        {
            return MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.CreateAssemblyCatalogWithCSharpAndVisualBasic()
                                         .WithPart(typeof(InProcRemoteHostClientFactory)));
        }
    }
}
