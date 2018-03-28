// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    public static class VisualStudioTestExportProvider
    {
        public static readonly ExportProvider ExportProvider;
        public static readonly ComposableCatalog PartCatalog;

        static VisualStudioTestExportProvider()
        {
            PartCatalog =
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(
                    MinimalTestExportProvider.CreateAssemblyCatalog(typeof(CSharpCodeModelService).Assembly));

            ExportProvider = MinimalTestExportProvider.CreateExportProvider(PartCatalog);
        }
    }
}
