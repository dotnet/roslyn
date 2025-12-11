' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Rename
    <ExportLanguageService(GetType(IRenameIssuesService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicRenameIssuesService
        Implements IRenameIssuesService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CheckLanguageSpecificIssues(semantic As SemanticModel, symbol As ISymbol, triggerToken As SyntaxToken, <NotNullWhen(True)> ByRef langError As String) As Boolean Implements IRenameIssuesService.CheckLanguageSpecificIssues
            Return False
        End Function

        Public Function CheckDeclarationConflict(symbol As ISymbol, newName As String, <NotNullWhen(True)> ByRef message As String) As Boolean Implements IRenameIssuesService.CheckDeclarationConflict
            message = Nothing

            ' Only check for members that have a containing type
            If symbol.ContainingType Is Nothing Then
                Return False
            End If

            ' Check if there's already a member with the new name in the same type
            Dim existingMembers = symbol.ContainingType.GetMembers(newName)
            For Each existingMember In existingMembers
                ' Skip the symbol being renamed itself
                If SymbolEqualityComparer.Default.Equals(existingMember, symbol) Then
                    Continue For
                End If

                ' Found a conflict
                message = String.Format(FeaturesResources.The_name_0_conflicts_with_an_existing_member_name, newName)
                Return True
            Next

            Return False
        End Function
    End Class
End Namespace
