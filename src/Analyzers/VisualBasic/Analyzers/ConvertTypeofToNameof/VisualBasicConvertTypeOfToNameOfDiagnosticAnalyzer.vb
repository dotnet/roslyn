' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ConvertTypeOfToNameOf
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertTypeOfToNameOf
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicConvertTypeOfToNameOfDiagnosticAnalyzer
        Inherits AbstractConvertTypeOfToNameOfDiagnosticAnalyzer

        Private Shared ReadOnly s_title As String = VisualBasicAnalyzersResources.GetType_can_be_converted_to_NameOf

        Public Sub New()
            MyBase.New(s_title)
        End Sub

        Protected Overrides Function IsValidTypeofAction(context As OperationAnalysisContext) As Boolean
            Dim node = context.Operation.Syntax
            Dim compilation = context.Compilation
            Dim isValidLanguage = DirectCast(compilation, VisualBasicCompilation).LanguageVersion >= LanguageVersion.VisualBasic14
            Dim isValidType = node.IsKind(SyntaxKind.GetTypeExpression)
            Dim isParentValid = node.Parent.GetType() Is GetType(MemberAccessExpressionSyntax)

            Return isValidLanguage And isValidType And isParentValid
        End Function
    End Class
End Namespace
