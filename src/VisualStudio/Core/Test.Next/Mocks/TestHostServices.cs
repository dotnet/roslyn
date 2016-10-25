// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;

namespace Roslyn.VisualStudio.Next.UnitTests.Mocks
{
    public static class TestHostServices
    {
        public static HostServices CreateHostServices()
        {
            return MefV1HostServices.Create(
                MinimalTestExportProvider.CreateExportProvider(
                    ServiceTestExportProvider.CreateAssemblyCatalog().WithPart(typeof(InProcRemoteHostClientFactory))).AsExportProvider());
        }
    }
}
