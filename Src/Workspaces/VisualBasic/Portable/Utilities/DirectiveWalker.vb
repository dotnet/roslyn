' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Linq
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    Friend Class DirectiveWalker
        Inherits VBSyntaxWalker

        Private ReadOnly _startEndMap As Dictionary(Of DirectiveTriviaSyntax, DirectiveTriviaSyntax)
        Private ReadOnly _conditionalMap As Dictionary(Of DirectiveTriviaSyntax, IEnumerable(Of DirectiveTriviaSyntax))
        Private ReadOnly _cancellationToken As CancellationToken

        Private ReadOnly _regionStack As New Stack(Of DirectiveTriviaSyntax)()
        Private ReadOnly _ifStack As New Stack(Of DirectiveTriviaSyntax)()

        Public Sub New(startEndMap As Dictionary(Of DirectiveTriviaSyntax, DirectiveTriviaSyntax),
                       conditionalMap As Dictionary(Of DirectiveTriviaSyntax, IEnumerable(Of DirectiveTriviaSyntax)),
                       cancellationToken As CancellationToken)
            MyBase.New(SyntaxWalkerDepth.StructuredTrivia)

            _startEndMap = startEndMap
            _conditionalMap = conditionalMap
            _cancellationToken = cancellationToken
        End Sub

        Public Overrides Sub DefaultVisit(node As SyntaxNode)
            _cancellationToken.ThrowIfCancellationRequested()

            If Not node.ContainsDirectives Then
                Return
            End If

            MyBase.DefaultVisit(node)
        End Sub

        Public Overrides Sub VisitToken(token As SyntaxToken)
            If Not token.ContainsDirectives Then
                Return
            End If

            VisitLeadingTrivia(token)
        End Sub

        Public Overrides Sub VisitIfDirectiveTrivia(directive As IfDirectiveTriviaSyntax)
            _ifStack.Push(directive)
            MyBase.VisitIfDirectiveTrivia(directive)
        End Sub

        Public Overrides Sub VisitElseDirectiveTrivia(directive As ElseDirectiveTriviaSyntax)
            _ifStack.Push(directive)
            MyBase.VisitElseDirectiveTrivia(directive)
        End Sub

        Public Overrides Sub VisitRegionDirectiveTrivia(directive As RegionDirectiveTriviaSyntax)
            _regionStack.Push(directive)
            MyBase.VisitRegionDirectiveTrivia(directive)
        End Sub

        Public Overrides Sub VisitEndIfDirectiveTrivia(directive As EndIfDirectiveTriviaSyntax)
            If Not _ifStack.IsEmpty() Then
                Dim condDirectives As New List(Of DirectiveTriviaSyntax)
                condDirectives.Add(directive)

                Do
                    Dim poppedDirective = _ifStack.Pop()
                    condDirectives.Add(poppedDirective)
                    If poppedDirective.VBKind = SyntaxKind.IfDirectiveTrivia Then
                        Exit Do
                    End If
                Loop Until _ifStack.IsEmpty()

                condDirectives.Sort(Function(n1, n2) n1.SpanStart.CompareTo(n2.SpanStart))

                For Each cond In condDirectives
                    _conditionalMap.Add(cond, condDirectives)
                Next

                ' #If should be the first one in sorted order
                Dim ifDirective = condDirectives.First()
                Contract.Assert(ifDirective.VBKind = SyntaxKind.IfDirectiveTrivia OrElse
                            ifDirective.VBKind = SyntaxKind.ElseIfDirectiveTrivia OrElse
                            ifDirective.VBKind = SyntaxKind.ElseDirectiveTrivia)

                _startEndMap.Add(directive, ifDirective)
                _startEndMap.Add(ifDirective, directive)
            End If

            MyBase.VisitEndIfDirectiveTrivia(directive)
        End Sub

        Public Overrides Sub VisitEndRegionDirectiveTrivia(directive As EndRegionDirectiveTriviaSyntax)
            If Not _regionStack.IsEmpty() Then
                Dim previousDirective = _regionStack.Pop()

                _startEndMap.Add(directive, previousDirective)
                _startEndMap.Add(previousDirective, directive)
            End If

            MyBase.VisitEndRegionDirectiveTrivia(directive)
        End Sub

        Friend Sub Finish()
            While _regionStack.Count > 0
                _startEndMap.Add(_regionStack.Pop(), Nothing)
            End While
        End Sub
    End Class
End Namespace