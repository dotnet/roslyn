' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.ConvertTypeOfToNameOf
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertTypeOfToNameOf
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicConvertTypeOfToNameOfDiagnosticAnalyzer
        Inherits AbstractConvertTypeOfToNameOfDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(IDEDiagnosticIds.ConvertTypeOfToNameOfDiagnosticId, [option]:=Nothing, title:=New LocalizableResourceString(NameOf(VisualBasicAnalyzersResources.GetType_can_be_converted_to_NameOf), VisualBasicAnalyzersResources.ResourceManager, GetType(VisualBasicAnalyzersResources)))
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
