' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Simplification
    Partial Friend Class VisualBasicVariableDeclaratorReducer
        Inherits AbstractVisualBasicReducer

        Private Shared ReadOnly s_pool As ObjectPool(Of IReductionRewriter) =
            New ObjectPool(Of IReductionRewriter)(Function() New Rewriter(s_pool))

        Private Shared ReadOnly s_simplifyVariableDeclarator As Func(Of VariableDeclaratorSyntax, SemanticModel, VisualBasicSimplifierOptions, CancellationToken, SyntaxNode) = AddressOf SimplifyVariableDeclarator

        Public Sub New()
            MyBase.New(s_pool)
        End Sub

        Public Overrides Function IsApplicable(options As VisualBasicSimplifierOptions) As Boolean
            Return True
        End Function

        Private Overloads Shared Function SimplifyVariableDeclarator(
            node As VariableDeclaratorSyntax,
            semanticModel As SemanticModel,
            options As VisualBasicSimplifierOptions,
            cancellationToken As CancellationToken
        ) As SyntaxNode
            Dim replacementNode As SyntaxNode = Nothing
            Dim issueSpan As TextSpan

            If Not TryReduceVariableDeclaratorWithoutType(
                node, semanticModel, replacementNode, issueSpan) Then
                Return node
            End If

            replacementNode = node.CopyAnnotationsTo(replacementNode).WithAdditionalAnnotations(Formatter.Annotation)
            Return replacementNode.WithoutAnnotations(Simplifier.Annotation)
        End Function

        Private Shared Function TryReduceVariableDeclaratorWithoutType(
            variableDeclarator As VariableDeclaratorSyntax,
            semanticModel As SemanticModel,
            <Out> ByRef replacementNode As SyntaxNode,
            <Out> ByRef issueSpan As TextSpan) As Boolean

            replacementNode = Nothing
            issueSpan = Nothing

            ' Failfast Conditions
            If variableDeclarator.AsClause Is Nothing OrElse
               Not variableDeclarator.Parent.IsKind(
                    SyntaxKind.LocalDeclarationStatement,
                    SyntaxKind.UsingStatement,
                    SyntaxKind.ForStatement,
                    SyntaxKind.ForEachStatement) Then
                Return False
            End If

            If variableDeclarator.Names.Count <> 1 Then
                Return False
            End If

            Dim parent = variableDeclarator.Parent
            Dim modifiedIdentifier = variableDeclarator.Names.Single()

            Dim simpleAsClause = TryCast(variableDeclarator.AsClause, SimpleAsClauseSyntax)
            If simpleAsClause Is Nothing Then
                Return False
            End If

            If (parent.IsKind(SyntaxKind.LocalDeclarationStatement, SyntaxKind.UsingStatement) AndAlso
                variableDeclarator.Initializer IsNot Nothing) Then

                ' Type Check

                Dim declaredSymbolType As ITypeSymbol = Nothing
                If Not HasValidDeclaredTypeSymbol(modifiedIdentifier, semanticModel, declaredSymbolType) Then
                    Return False
                End If

                Dim initializerType As ITypeSymbol

                If declaredSymbolType.IsArrayType() AndAlso variableDeclarator.Initializer.Value.Kind() = SyntaxKind.CollectionInitializer Then
                    ' Get type of the array literal in context without the target type
                    initializerType = semanticModel.GetSpeculativeTypeInfo(variableDeclarator.Initializer.Value.SpanStart, variableDeclarator.Initializer.Value, SpeculativeBindingOption.BindAsExpression).ConvertedType
                Else
                    initializerType = semanticModel.GetTypeInfo(variableDeclarator.Initializer.Value).Type
                End If

                If Not declaredSymbolType.Equals(initializerType) Then
                    Return False
                End If

                Dim newModifiedIdentifier = SyntaxFactory.ModifiedIdentifier(modifiedIdentifier.Identifier) ' LeadingTrivia is copied here
                replacementNode = SyntaxFactory.VariableDeclarator(SyntaxFactory.SingletonSeparatedList(newModifiedIdentifier.WithTrailingTrivia(variableDeclarator.AsClause.GetTrailingTrivia())),
                                                                   asClause:=Nothing,
                                                                   initializer:=variableDeclarator.Initializer) 'TrailingTrivia is copied here
                issueSpan = variableDeclarator.Span
                Return True
            End If

            If (parent.IsKind(SyntaxKind.ForEachStatement, SyntaxKind.ForStatement)) Then
                ' Type Check for ForStatement
                If parent.IsKind(SyntaxKind.ForStatement) Then
                    Dim declaredSymbolType As ITypeSymbol = Nothing
                    If Not HasValidDeclaredTypeSymbol(modifiedIdentifier, semanticModel, declaredSymbolType) Then
                        Return False
                    End If

                    Dim valueType = semanticModel.GetTypeInfo(DirectCast(parent, ForStatementSyntax).ToValue).Type

                    If Not valueType.Equals(declaredSymbolType) Then
                        Return False
                    End If
                End If

                If parent.IsKind(SyntaxKind.ForEachStatement) Then
                    Dim forEachStatementInfo = semanticModel.GetForEachStatementInfo(DirectCast(parent, ForEachStatementSyntax))
                    If Not forEachStatementInfo.ElementConversion.IsIdentity Then
                        Return False
                    End If
                End If

                Dim newIdentifierName = SyntaxFactory.IdentifierName(modifiedIdentifier.Identifier) ' Leading Trivia is copied here
                replacementNode = newIdentifierName.WithTrailingTrivia(variableDeclarator.AsClause.GetTrailingTrivia()) ' Trailing Trivia is copied here
                issueSpan = variableDeclarator.Span
                Return True
            End If

            Return False
        End Function

        Private Shared Function HasValidDeclaredTypeSymbol(
            modifiedIdentifier As ModifiedIdentifierSyntax,
            semanticModel As SemanticModel,
            <Out> ByRef typeSymbol As ITypeSymbol) As Boolean

            Dim declaredSymbol = semanticModel.GetDeclaredSymbol(modifiedIdentifier)
            If declaredSymbol Is Nothing OrElse
               (TypeOf declaredSymbol IsNot ILocalSymbol AndAlso TypeOf declaredSymbol IsNot IFieldSymbol) Then
                Return False
            End If

            Dim localSymbol = TryCast(declaredSymbol, ILocalSymbol)
            If localSymbol IsNot Nothing AndAlso TypeOf localSymbol IsNot IErrorTypeSymbol AndAlso TypeOf localSymbol.Type IsNot IErrorTypeSymbol Then
                typeSymbol = localSymbol.Type
                Return True
            End If

            Return False
        End Function

    End Class
End Namespace
