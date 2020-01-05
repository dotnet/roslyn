// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
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
            return ExportProviderCache.GetOrCreateAssemblyCatalog(GetLanguageNeutralTypes().Select(t => t.Assembly).Distinct(), ExportProviderCache.CreateResolver())
                .WithParts(MinimalTestExportProvider.GetEditorAssemblyCatalog());
        }

        public static Type[] GetLanguageNeutralTypes()
        {
            var types = new[]
            {
                // ROSLYN
                typeof(CodeAnalysis.UnitTests.NoCompilationLanguageServiceFactory),
                typeof(Workspaces.NoCompilationContentTypeDefinitions),
                typeof(Workspaces.NoCompilationContentTypeLanguageService),
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent.SmartIndentProvider),
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.ForegroundNotification.ForegroundNotificationService),
                typeof(Implementation.InlineRename.InlineRenameService), // Ensure that EditorFeatures.Wpf is included in the composition
                typeof(IncrementalCaches.SymbolTreeInfoIncrementalAnalyzerProvider)
            };

            return MinimalTestExportProvider.GetLanguageNeutralTypes().Concat(types).Distinct().ToArray();
        }
    }
}
