' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.UseCollectionExpression
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
            AssignmentStatementSyntax,
            LocalDeclarationStatementSyntax,
            VariableDeclaratorSyntax,
            VisualBasicCollectionInitializerAnalyzer)

        Protected Overrides ReadOnly Property SyntaxHelper As IUpdateExpressionSyntaxHelper(Of ExpressionSyntax, StatementSyntax) =
            VisualBasicUpdateExpressionSyntaxHelper.Instance

        ' VB has no compound-in-initializer language feature; only `=` member initializers
        ' are valid in `With { ... }`, so subsequent compound expression-statements are never
        ' foldable. Pass 3 of the IDE0017+IDE0028 unification routes member-init detection
        ' through this walk for both languages; this hook keeps VB on the legacy behavior.
        Protected Overrides Function SupportsCompoundAssignmentInInitializer(options As ParseOptions) As Boolean
            Return False
        End Function

        ' VB keeps object initializers (`With { ... }`) and collection initializers
        ' (`From { ... }`) strictly separate; the mixed object/collection initializer feature
        ' (csharplang#10185) is C#-only.
        Protected Overrides Function SupportsMixedObjectAndCollectionInitializers(options As ParseOptions) As Boolean
            Return False
        End Function

        Protected Overrides Function IsInitializerOfLocalDeclarationStatement(localDeclarationStatement As LocalDeclarationStatementSyntax, rootExpression As ObjectCreationExpressionSyntax, ByRef variableDeclarator As VariableDeclaratorSyntax) As Boolean
            Return VisualBasicObjectCreationHelpers.IsInitializerOfLocalDeclarationStatement(localDeclarationStatement, rootExpression, variableDeclarator)
        End Function

        Protected Overrides Function IsComplexElementInitializer(expression As SyntaxNode, ByRef initializerElementCount As Integer) As Boolean
            ' Only called for collection expressions, which VB does not support
            Throw ExceptionUtilities.Unreachable()
        End Function

        Protected Overrides Function HasExistingInvalidInitializerForCollection() As Boolean
            ' In VB we cannot add a `From { }` initializer to an object if it already has a `With { }` initializer.
            Return TypeOf _objectCreationExpression.Initializer Is ObjectMemberInitializerSyntax
        End Function

        Protected Overrides Function AnalyzeMatchesAndCollectionConstructorForCollectionExpression(
                preMatches As ArrayBuilder(Of InitializerMatch(Of SyntaxNode)),
                postMatches As ArrayBuilder(Of InitializerMatch(Of SyntaxNode)),
                ByRef changesSemantics As Boolean,
                cancellationToken As CancellationToken) As Boolean
            ' Only called for collection expressions, which VB does not support
            Throw ExceptionUtilities.Unreachable()
        End Function
    End Class
End Namespace
