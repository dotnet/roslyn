' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.UseInterpolatedString
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseInterpolatedString
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicUseInterpolatedStringDiagnosticAnalyzer
        Inherits AbstractUseInterpolatedStringDiagnosticAnalyzer (Of SyntaxKind)

        Public Sub New()
            MyBase.New()
        End Sub

        Protected Overrides Function GetSyntaxKinds() As ImmutableArray(Of SyntaxKind)
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
