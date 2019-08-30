// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    public static class VisualStudioTestExportProvider
    {
        public static readonly IExportProviderFactory Factory;

        static VisualStudioTestExportProvider()
        {
            Factory = ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic
                    .WithParts(ExportProviderCache.GetOrCreateAssemblyCatalog(typeof(CSharpCodeModelService).Assembly))
                    .WithPart(typeof(LanguageServices.UnitTests.VisualStudioTestExportProvider.MockWorkspaceEventListenerProvider)));
        }
    }
}
