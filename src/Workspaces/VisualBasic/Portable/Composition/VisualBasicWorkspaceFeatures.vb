' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Composition
#If False Then
    Public Class VisualBasicWorkspaceFeatures
        Inherits FeaturePack

        Public Shared ReadOnly Instance As New VisualBasicWorkspaceFeatures()

        Private Sub New()
        End Sub

        Friend Overrides Function ComposeExports(root As ExportSource) As ExportSource

            Dim list = New ExportList()

            ' Case Correction 
            list.Add(
                New Lazy(Of ILanguageServiceFactory, LanguageServiceMetadata)(
                    Function() New Microsoft.CodeAnalysis.VisualBasic.CaseCorrection.VisualBasicCaseCorrectionServiceFactory(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(Microsoft.CodeAnalysis.CaseCorrection.ICaseCorrectionService), ServiceLayer.Default)))

            ' Code Cleanup
            list.Add(
                New Lazy(Of ILanguageServiceFactory, LanguageServiceMetadata)(
                    Function() New Microsoft.CodeAnalysis.VisualBasic.CodeCleanup.VisualBasicCodeCleanerServiceFactory(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(Microsoft.CodeAnalysis.CodeCleanup.ICodeCleanerService), ServiceLayer.Default)))

            list.Add(
                New Lazy(Of Microsoft.CodeAnalysis.CodeCleanup.Providers.ICodeCleanupProvider, OrderableLanguageMetadata)(
                    Function() New Microsoft.CodeAnalysis.CodeCleanup.Providers.AddMissingTokensCodeCleanupProvider(),
                    New OrderableLanguageMetadata(Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.AddMissingTokens, LanguageNames.VisualBasic,
                                              before:={Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.Simplification})))

            list.Add(
                New Lazy(Of Microsoft.CodeAnalysis.CodeCleanup.Providers.ICodeCleanupProvider, OrderableLanguageMetadata)(
                    Function() New Microsoft.CodeAnalysis.CodeCleanup.Providers.CaseCorrectionCodeCleanupProvider(),
                    New OrderableLanguageMetadata(Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.CaseCorrection, LanguageNames.VisualBasic,
                                              before:={Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.Simplification})))

            list.Add(
                New Lazy(Of Microsoft.CodeAnalysis.CodeCleanup.Providers.ICodeCleanupProvider, OrderableLanguageMetadata)(
                    Function() New Microsoft.CodeAnalysis.CodeCleanup.Providers.FixIncorrectTokensCodeCleanupProvider(),
                    New OrderableLanguageMetadata(Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.FixIncorrectTokens, LanguageNames.VisualBasic,
                                              after:={Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.AddMissingTokens},
                                              before:={Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.Simplification})))

            list.Add(
                New Lazy(Of Microsoft.CodeAnalysis.CodeCleanup.Providers.ICodeCleanupProvider, OrderableLanguageMetadata)(
                    Function() New Microsoft.CodeAnalysis.CodeCleanup.Providers.NormalizeModifiersOrOperatorsCodeCleanupProvider(),
                    New OrderableLanguageMetadata(Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.NormalizeModifiersOrOperators, LanguageNames.VisualBasic,
                                              after:={Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.AddMissingTokens},
                                              before:={Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.Simplification})))

            list.Add(
                New Lazy(Of Microsoft.CodeAnalysis.CodeCleanup.Providers.ICodeCleanupProvider, OrderableLanguageMetadata)(
                    Function() New Microsoft.CodeAnalysis.CodeCleanup.Providers.ReduceTokensCodeCleanupProvider(),
                    New OrderableLanguageMetadata(Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.ReduceTokens, LanguageNames.VisualBasic,
                                              after:={Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.AddMissingTokens},
                                              before:={Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.Simplification})))

            list.Add(
                New Lazy(Of Microsoft.CodeAnalysis.CodeCleanup.Providers.ICodeCleanupProvider, OrderableLanguageMetadata)(
                    Function() New Microsoft.CodeAnalysis.CodeCleanup.Providers.RemoveUnnecessaryLineContinuationCodeCleanupProvider(),
                    New OrderableLanguageMetadata(Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.RemoveUnnecessaryLineContinuation, LanguageNames.VisualBasic,
                                              after:={Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.NormalizeModifiersOrOperators},
                                              before:={Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.Simplification})))

            list.Add(
                New Lazy(Of Microsoft.CodeAnalysis.CodeCleanup.Providers.ICodeCleanupProvider, OrderableLanguageMetadata)(
                    Function() New Microsoft.CodeAnalysis.CodeCleanup.Providers.SimplificationCodeCleanupProvider(),
                    New OrderableLanguageMetadata(Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.Simplification, LanguageNames.VisualBasic,
                                              before:={Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.Format})))

            list.Add(
                New Lazy(Of Microsoft.CodeAnalysis.CodeCleanup.Providers.ICodeCleanupProvider, OrderableLanguageMetadata)(
                    Function() New Microsoft.CodeAnalysis.CodeCleanup.Providers.FormatCodeCleanupProvider(),
                    New OrderableLanguageMetadata(Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.Format, LanguageNames.VisualBasic,
                                              after:={Microsoft.CodeAnalysis.CodeCleanup.Providers.PredefinedCodeCleanupProviderNames.Simplification})))

            ' Code Generation
            list.Add(
                New Lazy(Of ILanguageServiceFactory, LanguageServiceMetadata)(
                    Function() New Microsoft.CodeAnalysis.VisualBasic.CodeGeneration.VisualBasicCodeGenerationServiceFactory(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(Microsoft.CodeAnalysis.CodeGeneration.ICodeGenerationService), ServiceLayer.Default)))

            list.Add(
                New Lazy(Of ILanguageService, LanguageServiceMetadata)(
                    Function() New Microsoft.CodeAnalysis.VisualBasic.CodeGeneration.VisualBasicSyntaxFactory(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(Microsoft.CodeAnalysis.CodeGeneration.ISyntaxFactoryService), ServiceLayer.Default)))

            ' Formatting
            list.Add(
                New Lazy(Of ILanguageService, LanguageServiceMetadata)(
                    Function() New Microsoft.CodeAnalysis.VisualBasic.Formatting.VisualBasicFormattingService(root),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(Microsoft.CodeAnalysis.Formatting.IFormattingService), ServiceLayer.Default)))

            list.Add(
                New Lazy(Of Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata)(
                    Function() New Microsoft.CodeAnalysis.VisualBasic.Formatting.AdjustSpaceFormattingRule(),
                    New OrderableLanguageMetadata(Microsoft.CodeAnalysis.VisualBasic.Formatting.AdjustSpaceFormattingRule.Name, LanguageNames.VisualBasic,
                                              after:={Microsoft.CodeAnalysis.VisualBasic.Formatting.ElasticTriviaFormattingRule.Name})))

            list.Add(
                New Lazy(Of Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata)(
                    Function() New Microsoft.CodeAnalysis.VisualBasic.Formatting.AlignTokensFormattingRule(),
                    New OrderableLanguageMetadata(Microsoft.CodeAnalysis.VisualBasic.Formatting.AlignTokensFormattingRule.Name, LanguageNames.VisualBasic,
                                              after:={Microsoft.CodeAnalysis.VisualBasic.Formatting.AdjustSpaceFormattingRule.Name})))

            list.Add(
                New Lazy(Of Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata)(
                    Function() New Microsoft.CodeAnalysis.VisualBasic.Formatting.ElasticTriviaFormattingRule(),
                    New OrderableLanguageMetadata(Microsoft.CodeAnalysis.VisualBasic.Formatting.ElasticTriviaFormattingRule.Name, LanguageNames.VisualBasic,
                                              after:={Microsoft.CodeAnalysis.VisualBasic.Formatting.StructuredTriviaFormattingRule.Name})))

            list.Add(
                New Lazy(Of Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata)(
                    Function() New Microsoft.CodeAnalysis.VisualBasic.Formatting.NodeBasedFormattingRule(),
                    New OrderableLanguageMetadata(Microsoft.CodeAnalysis.VisualBasic.Formatting.NodeBasedFormattingRule.Name, LanguageNames.VisualBasic,
                                              after:={Microsoft.CodeAnalysis.VisualBasic.Formatting.AlignTokensFormattingRule.Name})))

            list.Add(
                New Lazy(Of Microsoft.CodeAnalysis.Formatting.Rules.IFormattingRule, OrderableLanguageMetadata)(
                    Function() New Microsoft.CodeAnalysis.VisualBasic.Formatting.StructuredTriviaFormattingRule(),
                    New OrderableLanguageMetadata(Microsoft.CodeAnalysis.VisualBasic.Formatting.StructuredTriviaFormattingRule.Name, LanguageNames.VisualBasic)))

            ' Recommendation service
            list.Add(
                New Lazy(Of ILanguageService, LanguageServiceMetadata)(
                    Function() New VisualBasicRecommendationService(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(IRecommendationService), ServiceLayer.Default)))

            ' Command Line Arguments
            list.Add(
                New Lazy(Of ILanguageService, LanguageServiceMetadata)(
                    Function() New VisualBasicCommandLineArgumentsFactoryService(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(ICommandLineArgumentsFactoryService), ServiceLayer.Default)))

            ' Compilation Factory
            list.Add(
                New Lazy(Of ILanguageService, LanguageServiceMetadata)(
                    Function() New VisualBasicCompilationFactoryService(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(ICompilationFactoryService), ServiceLayer.Default)))

            ' Project File Loader
            list.Add(
                New Lazy(Of ILanguageService, LanguageServiceMetadata)(
                    Function() New VisualBasicProjectFileLoaderService(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(Host.ProjectFileLoader.IProjectFileLoaderLanguageService), ServiceLayer.Default)))

            ' Semantic Facts
            list.Add(
                New Lazy(Of ILanguageService, LanguageServiceMetadata)(
                    Function() New VisualBasicSemanticFactsService(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(ISemanticFactsService), ServiceLayer.Default)))

            ' Symbol Declaration
            list.Add(
                New Lazy(Of ILanguageService, LanguageServiceMetadata)(
                    Function() New VisualBasicSymbolDeclarationService(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(ISymbolDeclarationService), ServiceLayer.Default)))

            ' Syntax Facts
            list.Add(
                New Lazy(Of ILanguageService, LanguageServiceMetadata)(
                    Function() New VisualBasicSyntaxFactsService(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(ISyntaxFactsService), ServiceLayer.Default)))

            ' SyntaxTree Factory
            list.Add(
                New Lazy(Of ILanguageServiceFactory, LanguageServiceMetadata)(
                    Function() New VisualBasicSyntaxTreeFactoryServiceFactory(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(ISyntaxTreeFactoryService), ServiceLayer.Default)))

            ' Syntax Version
            list.Add(
                New Lazy(Of ILanguageService, LanguageServiceMetadata)(
                    Function() New VisualBasicSyntaxVersionLanguageService(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(ISyntaxVersionLanguageService), ServiceLayer.Default)))

            ' Type Inference
            list.Add(
                New Lazy(Of ILanguageService, LanguageServiceMetadata)(
                    Function() New VisualBasicTypeInferenceService(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(ITypeInferenceService), ServiceLayer.Default)))

            ' Rename Rewriter
            list.Add(
                New Lazy(Of ILanguageServiceFactory, LanguageServiceMetadata)(
                    Function() New Microsoft.CodeAnalysis.VisualBasic.Rename.VisualBasicRenameRewriterLanguageServiceFactory(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(Microsoft.CodeAnalysis.Rename.IRenameRewriterLanguageService), ServiceLayer.Default)))

            ' Simplification
            list.Add(
                New Lazy(Of ILanguageService, LanguageServiceMetadata)(
                    Function() New Microsoft.CodeAnalysis.VisualBasic.Simplification.VisualBasicSimplificationService(),
                    New LanguageServiceMetadata(LanguageNames.VisualBasic, GetType(Microsoft.CodeAnalysis.Simplification.ISimplificationService), ServiceLayer.Default)))

            Return list
        End Function
    End Class
#End If
End Namespace
