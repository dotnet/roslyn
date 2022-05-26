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
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.MakeTypeAbstract), [Shared]>
    Friend NotInheritable Class VisualBasicMakeTypeAbstractCodeFixProvider
        Inherits AbstractMakeTypeAbstractCodeFixProvider(Of ClassBlockSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create("BC31411")

        Protected Overrides Function IsValidRefactoringContext(node As SyntaxNode, ByRef typeDeclaration As ClassBlockSyntax) As Boolean
            Dim classStatement = TryCast(node, ClassStatementSyntax)
            If classStatement Is Nothing Then
                Return False
            End If

            If classStatement.Modifiers.Any(SyntaxKind.MustInheritKeyword) OrElse
               classStatement.Modifiers.Any(SyntaxKind.StaticKeyword) Then
                Return False
            End If

            typeDeclaration = TryCast(classStatement.Parent, ClassBlockSyntax)
            Return typeDeclaration IsNot Nothing
        End Function
    End Class
End Namespace
