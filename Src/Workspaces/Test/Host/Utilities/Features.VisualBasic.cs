// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CaseCorrection;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.CaseCorrection;
using Microsoft.CodeAnalysis.VisualBasic.CodeCleanup;
using Microsoft.CodeAnalysis.VisualBasic.CodeGeneration;
using Microsoft.CodeAnalysis.VisualBasic.Formatting;
using Microsoft.CodeAnalysis.VisualBasic.Recommendations;
using Microsoft.CodeAnalysis.VisualBasic.Rename;
using Microsoft.CodeAnalysis.VisualBasic.Simplification;

namespace Microsoft.CodeAnalysis.Host.UnitTests
{
    internal static partial class Features
    {
        public static class VisualBasic
        {
            /// <summary>
            ///  All LanguageServices and LanguageServiceProviders in VB.
            /// </summary>
            public static ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>> Services
            {
                get
                {
                    return visualBasicServices.Value;
                }
            }

            /// <summary>
            /// All ILanguageService implementations in VB.
            /// </summary>
            public static ImmutableList<Lazy<ILanguageService, LanguageServiceMetadata>> LanguageServices
            {
                get
                {
                    return visualBasicLanguageServices.Value;
                }
            }

            /// <summary>
            /// All ILanguageServiceFactory implementations in VB.
            /// </summary>
            public static ImmutableList<Lazy<ILanguageServiceFactory, LanguageServiceMetadata>> LanguageServiceFactories
            {
                get
                {
                    return visualBasicLanguageServiceFactories.Value;
                }
            }

            /// <summary>
            /// All IFormattingRule implementations in VB.
            /// </summary>
            public static ImmutableList<Lazy<IFormattingRule, OrderableLanguageMetadata>> FormattingRules
            {
                get
                {
                    return visualbasicRules.Value;
                }
            }

            /// <summary>
            /// All ICodeCleanupProvider implementations in VB.
            /// </summary>
            public static ImmutableList<Lazy<ICodeCleanupProvider, OrderableLanguageMetadata>> CodeCleanupProviders
            {
                get
                {
                    return visualbasicCodeCleanupProviders.Value;
                }
            }

            private static Lazy<ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>>> visualBasicServices =
                new Lazy<ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>>>(
                    () => LanguageServices.Select(ls =>
                        new KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>(ls.Metadata, (lsp) => ls.Value))
                            .Concat(LanguageServiceFactories.Select(lsf =>
                                new KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>(lsf.Metadata, (lsp) =>
                                    lsf.Value.CreateLanguageService(lsp)))).ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<ILanguageService, LanguageServiceMetadata>>> visualBasicLanguageServices =
                new Lazy<ImmutableList<Lazy<ILanguageService, LanguageServiceMetadata>>>(() => EnumerateVisualBasicLanguageServices().ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<ILanguageServiceFactory, LanguageServiceMetadata>>> visualBasicLanguageServiceFactories =
                new Lazy<ImmutableList<Lazy<ILanguageServiceFactory, LanguageServiceMetadata>>>(() => EnumerateVisualBasicLanguageServiceFactories().ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<IFormattingRule, OrderableLanguageMetadata>>> visualbasicRules =
                new Lazy<ImmutableList<Lazy<IFormattingRule, OrderableLanguageMetadata>>>(() => EnumerateVisualBasicFormattingRules().ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<ICodeCleanupProvider, OrderableLanguageMetadata>>> visualbasicCodeCleanupProviders =
                new Lazy<ImmutableList<Lazy<ICodeCleanupProvider, OrderableLanguageMetadata>>>(
                    () => EnumerateVisualBasicCodeCleanupProvider().ToImmutableList(), true);

            private static IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> EnumerateVisualBasicLanguageServices()
            {
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new VisualBasicSyntaxFactory(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ISyntaxFactoryService), WorkspaceKind.Any), true);

                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new VisualBasicSyntaxFormattingService(Features.VisualBasic.FormattingRules),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ISyntaxFormattingService), WorkspaceKind.Any), true);

                // Recommendation service
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new VisualBasicRecommendationService(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(IRecommendationService), WorkspaceKind.Any), true);

                // Command Line Arguments
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new VisualBasicCommandLineArgumentsFactoryService(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ICommandLineArgumentsFactoryService), WorkspaceKind.Any), true);

                // Compilation Factory
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new VisualBasicCompilationFactoryService(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ICompilationFactoryService), WorkspaceKind.Any), true);

                // Project File Loader
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new VisualBasicProjectFileLoader(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(IProjectFileLoader), WorkspaceKind.Any), true);

                // Semantic Facts
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new VisualBasicSemanticFactsService(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ISemanticFactsService), WorkspaceKind.Any), true);

                // Symbol Declaration
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new VisualBasicSymbolDeclarationService(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ISymbolDeclarationService), WorkspaceKind.Any), true);

                // Syntax Facts
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new VisualBasicSyntaxFactsService(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ISyntaxFactsService), WorkspaceKind.Any), true);

                // Syntax Version
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new VisualBasicSyntaxVersionLanguageService(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ISyntaxVersionLanguageService), WorkspaceKind.Any), true);

                // Type Inference
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new VisualBasicTypeInferenceService(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ITypeInferenceService), WorkspaceKind.Any), true);

                // Simplification
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new VisualBasicSimplificationService(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ISimplificationService), WorkspaceKind.Any), true);
            }

            private static IEnumerable<Lazy<ILanguageServiceFactory, LanguageServiceMetadata>> EnumerateVisualBasicLanguageServiceFactories()
            {
                yield return new Lazy<ILanguageServiceFactory, LanguageServiceMetadata>(
                    () => new VisualBasicCaseCorrectionServiceFactory(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ICaseCorrectionService), WorkspaceKind.Any), true);

                yield return new Lazy<ILanguageServiceFactory, LanguageServiceMetadata>(
                    () => new VisualBasicCodeCleanerServiceFactory(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ICodeCleanerService), WorkspaceKind.Any), true);

                yield return new Lazy<ILanguageServiceFactory, LanguageServiceMetadata>(
                    () => new VisualBasicCodeGenerationServiceFactory(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ICodeGenerationService), WorkspaceKind.Any), true);

                yield return new Lazy<ILanguageServiceFactory, LanguageServiceMetadata>(
                    () => new VisualBasicRenameRewriterLanguageServiceFactory(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(IRenameRewriterLanguageService), WorkspaceKind.Any), true);

                yield return new Lazy<ILanguageServiceFactory, LanguageServiceMetadata>(
                    () => new VisualBasicSyntaxTreeFactoryServiceFactory(),
                    new LanguageServiceMetadata(LanguageNames.VisualBasic, typeof(ISyntaxTreeFactoryService), WorkspaceKind.Any), true);
            }

            private static IEnumerable<Lazy<IFormattingRule, OrderableLanguageMetadata>> EnumerateVisualBasicFormattingRules()
            {
                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new AdjustSpaceFormattingRule(),
                    new OrderableLanguageMetadata(AdjustSpaceFormattingRule.Name, LanguageNames.VisualBasic,
                                              after: new[] { ElasticTriviaFormattingRule.Name }), true);

                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new AlignTokensFormattingRule(),
                    new OrderableLanguageMetadata(AlignTokensFormattingRule.Name, LanguageNames.VisualBasic,
                                              after: new[] { AdjustSpaceFormattingRule.Name }), true);

                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new ElasticTriviaFormattingRule(),
                    new OrderableLanguageMetadata(ElasticTriviaFormattingRule.Name, LanguageNames.VisualBasic,
                                              after: new[] { StructuredTriviaFormattingRule.Name }), true);

                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new NodeBasedFormattingRule(),
                    new OrderableLanguageMetadata(NodeBasedFormattingRule.Name, LanguageNames.VisualBasic,
                                              after: new[] { AlignTokensFormattingRule.Name }), true);

                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new StructuredTriviaFormattingRule(),
                    new OrderableLanguageMetadata(StructuredTriviaFormattingRule.Name, LanguageNames.VisualBasic), true);
            }

            private static IEnumerable<Lazy<ICodeCleanupProvider, OrderableLanguageMetadata>> EnumerateVisualBasicCodeCleanupProvider()
            {
                yield return new Lazy<ICodeCleanupProvider, OrderableLanguageMetadata>(
                    () => new AddMissingTokensCodeCleanupProvider(),
                    new OrderableLanguageMetadata(PredefinedCodeCleanupProviderNames.AddMissingTokens, LanguageNames.VisualBasic,
                                              before: new[] { PredefinedCodeCleanupProviderNames.Simplification }), true);

                yield return new Lazy<ICodeCleanupProvider, OrderableLanguageMetadata>(
                     () => new CaseCorrectionCodeCleanupProvider(),
                    new OrderableLanguageMetadata(PredefinedCodeCleanupProviderNames.CaseCorrection, LanguageNames.VisualBasic,
                                              before: new[] { PredefinedCodeCleanupProviderNames.Simplification }), true);

                yield return new Lazy<ICodeCleanupProvider, OrderableLanguageMetadata>(
                     () => new FixIncorrectTokensCodeCleanupProvider(),
                    new OrderableLanguageMetadata(PredefinedCodeCleanupProviderNames.FixIncorrectTokens, LanguageNames.VisualBasic,
                                              after: new[] { PredefinedCodeCleanupProviderNames.AddMissingTokens },
                                              before: new[] { PredefinedCodeCleanupProviderNames.Simplification }), true);

                yield return new Lazy<ICodeCleanupProvider, OrderableLanguageMetadata>(
                     () => new NormalizeModifiersOrOperatorsCodeCleanupProvider(),
                    new OrderableLanguageMetadata(PredefinedCodeCleanupProviderNames.NormalizeModifiersOrOperators, LanguageNames.VisualBasic,
                                              after: new[] { PredefinedCodeCleanupProviderNames.AddMissingTokens },
                                              before: new[] { PredefinedCodeCleanupProviderNames.Simplification }), true);

                yield return new Lazy<ICodeCleanupProvider, OrderableLanguageMetadata>(
                     () => new ReduceTokensCodeCleanupProvider(),
                    new OrderableLanguageMetadata(PredefinedCodeCleanupProviderNames.ReduceTokens, LanguageNames.VisualBasic,
                                              after: new[] { PredefinedCodeCleanupProviderNames.AddMissingTokens },
                                              before: new[] { PredefinedCodeCleanupProviderNames.Simplification }), true);

                yield return new Lazy<ICodeCleanupProvider, OrderableLanguageMetadata>(
                     () => new RemoveUnnecessaryLineContinuationCodeCleanupProvider(),
                    new OrderableLanguageMetadata(PredefinedCodeCleanupProviderNames.RemoveUnnecessaryLineContinuation, LanguageNames.VisualBasic,
                                              after: new[] { PredefinedCodeCleanupProviderNames.NormalizeModifiersOrOperators },
                                              before: new[] { PredefinedCodeCleanupProviderNames.Simplification }), true);

                yield return new Lazy<ICodeCleanupProvider, OrderableLanguageMetadata>(
                     () => new SimplificationCodeCleanupProvider(),
                    new OrderableLanguageMetadata(PredefinedCodeCleanupProviderNames.Simplification, LanguageNames.VisualBasic,
                                              before: new[] { PredefinedCodeCleanupProviderNames.Format }), true);

                yield return new Lazy<ICodeCleanupProvider, OrderableLanguageMetadata>(
                     () => new FormatCodeCleanupProvider(),
                    new OrderableLanguageMetadata(PredefinedCodeCleanupProviderNames.Format, LanguageNames.VisualBasic,
                                              after: new[] { PredefinedCodeCleanupProviderNames.Simplification }), true);
            }
        }
    }
}
