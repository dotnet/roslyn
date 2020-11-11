' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.MakeTypeAbstract
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeTypeAbstract
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicMakeTypeAbstractCodeFixProvider)), [Shared]>
    Friend NotInheritable Class VisualBasicMakeTypeAbstractCodeFixProvider
        Inherits AbstractMakeTypeAbstractCodeFixProvider(Of ClassStatementSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(
                "BC31411"
            )

        Protected Overrides Function IsValidRefactoringContext(node As SyntaxNode, ByRef typeDeclaration As ClassStatementSyntax) As Boolean
            If node Is Nothing OrElse Not (node.IsKind(SyntaxKind.ClassStatement)) Then
                Return False
            End If

            typeDeclaration = CType(node, ClassStatementSyntax)

            Return Not (typeDeclaration.Modifiers.Any(SyntaxKind.MustInheritKeyword) OrElse typeDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
        End Function
    End Class
End Namespace
