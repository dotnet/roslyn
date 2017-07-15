' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    ''' <summary>
    ''' Complexify makes inferred names explicit for tuple elements and anonymous type members. This
    ''' class considers which ones of those can be simplified (after the refactoring was done).
    ''' If the inferred name of the member matches, the explicit name (from Complexifiy) can be removed.
    ''' </summary>
    Partial Friend Class VisualBasicInferredMemberNameReducer
        Inherits AbstractVisualBasicReducer

        Private Shared ReadOnly s_pool As ObjectPool(Of IReductionRewriter) =
            New ObjectPool(Of IReductionRewriter)(Function() New Rewriter(s_pool))

        Public Sub New()
            MyBase.New(s_pool)
        End Sub

        Friend Shared Function CanSimplifyTupleName(node As SimpleArgumentSyntax, parseOptions As VisualBasicParseOptions) As Boolean
            ' Tuple elements are arguments in a tuple expression
            If node.NameColonEquals Is Nothing OrElse Not node.IsParentKind(SyntaxKind.TupleExpression) Then
                Return False
            End If

            If parseOptions.LanguageVersion < LanguageVersion.VisualBasic15_3 Then
                Return False
            End If

            Dim inferredName = node.Expression.TryGetInferredMemberName()
            If inferredName Is Nothing OrElse
                Not CaseInsensitiveComparison.Equals(inferredName, node.NameColonEquals.Name.Identifier.ValueText) Then

                Return False
            End If

            Return True
        End Function

        Private Shared ReadOnly s_simplifyNamedFieldInitializer As Func(Of NamedFieldInitializerSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode) = AddressOf SimplifyNamedFieldInitializer

        Private Shared Function SimplifyNamedFieldInitializer(node As NamedFieldInitializerSyntax, arg2 As SemanticModel, optionSet As OptionSet, arg4 As CancellationToken) As SyntaxNode
            If CanSimplifyNamedFieldInitializer(node) Then
                Return SyntaxFactory.InferredFieldInitializer(node.Expression).WithTriviaFrom(node)
            End If

            Return node
        End Function

        Friend Shared Function CanSimplifyNamedFieldInitializer(node As NamedFieldInitializerSyntax) As Boolean
            Dim inferredName = node.Expression.TryGetInferredMemberName()
            If inferredName Is Nothing OrElse
                    Not CaseInsensitiveComparison.Equals(inferredName, node.Name.Identifier.ValueText) Then
                Return False
            End If

            Return True
        End Function
    End Class
End Namespace
