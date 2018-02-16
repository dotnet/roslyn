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
        private static Lazy<ComposableCatalog> s_composableCatalog = new Lazy<ComposableCatalog>(CreateComposableCatalog);

        public static ExportProvider ExportProvider => ExportProviderCache.CreateExportProvider(s_composableCatalog.Value);

        private static ComposableCatalog CreateComposableCatalog()
        {
            var assemblies = TestExportProvider
                .GetCSharpAndVisualBasicAssemblies()
                .Concat(new[] { typeof(EditorServicesUtil).Assembly });
            return ExportProviderCache.CreateAssemblyCatalog(assemblies, ExportProviderCache.CreateResolver());
        }
    }
}
