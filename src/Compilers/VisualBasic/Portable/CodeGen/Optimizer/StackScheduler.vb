' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen
    Partial Friend Class StackScheduler

        Public Shared Function OptimizeLocalsOut(
                         container As Symbol,
                         src As BoundStatement,
                         debugFriendly As Boolean,
                         <Out> ByRef stackLocals As HashSet(Of LocalSymbol)) As BoundStatement

            Dim locals As Dictionary(Of LocalSymbol, LocalDefUseInfo) = Nothing
            src = DirectCast(Analyzer.Analyze(container, src, debugFriendly, locals), BoundStatement)

            locals = FilterValidStackLocals(locals)

            If locals.Count = 0 Then
                stackLocals = New HashSet(Of LocalSymbol)()
                Return src
            Else
                stackLocals = New HashSet(Of LocalSymbol)(locals.Keys)
                Return Rewriter.Rewrite(src, locals)
            End If
        End Function

        Private Shared Function FilterValidStackLocals(info As Dictionary(Of LocalSymbol, LocalDefUseInfo)) As Dictionary(Of LocalSymbol, LocalDefUseInfo)
            '  remove fake dummies and variable that cannot be scheduled
            Dim dummies As New List(Of LocalDefUseInfo)

            For Each local In info.Keys.ToArray()
                Dim locInfo = info(local)

                If TypeOf local Is DummyLocal Then
                    dummies.Add(locInfo)
                    info.Remove(local)
                ElseIf locInfo.CannotSchedule Then
                    info.Remove(local)
                End If
            Next

            If info.Count = 0 Then
                ' nothing to filter
                Return info
            End If

            ' Add dummy definitions. 
            ' Although we do not schedule dummies we intend to guarantee that no local definition 
            ' span intersects with definition spans of a dummy that will ensure that at any access 
            ' to dummy is done on same stack state.
            Dim defs As New List(Of LocalDefUseSpan)
            For Each dummy In dummies
                For Each def In dummy.localDefs
                    ' not interested in single node definitions
                    If def.Start < def.End Then
                        defs.Add(def)
                    End If
                Next
            Next

            Dim dummyCnt = defs.Count

            ' TODO: perf. This can be simplified to not use a query.
            ' order locals by the number of usages, then by the declaration in descending order
            For Each localInfo In From i In info
                                  Where i.Value.localDefs.Count > 0
                                  Order By i.Value.localDefs.Count Descending, i.Value.localDefs(0).Start Descending
                                  Select i

                Debug.Assert(Not TypeOf localInfo.Key Is DummyLocal)

                If Not info.ContainsKey(localInfo.Key) Then
                    ' this pair belongs to a local that is already rejected
                    ' no need to waste time on it
                    Continue For
                End If

                Dim intersects As Boolean = False
                Dim additionalDefs = ArrayBuilder(Of LocalDefUseSpan).GetInstance()

                For Each newDef In localInfo.Value.localDefs
                    Debug.Assert(Not intersects)

                    For i = 0 To dummyCnt - 1
                        If newDef.ConflictsWithDummy(defs(i)) Then
                            intersects = True
                            Exit For
                        End If
                    Next

                    If Not intersects Then
                        For i = dummyCnt To defs.Count - 1
                            If newDef.ConflictsWith(defs(i)) Then
                                intersects = True
                                Exit For
                            End If
                        Next
                    End If

                    If intersects Then
                        info.Remove(localInfo.Key)
                        Exit For
                    Else
                        additionalDefs.Add(newDef)
                    End If
                Next

                If Not intersects Then
                    defs.AddRange(additionalDefs)
                End If
                additionalDefs.Free()
            Next

            Return info
        End Function

    End Class
End Namespace

