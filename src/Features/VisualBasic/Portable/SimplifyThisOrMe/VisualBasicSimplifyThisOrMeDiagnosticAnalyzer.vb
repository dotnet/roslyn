' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Simplification.Simplifiers
Imports Microsoft.CodeAnalysis.SimplifyThisOrMe
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification.Simplifiers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyThisOrMe
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicSimplifyThisOrMeDiagnosticAnalyzer
        Inherits AbstractSimplifyThisOrMeDiagnosticAnalyzer(Of
            SyntaxKind,
            ExpressionSyntax,
            MeExpressionSyntax,
            MemberAccessExpressionSyntax)

        Protected Overrides ReadOnly Property SyntaxKinds As ISyntaxKinds = VisualBasicSyntaxKinds.Instance

        Protected Overrides ReadOnly Property Simplification As ISimplification
            Get
                Return VisualBasicSimplification.Instance
            End Get
        End Property

        Protected Overrides ReadOnly Property Simplifier As AbstractMemberAccessExpressionSimplifier(Of ExpressionSyntax, MemberAccessExpressionSyntax, MeExpressionSyntax) = MemberAccessExpressionSimplifier.Instance
    End Class
End Namespace
