// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
{
    public static class EditorServicesUtil
    {
        private static readonly Lazy<IExportProviderFactory> s_exportProviderFactory = new Lazy<IExportProviderFactory>(CreateExportProviderFactory);

        public static ExportProvider ExportProvider => s_exportProviderFactory.Value.CreateExportProvider();

        private static IExportProviderFactory CreateExportProviderFactory()
        {
            var catalog = TestExportProvider.GetCSharpAndVisualBasicAssemblyCatalog()
                .WithParts(ExportProviderCache.GetOrCreateAssemblyCatalog(new[] { typeof(EditorServicesUtil).Assembly }, ExportProviderCache.CreateResolver()));
            return ExportProviderCache.GetOrCreateExportProviderFactory(catalog);
        }
    }
}
