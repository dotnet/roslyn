' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 9.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.ConvertIfToSwitch

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertIfToSwitch
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertIfToSwitchCodeRefactoringProvider)), [Shared]>
    Partial Friend NotInheritable Class VisualBasicConvertIfToSwitchCodeRefactoringProvider
        Inherits AbstractConvertIfToSwitchCodeRefactoringProvider(Of ExecutableStatementSyntax, ExpressionSyntax, SyntaxNode, SyntaxNode)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides Function GetTitle(forSwitchExpression As Boolean) As String
            Debug.Assert(Not forSwitchExpression)
            Return VBFeaturesResources.Convert_to_Select_Case
        End Function

        Public Overrides Function CreateAnalyzer(syntaxFacts As ISyntaxFactsService, options As ParseOptions) As Analyzer
            Return New VisualBasicAnalyzer(syntaxFacts, Feature.RangePattern Or Feature.RelationalPattern)
        End Function
    End Class
End Namespace

