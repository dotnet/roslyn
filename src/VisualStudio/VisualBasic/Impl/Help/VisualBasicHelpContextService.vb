' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.Implementation.F1Help

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Help
    <ExportLanguageService(GetType(IHelpContextService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicHelpContextService
        Inherits AbstractHelpContextService

        Public Overrides ReadOnly Property Language As String
            Get
                Return "VB"
            End Get
        End Property

        Public Overrides ReadOnly Property Product As String
            Get
                Return "VB"
            End Get
        End Property

        Public Overrides Async Function GetHelpTermAsync(document As Document, span As TextSpan, cancellationToken As CancellationToken) As Task(Of String)
            Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim token = tree.GetRoot(cancellationToken).FindToken(span.Start, findInsideTrivia:=True)

            If TokenIsHelpKeyword(token) Then
                Return "vb." + token.Text
            End If

            If token.Span.IntersectsWith(span) OrElse token.GetAncestor(Of XmlElementSyntax)() IsNot Nothing Then
                Dim visitor = New Visitor(token.Span, Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False), document.Project.Solution.Workspace.Kind <> WorkspaceKind.MetadataAsSource, Me, cancellationToken)
                visitor.Visit(token.Parent)
                Return visitor.result
            End If

            Dim trivia = tree.GetRoot().FindTrivia(span.Start, findInsideTrivia:=True)

            Dim text = If(trivia.ToFullString(), String.Empty).Replace(" ", "").TrimStart("'"c)
            If text.StartsWith("TODO:", StringComparison.CurrentCultureIgnoreCase) Then
                Return HelpKeywords.TaskListUserComments
            End If

            If trivia.IsKind(SyntaxKind.CommentTrivia) Then
                Return "vb.Rem"
            End If

            Return String.Empty
        End Function

        Private Function TokenIsHelpKeyword(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.SharedKeyword, SyntaxKind.WideningKeyword, SyntaxKind.CTypeKeyword, SyntaxKind.NarrowingKeyword,
                                     SyntaxKind.OperatorKeyword, SyntaxKind.AddHandlerKeyword, SyntaxKind.RemoveHandlerKeyword, SyntaxKind.AnsiKeyword,
                                     SyntaxKind.AutoKeyword, SyntaxKind.UnicodeKeyword, SyntaxKind.HandlesKeyword, SyntaxKind.NotKeyword)
        End Function
    End Class
End Namespace
