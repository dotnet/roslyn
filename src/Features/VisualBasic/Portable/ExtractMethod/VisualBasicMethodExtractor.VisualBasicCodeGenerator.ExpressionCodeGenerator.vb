' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Partial Private Class VisualBasicCodeGenerator
            Private Class ExpressionCodeGenerator
                Inherits VisualBasicCodeGenerator

                Public Sub New(selectionResult As VisualBasicSelectionResult, analyzerResult As AnalyzerResult, options As VisualBasicCodeGenerationOptions)
                    MyBase.New(selectionResult, analyzerResult, options)
                End Sub

                Public Shared Function IsExtractMethodOnExpression(code As VisualBasicSelectionResult) As Boolean
                    Return code.SelectionInExpression
                End Function

                Protected Overrides Function CreateMethodName() As SyntaxToken
                    Dim methodName = "NewMethod"
                    Dim containingScope = Me.SelectionResult.GetContainingScope()

                    methodName = GetMethodNameBasedOnExpression(methodName, containingScope)

                    Dim semanticModel = SemanticDocument.SemanticModel
                    Dim nameGenerator = New UniqueNameGenerator(semanticModel)
                    Return SyntaxFactory.Identifier(
                        nameGenerator.CreateUniqueMethodName(containingScope, methodName))
                End Function

                Private Shared Function GetMethodNameBasedOnExpression(methodName As String, expression As SyntaxNode) As String
                    If expression.IsParentKind(SyntaxKind.EqualsValue) AndAlso
                       expression.Parent.IsParentKind(SyntaxKind.VariableDeclarator) Then

                        Dim varDecl = DirectCast(expression.Parent.Parent, VariableDeclaratorSyntax)
                        If varDecl.Names.Count <> 1 Then
                            Return methodName
                        End If

                        Dim identifierNode = varDecl.Names(0)
                        If identifierNode Is Nothing Then
                            Return methodName
                        End If

                        Dim name = identifierNode.Identifier.ValueText
                        Return If(name IsNot Nothing AndAlso name.Length > 0, MakeMethodName("Get", name, camelCase:=False), methodName)
                    End If

                    If TypeOf expression Is MemberAccessExpressionSyntax Then
                        expression = CType(expression, MemberAccessExpressionSyntax).Name
                    End If

                    If TypeOf expression Is NameSyntax Then
                        Dim lastDottedName = CType(expression, NameSyntax).GetLastDottedName()
                        Dim plainName = CType(lastDottedName, SimpleNameSyntax).Identifier.ValueText
                        Return If(plainName IsNot Nothing AndAlso plainName.Length > 0, MakeMethodName("Get", plainName, camelCase:=False), methodName)
                    End If

                    Return methodName
                End Function

                Protected Overrides Function GetInitialStatementsForMethodDefinitions() As ImmutableArray(Of StatementSyntax)
                    Contract.ThrowIfFalse(IsExtractMethodOnExpression(Me.SelectionResult))

                    Dim expression = DirectCast(Me.SelectionResult.GetContainingScope(), ExpressionSyntax)

                    Dim statement As StatementSyntax
                    If Me.AnalyzerResult.HasReturnType Then
                        statement = SyntaxFactory.ReturnStatement(expression:=expression)
                    Else
                        ' we have expression for void method (Sub). make the expression as call
                        ' statement if possible we can create call statement only from invocation
                        ' and member access expression. otherwise, it is not a valid expression.
                        ' return error code
                        If expression.Kind <> SyntaxKind.InvocationExpression AndAlso
                           expression.Kind <> SyntaxKind.SimpleMemberAccessExpression Then
                            Return ImmutableArray(Of StatementSyntax).Empty
                        End If

                        statement = SyntaxFactory.ExpressionStatement(expression:=expression)
                    End If

                    Return ImmutableArray.Create(statement)
                End Function

                Protected Overrides Function GetFirstStatementOrInitializerSelectedAtCallSite() As StatementSyntax
                    Return Me.SelectionResult.GetContainingScopeOf(Of StatementSyntax)()
                End Function

                Protected Overrides Function GetLastStatementOrInitializerSelectedAtCallSite() As StatementSyntax
                    Return GetFirstStatementOrInitializerSelectedAtCallSite()
                End Function

                Protected Overrides Async Function GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(cancellationToken As CancellationToken) As Task(Of StatementSyntax)
                    Dim enclosingStatement = GetFirstStatementOrInitializerSelectedAtCallSite()
                    Dim callSignature = CreateCallSignature().WithAdditionalAnnotations(CallSiteAnnotation)

                    Dim sourceNode = Me.SelectionResult.GetContainingScope()
                    Contract.ThrowIfTrue(
                        sourceNode.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) AndAlso
                        DirectCast(sourceNode.Parent, MemberAccessExpressionSyntax).Name Is sourceNode,
                        "invalid scope. scope is not an expression")

                    ' To lower the chances that replacing sourceNode with callSignature will break the user's
                    ' code, we make the enclosing statement semantically explicit. This ends up being a little
                    ' bit more work because we need to annotate the sourceNode so that we can get back to it
                    ' after rewriting the enclosing statement.
                    Dim sourceNodeAnnotation = New SyntaxAnnotation()
                    Dim enclosingStatementAnnotation = New SyntaxAnnotation()
                    Dim newEnclosingStatement = enclosingStatement _
                        .ReplaceNode(sourceNode, sourceNode.WithAdditionalAnnotations(sourceNodeAnnotation)) _
                        .WithAdditionalAnnotations(enclosingStatementAnnotation)

                    Dim updatedDocument = Await Me.SemanticDocument.Document.ReplaceNodeAsync(enclosingStatement, newEnclosingStatement, cancellationToken).ConfigureAwait(False)
                    Dim updatedRoot = Await updatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

                    newEnclosingStatement = DirectCast(updatedRoot.GetAnnotatedNodesAndTokens(enclosingStatementAnnotation).Single().AsNode(), StatementSyntax)

                    ' because of the complexification we cannot guarantee that there is only one annotation.
                    ' however complexification of names is prepended, so the last annotation should be the original one.
                    sourceNode = updatedRoot.GetAnnotatedNodesAndTokens(sourceNodeAnnotation).Last().AsNode()

                    Return newEnclosingStatement.ReplaceNode(sourceNode, callSignature)
                End Function
            End Class
        End Class
    End Class
End Namespace
