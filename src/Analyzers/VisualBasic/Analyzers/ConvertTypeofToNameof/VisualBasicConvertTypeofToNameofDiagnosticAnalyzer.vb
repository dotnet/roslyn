' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.ConvertTypeOfToNameOf
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertTypeOfToNameOf
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicConvertTypeOfToNameOfDiagnosticAnalyzer
        Inherits AbstractConvertTypeOfToNameOfDiagnosticAnalyzer

        Protected Overrides Function IsValidTypeofAction(context As OperationAnalysisContext) As Boolean
            Dim node As SyntaxNode
            Dim compilation As Compilation
            Dim isValidLanguage As Boolean

            node = context.Operation.Syntax
            compilation = context.Compilation
            isValidLanguage = DirectCast(compilation, VisualBasicCompilation).LanguageVersion >= LanguageVersion.VisualBasic14

            Return isValidLanguage And
                (node.GetType() Is GetType(GetTypeExpressionSyntax)) And
                (node.Parent.GetType() Is GetType(MemberAccessExpressionSyntax))
        End Function
    End Class
End Namespace
