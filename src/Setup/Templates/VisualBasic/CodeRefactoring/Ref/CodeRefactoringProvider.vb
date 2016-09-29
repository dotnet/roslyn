<ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf($saferootidentifiername$CodeRefactoringProvider)), [Shared]>
Friend Class $saferootidentifiername$CodeRefactoringProvider
    Inherits CodeRefactoringProvider

    Public NotOverridable Overrides Async Function ComputeRefactoringsAsync(context As CodeRefactoringContext) As Task
        ' TODO: Replace the following code with your own analysis, generating a CodeAction for each refactoring to offer

        Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)

        ' Find the node at the selection.
        Dim node = root.FindNode(context.Span)

        ' Only offer a refactoring if the selected node is a type statement node.
        Dim typeDecl = TryCast(node, TypeStatementSyntax)
        If typeDecl Is Nothing Then
            Return
        End If

        ' For any type statement node, create a code action to reverse the identifier text.
        Dim action = CodeAction.Create("Reverse type name", Function(c) ReverseTypeNameAsync(context.Document, typeDecl, c))

        ' Register this code action.
        context.RegisterRefactoring(action)
    End Function

    Private Async Function ReverseTypeNameAsync(document As Document, typeStmt As TypeStatementSyntax, cancellationToken As CancellationToken) As Task(Of Solution)
        ' Produce a reversed version of the type statement's identifier token.
        Dim identifierToken = typeStmt.Identifier
        Dim newName = New String(identifierToken.Text.ToCharArray.Reverse.ToArray)

        ' Get the symbol representing the type to be renamed.
        Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken)
        Dim typeSymbol = semanticModel.GetDeclaredSymbol(typeStmt, cancellationToken)

        ' Produce a new solution that has all references to that type renamed, including the declaration.
        Dim originalSolution = document.Project.Solution
        Dim optionSet = originalSolution.Workspace.Options
        Dim newSolution = Await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(False)

        ' Return the new solution with the now-uppercase type name.
        Return newSolution
    End Function
End Class