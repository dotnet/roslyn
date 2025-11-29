' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddImport
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.AddImport), [Shared]>
    Friend NotInheritable Class VisualBasicAddImportCodeRefactoringProvider
        Inherits AbstractAddImportCodeRefactoringProvider(Of
            ExpressionSyntax,
            MemberAccessExpressionSyntax,
            NameSyntax,
            SimpleNameSyntax,
            QualifiedNameSyntax,
            GlobalNameSyntax,
            ImportsStatementSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
            MyBase.New(VisualBasicSyntaxFacts.Instance)
        End Sub

        Protected Overrides ReadOnly Property AddImportTitle As String
            Get
                Return VBFeaturesResources.Add_Imports_0
            End Get
        End Property

        Protected Overrides ReadOnly Property AddImportAndSimplifyAllOccurrencesTitle As String
            Get
                Return VBFeaturesResources.Add_Imports_0_and_simplify_all_occurrences
            End Get
        End Property
    End Class
End Namespace
