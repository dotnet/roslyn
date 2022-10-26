' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.MakeDeclarationPartial
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeDeclarationPartial
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.MakeDeclarationPartial), [Shared]>
    Friend Class VisualBasicMakeDeclarationPartialCodeFixProvider
        Inherits AbstractMakeDeclarationPartialCodeFixProvider(Of TypeStatementSyntax)

        Private Const BC40046 As String = NameOf(BC40046)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC40046)

        Protected Overrides Function GetDeclarationName(node As TypeStatementSyntax) As String
            Return node.Identifier.ValueText
        End Function
    End Class
End Namespace
