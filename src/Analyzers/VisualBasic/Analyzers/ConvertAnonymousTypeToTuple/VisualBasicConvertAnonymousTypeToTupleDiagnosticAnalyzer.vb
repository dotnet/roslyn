' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertAnonymousTypeToTuple
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicConvertAnonymousTypeToTupleDiagnosticAnalyzer
        Inherits AbstractConvertAnonymousTypeToTupleDiagnosticAnalyzer(Of
            SyntaxKind, AnonymousObjectCreationExpressionSyntax)

        Public Sub New()
            MyBase.New(VisualBasicSyntaxKinds.Instance)
        End Sub

        Protected Overrides Function GetInitializerCount(anonymousType As AnonymousObjectCreationExpressionSyntax) As Integer
            Return anonymousType.Initializer.Initializers.Count
        End Function
    End Class
End Namespace
