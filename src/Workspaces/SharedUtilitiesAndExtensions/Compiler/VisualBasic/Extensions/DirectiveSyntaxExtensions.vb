' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Threading
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
            Dim root = node.GetAbsoluteRoot()
            Dim info = s_rootToDirectiveInfo.GetValue(root,
                Function(r)
                    Dim startEndMap = New Dictionary(Of DirectiveTriviaSyntax, DirectiveTriviaSyntax)(DirectiveSyntaxEqualityComparer.Instance)
                    Dim conditionalMap = New Dictionary(Of DirectiveTriviaSyntax, ImmutableArray(Of DirectiveTriviaSyntax))(DirectiveSyntaxEqualityComparer.Instance)
                    Dim walker = New DirectiveWalker(startEndMap, conditionalMap, cancellationToken)
                    walker.Visit(r)
                    walker.Finish()
                    Return New DirectiveInfo(startEndMap, conditionalMap, inactiveRegionLines:=Nothing)
                End Function)
            Return info
        End Function

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
