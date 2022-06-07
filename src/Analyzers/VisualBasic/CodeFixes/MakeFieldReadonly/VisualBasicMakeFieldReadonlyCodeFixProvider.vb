' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.MakeFieldReadonly
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeFieldReadonly
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.MakeFieldReadonly), [Shared]>
    Friend Class VisualBasicMakeFieldReadonlyCodeFixProvider
        Inherits AbstractMakeFieldReadonlyCodeFixProvider(Of ModifiedIdentifierSyntax, FieldDeclarationSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetVariableDeclarators(declaration As FieldDeclarationSyntax) As ImmutableList(Of ModifiedIdentifierSyntax)
            Return declaration.Declarators.SelectMany(Function(d) d.Names).ToImmutableListOrEmpty()
        End Function

        Protected Overrides Function GetInitializerNode(declaration As ModifiedIdentifierSyntax) As SyntaxNode
            Dim initializer = CType(declaration.Parent, VariableDeclaratorSyntax).Initializer?.Value
            If initializer Is Nothing Then
                initializer = TryCast(CType(declaration.Parent, VariableDeclaratorSyntax).AsClause, AsNewClauseSyntax)?.NewExpression
            End If

            Return initializer
        End Function
    End Class
End Namespace
