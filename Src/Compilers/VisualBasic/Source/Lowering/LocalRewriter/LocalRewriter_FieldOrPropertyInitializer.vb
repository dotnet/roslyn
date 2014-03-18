' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind


Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter

        ''' <summary>
        ''' Field initializers need to be rewritten multiple times in case of an AsNew declaration with multiple field names because the 
        ''' initializer may contain references to the current field like in the following example:
        ''' Class C1
        '''     Public x, y As New RefType() With {.Field1 = .Field2}
        ''' End Class 
        ''' 
        ''' in this example .Field2 references the temp that is created for x and y.
        ''' 
        ''' We moved the final rewriting for field initializers to the local 
        ''' rewriters because here we already have the infrastructure to replace placeholders. 
        ''' </summary>
        Public Overrides Function VisitFieldOrPropertyInitializer(node As BoundFieldOrPropertyInitializer) As BoundNode
            Dim initializedSymbolsCount = node.InitializedSymbols.Length
            Dim statements(initializedSymbolsCount - 1) As BoundStatement

            ' it's enough to create one me reference if the symbols are not shared that gets reused for all following rewritings.
            Dim meReferenceOpt As BoundExpression = Nothing
            If Not node.InitializedSymbols.First.IsShared Then
                ' create me reference if needed
                Debug.Assert(currentMethodOrLambda IsNot Nothing)
                meReferenceOpt = New BoundMeReference(node.Syntax, currentMethodOrLambda.ContainingType)
                meReferenceOpt.SetWasCompilerGenerated()
            End If

            Dim objectInitializer As BoundObjectInitializerExpression = Nothing
            Dim createTemporary = True
            If node.InitialValue.Kind = BoundKind.ObjectCreationExpression OrElse node.InitialValue.Kind = BoundKind.NewT Then
                Dim objectCreationExpression = DirectCast(node.InitialValue, BoundObjectCreationExpressionBase)
                If objectCreationExpression.InitializerOpt IsNot Nothing AndAlso
                    objectCreationExpression.InitializerOpt.Kind = BoundKind.ObjectInitializerExpression Then
                    objectInitializer = DirectCast(objectCreationExpression.InitializerOpt, BoundObjectInitializerExpression)
                    createTemporary = objectInitializer.CreateTemporaryLocalForInitialization
                End If
            End If

            For symbolIndex = 0 To initializedSymbolsCount - 1
                Dim symbol = node.InitializedSymbols(symbolIndex)

                Debug.Assert(node.AsNewSyntaxNodesOpt.IsDefault OrElse initializedSymbolsCount > 1)
                Dim syntaxForSequencePoint = If(initializedSymbolsCount > 1, node.AsNewSyntaxNodesOpt(symbolIndex), node.Syntax.Parent)
                Dim accessExpression As BoundExpression

                ' if there are more than one symbol we need to create a field access for each of them
                If initializedSymbolsCount > 1 Then
                    Dim fieldSymbol = DirectCast(symbol, FieldSymbol)
                    accessExpression = New BoundFieldAccess(node.Syntax, meReferenceOpt, fieldSymbol, True, fieldSymbol.Type)
                Else
                    Debug.Assert(node.MemberAccessExpressionOpt IsNot Nothing)

                    ' otherwise use the node stored in the bound initializer node
                    accessExpression = node.MemberAccessExpressionOpt
                End If

                Dim rewrittenStatement As BoundStatement

                If Not createTemporary Then
                    Debug.Assert(objectInitializer.PlaceholderOpt IsNot Nothing)

                    ' we need to replace the placeholder with it, so add it to the replacement map
                    AddPlaceholderReplacement(objectInitializer.PlaceholderOpt, accessExpression)

                    rewrittenStatement = VisitExpressionNode(node.InitialValue).ToStatement

                    RemovePlaceholderReplacement(objectInitializer.PlaceholderOpt)
                Else
                    ' in all other cases we want the initial value be assigned to the member (field or property)
                    rewrittenStatement = DirectCast(Visit(New BoundAssignmentOperator(DirectCast(syntaxForSequencePoint, VisualBasicSyntaxNode),
                                                                          accessExpression,
                                                                          node.InitialValue,
                                                                          False).ToStatement), BoundStatement)
                End If

                statements(symbolIndex) = rewrittenStatement
            Next

            Return New BoundStatementList(node.Syntax, statements.AsImmutableOrNull)
        End Function

    End Class
End Namespace
