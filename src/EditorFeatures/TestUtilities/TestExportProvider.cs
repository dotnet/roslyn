// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    /// <summary>
    /// This type caches MEF compositions for our unit tests.  MEF composition is a relatively expensive
    /// operation and caching yields demonstrable benefits for testing.
    /// </summary>
    public static class TestExportProvider
    {
        private static Lazy<ComposableCatalog> s_lazyEntireAssemblyCatalogWithCSharpAndVisualBasic =
            new Lazy<ComposableCatalog>(() => CreateAssemblyCatalogWithCSharpAndVisualBasic());

        public static ComposableCatalog EntireAssemblyCatalogWithCSharpAndVisualBasic
            => s_lazyEntireAssemblyCatalogWithCSharpAndVisualBasic.Value;

        private static Lazy<ExportProvider> s_lazyExportProviderWithCSharpAndVisualBasic
            = new Lazy<ExportProvider>(CreateExportProviderWithCSharpAndVisualBasic);

        public static ExportProvider ExportProviderWithCSharpAndVisualBasic
            => s_lazyExportProviderWithCSharpAndVisualBasic.Value;

        private static Lazy<ComposableCatalog> s_lazyMinimumCatalogWithCSharpAndVisualBasic =
            new Lazy<ComposableCatalog>(() => MinimalTestExportProvider.CreateTypeCatalog(GetNeutralAndCSharpAndVisualBasicTypes())
                        .WithParts(MinimalTestExportProvider.CreateAssemblyCatalog(MinimalTestExportProvider.GetVisualStudioAssemblies())));

        public static ComposableCatalog MinimumCatalogWithCSharpAndVisualBasic
            => s_lazyMinimumCatalogWithCSharpAndVisualBasic.Value;

        private static Type[] GetNeutralAndCSharpAndVisualBasicTypes()
        {
            var types = new[]
            {
                // ROSLYN
                typeof(CodeAnalysis.CSharp.IntroduceVariable.CSharpIntroduceVariableService), // Ensures that CSharpFeatures is included in the composition
                typeof(CodeAnalysis.VisualBasic.IntroduceVariable.VisualBasicIntroduceVariableService), // Ensures that BasicFeatures is included in the composition
                typeof(CSharp.ContentType.ContentTypeDefinitions), // CSharp Content Type
                typeof(VisualBasic.ContentType.ContentTypeDefinitions), // VB Content Type
                typeof(VisualBasic.Formatting.Indentation.VisualBasicIndentationService),
                typeof(CSharp.Formatting.Indentation.CSharpIndentationService),
                typeof(CodeAnalysis.CSharp.CSharpCompilationFactoryService),
                typeof(CodeAnalysis.VisualBasic.VisualBasicCompilationFactoryService),
                typeof(CodeAnalysis.CSharp.CSharpSyntaxTreeFactoryServiceFactory), // CSharpServicesCore
                typeof(CodeAnalysis.VisualBasic.VisualBasicSyntaxTreeFactoryServiceFactory), // BasicServicesCore
                typeof(CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationServiceFactory),
                typeof(CodeAnalysis.VisualBasic.CodeGeneration.VisualBasicCodeGenerationServiceFactory),
                typeof(CodeAnalysis.CSharp.CSharpSyntaxFactsServiceFactory),
                typeof(CodeAnalysis.VisualBasic.VisualBasicSyntaxFactsServiceFactory),
                typeof(CodeAnalysis.CSharp.FindSymbols.CSharpDeclaredSymbolInfoFactoryService),
                typeof(CodeAnalysis.VisualBasic.FindSymbols.VisualBasicDeclaredSymbolInfoFactoryService),
                typeof(CodeAnalysis.CSharp.CSharpSymbolDeclarationService),
                typeof(CodeAnalysis.VisualBasic.VisualBasicSymbolDeclarationService),
                typeof(CodeAnalysis.CSharp.Formatting.CSharpFormattingService),
                typeof(CodeAnalysis.VisualBasic.Formatting.VisualBasicFormattingService),
                typeof(CSharp.LanguageServices.CSharpSymbolDisplayServiceFactory),
                typeof(CSharp.Interactive.CSharpInteractiveEvaluator),
                typeof(VisualBasic.LanguageServices.VisualBasicSymbolDisplayServiceFactory),
                typeof(VisualBasic.Interactive.VisualBasicInteractiveEvaluator),
                typeof(CodeAnalysis.CSharp.Simplification.CSharpSimplificationService),
                typeof(CodeAnalysis.VisualBasic.Simplification.VisualBasicSimplificationService),
                typeof(CodeAnalysis.CSharp.Rename.CSharpRenameConflictLanguageService),
                typeof(CodeAnalysis.VisualBasic.Rename.VisualBasicRenameRewriterLanguageServiceFactory),
                typeof(CodeAnalysis.CSharp.CSharpSemanticFactsServiceFactory),
                typeof(CodeAnalysis.VisualBasic.VisualBasicSemanticFactsServiceFactory),
                typeof(CodeAnalysis.CSharp.CodeGeneration.CSharpSyntaxGenerator),
                typeof(CodeAnalysis.VisualBasic.CodeGeneration.VisualBasicSyntaxGenerator),
                typeof(CSharp.LanguageServices.CSharpContentTypeLanguageService),
                typeof(VisualBasic.LanguageServices.VisualBasicContentTypeLanguageService),
                typeof(CodeAnalysis.CSharp.Execution.CSharpOptionsSerializationService),
                typeof(CodeAnalysis.VisualBasic.Execution.VisualBasicOptionsSerializationService),
                typeof(CodeAnalysis.Execution.DesktopReferenceSerializationServiceFactory),
                typeof(TestExportProvider)
            };

            return ServiceTestExportProvider.GetLanguageNeutralTypes()
                .Concat(types)
                .Concat(DesktopTestHelpers.GetAllTypesImplementingGivenInterface(
                    typeof(CodeAnalysis.CSharp.Formatting.DefaultOperationProvider).Assembly, typeof(ISyntaxFormattingService)))
                .Concat(DesktopTestHelpers.GetAllTypesImplementingGivenInterface(
                    typeof(CodeAnalysis.VisualBasic.Formatting.DefaultOperationProvider).Assembly, typeof(ISyntaxFormattingService)))
                .Concat(DesktopTestHelpers.GetAllTypesImplementingGivenInterface(
                    typeof(CodeAnalysis.CSharp.Formatting.DefaultOperationProvider).Assembly, typeof(IFormattingRule)))
                .Concat(DesktopTestHelpers.GetAllTypesImplementingGivenInterface(
                    typeof(CodeAnalysis.VisualBasic.Formatting.DefaultOperationProvider).Assembly, typeof(IFormattingRule)))
                .Concat(DesktopTestHelpers.GetAllTypesImplementingGivenInterface(
                    typeof(CodeAnalysis.CSharp.Formatting.DefaultOperationProvider).Assembly, typeof(ICodeGenerationService)))
                .Concat(DesktopTestHelpers.GetAllTypesImplementingGivenInterface(
                    typeof(CodeAnalysis.VisualBasic.Formatting.DefaultOperationProvider).Assembly, typeof(ICodeGenerationService)))
                .Concat(TestHelpers.GetAllTypesWithStaticFieldsImplementingType(typeof(CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions).Assembly, typeof(CodeAnalysis.Options.IOption)))
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Create fresh ExportProvider that doesn't share anything with others. Tests can use this
        /// export provider to create all new MEF components not shared with others.
        /// </summary>
        public static ExportProvider CreateExportProviderWithCSharpAndVisualBasic()
        {
            return MinimalTestExportProvider.CreateExportProvider(CreateAssemblyCatalogWithCSharpAndVisualBasic());
        }

        /// <summary>
        /// Create fresh ComposableCatalog that doest share anything with others. Everything under
        /// this catalog should have been created from scratch that doesn't share anything with 
        /// others.
        /// </summary>
        public static ComposableCatalog CreateAssemblyCatalogWithCSharpAndVisualBasic()
        {
            return MinimalTestExportProvider.CreateAssemblyCatalog(
                GetCSharpAndVisualBasicAssemblies(),
                MinimalTestExportProvider.CreateResolver());
        }

        public static IEnumerable<Assembly> GetCSharpAndVisualBasicAssemblies()
        {
            return GetNeutralAndCSharpAndVisualBasicTypes().Select(t => t.Assembly).Distinct().Concat(MinimalTestExportProvider.GetVisualStudioAssemblies());
        }
    }
}
