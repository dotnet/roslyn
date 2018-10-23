' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SplitOrMergeIfStatements

Namespace Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements
    <ExportLanguageService(GetType(IIfStatementSyntaxService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicIfStatementSyntaxService
        Implements IIfStatementSyntaxService

        Public ReadOnly Property IfKeywordKind As Integer =
            SyntaxKind.IfKeyword Implements IIfStatementSyntaxService.IfKeywordKind

        Public ReadOnly Property LogicalAndExpressionKind As Integer =
            SyntaxKind.AndAlsoExpression Implements IIfStatementSyntaxService.LogicalAndExpressionKind

        Public ReadOnly Property LogicalOrExpressionKind As Integer =
            SyntaxKind.OrElseExpression Implements IIfStatementSyntaxService.LogicalOrExpressionKind
    End Class
End Namespace
