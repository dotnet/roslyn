' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend NotInheritable Class MultiLineIfBlockStructureProvider
        Inherits InternalStructureBlockStructureProvider(Of
            MultiLineIfBlockSyntax,
            IfStatementSyntax,
            ElseIfBlockSyntax,
            ElseBlockSyntax,
            ElseStatementSyntax,
            EndBlockStatementSyntax)

        Friend Overrides Function HeaderOfFullBlock(ifBlock As MultiLineIfBlockSyntax) As IfStatementSyntax
            Return ifBlock.IfStatement
        End Function

        Friend Overrides Function GetInnerBlocks(ifBlock As MultiLineIfBlockSyntax) As SyntaxList(Of ElseIfBlockSyntax)
            Return ifBlock.ElseIfBlocks
        End Function

        Friend Overrides Function InnerBlock_Text([ElseIf] As ElseIfBlockSyntax) As String
            Return [ElseIf].ElseIfStatement.ToString
        End Function

        Friend Overrides Function Epilogue(ifBlock As MultiLineIfBlockSyntax) As ElseBlockSyntax
            Return ifBlock.ElseBlock
        End Function

        Friend Overrides Function Epilogue_Text(epilogueNode As ElseStatementSyntax) As String
            Return epilogueNode.ElseKeyword.Text
        End Function

        Friend Overrides Function Epilogue_Statement(epilogueNode As ElseBlockSyntax) As ElseStatementSyntax
            Return epilogueNode.ElseStatement
        End Function

        Friend Overrides Function EndOfBlockStatement(ifBlock As MultiLineIfBlockSyntax) As EndBlockStatementSyntax
            Return ifBlock.EndIfStatement
        End Function
    End Class
End Namespace
