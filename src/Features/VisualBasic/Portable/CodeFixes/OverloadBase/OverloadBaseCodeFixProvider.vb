' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.OverloadBase
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddOverloads), [Shared]>
    Partial Friend Class OverloadBaseCodeFixProvider
        Inherits CodeFixProvider

        Friend Const BC40003 As String = "BC40003" ' '{0} '{1}' shadows an overloadable member declared in the base class '{2}'.  If you want to overload the base method, this method must be declared 'Overloads'.

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC40003)
            End Get
        End Property

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)

            Dim diagnostic = context.Diagnostics.First()
            Dim diagnosticSpan = diagnostic.Location.SourceSpan

            Dim token = root.FindToken(diagnosticSpan.Start)

            If TypeOf token.Parent IsNot PropertyStatementSyntax AndAlso TypeOf token.Parent IsNot MethodStatementSyntax Then
                Return
            End If

            Dim newNode = GetNewNode(context.Document, token.Parent, context.CancellationToken)

            context.RegisterCodeFix(New AddOverloadsKeywordAction(context.Document, token.Parent, newNode), context.Diagnostics)
        End Function

        Private Function GetNewNode(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As SyntaxNode
            Dim newNode As SyntaxNode = Nothing

            Dim propertyStatement = TryCast(node, PropertyStatementSyntax)
            If propertyStatement IsNot Nothing Then
                newNode = propertyStatement.AddModifiers(SyntaxFactory.Token(SyntaxKind.OverloadsKeyword))
            End If

            Dim methodStatement = TryCast(node, MethodStatementSyntax)
            If methodStatement IsNot Nothing Then
                newNode = methodStatement.AddModifiers(SyntaxFactory.Token(SyntaxKind.OverloadsKeyword))
            End If

            Dim cleanupService = document.GetLanguageService(Of ICodeCleanerService)

            If cleanupService IsNot Nothing AndAlso newNode IsNot Nothing Then
                newNode = cleanupService.Cleanup(newNode, {newNode.Span}, document.Project.Solution.Workspace, cleanupService.GetDefaultProviders(), cancellationToken)
            End If

            Return newNode
        End Function
    End Class
End Namespace