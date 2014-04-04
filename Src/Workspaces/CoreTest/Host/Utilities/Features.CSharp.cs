// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CaseCorrection;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CaseCorrection;
using Microsoft.CodeAnalysis.CSharp.CodeCleanup;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Recommendations;
using Microsoft.CodeAnalysis.CSharp.Rename;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Host.UnitTests
{
    internal static partial class Features
    {
        public static class CSharp
        {
            /// <summary>
            ///  All LanguageServices and LanguageServiceProviders in C#.
            /// </summary>
            public static ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>> Services
            {
                get
                {
                    return csharpServices.Value;
                }
            }

            /// <summary>
            /// All ILanguageService implementation in C#.
            /// </summary>
            public static ImmutableList<Lazy<ILanguageService, LanguageServiceMetadata>> LanguageServices
            {
                get
                {
                    return csharpLanguageServices.Value;
                }
            }

            /// <summary>
            /// All ILanguageServiceFactory implementations in C#.
            /// </summary>
            public static ImmutableList<Lazy<ILanguageServiceFactory, LanguageServiceMetadata>> LanguageServiceFactories
            {
                get
                {
                    return csharpLanguageServiceFactories.Value;
                }
            }

            /// <summary>
            /// All IFormattingRule implementations in C#.
            /// </summary>
            public static ImmutableList<Lazy<IFormattingRule, OrderableLanguageMetadata>> FormattingRules
            {
                get
                {
                    return csharpRules.Value;
                }
            }

            /// <summary>
            /// All IOption implementations in listed in the C# Feature Pack.
            /// </summary>
            public static ImmutableList<Lazy<IOption>> Options
            {
                get
                {
                    return csharpOptions.Value;
                }
            }

            private static Lazy<ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>>> csharpServices = 
                new Lazy<ImmutableList<KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>>>(
                () => LanguageServices.Select(ls =>
                    new KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>(ls.Metadata, (lsp) => ls.Value))
                        .Concat(LanguageServiceFactories.Select(lsf =>
                            new KeyValuePair<LanguageServiceMetadata, Func<ILanguageServiceProvider, ILanguageService>>(lsf.Metadata, (lsp) =>
                                lsf.Value.CreateLanguageService(lsp)))).ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<ILanguageService, LanguageServiceMetadata>>> csharpLanguageServices =
                new Lazy<ImmutableList<Lazy<ILanguageService, LanguageServiceMetadata>>>(() => EnumerateCharpLanguageServices().ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<ILanguageServiceFactory, LanguageServiceMetadata>>> csharpLanguageServiceFactories =
                new Lazy<ImmutableList<Lazy<ILanguageServiceFactory, LanguageServiceMetadata>>>(() => EnumerateCharpLanguageServiceFactories().ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<IFormattingRule, OrderableLanguageMetadata>>> csharpRules =
                new Lazy<ImmutableList<Lazy<IFormattingRule, OrderableLanguageMetadata>>>(() => EnumerateCSharpFormattingRules().ToImmutableList(), true);

            private static Lazy<ImmutableList<Lazy<IOption>>> csharpOptions =
                new Lazy<ImmutableList<Lazy<IOption>>>(() => EnumerateCSharpOptions().ToImmutableList(), true);

            private static IEnumerable<Lazy<ILanguageService, LanguageServiceMetadata>> EnumerateCharpLanguageServices()
            {
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                     () => new CSharpCaseCorrectionService(),
                     new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ICaseCorrectionService), WorkspaceKind.Any), true);

                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpSyntaxFactory(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ISyntaxFactoryService), WorkspaceKind.Any), true);

                // syntax formatting service
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpSyntaxFormattingService(Features.CSharp.FormattingRules),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ISyntaxFormattingService), WorkspaceKind.Any), true);

                // Recommendation service
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpRecommendationService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(IRecommendationService), WorkspaceKind.Any), true);

                // Command line arguments service
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpCommandLineArgumentsFactoryService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ICommandLineArgumentsFactoryService), WorkspaceKind.Any), true);

                // Compilation factory service
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpCompilationFactoryService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ICompilationFactoryService), WorkspaceKind.Any), true);

                // Project File Loader service
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpProjectFileLoader(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(IProjectFileLoader), WorkspaceKind.Any), true);

                // Semantic Facts service
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpSemanticFactsService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ISemanticFactsService), WorkspaceKind.Any), true);

                // Symbol Declaration service
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpSymbolDeclarationService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ISymbolDeclarationService), WorkspaceKind.Any), true);

                // Syntax Facts service
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpSyntaxFactsService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ISyntaxFactsService), WorkspaceKind.Any), true);

                // SyntaxVersion service
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpSyntaxVersionService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ISyntaxVersionLanguageService), WorkspaceKind.Any), true);

                // Type Inference service
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpTypeInferenceService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ITypeInferenceService), WorkspaceKind.Any), true);

                // Rename conflicts service
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpRenameConflictLanguageService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(IRenameRewriterLanguageService), WorkspaceKind.Any), true);

                // Simplification service
                yield return new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpSimplificationService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ISimplificationService), WorkspaceKind.Any), true);
            }

            private static IEnumerable<Lazy<ILanguageServiceFactory, LanguageServiceMetadata>> EnumerateCharpLanguageServiceFactories()
            {
                yield return new Lazy<ILanguageServiceFactory, LanguageServiceMetadata>(
                    () => new CSharpCodeCleanerServiceFactory(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ICodeCleanerService), WorkspaceKind.Any), true);

                // code generation
                yield return new Lazy<ILanguageServiceFactory, LanguageServiceMetadata>(
                    () => new CSharpCodeGenerationServiceFactory(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ICodeGenerationService), WorkspaceKind.Any), true);

                // SyntaxTree Factory service
                yield return new Lazy<ILanguageServiceFactory, LanguageServiceMetadata>(
                    () => new CSharpSyntaxTreeFactoryServiceFactory(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(ISyntaxTreeFactoryService), WorkspaceKind.Any), true);
            }

            private static IEnumerable<Lazy<IFormattingRule, OrderableLanguageMetadata>> EnumerateCSharpFormattingRules()
            {
                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new AlignTokensFormattingRule(),
                    new OrderableLanguageMetadata(AlignTokensFormattingRule.Name, LanguageNames.CSharp), true);

                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new AnchorIndentationFormattingRule(),
                    new OrderableLanguageMetadata(AnchorIndentationFormattingRule.Name, LanguageNames.CSharp,
                        after: new string[] { SuppressFormattingRule.Name }), true);

                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new ElasticTriviaFormattingRule(),
                    new OrderableLanguageMetadata(ElasticTriviaFormattingRule.Name, LanguageNames.CSharp), true);

                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new EndOfFileTokenFormattingRule(),
                    new OrderableLanguageMetadata(EndOfFileTokenFormattingRule.Name, LanguageNames.CSharp,
                        after: new string[] { ElasticTriviaFormattingRule.Name }), true);

                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new IndentBlockFormattingRule(),
                    new OrderableLanguageMetadata(IndentBlockFormattingRule.Name, LanguageNames.CSharp,
                        after: new string[] { StructuredTriviaFormattingRule.Name }), true);

                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new QueryExpressionFormattingRule(),
                    new OrderableLanguageMetadata(QueryExpressionFormattingRule.Name, LanguageNames.CSharp,
                        after: new string[] { AnchorIndentationFormattingRule.Name }), true);

                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new StructuredTriviaFormattingRule(),
                    new OrderableLanguageMetadata(StructuredTriviaFormattingRule.Name, LanguageNames.CSharp,
                        after: new string[] { EndOfFileTokenFormattingRule.Name }), true);

                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new SuppressFormattingRule(),
                    new OrderableLanguageMetadata(SuppressFormattingRule.Name, LanguageNames.CSharp,
                        after: new string[] { IndentBlockFormattingRule.Name }), true);

                yield return new Lazy<IFormattingRule, OrderableLanguageMetadata>(
                    () => new TokenBasedFormattingRule(),
                    new OrderableLanguageMetadata(TokenBasedFormattingRule.Name, LanguageNames.CSharp,
                        after: new string[] { QueryExpressionFormattingRule.Name }), true);
            }

            private static IEnumerable<Lazy<CodeAnalysis.Options.IOption>> EnumerateCSharpOptions()
            {
                yield return new Lazy<Options.IOption>(
                   () => CSharpFormattingOptions.MethodDeclarationNameParenthesis);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.MethodDeclarationParenthesisArgumentList);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.MethodDeclarationEmptyArgument);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.MethodCallNameParenthesis);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.MethodCallArgumentList);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.MethodCallEmptyArgument);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OtherAfterControlFlowKeyword);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OtherBetweenParenthesisExpression);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OtherParenthesisTypeCast);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OtherParenControlFlow);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OtherParenAfterCast);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OtherSpacesDeclarationIgnore);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.SquareBracesBefore);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.SquareBracesEmpty);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.SquareBracesAndValue);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.DelimitersAfterColonInTypeDeclaration);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.DelimitersAfterCommaInParameterArgument);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.DelimitersAfterDotMemberAccessQualifiedName);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.DelimitersAfterSemiColonInForStatement);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.DelimitersBeforeColonInTypeDeclaration);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.DelimitersBeforeCommaInParameterArgument);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.DelimitersBeforeDotMemberAccessQualifiedName);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.DelimitersBeforeSemiColonInForStatement);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.SpacingAroundBinaryOperator);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OpenCloseBracesIndent);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.IndentBlock);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.IndentSwitchSection);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.IndentSwitchCaseSection);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.LabelPositioning);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.LeaveStatementMethodDeclarationSameLine);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OpenBracesInNewLineForTypes);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OpenBracesInNewLineForMethods);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OpenBracesInNewLineForAnonymousMethods);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OpenBracesInNewLineForControl);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OpenBracesInNewLineForAnonymousType);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OpenBracesInNewLineForObjectInitializers);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.OpenBracesInNewLineForLambda);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.NewLineForElse);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.NewLineForCatch);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.NewLineForFinally);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.NewLineForMembersInObjectInit);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.NewLineForMembersInAnonymousTypes);
                yield return new Lazy<Options.IOption>(
                    () => CSharpFormattingOptions.NewLineForClausesInQuery);
            }
        }
    }
}
