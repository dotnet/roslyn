' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.UseObjectInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer

Namespace Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer
    Friend NotInheritable Class VisualBasicUseNamedMemberInitializerAnalyzer
        Inherits AbstractUseNamedMemberInitializerAnalyzer(Of
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            AssignmentStatementSyntax,
            LocalDeclarationStatementSyntax,
            VariableDeclaratorSyntax,
            VisualBasicUseNamedMemberInitializerAnalyzer)

        Protected Overrides Function IsInitializerOfLocalDeclarationStatement(localDeclarationStatement As LocalDeclarationStatementSyntax, rootExpression As ObjectCreationExpressionSyntax, ByRef variableDeclarator As VariableDeclaratorSyntax) As Boolean
            Return VisualBasicObjectCreationHelpers.IsInitializerOfLocalDeclarationStatement(localDeclarationStatement, rootExpression, variableDeclarator)
        End Function

        ' Visual Basic has no compound-in-initializer language feature; only `=` member initializers are
        ' valid in `With { ... }`, so subsequent compound expression-statements are never foldable.
        Protected Overrides Function SupportsCompoundAssignmentInInitializer(options As ParseOptions) As Boolean
            Return False
        End Function

        ' Visual Basic keeps object initializers (`With { ... }`) and collection initializers
        ' (`From { ... }`) strictly separate; the mixed object/collection initializer language
        ' feature is C#-only, so a subsequent `Add` invocation is never foldable into a `With` block.
        Protected Overrides Function SupportsMixedObjectAndCollectionInitializers(options As ParseOptions) As Boolean
            Return False
        End Function
    End Class
End Namespace
