// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Composition;
using System;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
{
    public static class EditorServicesUtil
    {
        private static Lazy<ExportProvider> s_lazyExportProvider = new Lazy<ExportProvider>(CreateExportProvider);

        public static ExportProvider ExportProvider => s_lazyExportProvider.Value;

        public static ExportProvider CreateExportProvider()
        {
            var assemblies = TestExportProvider
                .GetCSharpAndVisualBasicAssemblies()
                .Concat(new[] { typeof(EditorServicesUtil).Assembly });
            var catalog = MinimalTestExportProvider.CreateAssemblyCatalog(assemblies, MinimalTestExportProvider.CreateResolver());
            return MinimalTestExportProvider.CreateExportProvider(catalog);
        }
    }
}
