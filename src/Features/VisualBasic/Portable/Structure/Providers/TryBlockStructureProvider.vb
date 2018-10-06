' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure

    Friend NotInheritable Class TryBlockStructureProvider
        Inherits InternalStructureBlockStructureProvider(Of
            TryBlockSyntax,
            TryStatementSyntax,
            CatchBlockSyntax,
            FinallyBlockSyntax,
            FinallyStatementSyntax,
            EndBlockStatementSyntax)

        Friend Overrides Function HeaderOfFullBlock(tryBlock As TryBlockSyntax) As TryStatementSyntax
            Return tryBlock.TryStatement
        End Function

        Friend Overrides Function GetInnerBlocks(tryBlock As TryBlockSyntax) As SyntaxList(Of CatchBlockSyntax)
            Return tryBlock.CatchBlocks
        End Function

        Friend Overrides Function InnerBlock_Text(catchBlock As CatchBlockSyntax) As String
            Return If(catchBlock IsNot Nothing,
                      If(catchBlock.CatchStatement IsNot Nothing, catchBlock.CatchStatement.ToString, String.Empty),
                      String.Empty)
        End Function

        Friend Overrides Function Epilogue_Statement(epilogueNode As FinallyBlockSyntax) As FinallyStatementSyntax
            Return epilogueNode.FinallyStatement
        End Function

        Friend Overrides Function Epilogue_Text(epilogueNode As FinallyStatementSyntax) As String
            Return epilogueNode.FinallyKeyword.Text
        End Function

        Friend Overrides Function Epilogue(tryBlock As TryBlockSyntax) As FinallyBlockSyntax
            Return tryBlock.FinallyBlock
        End Function

        Friend Overrides Function EndOfBlockStatement(tryBlock As TryBlockSyntax) As EndBlockStatementSyntax
            Return tryBlock.EndTryStatement
        End Function
    End Class
End Namespace
