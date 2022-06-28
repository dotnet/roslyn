' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.MoveStaticMembers

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.MoveStaticMembers
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.MoveStaticMembers), [Shared]>
    Friend Class VisualBasicMoveStaticMembersRefactoringProvider
        Inherits AbstractMoveStaticMembersRefactoringProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New()
        End Sub

        Protected Overrides Async Function GetSelectedNodesAsync(context As CodeRefactoringContext) As Task(Of ImmutableArray(Of SyntaxNode))
            Return Await GetSelectedMemberDeclarationAsync(context).ConfigureAwait(False)
        End Function
    End Class
End Namespace
