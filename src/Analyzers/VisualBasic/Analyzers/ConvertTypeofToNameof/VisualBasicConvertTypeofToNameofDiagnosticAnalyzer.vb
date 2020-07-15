' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.ConvertTypeofToNameof
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertTypeofToNameof
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicConvertTypeofToNameofDiagnosticAnalyzer
        Inherits AbstractConvertTypeofToNameofDiagnosticAnalyzer

        Protected Overrides Function IsValidTypeofAction(context As OperationAnalysisContext) As Boolean
            Dim node As SyntaxNode
            node = context.Operation.Syntax

            Return (node.GetType() Is GetType(GetTypeExpressionSyntax)) And (node.Parent.GetType() Is GetType(MemberAccessExpressionSyntax))
        End Function
    End Class
End Namespace
