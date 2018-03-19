// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    public static class MinimalTestExportProvider
    {
        private static readonly PartDiscovery s_partDiscovery = ExportProviderCache.CreatePartDiscovery(Resolver.DefaultInstance);

        public static Type[] GetLanguageNeutralTypes()
        {
            var types = new[]
            {
                // ROSLYN
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.Workspaces.EditorTaskSchedulerFactory),
                typeof(Microsoft.CodeAnalysis.Host.WorkspaceTaskSchedulerFactory),
                typeof(Microsoft.CodeAnalysis.Formatting.Rules.DefaultFormattingRuleFactoryServiceFactory),
                typeof(Microsoft.CodeAnalysis.Host.PersistentStorageServiceFactory),
                typeof(Microsoft.CodeAnalysis.Text.Implementation.TextBufferFactoryService.TextBufferCloneServiceFactory),
                typeof(Microsoft.CodeAnalysis.Host.MetadataServiceFactory),
                typeof(Microsoft.CodeAnalysis.Host.TemporaryStorageServiceFactory),
                typeof(Microsoft.CodeAnalysis.Host.TextFactoryService),
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.Workspaces.ProjectCacheHostServiceFactory),
                typeof(Solution), // ServicesCore
                typeof(Microsoft.CodeAnalysis.Options.GlobalOptionService),
                typeof(Microsoft.CodeAnalysis.Options.OptionServiceFactory),
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent.SmartIndentProvider),
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.ForegroundNotification.ForegroundNotificationService),
                typeof(Microsoft.CodeAnalysis.Editor.UnitTests.TestOptionsServiceFactory),
                typeof(Implementation.Classification.ClassificationTypeFormatDefinitions), // to include EditorFeatures.Wpf
                typeof(DefaultSymbolMappingService),
                typeof(TestWaitIndicator),
                typeof(TestExtensionErrorHandler),
                typeof(TestExportJoinableTaskContext), // Needed by editor components, but not actually exported anywhere else
                typeof(TestObscuringTipManager) // Needed by editor components, but only exported in editor VS layer. Tracked by https://devdiv.visualstudio.com/DevDiv/_workitems?id=544569.
            };

            return types//.Concat(TestHelpers.GetAllTypesWithStaticFieldsImplementingType(typeof(InternalSolutionCrawlerOptions).Assembly, typeof(Microsoft.CodeAnalysis.Options.IOption)))
                        //.Concat(TestHelpers.GetAllTypesWithStaticFieldsImplementingType(typeof(EditorComponentOnOffOptions).Assembly, typeof(Microsoft.CodeAnalysis.Options.IOption)))
                        //.Concat(TestHelpers.GetAllTypesWithStaticFieldsImplementingType(typeof(ServiceComponentOnOffOptions).Assembly, typeof(Microsoft.CodeAnalysis.Options.IOption)))
                        //.Concat(TestHelpers.GetAllTypesWithStaticFieldsImplementingType(typeof(Microsoft.CodeAnalysis.Formatting.FormattingOptions).Assembly, typeof(Microsoft.CodeAnalysis.Options.IOption)))
                        .Distinct()
                        .ToArray();
        }

        public static IEnumerable<Assembly> GetEditorAssemblies()
        {
            var assemblies = new[]
            {
                // EDITOR

                // Microsoft.VisualStudio.Platform.VSEditor.dll:
                Assembly.LoadFrom("Microsoft.VisualStudio.Platform.VSEditor.dll"),

                // Microsoft.VisualStudio.Text.Logic.dll:
                //   Must include this because several editor options are actually stored as exported information 
                //   on this DLL.  Including most importantly, the tab size information.
                typeof(Microsoft.VisualStudio.Text.Editor.DefaultOptions).Assembly,

                // Microsoft.VisualStudio.Text.UI.dll:
                //   Include this DLL to get several more EditorOptions including WordWrapStyle.
                typeof(Microsoft.VisualStudio.Text.Editor.WordWrapStyle).Assembly,

                // Microsoft.VisualStudio.Text.UI.Wpf.dll:
                //   Include this DLL to get more EditorOptions values.
                typeof(Microsoft.VisualStudio.Text.Editor.HighlightCurrentLineOption).Assembly,

                // BasicUndo.dll:
                //   Include this DLL to satisfy ITextUndoHistoryRegistry
                typeof(BasicUndo.IBasicUndoHistory).Assembly,

                // Microsoft.VisualStudio.Language.StandardClassification.dll:
                typeof(Microsoft.VisualStudio.Language.StandardClassification.PredefinedClassificationTypeNames).Assembly
            };

            return assemblies;
        }

        public static Resolver CreateResolver()
        {
            // simple assembly loader is stateless, so okay to share
            return new Resolver(SimpleAssemblyLoader.Instance);
        }

        public static ExportProvider CreateExportProvider(ComposableCatalog catalog)
        {
            // make sure we enable this for all unit tests
            AsynchronousOperationListenerProvider.Enable(true);

            var configuration = CompositionConfiguration.Create(catalog.WithCompositionService());
            var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
            return runtimeComposition.CreateExportProviderFactory().CreateExportProvider();
        }

        private class SimpleAssemblyLoader : IAssemblyLoader
        {
            public static readonly IAssemblyLoader Instance = new SimpleAssemblyLoader();

            public Assembly LoadAssembly(AssemblyName assemblyName)
            {
                return Assembly.Load(assemblyName);
            }

            public Assembly LoadAssembly(string assemblyFullName, string codeBasePath)
            {
                var assemblyName = new AssemblyName(assemblyFullName);
                if (!string.IsNullOrEmpty(codeBasePath))
                {
                    assemblyName.CodeBase = codeBasePath;
                }

                return this.LoadAssembly(assemblyName);
            }
        }
    }
}
