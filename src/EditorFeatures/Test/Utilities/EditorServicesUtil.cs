// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
{
    public static class EditorServicesUtil
    {
        private static Lazy<IExportProviderFactory> s_exportProviderFactory = new Lazy<IExportProviderFactory>(CreateExportProviderFactory);

        public static ExportProvider ExportProvider => s_exportProviderFactory.Value.CreateExportProvider();

        private static IExportProviderFactory CreateExportProviderFactory()
        {
            var assemblies = TestExportProvider
                .GetCSharpAndVisualBasicAssemblies()
                .Concat(new[] { typeof(EditorServicesUtil).Assembly });
            return ExportProviderCache.GetOrCreateExportProviderFactory(ExportProviderCache.GetOrCreateAssemblyCatalog(assemblies, ExportProviderCache.CreateResolver()));
        }
    }
}
