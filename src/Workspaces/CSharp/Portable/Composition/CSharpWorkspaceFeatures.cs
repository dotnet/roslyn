// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Composition
{
#if false
    public class CSharpWorkspaceFeatures : FeaturePack
    {
        private CSharpWorkspaceFeatures()
        {
        }

        public static readonly FeaturePack Instance = new CSharpWorkspaceFeatures();

        internal override ExportSource ComposeExports(ExportSource root)
        {
            return new ExportList()
            {
                // case correction
                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.CaseCorrection.CSharpCaseCorrectionService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.CaseCorrection.ICaseCorrectionService), ServiceLayer.Default)),

                // code clean up
                new Lazy<ILanguageServiceFactory, LanguageServiceMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.CodeCleanup.CSharpCodeCleanerServiceFactory(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.CodeCleanup.ICodeCleanerService), ServiceLayer.Default)),

                // code generation
                new Lazy<ILanguageServiceFactory, LanguageServiceMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationServiceFactory(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.CodeGeneration.ICodeGenerationService), ServiceLayer.Default)),

                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpSyntaxFactory(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.CodeGeneration.ISyntaxFactoryService), ServiceLayer.Default)),

                // formatting service
                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingService(root),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.Formatting.IFormattingService), ServiceLayer.Default)),

                // formatting rules
                new Lazy<Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.Formatting.AlignTokensFormattingRule(),
                    new OrderableLanguageMetadata(Microsoft.CodeAnalysis.CSharp.Formatting.AlignTokensFormattingRule.Name, LanguageNames.CSharp)),

                new Lazy<Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.Formatting.AnchorIndentationFormattingRule(),
                    new OrderableLanguageMetadata(Microsoft.CodeAnalysis.CSharp.Formatting.AnchorIndentationFormattingRule.Name, LanguageNames.CSharp,
                        after: new string[] { Microsoft.CodeAnalysis.CSharp.Formatting.SuppressFormattingRule.Name })),

                new Lazy<Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.Formatting.ElasticTriviaFormattingRule(),
                    new OrderableLanguageMetadata(Microsoft.CodeAnalysis.CSharp.Formatting.ElasticTriviaFormattingRule.Name, LanguageNames.CSharp)),

                new Lazy<Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.Formatting.EndOfFileTokenFormattingRule(),
                    new OrderableLanguageMetadata(Microsoft.CodeAnalysis.CSharp.Formatting.EndOfFileTokenFormattingRule.Name, LanguageNames.CSharp, 
                        after: new string[] { Microsoft.CodeAnalysis.CSharp.Formatting.ElasticTriviaFormattingRule.Name })),

                new Lazy<Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.Formatting.IndentBlockFormattingRule(),
                    new OrderableLanguageMetadata(Microsoft.CodeAnalysis.CSharp.Formatting.IndentBlockFormattingRule.Name, LanguageNames.CSharp,
                        after: new string[] { Microsoft.CodeAnalysis.CSharp.Formatting.StructuredTriviaFormattingRule.Name })),

                new Lazy<Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.Formatting.QueryExpressionFormattingRule(),
                    new OrderableLanguageMetadata(Microsoft.CodeAnalysis.CSharp.Formatting.QueryExpressionFormattingRule.Name, LanguageNames.CSharp, 
                        after: new string[] { Microsoft.CodeAnalysis.CSharp.Formatting.AnchorIndentationFormattingRule.Name })),

                new Lazy<Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.Formatting.StructuredTriviaFormattingRule(),
                    new OrderableLanguageMetadata(Microsoft.CodeAnalysis.CSharp.Formatting.StructuredTriviaFormattingRule.Name, LanguageNames.CSharp, 
                        after: new string[] { Microsoft.CodeAnalysis.CSharp.Formatting.EndOfFileTokenFormattingRule.Name })),

                new Lazy<Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.Formatting.SuppressFormattingRule(),
                    new OrderableLanguageMetadata(Microsoft.CodeAnalysis.CSharp.Formatting.SuppressFormattingRule.Name, LanguageNames.CSharp, 
                        after: new string[] { Microsoft.CodeAnalysis.CSharp.Formatting.IndentBlockFormattingRule.Name })),

                new Lazy<Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.Formatting.TokenBasedFormattingRule(),
                    new OrderableLanguageMetadata(Microsoft.CodeAnalysis.CSharp.Formatting.TokenBasedFormattingRule.Name, LanguageNames.CSharp, 
                        after: new string[] { Microsoft.CodeAnalysis.CSharp.Formatting.QueryExpressionFormattingRule.Name })),

                // formatting options
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.MethodDeclarationNameParenthesis),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.MethodDeclarationParenthesisArgumentList),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.MethodDeclarationEmptyArgument),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.MethodCallNameParenthesis),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.MethodCallArgumentList),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.MethodCallEmptyArgument),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OtherAfterControlFlowKeyword),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OtherBetweenParenthesisExpression),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OtherParenthesisTypeCast),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OtherParenControlFlow),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OtherParenAfterCast),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OtherSpacesDeclarationIgnore),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SquareBracesBefore),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SquareBracesEmpty),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SquareBracesAndValue),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.DelimitersAfterColonInTypeDeclaration),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.DelimitersAfterCommaInParameterArgument),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.DelimitersAfterDotMemberAccessQualifiedName),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.DelimitersAfterSemiColonInForStatement),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.DelimitersBeforeColonInTypeDeclaration),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.DelimitersBeforeCommaInParameterArgument),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.DelimitersBeforeDotMemberAccessQualifiedName),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.DelimitersBeforeSemiColonInForStatement),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.SpacingAroundBinaryOperator),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OpenCloseBracesIndent),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.IndentBlock),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.IndentSwitchSection),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.IndentSwitchCaseSection),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.LabelPositioning),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.LeaveStatementMethodDeclarationSameLine),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OpenBracesInNewLineForTypes),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OpenBracesInNewLineForMethods),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OpenBracesInNewLineForAnonymousMethods),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OpenBracesInNewLineForControl),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OpenBracesInNewLineForAnonymousType),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OpenBracesInNewLineForObjectInitializers),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.OpenBracesInNewLineForLambda),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForElse),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForCatch),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForFinally),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForMembersInObjectInit),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForMembersInAnonymousTypes),
                new Lazy<Options.IOption>(
                    () => Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions.NewLineForClausesInQuery),

                // Recommendation service
                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpRecommendationService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.Recommendations.IRecommendationService), ServiceLayer.Default)),

                // Command line arguments service
                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpCommandLineArgumentsFactoryService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.LanguageServices.ICommandLineArgumentsFactoryService), ServiceLayer.Default)),

                // Compilation factory service
                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpCompilationFactoryService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.LanguageServices.ICompilationFactoryService), ServiceLayer.Default)),

                // Project File Loader service
                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpProjectFileLoaderService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.Host.ProjectFileLoader.IProjectFileLoaderLanguageService), ServiceLayer.Default)),

                // Semantic Facts service
                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpSemanticFactsService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.LanguageServices.ISemanticFactsService), ServiceLayer.Default)),

                // Symbol Declaration service
                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpSymbolDeclarationService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.LanguageServices.ISymbolDeclarationService), ServiceLayer.Default)),

                // Syntax Facts service
                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpSyntaxFactsService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.LanguageServices.ISyntaxFactsService), ServiceLayer.Default)),

                // SyntaxTree Factory service
                new Lazy<ILanguageServiceFactory, LanguageServiceMetadata>(
                    () => new CSharpSyntaxTreeFactoryServiceFactory(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.LanguageServices.ISyntaxTreeFactoryService), ServiceLayer.Default)),

                // SyntaxVersion service
                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpSyntaxVersionService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.LanguageServices.ISyntaxVersionLanguageService), ServiceLayer.Default)),

                // Type Inference service
                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new CSharpTypeInferenceService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.LanguageServices.ITypeInferenceService), ServiceLayer.Default)),

                // Rename conflicts service
                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.Rename.CSharpRenameConflictLanguageService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.Rename.IRenameRewriterLanguageService), ServiceLayer.Default)),

                // Simplification service
                new Lazy<ILanguageService, LanguageServiceMetadata>(
                    () => new Microsoft.CodeAnalysis.CSharp.Simplification.CSharpSimplificationService(),
                    new LanguageServiceMetadata(LanguageNames.CSharp, typeof(Microsoft.CodeAnalysis.Simplification.ISimplificationService), ServiceLayer.Default))
            };
        }
    }
#endif
}
