' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        ''' <summary>
        ''' A using statement of the form:
        '''      using Expression
        '''          list_of_statements
        '''      end using
        '''
        ''' will be rewritten into:
        '''
        '''      temp = Expression
        '''      Try
        '''          list_of_statements
        '''      Finally
        '''          If Temp IsNot Nothing Then
        '''              CType(temp, IDisposable).Dispose()
        '''          End If
        '''      End Try
        '''
        ''' when the resource is a using locally declared variable no temporary is generated but the variable is read-only
        ''' A using statement of the form:
        '''      Using v As New MyDispose()
        '''          list_of_statements
        '''      End Using
        '''
        ''' is rewritten to:
        ''' 
        '''      Dim v As New MyDispose()
        '''      Try
        '''         list_of_statements
        '''      Finally
        '''          If v IsNot Nothing Then
        '''              CType(v, IDisposable).Dispose()
        '''          End If
        '''      End Try
        '''
        ''' A using with multiple variable resources are equivalent to a nested using statement.
        ''' So a using statement of the form:
        '''      Using v1 As New MyDispose(), v2 As myDispose = New MyDispose()
        '''          list_of_statements
        '''      end using
        '''
        ''' is rewritten to:
        '''      Dim v1 As New MyDispose
        '''      Try
        '''          Dim v2 As MyDispose = new MyDispose()
        '''          Try
        '''              list_of_statements
        '''          Finally
        '''              If v2 IsNot Nothing Then
        '''                  CType(v2, IDisposable).Dispose()
        '''              End If
        '''          End Try
        '''      Finally
        '''          If v1 IsNot Nothing Then
        '''              CType(v1, IDisposable).Dispose()
        '''          End If
        '''      end try
        '''</summary>
        Public Overrides Function VisitUsingStatement(node As BoundUsingStatement) As BoundNode
            Dim saveState As UnstructuredExceptionHandlingContext = LeaveUnstructuredExceptionHandlingContext(node)

            Dim blockSyntax = DirectCast(node.Syntax, UsingBlockSyntax)

            ' rewrite the original using body only once here.
            Dim currentBody = DirectCast(Visit(node.Body), BoundBlock)
            Dim locals As ImmutableArray(Of LocalSymbol) = node.Locals
            Dim placeholderInfo As ValueTuple(Of BoundRValuePlaceholder, BoundExpression, BoundExpression)

            ' the initialization expressions (variable declaration & expression case) will be rewritten in 
            ' "RewriteToInnerTryConstructForVariableDeclaration" to avoid code duplication

            If Not node.ResourceList.IsDefault Then
                ' Case "Using <variable declarations>"  

                ' the try statements will be nested. To avoid re-rewriting we're iterating through the resource list in reverse
                For declarationIndex = node.ResourceList.Length - 1 To 0 Step -1
                    Dim localDeclaration = node.ResourceList(declarationIndex)

                    If localDeclaration.Kind = BoundKind.LocalDeclaration Then
                        Dim localVariableDeclaration = DirectCast(localDeclaration, BoundLocalDeclaration)

                        placeholderInfo = node.UsingInfo.PlaceholderInfo(localVariableDeclaration.LocalSymbol.Type)
                        currentBody = RewriteSingleUsingToTryFinally(node,
                                                                     declarationIndex,
                                                                     localVariableDeclaration.LocalSymbol,
                                                                     localVariableDeclaration.InitializerOpt,
                                                                     placeholderInfo,
                                                                     currentBody)
                    Else
                        Dim localAsNewDeclaration = DirectCast(localDeclaration, BoundAsNewLocalDeclarations)

                        Dim variableCount = localAsNewDeclaration.LocalDeclarations.Length

                        placeholderInfo = node.UsingInfo.PlaceholderInfo(localAsNewDeclaration.LocalDeclarations.First.LocalSymbol.Type)

                        For initializedVariableIndex = localAsNewDeclaration.LocalDeclarations.Length - 1 To 0 Step -1
                            currentBody = RewriteSingleUsingToTryFinally(node,
                                                                         declarationIndex,
                                                                         localAsNewDeclaration.LocalDeclarations(initializedVariableIndex).LocalSymbol,
                                                                         localAsNewDeclaration.Initializer,
                                                                         placeholderInfo,
                                                                         currentBody)
                        Next
                    End If
                Next
            Else
                ' Case "Using <expression>"
                Debug.Assert(node.ResourceExpressionOpt IsNot Nothing)

                Dim initializationExpression = node.ResourceExpressionOpt
                placeholderInfo = node.UsingInfo.PlaceholderInfo(initializationExpression.Type)

                Dim tempResourceSymbol As LocalSymbol = New SynthesizedLocal(Me._currentMethodOrLambda,
                                                                            initializationExpression.Type,
                                                                            SynthesizedLocalKind.Using,
                                                                            blockSyntax.UsingStatement)

                currentBody = RewriteSingleUsingToTryFinally(node,
                                                             0, ' There is only one resource - the expression
                                                             tempResourceSymbol,
                                                             initializationExpression,
                                                             placeholderInfo,
                                                             currentBody)

                locals = locals.Add(tempResourceSymbol)
            End If

            RestoreUnstructuredExceptionHandlingContext(node, saveState)

            Dim statements As ImmutableArray(Of BoundStatement) = currentBody.Statements

            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                statements = RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, canThrow:=True).Concat(statements)
            End If

            currentBody = New BoundBlock(node.Syntax,
                                         currentBody.StatementListSyntax,
                                         locals,
                                         statements)

            Dim prologue As BoundStatement = Nothing

            If Instrument(node) Then
                ' create a sequence point that contains the whole using statement as the first reachable sequence point
                ' of the using statement. The resource variables are not yet in scope.
                prologue = _instrumenterOpt.CreateUsingStatementPrologue(node)
            End If

            If prologue IsNot Nothing Then
                Return New BoundStatementList(node.UsingInfo.UsingStatementSyntax, ImmutableArray.Create(Of BoundStatement)(prologue, currentBody))
            Else
                Return New BoundStatementList(node.UsingInfo.UsingStatementSyntax, ImmutableArray.Create(Of BoundStatement)(currentBody))
            End If
        End Function

        ''' <summary>
        ''' Creates a TryFinally Statement for the given resource.
        ''' 
        ''' This method creates the following for the arguments:
        '''      &lt;localSymbol&gt; = &lt;initializationExpression&gt;
        '''      Try
        '''         &lt;currentBody&gt;
        '''      Finally
        '''          If &lt;disposeCondition&gt; Then
        '''              &lt;disposeConversion&gt;.Dispose()
        '''          End If
        '''      End Try
        ''' 
        ''' Note: this is used for both kinds of using statements (resource locals and resource expressions).
        ''' 
        ''' </summary>
        ''' <returns>The new bound block containing the assignment of the initialization and the try/finally statement with
        ''' the passed body.</returns>
        Private Function RewriteSingleUsingToTryFinally(
            node As BoundUsingStatement,
            resourceIndex As Integer,
            localSymbol As LocalSymbol,
            initializationExpression As BoundExpression,
            ByRef placeholderInfo As ValueTuple(Of BoundRValuePlaceholder, BoundExpression, BoundExpression),
            currentBody As BoundBlock
        ) As BoundBlock
            Dim syntaxNode = DirectCast(node.Syntax, UsingBlockSyntax)
            Dim resourceType = localSymbol.Type
            Dim boundResourceLocal As BoundLocal = New BoundLocal(syntaxNode, localSymbol, isLValue:=True, type:=resourceType)

            Dim resourcePlaceholder As BoundRValuePlaceholder = placeholderInfo.Item1
            Dim disposeConversion As BoundExpression = placeholderInfo.Item2
            Dim disposeCondition As BoundExpression = placeholderInfo.Item3

            AddPlaceholderReplacement(resourcePlaceholder, boundResourceLocal.MakeRValue())

            ' add a sequence point to stop on the "End Using" statement
            ' because there are a lot of hidden sequence points between the dispose call and the "end using" in Roslyn
            ' (caused by emitting try catch), we need to add a sequence point after each call with the syntax of the end using
            ' to match the Dev10 debugging experience.
            Dim newBody = DirectCast(Concat(currentBody, SyntheticBoundNodeFactory.HiddenSequencePoint()), BoundBlock)

            ' assign initialization to variable
            Dim boundResourceInitializationAssignment As BoundStatement = New BoundAssignmentOperator(syntaxNode,
                                                                                                      boundResourceLocal,
                                                                                                      VisitAndGenerateObjectCloneIfNeeded(initializationExpression, suppressObjectClone:=True),
                                                                                                      suppressObjectClone:=True,
                                                                                                      type:=resourceType).ToStatement

            Dim instrument As Boolean = Me.Instrument(node)

            If instrument Then
                boundResourceInitializationAssignment = _instrumenterOpt.InstrumentUsingStatementResourceCapture(node, resourceIndex, boundResourceInitializationAssignment)
            End If

            ' create if statement with dispose call
            Dim disposeCall = GenerateDisposeCallForForeachAndUsing(syntaxNode, boundResourceLocal,
                                                                    VisitExpressionNode(disposeCondition), True,
                                                                    VisitExpressionNode(disposeConversion))

            Dim disposePrologue As BoundStatement = Nothing

            If instrument Then
                ' The block should start with a sequence point that points to the "End Using" statement. This is required in order to
                ' highlight the end using when someone step next after the last statement of the original body and in case an exception
                ' was thrown.
                disposePrologue = _instrumenterOpt.CreateUsingStatementDisposePrologue(node)
            End If

            Dim finallyStatements As ImmutableArray(Of BoundStatement)
            If disposePrologue IsNot Nothing Then
                finallyStatements = ImmutableArray.Create(Of BoundStatement)(disposePrologue, disposeCall)
            Else
                finallyStatements = ImmutableArray.Create(Of BoundStatement)(disposeCall)
            End If

            ' create finally block from the dispose call
            Dim finallyBlock = New BoundBlock(syntaxNode,
                                              Nothing, ImmutableArray(Of LocalSymbol).Empty,
                                              finallyStatements)

            ' rewrite try/finally block
            Dim tryFinally = RewriteTryStatement(syntaxNode, newBody, ImmutableArray(Of BoundCatchBlock).Empty, finallyBlock, Nothing)

            newBody = New BoundBlock(syntaxNode,
                                     Nothing,
                                     ImmutableArray(Of LocalSymbol).Empty,
                                     ImmutableArray.Create(Of BoundStatement)(boundResourceInitializationAssignment,
                                                                                 tryFinally))

            RemovePlaceholderReplacement(resourcePlaceholder)

            Return newBody
        End Function

    End Class
End Namespace
