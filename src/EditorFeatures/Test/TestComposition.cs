// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    [Export]
    public class TestComposition
    {
        public readonly ComposablePartCatalog PartCatalog;

        public readonly ExportProvider ExportProvider;

        public readonly ComposablePartCatalog MinimumCatalog;

        public TestComposition()
        {
            PartCatalog = GetAssemblyCatalog(GetTypes());
            ExportProvider = new CompositionContainer(PartCatalog, isThreadSafe: true);
            MinimumCatalog = new AggregateCatalog(new TypeCatalog(GetRoslynTypes()), GetAssemblyCatalog(GetVisualStudioTypes()));
        }

        private static Type[] GetRoslynTypes()
        {
            var types = new[]
            {
                // ROSLYN

                typeof(Microsoft.CodeAnalysis.WorkspaceServices.WorkspaceServiceProviderFactory),
                typeof(Microsoft.CodeAnalysis.LanguageServices.LanguageServiceProviderFactoryWorkspaceServiceFactory),
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.Workspaces.WorkspaceTaskSchedulerFactoryFactory),
                typeof(Microsoft.CodeAnalysis.Host.WorkspaceTaskSchedulerFactoryFactory),
                typeof(Microsoft.CodeAnalysis.Host.BackgroundCompilerFactoryFactory),
                typeof(Microsoft.CodeAnalysis.Formatting.Rules.DefaultBaseIndentationFormattingRuleFactoryServiceFactory),
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.Workspaces.PersistenceServiceFactory),
                typeof(Microsoft.CodeAnalysis.Text.Implementation.TextBufferFactoryService.TextBufferCloneServiceFactory),
                typeof(Microsoft.CodeAnalysis.Host.PersistentStorageServiceFactory),
                typeof(Microsoft.CodeAnalysis.Host.AssemblyShadowCopyProviderServiceFactory),
                typeof(Microsoft.CodeAnalysis.Host.TemporaryStorageServiceFactory),
                typeof(Microsoft.CodeAnalysis.Host.TextFactoryServiceFactory),
                typeof(Microsoft.CodeAnalysis.Host.SyntaxTreeCacheServiceFactory),
                typeof(Microsoft.CodeAnalysis.Host.TextCacheServiceFactory),
                typeof(Microsoft.CodeAnalysis.Host.CompilationCacheServiceFactory),
                typeof(Microsoft.CodeAnalysis.Host.BackgroundParserFactoryFactory),
                typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilationFactoryService),
                typeof(Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationFactoryService),
                typeof(Solution), // ServicesCore
                typeof(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTreeFactoryServiceFactory), // CSharpServicesCore
                typeof(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTreeFactoryServiceFactory), // BasicServicesCore
                typeof(Microsoft.CodeAnalysis.CSharp.CodeActions.AbstractCSharpCodeIssueProvider),  // CSharpFeatures
                typeof(Microsoft.CodeAnalysis.VisualBasic.CodeActions.AbstractVisualBasicCodeIssueProvider), // BasicFeatures
                typeof(Microsoft.CodeAnalysis.Options.OptionService), // Service
                typeof(Microsoft.CodeAnalysis.Options.OptionsServiceFactory),
                typeof(Microsoft.CodeAnalysis.Options.Providers.ExportedOptionProvider),
                typeof(Microsoft.CodeAnalysis.Editor.CSharp.ContentType.ContentTypeDefinitions), // CSharp Content Type
                typeof(Microsoft.CodeAnalysis.Editor.VisualBasic.ContentType.ContentTypeDefinitions), // VB Content Type
                typeof(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxFactsService),
                typeof(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxFactsService),
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.Workspaces.EditorCompletionOptionsFactoryServiceFactory),
                typeof(Microsoft.CodeAnalysis.Editor.CSharp.Formatting.Indentation.SmartIndentProvider),
                typeof(Microsoft.CodeAnalysis.Editor.VisualBasic.Formatting.Indentation.SmartIndentProvider),
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.ForegroundNotification.ForegroundNotificationService),
                typeof(TestWaitIndicator),
                typeof(TestExtensionErrorHandler),
                typeof(IContentTypeAndTextViewRoleMetadata),
                typeof(TestExportProvider),
                typeof(Microsoft.CodeAnalysis.Diagnostics.CompilerDiagnosticService)
            };

            return types.Concat(TestHelpers.GetAllTypesImplementingGivenInterface(typeof(Microsoft.CodeAnalysis.CSharp.Formatting.DefaultOperationProvider).Assembly, typeof(IFormattingService)))
                        .Concat(TestHelpers.GetAllTypesImplementingGivenInterface(typeof(Microsoft.CodeAnalysis.VisualBasic.Formatting.DefaultOperationProvider).Assembly, typeof(IFormattingService)))
                        .Concat(TestHelpers.GetAllTypesImplementingGivenInterface(typeof(Microsoft.CodeAnalysis.CSharp.Formatting.DefaultOperationProvider).Assembly, typeof(IFormattingRule)))
                        .Concat(TestHelpers.GetAllTypesImplementingGivenInterface(typeof(Microsoft.CodeAnalysis.VisualBasic.Formatting.DefaultOperationProvider).Assembly, typeof(IFormattingRule)))
                        .Concat(TestHelpers.GetAllTypesImplementingGivenInterface(typeof(Microsoft.CodeAnalysis.CSharp.Formatting.DefaultOperationProvider).Assembly, typeof(ICodeGenerationService)))
                        .Concat(TestHelpers.GetAllTypesImplementingGivenInterface(typeof(Microsoft.CodeAnalysis.VisualBasic.Formatting.DefaultOperationProvider).Assembly, typeof(ICodeGenerationService)))
                        .Concat(TestHelpers.GetAllTypesWithStaticFieldsImplementingType(typeof(SolutionCrawlerOptions).Assembly, typeof(Microsoft.CodeAnalysis.Options.IOption)))
                        .Concat(TestHelpers.GetAllTypesWithStaticFieldsImplementingType(typeof(EditorComponentOnOffOptions).Assembly, typeof(Microsoft.CodeAnalysis.Options.IOption)))
                        .Concat(TestHelpers.GetAllTypesWithStaticFieldsImplementingType(typeof(ServiceComponentOnOffOptions).Assembly, typeof(Microsoft.CodeAnalysis.Options.IOption)))
                        .Concat(TestHelpers.GetAllTypesWithStaticFieldsImplementingType(typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions).Assembly, typeof(Microsoft.CodeAnalysis.Options.IOption)))
                        .Concat(TestHelpers.GetAllTypesWithStaticFieldsImplementingType(typeof(Microsoft.CodeAnalysis.Formatting.FormattingOptions).Assembly, typeof(Microsoft.CodeAnalysis.Options.IOption)))
                        .Distinct()
                        .ToArray();
        }

        private static Type[] GetVisualStudioTypes()
        {
            var types = new[]
            {
                // EDITOR

                // Microsoft.VisualStudio.Platform.VSEditor.dll:
                typeof(Microsoft.VisualStudio.Platform.VSEditor.EventArgsHelper),

                // Microsoft.VisualStudio.Text.Logic.dll:
                //   Must include this because several editor options are actually stored as exported information 
                //   on this DLL.  Including most importantly, the tab size information.
                typeof(Microsoft.VisualStudio.Text.Editor.DefaultOptions),

                // Microsoft.VisualStudio.Text.UI.dll:
                //   Include this DLL to get several more EditorOptions including WordWrapStyle.
                typeof(Microsoft.VisualStudio.Text.Editor.WordWrapStyle),

                // Microsoft.VisualStudio.Text.UI.Wpf.dll:
                //   Include this DLL to get more EditorOptions values.
                typeof(Microsoft.VisualStudio.Text.Editor.HighlightCurrentLineOption),

                // StandaloneUndo.dll:
                //   Include this DLL to get more undo operations.
                typeof(Microsoft.VisualStudio.Text.Operations.Standalone.NullMergeUndoTransactionPolicy),

                // Microsoft.VisualStudio.Language.StandardClassification.dll:
                typeof(Microsoft.VisualStudio.Language.StandardClassification.PredefinedClassificationTypeNames)
            };

            return types;
        }

        private static Type[] GetTypes()
        {
            return GetRoslynTypes().Concat(GetVisualStudioTypes()).ToArray();
        }

        private static ComposablePartCatalog GetAssemblyCatalog(IEnumerable<Type> types)
        {
            return new AggregateCatalog(types.Select(t => t.Assembly).Distinct().Select(a => new AssemblyCatalog(a)));
        }
    }
}
