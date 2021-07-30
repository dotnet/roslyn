' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.MoveStaticMembers

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.MoveStaticMembers
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.MoveStaticMembers), [Shared]>
    Friend Class VisualBasicMoveStaticMembersRefactoringProvider
        Inherits AbstractMoveStaticMembersRefactoringProvider

        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        <ImportingConstructor>
        Public Sub New()
            MyBase.New(Nothing)
        End Sub

        Protected Overrides Async Function GetSelectedNodeAsync(context As CodeRefactoringContext) As Task(Of SyntaxNode)
            Return Await GetSelectedMemberDeclarationAsync(context).ConfigureAwait(False)
        End Function
    End Class
End Namespace
