﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class DirectiveTriviaSyntax

        Public Function GetRelatedDirectives() As List(Of DirectiveTriviaSyntax)
            Dim list = New List(Of DirectiveTriviaSyntax)()
            GetRelatedDirectives(list)
            Return list
        End Function

        Private Sub GetRelatedDirectives(list As List(Of DirectiveTriviaSyntax))
            list.Clear()
            Dim p = GetPreviousRelatedDirective()
            While p IsNot Nothing
                list.Add(p)
                p = p.GetPreviousRelatedDirective()
            End While

            list.Reverse()
            list.Add(Me)
            Dim n = GetNextRelatedDirective()
            While n IsNot Nothing
                list.Add(n)
                n = n.GetNextRelatedDirective()
            End While
        End Sub

        Private Function GetNextRelatedDirective() As DirectiveTriviaSyntax
            Dim d As DirectiveTriviaSyntax = Me
            Select Case d.Kind
                Case SyntaxKind.IfDirectiveTrivia
                    While d IsNot Nothing
                        Select Case d.Kind
                            Case SyntaxKind.ElseIfDirectiveTrivia, SyntaxKind.ElseDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia
                                Return d
                        End Select

                        d = d.GetNextPossiblyRelatedDirective()
                    End While


                Case SyntaxKind.ElseIfDirectiveTrivia
                    While d IsNot Nothing
                        Select Case d.Kind
                            Case SyntaxKind.ElseDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia
                                Return d
                        End Select

                        d = d.GetNextPossiblyRelatedDirective()
                    End While


                Case SyntaxKind.ElseDirectiveTrivia
                    While d IsNot Nothing
                        If d.Kind = SyntaxKind.EndIfDirectiveTrivia Then
                            Return d
                        End If

                        d = d.GetNextPossiblyRelatedDirective()
                    End While


                Case SyntaxKind.RegionDirectiveTrivia
                    While d IsNot Nothing
                        If d.Kind = SyntaxKind.EndRegionDirectiveTrivia Then
                            Return d
                        End If

                        d = d.GetNextPossiblyRelatedDirective()
                    End While


            End Select

            Return Nothing
        End Function

        Private Function GetNextPossiblyRelatedDirective() As DirectiveTriviaSyntax
            Dim d As DirectiveTriviaSyntax = Me
            While d IsNot Nothing
                d = d.GetNextDirective()
                If d IsNot Nothing Then
                    Select Case d.Kind
                        Case SyntaxKind.IfDirectiveTrivia
                            While d IsNot Nothing AndAlso d.Kind <> SyntaxKind.EndIfDirectiveTrivia
                                d = d.GetNextRelatedDirective()
                            End While

                            Continue While
                        Case SyntaxKind.RegionDirectiveTrivia
                            While d IsNot Nothing AndAlso d.Kind <> SyntaxKind.EndRegionDirectiveTrivia
                                d = d.GetNextRelatedDirective()
                            End While

                            Continue While
                    End Select
                End If

                Return d
            End While

            Return Nothing
        End Function

        Private Function GetPreviousRelatedDirective() As DirectiveTriviaSyntax
            Dim d As DirectiveTriviaSyntax = Me
            Select Case d.Kind
                Case SyntaxKind.EndIfDirectiveTrivia
                    While d IsNot Nothing
                        Select Case d.Kind
                            Case SyntaxKind.IfDirectiveTrivia, SyntaxKind.ElseIfDirectiveTrivia, SyntaxKind.ElseDirectiveTrivia
                                Return d
                        End Select

                        d = d.GetPreviousPossiblyRelatedDirective()
                    End While


                Case SyntaxKind.ElseIfDirectiveTrivia
                    While d IsNot Nothing
                        If d.Kind = SyntaxKind.IfDirectiveTrivia Then
                            Return d
                        End If

                        d = d.GetPreviousPossiblyRelatedDirective()
                    End While


                Case SyntaxKind.ElseDirectiveTrivia
                    While d IsNot Nothing
                        Select Case d.Kind
                            Case SyntaxKind.IfDirectiveTrivia, SyntaxKind.ElseIfDirectiveTrivia
                                Return d
                        End Select

                        d = d.GetPreviousPossiblyRelatedDirective()
                    End While


                Case SyntaxKind.EndRegionDirectiveTrivia
                    While d IsNot Nothing
                        If d.Kind = SyntaxKind.RegionDirectiveTrivia Then
                            Return d
                        End If

                        d = d.GetPreviousPossiblyRelatedDirective()
                    End While


            End Select

            Return Nothing
        End Function

        Private Function GetPreviousPossiblyRelatedDirective() As DirectiveTriviaSyntax
            Dim d As DirectiveTriviaSyntax = Me
            While d IsNot Nothing
                d = d.GetPreviousDirective()
                If d IsNot Nothing Then
                    Select Case d.Kind
                        Case SyntaxKind.EndIfDirectiveTrivia
                            While d IsNot Nothing AndAlso d.Kind <> SyntaxKind.IfDirectiveTrivia
                                d = d.GetPreviousRelatedDirective()
                            End While

                            Continue While
                        Case SyntaxKind.EndRegionDirectiveTrivia
                            While d IsNot Nothing AndAlso d.Kind <> SyntaxKind.RegionDirectiveTrivia
                                d = d.GetPreviousRelatedDirective()
                            End While

                            Continue While
                    End Select
                End If

                Return d
            End While

            Return Nothing
        End Function

    End Class

End Namespace
