' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Projection
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Microsoft.VisualStudio.Utilities
Imports TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    Friend Class VisualBasicDebuggerIntelliSenseContext
        Inherits AbstractDebuggerIntelliSenseContext

        Private _innerMostContainingNodeIsExpression As Boolean

        Public Sub New(wpfTextView As IWpfTextView,
                vsTextView As IVsTextView,
                debuggerBuffer As IVsTextLines,
                contextBuffer As ITextBuffer,
                currentStatementSpan As TextSpan(),
                componentModel As IComponentModel,
                serviceProvider As IServiceProvider)

            MyBase.New(
                wpfTextView,
                vsTextView,
                debuggerBuffer,
                contextBuffer,
                currentStatementSpan,
                componentModel,
                serviceProvider,
                componentModel.GetService(Of IContentTypeRegistryService).GetContentType(ContentTypeNames.VisualBasicContentType))
        End Sub

        ' Test constructor
        Public Sub New(wpfTextView As IWpfTextView,
                textBuffer As ITextBuffer,
                span As TextSpan(),
                componentModel As IComponentModel,
                isImmediateWindow As Boolean)

            MyBase.New(
                wpfTextView,
                textBuffer,
                span,
                componentModel,
                componentModel.GetService(Of IContentTypeRegistryService).GetContentType(ContentTypeNames.VisualBasicContentType),
                isImmediateWindow)
        End Sub

        Protected Overrides Function GetAdjustedContextPoint(contextPoint As Integer, document As Document) As Integer
            Dim tree = document.GetSyntaxTreeSynchronously(CancellationToken.None)
            Dim token = tree.FindTokenOnLeftOfPosition(contextPoint, CancellationToken.None)

            Dim containingNode = token.Parent.AncestorsAndSelf().Where(Function(s) TypeOf s Is ExpressionSyntax OrElse
                                                                            TypeOf s Is MethodBaseSyntax OrElse
                                                                             s.IsExecutableBlock()).FirstOrDefault()
            If containingNode IsNot Nothing Then
                If TypeOf containingNode Is ExpressionSyntax AndAlso Not IsRightSideOfLocalDeclaration(containingNode) Then
                    _innerMostContainingNodeIsExpression = True
                    Return containingNode.Span.End
                Else
                    Dim statement = containingNode.GetExecutableBlockStatements().FirstOrDefault()
                    If statement IsNot Nothing Then
                        Return statement.FullSpan.End
                    ElseIf TypeOf containingNode Is MethodBlockBaseSyntax
                        ' Something like
                        ' Sub Goo(o as integer)
                        ' [| End Sub |]
                        Return DirectCast(containingNode, MethodBlockBaseSyntax).EndBlockStatement.SpanStart
                    Else
                        Return containingNode.Span.End
                    End If
                End If
            End If

            Return token.FullSpan.End
        End Function

        Private Shared Function IsRightSideOfLocalDeclaration(containingNode As SyntaxNode) As Boolean
            ' Right side of a variable declaration but not inside a lambda or query clause
            Dim variableDeclarator = containingNode.GetAncestor(Of VariableDeclaratorSyntax)
            If variableDeclarator IsNot Nothing Then
                Dim methodBase = containingNode.GetAncestor(Of LambdaExpressionSyntax)()
                Dim queryClause = containingNode.GetAncestor(Of QueryClauseSyntax)()

                If (methodBase Is Nothing OrElse methodBase.DescendantNodes().Contains(variableDeclarator)) AndAlso
                    (queryClause Is Nothing OrElse queryClause.DescendantNodes().Contains(variableDeclarator)) Then

                    Dim equalsValueClause = containingNode.GetAncestor(Of EqualsValueSyntax)
                    Return equalsValueClause.IsChildNode(Of VariableDeclaratorSyntax)(Function(v) v.Initializer)
                End If
            End If

            Return False
        End Function

        Protected Overrides Function GetPreviousStatementBufferAndSpan(contextPoint As Integer, document As Document) As ITrackingSpan
            ' This text can be validly inserted at the end of an expression context to allow
            ' intellisense to trigger a new expression context
            Dim forceExpressionContext = ".__o("

            If Not _innerMostContainingNodeIsExpression Then
                ' We're after some statement, could be a for loop, using block, try block, etc, fake a
                ' local declaration on the following line
                forceExpressionContext = vbCrLf + "Dim __o = "
            End If

            ' Since VB is line-based, we're going to add 
            Dim previousTrackingSpan = ContextBuffer.CurrentSnapshot.CreateTrackingSpan(Span.FromBounds(0, contextPoint), SpanTrackingMode.EdgeNegative)

            Dim buffer = ProjectionBufferFactoryService.CreateProjectionBuffer(
                projectionEditResolver:=Nothing,
                sourceSpans:={previousTrackingSpan, forceExpressionContext},
                options:=ProjectionBufferOptions.None,
                contentType:=Me.ContentType)

            Return buffer.CurrentSnapshot.CreateTrackingSpan(0, buffer.CurrentSnapshot.Length, SpanTrackingMode.EdgeNegative)
        End Function

        Public Overrides ReadOnly Property CompletionStartsOnQuestionMark As Boolean
            Get
                Return True
            End Get
        End Property

        Protected Overrides ReadOnly Property StatementTerminator As String
            Get
                Return vbCrLf
            End Get
        End Property
    End Class
End Namespace
