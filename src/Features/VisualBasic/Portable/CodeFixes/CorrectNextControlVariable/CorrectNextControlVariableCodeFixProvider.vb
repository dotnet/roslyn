' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.CorrectNextControlVariable
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.CorrectNextControlVariable), [Shared]>
    Partial Friend Class CorrectNextControlVariableCodeFixProvider
        Inherits CodeFixProvider

        Friend Const BC30070 As String = "BC30070" ' Next control variable does not match For loop control variable 'x'.
        Friend Const BC30451 As String = "BC30451" 'BC30451: 'y' is not declared. It may be inaccessible due to its protection level.

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30070, BC30451)
            End Get
        End Property

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            ' Fix All is not supported by this code fix
            ' https://github.com/dotnet/roslyn/issues/34470
            Return Nothing
        End Function

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)

            Dim node = root.FindNode(context.Span, getInnermostNodeForTie:=True)
            Dim nextStatement = node.FirstAncestorOrSelf(Of NextStatementSyntax)()

            If node Is Nothing OrElse nextStatement Is Nothing Then
                Return
            End If

            ' A Next statement could have multiple control variables. Find the index of the variable so that we 
            ' can find the correct nested ForBlock and it's control variable.

            ' The span of the diagnostic could be over just part of the controlvariable in case of unbound identifiers
            ' and so find the full expression for the control variable to be replaced.
            Dim indexOfControlVariable = nextStatement.ControlVariables.IndexOf(Function(n) n.Span.Contains(context.Span))
            If indexOfControlVariable = -1 Then
                Return
            End If

            Dim nodeToReplace = nextStatement.ControlVariables(indexOfControlVariable)
            Dim controlVariable = FindControlVariable(nextStatement, indexOfControlVariable)
            If controlVariable Is Nothing Then
                Return
            End If

            Dim newNode = SyntaxFactory.IdentifierName(controlVariable.Value).
                                    WithLeadingTrivia(nodeToReplace.GetLeadingTrivia()).
                                    WithTrailingTrivia(nodeToReplace.GetTrailingTrivia())

            context.RegisterCodeFix(New CorrectNextControlVariableCodeAction(context.Document, nodeToReplace, newNode), context.Diagnostics)
        End Function

        Private Function FindControlVariable(nextStatement As NextStatementSyntax, nestingLevel As Integer) As SyntaxToken?
            Debug.Assert(nestingLevel >= 0)

            ' If we have code like this:
            ' For Each x In {1,2}
            '     For Each y in {1,3}
            '     Next y, x
            ' The Next statement is attached to the innermost for block. Starting from that block, the nesting level
            ' is the number of loops that we have to step up.
            Dim currentNode As SyntaxNode = nextStatement
            Dim forBlock As ForOrForEachBlockSyntax = Nothing
            For i = 0 To nestingLevel
                forBlock = currentNode.GetAncestor(Of ForOrForEachBlockSyntax)()
                If forBlock Is Nothing Then
                    Return Nothing
                End If
                currentNode = forBlock
            Next

            ' A ForBlockSyntax can either be a ForBlock or a ForEachBlock. Get the control variable
            ' from that.
            Dim controlVariable As SyntaxNode = Nothing
            Select Case forBlock.Kind()
                Case SyntaxKind.ForBlock
                    Dim forStatement = DirectCast(forBlock.ForOrForEachStatement, ForStatementSyntax)
                    controlVariable = forStatement.ControlVariable
                    Exit Select
                Case SyntaxKind.ForEachBlock
                    Dim forEachStatement = DirectCast(forBlock.ForOrForEachStatement, ForEachStatementSyntax)
                    controlVariable = forEachStatement.ControlVariable
                    Exit Select
                Case Else
                    Debug.Assert(False, "Unknown next statement")
                    Return Nothing
            End Select

            ' The control variable can either be:
            ' For x = 1 to 10
            ' For x As Integer = 1 to 10
            Select Case controlVariable.Kind()
                Case SyntaxKind.IdentifierName
                    Return DirectCast(controlVariable, IdentifierNameSyntax).Identifier
                Case SyntaxKind.VariableDeclarator
                    Return DirectCast(controlVariable, VariableDeclaratorSyntax).Names.Single().Identifier
                Case Else
                    Debug.Assert(False, "Unknown control variable expression")
                    Return Nothing
            End Select
        End Function
    End Class
End Namespace
