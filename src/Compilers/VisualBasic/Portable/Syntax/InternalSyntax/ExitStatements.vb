' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

' These methods are to maintain backwards compatibility with existing Public APIs.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Namespace Syntax

        Partial Public Class ContinueStatementSyntax

            Public Function Update(kind As SyntaxKind, continueKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ContinueStatementSyntax
                Return Update(kind, continueKeyword, blockKeyword, Nothing)
            End Function

        End Class

        Partial Public Class ExitStatementSyntax

            Function Update(kind As SyntaxKind, exitKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ExitStatementSyntax
                Return Update(kind, exitKeyword, blockKeyword, Nothing)
            End Function

        End Class

    End Namespace

    Partial Public Class SyntaxFactory

        Public Shared Function ExitPropertyStatement(exitKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ExitStatementSyntax
            Return SyntaxFactory.ExitPropertyStatement(exitKeyword, blockKeyword, nothing)
        End Function

        Public Shared Function ContinueDoStatement(continueKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ContinueStatementSyntax
            Return SyntaxFactory.ContinueDoStatement(continueKeyword, blockKeyword, Nothing)
        End Function

        Public Shared Function ContinueForStatement(continueKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ContinueStatementSyntax
            Return SyntaxFactory.ContinueForStatement(continueKeyword, blockKeyword, Nothing)
        End Function

        Public Shared Function ContinueStatement(kind As SyntaxKind, continueKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ContinueStatementSyntax
            Return SyntaxFactory.ContinueStatement(kind, continueKeyword, blockKeyword, Nothing)
        End Function

        Public Shared Function ContinueWhileStatement(continueKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ContinueStatementSyntax
            Return SyntaxFactory.ContinueWhileStatement(continueKeyword, blockKeyword, Nothing)
        End Function

        Public Shared Function ExitDoStatement(exitKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ExitStatementSyntax
            Return SyntaxFactory.ExitDoStatement(exitKeyword, blockKeyword, Nothing)
        End Function

        Public Shared Function ExitForStatement(exitKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ExitStatementSyntax
            Return SyntaxFactory.ExitDoStatement(exitKeyword, blockKeyword, Nothing)
        End Function

        Public Shared Function ExitFunctionStatement(exitKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ExitStatementSyntax
            Return SyntaxFactory.ExitFunctionStatement(exitKeyword, blockKeyword, Nothing)
        End Function

        Public Shared Function ExitOperatorStatement(exitKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ExitStatementSyntax
            Return SyntaxFactory.ExitOperatorStatement(exitKeyword, blockKeyword, Nothing)
        End Function

        Public Shared Function ExitSelectStatement(exitKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ExitStatementSyntax
            Return SyntaxFactory.ExitSelectStatement(exitKeyword, blockKeyword, Nothing)
        End Function

        Public Shared Function ExitStatement(kind As SyntaxKind, exitKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ExitStatementSyntax
            Return SyntaxFactory.ExitStatement(kind,exitKeyword, blockKeyword, Nothing)
        End Function

        Public Shared Function ExitSubStatement(exitKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ExitStatementSyntax
            Return SyntaxFactory.ExitSubStatement(exitKeyword, blockKeyword, Nothing)
        End Function

        Public Shared Function ExitTryStatement(exitKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ExitStatementSyntax
            Return SyntaxFactory.ExitTryStatement(exitKeyword, blockKeyword, Nothing)
        End Function

        Public Shared Function ExitWhileStatement(exitKeyword As SyntaxToken, blockKeyword As SyntaxToken) As ExitStatementSyntax
            Return SyntaxFactory.ExitWhileStatement(exitKeyword, blockKeyword, Nothing)
        End Function

    End Class

End Namespace
