' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend MustInherit Class SingleLineIfOrElseBlockContext
        Inherits ExecutableStatementContext

        Protected Sub New(kind As SyntaxKind, statement As StatementSyntax, prevContext As BlockContext)
            MyBase.New(kind, statement, prevContext)
        End Sub

        Friend Overrides Function ResyncAndProcessStatementTerminator(statement As StatementSyntax, lambdaContext As BlockContext) As BlockContext
            Return ProcessStatementTerminator(lambdaContext)
        End Function

        Friend Overrides ReadOnly Property IsSingleLine As Boolean
            Get
                Return True
            End Get
        End Property
    End Class

End Namespace
