// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;

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
    public static class TestExportProvider
    {
        [ThreadStatic]
        private static Lazy<ComposableCatalog> t_lazyEntireAssemblyCatalogWithCSharpAndVisualBasic;

        public static ComposableCatalog EntireAssemblyCatalogWithCSharpAndVisualBasic
        {
            get
            {
                if (t_lazyEntireAssemblyCatalogWithCSharpAndVisualBasic == null)
                {
                    t_lazyEntireAssemblyCatalogWithCSharpAndVisualBasic = new Lazy<ComposableCatalog>(() => CreateAssemblyCatalogWithCSharpAndVisualBasic());
                }

                return t_lazyEntireAssemblyCatalogWithCSharpAndVisualBasic.Value;
            }
        }

        [ThreadStatic]
        private static Lazy<ExportProvider> t_lazyExportProviderWithCSharpAndVisualBasic;

        public static ExportProvider ExportProviderWithCSharpAndVisualBasic
        {
            get
            {
                if (t_lazyExportProviderWithCSharpAndVisualBasic == null)
                {
                    t_lazyExportProviderWithCSharpAndVisualBasic = new Lazy<ExportProvider>(CreateExportProviderWithCSharpAndVisualBasic);
                }

                return t_lazyExportProviderWithCSharpAndVisualBasic.Value;
            }
        }

        [ThreadStatic]
        private static Lazy<ComposableCatalog> t_lazyMinimumCatalogWithCSharpAndVisualBasic;

        public static ComposableCatalog MinimumCatalogWithCSharpAndVisualBasic
        {
            get
            {
                if (t_lazyMinimumCatalogWithCSharpAndVisualBasic == null)
                {
                    t_lazyMinimumCatalogWithCSharpAndVisualBasic = new Lazy<ComposableCatalog>(() => MinimalTestExportProvider.CreateTypeCatalog(GetNeutralAndCSharpAndVisualBasicTypes())
                        .WithParts(MinimalTestExportProvider.CreateAssemblyCatalog(MinimalTestExportProvider.GetVisualStudioAssemblies())));
                }

                return t_lazyMinimumCatalogWithCSharpAndVisualBasic.Value;
            }
        }

        private static Type[] GetNeutralAndCSharpAndVisualBasicTypes()
        {
            var types = new[]
            {
                // ROSLYN
                typeof(Workspaces.NoCompilationLanguageServiceFactory),
                typeof(Workspaces.NoCompilationContentTypeDefinitions),
                typeof(Workspaces.NoCompilationContentTypeLanguageService),
                typeof(Microsoft.CodeAnalysis.CSharp.IntroduceVariable.CSharpIntroduceVariableService), // Ensures that CSharpFeatures is included in the composition
                typeof(Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable.VisualBasicIntroduceVariableService), // Ensures that BasicFeatures is included in the composition
                typeof(Microsoft.CodeAnalysis.Editor.CSharp.ContentType.ContentTypeDefinitions), // CSharp Content Type
                typeof(Microsoft.CodeAnalysis.Editor.VisualBasic.ContentType.ContentTypeDefinitions), // VB Content Type
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent.SmartIndentProvider),
                typeof(Microsoft.CodeAnalysis.Editor.VisualBasic.Formatting.Indentation.VisualBasicIndentationService),
                typeof(Microsoft.CodeAnalysis.Editor.CSharp.Formatting.Indentation.CSharpIndentationService),
                typeof(Microsoft.CodeAnalysis.Editor.Implementation.ForegroundNotification.ForegroundNotificationService),
                typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilationFactoryService),
                typeof(Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationFactoryService),
                typeof(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTreeFactoryServiceFactory), // CSharpServicesCore
                typeof(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTreeFactoryServiceFactory), // BasicServicesCore
                typeof(CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationServiceFactory),
                typeof(CodeAnalysis.VisualBasic.CodeGeneration.VisualBasicCodeGenerationServiceFactory),
                typeof(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxFactsService),
                typeof(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxFactsService),
                typeof(CodeAnalysis.CSharp.CSharpSymbolDeclarationService),
                typeof(CodeAnalysis.VisualBasic.VisualBasicSymbolDeclarationService),
                typeof(CodeAnalysis.Editor.CSharp.LanguageServices.CSharpSymbolDisplayServiceFactory),
                typeof(Microsoft.CodeAnalysis.Editor.CSharp.Interactive.CSharpInteractiveEvaluator),
                typeof(CodeAnalysis.Editor.VisualBasic.LanguageServices.VisualBasicSymbolDisplayServiceFactory),
                typeof(Microsoft.CodeAnalysis.Editor.VisualBasic.Interactive.VisualBasicInteractiveEvaluator),
                typeof(CodeAnalysis.CSharp.Simplification.CSharpSimplificationService),
                typeof(CodeAnalysis.VisualBasic.Simplification.VisualBasicSimplificationService),
                typeof(CodeAnalysis.CSharp.Rename.CSharpRenameConflictLanguageService),
                typeof(CodeAnalysis.VisualBasic.Rename.VisualBasicRenameRewriterLanguageServiceFactory),
                typeof(CodeAnalysis.CSharp.CSharpSemanticFactsService),
                typeof(CodeAnalysis.VisualBasic.VisualBasicSemanticFactsService),
                typeof(CodeAnalysis.CSharp.CodeGeneration.CSharpSyntaxGenerator),
                typeof(CodeAnalysis.VisualBasic.CodeGeneration.VisualBasicSyntaxGenerator),
                typeof(CSharp.LanguageServices.CSharpContentTypeLanguageService),
                typeof(VisualBasic.LanguageServices.VisualBasicContentTypeLanguageService),
                typeof(IncrementalCaches.SymbolTreeInfoIncrementalAnalyzerProvider),
            };

            return MinimalTestExportProvider.GetLanguageNeutralTypes()
                .Concat(types)
                .Concat(TestHelpers.GetAllTypesImplementingGivenInterface(typeof(Microsoft.CodeAnalysis.CSharp.Formatting.DefaultOperationProvider).Assembly, typeof(ISyntaxFormattingService)))
                .Concat(TestHelpers.GetAllTypesImplementingGivenInterface(typeof(Microsoft.CodeAnalysis.VisualBasic.Formatting.DefaultOperationProvider).Assembly, typeof(ISyntaxFormattingService)))
                .Concat(TestHelpers.GetAllTypesImplementingGivenInterface(typeof(Microsoft.CodeAnalysis.CSharp.Formatting.DefaultOperationProvider).Assembly, typeof(IFormattingRule)))
                .Concat(TestHelpers.GetAllTypesImplementingGivenInterface(typeof(Microsoft.CodeAnalysis.VisualBasic.Formatting.DefaultOperationProvider).Assembly, typeof(IFormattingRule)))
                .Concat(TestHelpers.GetAllTypesImplementingGivenInterface(typeof(Microsoft.CodeAnalysis.CSharp.Formatting.DefaultOperationProvider).Assembly, typeof(ICodeGenerationService)))
                .Concat(TestHelpers.GetAllTypesImplementingGivenInterface(typeof(Microsoft.CodeAnalysis.VisualBasic.Formatting.DefaultOperationProvider).Assembly, typeof(ICodeGenerationService)))
                .Concat(TestHelpers.GetAllTypesWithStaticFieldsImplementingType(typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions).Assembly, typeof(Microsoft.CodeAnalysis.Options.IOption)))
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Create fresh ExportProvider that doesnt share anything with others. 
        /// test can use this export provider to create all new MEF components not shared with others.
        /// </summary>
        public static ExportProvider CreateExportProviderWithCSharpAndVisualBasic()
        {
            return MinimalTestExportProvider.CreateExportProvider(CreateAssemblyCatalogWithCSharpAndVisualBasic());
        }

        /// <summary>
        /// Create fresh ComposableCatalog that doesnt share anything with others.
        /// everything under this catalog should have been created from scratch that doesnt share anything with others.
        /// </summary>
        public static ComposableCatalog CreateAssemblyCatalogWithCSharpAndVisualBasic()
        {
            return MinimalTestExportProvider.CreateAssemblyCatalog(
                GetNeutralAndCSharpAndVisualBasicTypes().Select(t => t.Assembly).Distinct().Concat(MinimalTestExportProvider.GetVisualStudioAssemblies()),
                MinimalTestExportProvider.CreateResolver());
        }
    }
}
