' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module DirectiveSyntaxExtensions
        Private ReadOnly s_rootToDirectiveInfo As New ConditionalWeakTable(Of SyntaxNode, DirectiveInfo(Of DirectiveTriviaSyntax))()

        Private Function GetDirectiveInfo(node As SyntaxNode, cancellationToken As CancellationToken) As DirectiveInfo(Of DirectiveTriviaSyntax)
            Return s_rootToDirectiveInfo.GetValue(node.GetAbsoluteRoot(), Function(r) GetDirectiveInfoForRoot(r, cancellationToken))
        End Function

        Private Function GetDirectiveInfoForRoot(root As SyntaxNode, cancellationToken As CancellationToken) As DirectiveInfo(Of DirectiveTriviaSyntax)
            Return [Shared].Extensions.SyntaxNodeExtensions.GetDirectiveInfoForRoot(Of DirectiveTriviaSyntax)(
                root, VisualBasicSyntaxKinds.Instance, cancellationToken)
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
            Return GetDirectiveInfo(syntaxTree.GetRoot(cancellationToken), cancellationToken).DirectiveMap.Keys.Where(
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
            GetDirectiveInfo(directive, cancellationToken).DirectiveMap.TryGetValue(directive, result)
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
