Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings
    <ExportLanguageService(GetType(IRefactoringHelpersService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicRefactoringHelpersService
        Inherits RefactoringHelpersService

        Public Overrides Function ExtractNodeFromDeclarationAndAssignment(Of TNode As SyntaxNode)(current As SyntaxNode) As SyntaxNode
            If TypeOf current Is LocalDeclarationStatementSyntax Then
                Dim localDeclaration = CType(current, LocalDeclarationStatementSyntax)
                If localDeclaration.Declarators.Count = 1 And localDeclaration.Declarators.First.Initializer IsNot Nothing Then
                    Dim initilizer = localDeclaration.Declarators.First.Initializer
                    Return TryCast(initilizer, TNode)
                End If
            ElseIf TypeOf current Is AssignmentStatementSyntax Then
                Dim assignmentStatement = CType(current, AssignmentStatementSyntax)
                If TypeOf assignmentStatement.Right Is TNode Then
                    Return TryCast(assignmentStatement.Right, TNode)
                End If
            End If

            Return current
        End Function
    End Class
End Namespace
