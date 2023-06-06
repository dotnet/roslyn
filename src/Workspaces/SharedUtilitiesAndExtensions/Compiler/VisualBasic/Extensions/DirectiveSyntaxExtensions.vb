' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Module DirectiveSyntaxExtensions
        Private Class DirectiveInfo
            ''' <summary>
            ''' Returns a map which maps from a DirectiveTriviaSyntax to it's corresponding start/end directive.
            ''' Directives like #ElseIf which exist in the middle of a start/end pair are not included.
            ''' </summary>
            Public ReadOnly StartEndMap As Dictionary(Of DirectiveTriviaSyntax, DirectiveTriviaSyntax)

            ''' <summary>
            ''' Maps a #If/#ElseIf/#Else/#EndIf directive to its list of matching #If/#ElseIf/#Else/#End directives.
            ''' </summary>
            Public ReadOnly ConditionalMap As Dictionary(Of DirectiveTriviaSyntax, ImmutableArray(Of DirectiveTriviaSyntax))

            ' A set of inactive regions spans.  The items in the tuple are the 
            ' start and end line *both inclusive* of the inactive region.  
            ' Actual PP lines are not continued within.
            '
            ' Note: an interval syntaxTree might be a better structure here if there are lots of
            ' inactive regions.  Consider switching to that if necessary.
            Private ReadOnly _inactiveRegionLines As ISet(Of Tuple(Of Integer, Integer))
            Public ReadOnly Property InactiveRegionLines() As ISet(Of Tuple(Of Integer, Integer))
                Get
                    Return _inactiveRegionLines
                End Get
            End Property

            Public Sub New(startEndMap As Dictionary(Of DirectiveTriviaSyntax, DirectiveTriviaSyntax),
                           conditionalMap As Dictionary(Of DirectiveTriviaSyntax, ImmutableArray(Of DirectiveTriviaSyntax)),
                           inactiveRegionLines As ISet(Of Tuple(Of Integer, Integer)))
                Me.StartEndMap = startEndMap
                Me.ConditionalMap = conditionalMap
                _inactiveRegionLines = inactiveRegionLines
            End Sub
        End Class

        Private ReadOnly s_rootToDirectiveInfo As New ConditionalWeakTable(Of SyntaxNode, DirectiveInfo)()

        Private Function GetDirectiveInfo(node As SyntaxNode, cancellationToken As CancellationToken) As DirectiveInfo
            Return s_rootToDirectiveInfo.GetValue(node.GetAbsoluteRoot(), Function(r) GetDirectiveInfoForRoot(r, cancellationToken))
        End Function

        Private Function GetDirectiveInfoForRoot(root As SyntaxNode, cancellationToken As CancellationToken) As DirectiveInfo
            Dim startEndMap = New Dictionary(Of DirectiveTriviaSyntax, DirectiveTriviaSyntax)(DirectiveSyntaxEqualityComparer.Instance)
            Dim conditionalMap = New Dictionary(Of DirectiveTriviaSyntax, ImmutableArray(Of DirectiveTriviaSyntax))(DirectiveSyntaxEqualityComparer.Instance)

            Dim regionStack As New Stack(Of DirectiveTriviaSyntax)()
            Dim ifStack As New Stack(Of DirectiveTriviaSyntax)()

            For Each token In root.DescendantTokens(Function(n) n.ContainsDirectives)
                cancellationToken.ThrowIfCancellationRequested()

                If Not token.ContainsDirectives Then
                    Continue For
                End If

                For Each trivia In token.LeadingTrivia
                    Dim directive = TryCast(trivia.GetStructure(), DirectiveTriviaSyntax)

                    If TypeOf directive Is IfDirectiveTriviaSyntax Then
                        ifStack.Push(directive)
                    ElseIf TypeOf directive Is ElseDirectiveTriviaSyntax Then
                        ifStack.Push(directive)
                    ElseIf TypeOf directive Is RegionDirectiveTriviaSyntax Then
                        regionStack.Push(directive)
                    ElseIf TypeOf directive Is EndIfDirectiveTriviaSyntax Then
                        FinishIf(startEndMap, conditionalMap, ifStack, directive)
                    ElseIf TypeOf directive Is EndRegionDirectiveTriviaSyntax Then
                        If Not regionStack.IsEmpty() Then
                            Dim previousDirective = regionStack.Pop()

                            startEndMap.Add(directive, previousDirective)
                            startEndMap.Add(previousDirective, directive)
                        End If
                    End If
                Next
            Next

            While regionStack.Count > 0
                startEndMap.Add(regionStack.Pop(), Nothing)
            End While

            While ifStack.Count > 0
                FinishIf(startEndMap, conditionalMap, ifStack, directiveOpt:=Nothing)
            End While

            Return New DirectiveInfo(startEndMap, conditionalMap, inactiveRegionLines:=Nothing)
        End Function

        Private Sub FinishIf(
                startEndMap As Dictionary(Of DirectiveTriviaSyntax, DirectiveTriviaSyntax),
                conditionalMap As Dictionary(Of DirectiveTriviaSyntax, ImmutableArray(Of DirectiveTriviaSyntax)),
                ifStack As Stack(Of DirectiveTriviaSyntax),
                directiveOpt As DirectiveTriviaSyntax)
            If ifStack.IsEmpty() Then
                Return
            End If

            Dim condDirectivesBuilder = ArrayBuilder(Of DirectiveTriviaSyntax).GetInstance()
            If directiveOpt IsNot Nothing Then
                condDirectivesBuilder.Add(directiveOpt)
            End If

            Do
                Dim poppedDirective = ifStack.Pop()
                condDirectivesBuilder.Add(poppedDirective)
                If poppedDirective.Kind = SyntaxKind.IfDirectiveTrivia Then
                    Exit Do
                End If
            Loop Until ifStack.IsEmpty()

            condDirectivesBuilder.Sort(Function(n1, n2) n1.SpanStart.CompareTo(n2.SpanStart))
            Dim condDirectives = condDirectivesBuilder.ToImmutableAndFree()

            For Each cond In condDirectives
                conditionalMap.Add(cond, condDirectives)
            Next

            ' #If should be the first one in sorted order
            Dim ifDirective = condDirectives.First()
            Debug.Assert(ifDirective.Kind = SyntaxKind.IfDirectiveTrivia OrElse
                         ifDirective.Kind = SyntaxKind.ElseIfDirectiveTrivia OrElse
                         ifDirective.Kind = SyntaxKind.ElseDirectiveTrivia)

            If directiveOpt IsNot Nothing Then
                startEndMap.Add(directiveOpt, ifDirective)
                startEndMap.Add(ifDirective, directiveOpt)
            End If
        End Sub

        <Extension()>
        Private Function GetAbsoluteRoot(node As SyntaxNode) As SyntaxNode
            While node IsNot Nothing AndAlso (node.Parent IsNot Nothing OrElse TypeOf node Is StructuredTriviaSyntax)
                If node.Parent IsNot Nothing Then
                    node = node.Parent
                Else
                    node = node.ParentTrivia.Token.Parent
                End If
            End While

            Return node
        End Function

        <Extension()>
        Public Function GetStartDirectives(syntaxTree As SyntaxTree, cancellationToken As CancellationToken) As IEnumerable(Of DirectiveTriviaSyntax)
            Return GetDirectiveInfo(syntaxTree.GetRoot(cancellationToken), cancellationToken).StartEndMap.Keys.Where(
                Function(d) d.Kind = SyntaxKind.RegionDirectiveTrivia OrElse d.Kind = SyntaxKind.IfDirectiveTrivia)
        End Function

        ''' <summary>
        ''' Given a starting or ending directive, return the matching directive, if it exists. For directives that live
        ''' the "middle" of a start/end pair, such as #ElseIf or #Else, this method will throw.
        ''' </summary>
        <Extension()>
        Public Function GetMatchingStartOrEndDirective(directive As DirectiveTriviaSyntax,
                                                       cancellationToken As CancellationToken) As DirectiveTriviaSyntax
            If directive Is Nothing Then
                Throw New ArgumentNullException(NameOf(directive))
            End If

            If directive.Kind = SyntaxKind.ElseIfDirectiveTrivia OrElse directive.Kind = SyntaxKind.ElseDirectiveTrivia Then
                Throw New ArgumentException("directive cannot be a ElseIfDirective or ElseDirective.")
            End If

            Dim result As DirectiveTriviaSyntax = Nothing
            GetDirectiveInfo(directive, cancellationToken).StartEndMap.TryGetValue(directive, result)
            Return result
        End Function

        ''' <summary>
        ''' Given a conditional directive (#If, #ElseIf, #Else, or #End If), returns a IEnumerable of all directives in
        ''' the set.
        ''' </summary>
        <Extension>
        Public Function GetMatchingConditionalDirectives(directive As DirectiveTriviaSyntax,
                                                         cancellationToken As CancellationToken) As ImmutableArray(Of DirectiveTriviaSyntax)
            If directive Is Nothing Then
                Throw New ArgumentNullException(NameOf(directive))
            End If

            Dim result As ImmutableArray(Of DirectiveTriviaSyntax) = Nothing
            Return If(GetDirectiveInfo(directive, cancellationToken).ConditionalMap.TryGetValue(directive, result),
                result,
                ImmutableArray(Of DirectiveTriviaSyntax).Empty)
        End Function
    End Module
End Namespace
