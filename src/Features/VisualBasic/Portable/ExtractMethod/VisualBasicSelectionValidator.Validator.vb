' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicSelectionValidator
        Public Shared Function Check(semanticModel As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            Return node.TypeSwitch(
                Function(expression As ExpressionSyntax) CheckExpression(semanticModel, expression, cancellationToken),
                Function(statement As StatementSyntax) CheckStatement(semanticModel, statement, cancellationToken))
        End Function

        Private Shared Function CheckExpression(semanticModel As SemanticModel, expression As ExpressionSyntax, cancellationToken As CancellationToken) As Boolean
            cancellationToken.ThrowIfCancellationRequested()

            ' TODO(cyrusn): This is probably unnecessary.  What we should be doing is binding
            ' the type of the expression and seeing if it contains an anonymous type.
            If TypeOf expression Is AnonymousObjectCreationExpressionSyntax Then
                Return False
            End If

            Return expression.CanReplaceWithRValue(semanticModel, cancellationToken) AndAlso Not expression.ContainsImplicitMemberAccess()
        End Function

        Private Shared Function CheckStatement(semanticModel As SemanticModel, statement As StatementSyntax, cancellationToken As CancellationToken) As Boolean
            If statement.GetAncestor(Of WithBlockSyntax)() IsNot Nothing Then
                If statement.ContainsImplicitMemberAccess() Then
                    Return False
                End If
            End If

            ' don't support malformed code (bug # 10875)
            Dim localDeclaration = TryCast(statement, LocalDeclarationStatementSyntax)
            If localDeclaration IsNot Nothing AndAlso localDeclaration.Declarators.Any(Function(d) d.Names.Count > 1 AndAlso d.Initializer IsNot Nothing) Then
                Return False
            End If

            If TypeOf statement Is WhileBlockSyntax OrElse
               TypeOf statement Is UsingBlockSyntax OrElse
               TypeOf statement Is WithBlockSyntax OrElse
               TypeOf statement Is ReturnStatementSyntax OrElse
               TypeOf statement Is SingleLineIfStatementSyntax OrElse
               TypeOf statement Is MultiLineIfBlockSyntax OrElse
               TypeOf statement Is TryBlockSyntax OrElse
               TypeOf statement Is ErrorStatementSyntax OrElse
               TypeOf statement Is SelectBlockSyntax OrElse
               TypeOf statement Is DoLoopBlockSyntax OrElse
               TypeOf statement Is ForOrForEachBlockSyntax OrElse
               TypeOf statement Is ThrowStatementSyntax OrElse
               TypeOf statement Is AssignmentStatementSyntax OrElse
               TypeOf statement Is CallStatementSyntax OrElse
               TypeOf statement Is ExpressionStatementSyntax OrElse
               TypeOf statement Is AddRemoveHandlerStatementSyntax OrElse
               TypeOf statement Is RaiseEventStatementSyntax OrElse
               TypeOf statement Is ReDimStatementSyntax OrElse
               TypeOf statement Is EraseStatementSyntax OrElse
               TypeOf statement Is LocalDeclarationStatementSyntax OrElse
               TypeOf statement Is SyncLockBlockSyntax Then
                Return True
            End If

            Return False
        End Function
    End Class
End Namespace
