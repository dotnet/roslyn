' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Friend Class VisualBasicWarningStateMap
        Inherits AbstractWarningStateMap(Of ReportDiagnostic)

        Public Sub New(tree As SyntaxTree)
            MyBase.New(tree, isGeneratedCode:=False)
        End Sub

        Protected Overrides Function CreateWarningStateMapEntries(syntaxTree As SyntaxTree) As WarningStateMapEntry()
            ' Accumulate all warning directives in source code order.
            Dim directives = ArrayBuilder(Of DirectiveTriviaSyntax).GetInstance()
            GetAllWarningDirectives(syntaxTree, directives)

            ' Create the warning state map.
            Return CreateWarningStateEntries(directives.ToImmutableAndFree())
        End Function

        ' Add all warning directives to the list in source code order.
        Private Shared Sub GetAllWarningDirectives(syntaxTree As SyntaxTree, directiveList As ArrayBuilder(Of DirectiveTriviaSyntax))
            For Each d As DirectiveTriviaSyntax In syntaxTree.GetRoot().GetDirectives()
                If d.IsKind(SyntaxKind.EnableWarningDirectiveTrivia) Then
                    Dim w = DirectCast(d, EnableWarningDirectiveTriviaSyntax)
                    If (Not w.EnableKeyword.IsMissing) AndAlso (Not w.EnableKeyword.ContainsDiagnostics) AndAlso
                       (Not w.WarningKeyword.IsMissing) AndAlso (Not w.WarningKeyword.ContainsDiagnostics) Then
                        directiveList.Add(w)
                    End If
                ElseIf d.IsKind(SyntaxKind.DisableWarningDirectiveTrivia) Then
                    Dim w = DirectCast(d, DisableWarningDirectiveTriviaSyntax)
                    If (Not w.DisableKeyword.IsMissing) AndAlso (Not w.DisableKeyword.ContainsDiagnostics) AndAlso
                       (Not w.WarningKeyword.IsMissing) AndAlso (Not w.WarningKeyword.ContainsDiagnostics) Then
                        directiveList.Add(w)
                    End If
                End If
            Next
        End Sub

        ' Given the ordered list of all warning directives in the syntax tree, return a list of entries
        ' each of which captures a position in the source and the set of warnings that are disabled at this position.
        Private Shared Function CreateWarningStateEntries(directiveList As ImmutableArray(Of DirectiveTriviaSyntax)) As WarningStateMapEntry()
            Dim entries = New WarningStateMapEntry(directiveList.Length) {}
            Dim index = 0
            entries(index) = New WarningStateMapEntry(0, ReportDiagnostic.Default, Nothing)

            ' Captures the general reporting state.
            Dim accumulatedGeneralWarningState = ReportDiagnostic.Default

            ' Captures reporting state for specific warnings. Diagnostic ids must be processed in case-insensitive fashion in VB.
            Dim accumulatedSpecificWarningState = ImmutableDictionary.Create(Of String, ReportDiagnostic)(CaseInsensitiveComparison.Comparer)

            While (index < directiveList.Length)
                Dim currentDirective = directiveList(index)
                Dim reportingState As ReportDiagnostic
                Dim codes As SeparatedSyntaxList(Of IdentifierNameSyntax)

                ' Compute the reporting state.
                If currentDirective.IsKind(SyntaxKind.EnableWarningDirectiveTrivia) Then
                    reportingState = ReportDiagnostic.Default
                    codes = DirectCast(currentDirective, EnableWarningDirectiveTriviaSyntax).ErrorCodes
                ElseIf currentDirective.IsKind(SyntaxKind.DisableWarningDirectiveTrivia) Then
                    reportingState = ReportDiagnostic.Suppress
                    codes = DirectCast(currentDirective, DisableWarningDirectiveTriviaSyntax).ErrorCodes
                End If

                ' Check if this directive impacts the general reporting state. (Is this "#Disable Warning" / "#Enable Warning" with no ids specified?)
                If codes.Count = 0 Then
                    ' Update the general reporting state and reset the specific one.
                    accumulatedGeneralWarningState = reportingState
                    ' Diagnostic ids must be processed in case-insensitive fashion in VB.
                    accumulatedSpecificWarningState = ImmutableDictionary.Create(Of String, ReportDiagnostic)(CaseInsensitiveComparison.Comparer)
                Else
                    For i As Integer = 0 To codes.Count - 1
                        Dim currentCode = codes(i)
                        If currentCode.IsMissing OrElse currentCode.ContainsDiagnostics Then
                            Continue For
                        End If

                        ' Update the specific reporting state for the current warning.
                        accumulatedSpecificWarningState = accumulatedSpecificWarningState.SetItem(currentCode.Identifier.ValueText, reportingState)
                    Next
                End If

                index += 1
                entries(index) = New WarningStateMapEntry(currentDirective.GetLocation().SourceSpan.End, accumulatedGeneralWarningState, accumulatedSpecificWarningState)
            End While

#If DEBUG Then
            ' Make sure the entries array is correctly sorted. 
            For i As Integer = 0 To entries.Length - 2
                Debug.Assert(entries(i).CompareTo(entries(i + 1)) < 0)
            Next
#End If

            Return entries
        End Function
    End Class
End Namespace
