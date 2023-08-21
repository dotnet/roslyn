﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.UseCollectionInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer
    Friend NotInheritable Class VisualBasicCollectionInitializerAnalyzer
        Inherits AbstractUseCollectionInitializerAnalyzer(Of
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            VariableDeclaratorSyntax,
            VisualBasicCollectionInitializerAnalyzer)

        Protected Overrides ReadOnly Property SyntaxHelper As IUpdateExpressionSyntaxHelper(Of ExpressionSyntax, StatementSyntax) =
            VisualBasicUpdateExpressionSyntaxHelper.Instance

        Protected Overrides Function IsComplexElementInitializer(expression As SyntaxNode) As Boolean
            ' Only called for collection expressions, which VB does not support
            Throw ExceptionUtilities.Unreachable()
        End Function

        Protected Overrides Function HasExistingInvalidInitializerForCollection(objectCreation As ObjectCreationExpressionSyntax) As Boolean
            ' In VB we cannot add a `From { }` initializer to an object if it already has a `With { }` initializer.
            Return TypeOf objectCreation.Initializer Is ObjectMemberInitializerSyntax
        End Function
    End Class
End Namespace
