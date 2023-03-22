' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertIfToSwitch
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertIfToSwitch
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ConvertIfToSwitch), [Shared]>
    Partial Friend NotInheritable Class VisualBasicConvertIfToSwitchCodeRefactoringProvider
        Inherits AbstractConvertIfToSwitchCodeRefactoringProvider(Of ExecutableStatementSyntax, ExpressionSyntax, SyntaxNode, SyntaxNode)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides Function GetTitle(forSwitchExpression As Boolean) As String
            Debug.Assert(Not forSwitchExpression)
            Return VBFeaturesResources.Convert_to_Select_Case
        End Function

        Public Overrides Function CreateAnalyzer(syntaxFacts As ISyntaxFacts, options As ParseOptions) As Analyzer
            Return New VisualBasicAnalyzer(syntaxFacts, Feature.RangePattern Or Feature.RelationalPattern Or Feature.InequalityPattern)
        End Function

        Protected Overrides Function GetLeadingTriviaToTransfer(syntaxToRemove As SyntaxNode) As SyntaxTriviaList
            ' Add cases here if we find there are vb cases with trivia to preserve
            Return Nothing
        End Function
    End Class
End Namespace

