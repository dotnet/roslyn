// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    /// <summary>
    /// This type caches MEF compositions for our unit tests.  MEF composition is a relatively expensive
    /// operation and caching yields demonstrable benefits for testing.
    /// 
    /// These caches must be done in a thread static manner.  Many of the stored values are non-frozen
    /// WPF elements which will throw if shared between threads.  It is legal for a given xUnit runner
    /// to execute classes on different threads hence we must handle this scenario.  
    /// </summary>
    public static class ServiceTestExportProvider
    {
        public static ComposableCatalog CreateAssemblyCatalog()
        {
            return MinimalTestExportProvider.CreateAssemblyCatalog(
                GetLanguageNeutralTypes().Select(t => t.Assembly).Distinct().Concat(MinimalTestExportProvider.GetVisualStudioAssemblies()), MinimalTestExportProvider.CreateResolver());
        }

        public static Type[] GetLanguageNeutralTypes()
        {
            var types = new[]
            {
                // ROSLYN
                typeof(Workspaces.NoCompilationLanguageServiceFactory),
                typeof(Workspaces.NoCompilationContentTypeDefinitions),
                typeof(Workspaces.NoCompilationContentTypeLanguageService),
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent.SmartIndentProvider),
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.ForegroundNotification.ForegroundNotificationService),
                typeof(IncrementalCaches.SymbolTreeInfoIncrementalAnalyzerProvider),
                typeof(CodeAnalysis.Diagnostics.EngineV2.InProcCodeAnalysisDiagnosticAnalyzerExecutor)
            };

            return MinimalTestExportProvider.GetLanguageNeutralTypes().Concat(types).Distinct().ToArray();
        }
    }
}
