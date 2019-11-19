' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Partial Private Class VisualBasicCodeGenerator
            Private Class ExpressionCodeGenerator
                Inherits VisualBasicCodeGenerator

                Public Sub New(insertionPoint As InsertionPoint, selectionResult As SelectionResult, analyzerResult As AnalyzerResult)
                    MyBase.New(insertionPoint, selectionResult, analyzerResult)
                End Sub

                Public Shared Function IsExtractMethodOnExpression(code As SelectionResult) As Boolean
                    Return code.SelectionInExpression
                End Function

                Protected Overrides Function CreateMethodName(generateLocalFunction As Boolean) As SyntaxToken
                    Dim methodName = "NewMethod"
                    Dim containingScope = CType(VBSelectionResult.GetContainingScope(), SyntaxNode)

                    methodName = GetMethodNameBasedOnExpression(methodName, containingScope)

                    Dim semanticModel = CType(SemanticDocument.SemanticModel, SemanticModel)
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
                        Return If(name IsNot Nothing AndAlso name.Length > 0, MakeMethodName("Get", name), methodName)
                    End If

                    If TypeOf expression Is MemberAccessExpressionSyntax Then
                        expression = CType(expression, MemberAccessExpressionSyntax).Name
                    End If

                    If TypeOf expression Is NameSyntax Then
                        Dim lastDottedName = CType(expression, NameSyntax).GetLastDottedName()
                        Dim plainName = CType(lastDottedName, SimpleNameSyntax).Identifier.ValueText
                        Return If(plainName IsNot Nothing AndAlso plainName.Length > 0, MakeMethodName("Get", plainName), methodName)
                    End If

                    Return methodName
                End Function

                Protected Overrides Function GetInitialStatementsForMethodDefinitions() As IEnumerable(Of StatementSyntax)
                    Contract.ThrowIfFalse(IsExtractMethodOnExpression(VBSelectionResult))

                    Dim expression = DirectCast(VBSelectionResult.GetContainingScope(), ExpressionSyntax)

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
                            Return SpecializedCollections.EmptyEnumerable(Of StatementSyntax)()
                        End If

                        statement = SyntaxFactory.ExpressionStatement(expression:=expression)
                    End If

                    Return SpecializedCollections.SingletonEnumerable(Of StatementSyntax)(statement)
                End Function

                Protected Overrides Function GetOutermostCallSiteContainerToProcess(cancellationToken As CancellationToken) As SyntaxNode
                    Dim callSiteContainer = GetCallSiteContainerFromOutermostMoveInVariable(cancellationToken)
                    Return If(callSiteContainer, (GetCallSiteContainerFromExpression()))
                End Function

                Private Function GetCallSiteContainerFromExpression() As SyntaxNode
                    Dim container = VBSelectionResult.InnermostStatementContainer()

                    Contract.ThrowIfNull(container)
                    Contract.ThrowIfFalse(container.IsStatementContainerNode() OrElse
                                          TypeOf container Is TypeBlockSyntax OrElse
                                          TypeOf container Is CompilationUnitSyntax)

                    Return container
                End Function

                Protected Overrides Function GetFirstStatementOrInitializerSelectedAtCallSite() As StatementSyntax
                    Return VBSelectionResult.GetContainingScopeOf(Of StatementSyntax)()
                End Function

                Protected Overrides Function GetLastStatementOrInitializerSelectedAtCallSite() As StatementSyntax
                    Return GetFirstStatementOrInitializerSelectedAtCallSite()
                End Function

                Protected Overrides Async Function GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(callSiteAnnotation As SyntaxAnnotation, cancellationToken As CancellationToken) As Task(Of StatementSyntax)
                    Dim enclosingStatement = GetFirstStatementOrInitializerSelectedAtCallSite()
                    Dim callSignature = CreateCallSignature().WithAdditionalAnnotations(callSiteAnnotation)
                    Dim invocation = If(TypeOf callSignature Is AwaitExpressionSyntax,
                                        DirectCast(callSignature, AwaitExpressionSyntax).Expression, callSignature)

                    Dim sourceNode = DirectCast(VBSelectionResult.GetContainingScope(), SyntaxNode)
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
                    sourceNode = DirectCast(updatedRoot.GetAnnotatedNodesAndTokens(sourceNodeAnnotation).Last().AsNode(), SyntaxNode)

                    ' we want to replace the old identifier with a invocation expression, but because of MakeExplicit we might have
                    ' a member access now instead of the identifier. So more syntax fiddling is needed.
                    If sourceNode.Parent.Kind = SyntaxKind.SimpleMemberAccessExpression AndAlso
                        DirectCast(sourceNode, ExpressionSyntax).IsRightSideOfDot() Then

                        Dim explicitMemberAccess = DirectCast(sourceNode.Parent, MemberAccessExpressionSyntax)
                        Dim replacementMemberAccess = SyntaxFactory.MemberAccessExpression(
                                sourceNode.Parent.Kind(),
                                explicitMemberAccess.Expression,
                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                DirectCast(DirectCast(invocation, InvocationExpressionSyntax).Expression, SimpleNameSyntax))
                        replacementMemberAccess = explicitMemberAccess.CopyAnnotationsTo(replacementMemberAccess)

                        Dim newInvocation = SyntaxFactory.InvocationExpression(
                            replacementMemberAccess,
                            DirectCast(invocation, InvocationExpressionSyntax).ArgumentList) _
                                .WithTrailingTrivia(sourceNode.GetTrailingTrivia())

                        Dim newCallSignature = If(callSignature IsNot invocation,
                                                  callSignature.ReplaceNode(invocation, newInvocation), invocation.CopyAnnotationsTo(newInvocation))

                        sourceNode = sourceNode.Parent
                        callSignature = newCallSignature
                    End If

                    Return newEnclosingStatement.ReplaceNode(sourceNode, callSignature)
                End Function
            End Class
        End Class
    End Class
End Namespace
