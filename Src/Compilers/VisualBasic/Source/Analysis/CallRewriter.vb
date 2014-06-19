' ==++==
'
' Copyright (c) Microsoft Corporation. All rights reserved.
'
' ==--==
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Roslyn.Compilers.Collections
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic

    Friend NotInheritable Class CallRewriter
        Inherits BoundTreeRewriter

        Private ReadOnly ContainingMember As Symbol

        Private Sub New(containingMember As Symbol)
            Me.ContainingMember = containingMember
        End Sub

        Public Shared Function Rewrite(containingMember As Symbol, node As BoundBlock) As BoundBlock
            Debug.Assert(node IsNot Nothing)
            Return DirectCast(New CallRewriter(containingMember).Visit(node), BoundBlock)
        End Function

        Public Overrides Function VisitCall(node As BoundCall) As BoundNode

            ' Avoid rewriting if node has errors since one or more
            ' of the arguments may not be the correct rvalue/lvalue.
            If node.HasErrors Then
                Return node
            End If

            If node.IsConstant Then
                Return node
            End If

            Dim result As BoundNode

            result = MyBase.VisitCall(node)

            If result.Kind = BoundKind.Call Then
                node = DirectCast(result, BoundCall)

                Dim sideEffects As Boolean = HasSideEffects(node.CopybackExpressionsOpt, node.TemporariesOpt)
                Dim curriedFrom As MethodSymbol = node.Method.CurriedFrom

                If curriedFrom IsNot Nothing AndAlso node.ReceiverOpt IsNot Nothing Then
                    ' This is an extension method call
                    Dim oldArgs As ReadOnlyArray(Of BoundExpression) = node.Arguments
                    Dim newArgs As ReadOnlyArray(Of BoundExpression)

                    If oldArgs.IsNullOrEmpty Then
                        newArgs = ReadOnlyArray.Singleton(Of BoundExpression)(node.ReceiverOpt)
                    Else
                        Dim array(oldArgs.Count) As BoundExpression

                        array(0) = node.ReceiverOpt
                        oldArgs.CopyTo(array, 1)
                        newArgs = array.AsReadOnlyWrap()
                    End If

                    result = New BoundCall(node.Syntax,
                                          node.SyntaxTree,
                                          Nothing,
                                          curriedFrom,
                                          newArgs,
                                          node.ConstantValueOpt,
                                          node.Type)

                ElseIf sideEffects Then
                    result = New BoundCall(node.Syntax,
                                          node.SyntaxTree,
                                          node.ReceiverOpt,
                                          node.Method,
                                          node.Arguments,
                                          node.ConstantValueOpt,
                                          node.Type)
                End If

                If sideEffects Then
                    result = New BoundSequenceValueSideEffects(node.Syntax,
                                                               node.SyntaxTree,
                                                               node.TemporariesOpt,
                                                               DirectCast(result, BoundExpression),
                                                               node.CopybackExpressionsOpt.NullToEmpty(),
                                                               node.Type)
                End If
            End If

            Return result
        End Function

        Public Overrides Function VisitBadExpression(node As BoundBadExpression) As BoundNode
            ' Cannot recurse into BadExpression children since the BadExpression
            ' may represent being unable to use the child as an lvalue or rvalue.
            Return node
        End Function

        Public Overrides Function VisitObjectCreationExpression(node As BoundObjectCreationExpression) As BoundNode

            ' Avoid rewriting if node has errors since one or more
            ' of the arguments may not be the correct rvalue/lvalue.
            If node.HasErrors Then
                Return node
            End If

            Dim result As BoundNode

            result = MyBase.VisitObjectCreationExpression(node)

            If result.Kind = BoundKind.ObjectCreationExpression Then
                node = DirectCast(result, BoundObjectCreationExpression)

                If HasSideEffects(node.CopybackExpressionsOpt, node.TemporariesOpt) Then
                    Dim value = New BoundObjectCreationExpression(node.Syntax,
                                                                  node.SyntaxTree,
                                                                  node.Constructor,
                                                                  node.Arguments,
                                                                  node.Type)
                    result = New BoundSequenceValueSideEffects(node.Syntax,
                                               node.SyntaxTree,
                                               node.TemporariesOpt,
                                               value,
                                               node.CopybackExpressionsOpt.NullToEmpty(),
                                               node.Type)
                End If
            End If

            Return result
        End Function

        Public Overrides Function VisitRedimStatement(node As BoundRedimStatement) As BoundNode
            ' NOTE: bound redim statement node represents a group of redim clauses; each of  
            '       those can be considered as a standalone statement. This rewrite just returns 
            '       the rewritten redim clause in case there is only one of them or groups 
            '       rewritten redim clauses into bound statement list node if there are more
            '
            ' This rewrite cannot be done later because we specify property access in VisitRedimClause 
            ' which need to be rewritten by call rewriter. We also want to see original property access 
            ' nodes to be able to enforce correct UseTwice semantics

            If node.Clauses.Count = 1 Then
                Return Me.Visit(node.Clauses(0))

            Else
                Dim statements = New BoundStatement(node.Clauses.Count - 1) {}
                For i = 0 To node.Clauses.Count - 1
                    statements(i) = DirectCast(Me.Visit(node.Clauses(i)), BoundStatement)
                Next
                Return New BoundStatementList(node.Syntax, node.SyntaxTree, statements.AsReadOnlyWrap())
            End If
        End Function

        Public Overrides Function VisitRedimClause(node As BoundRedimClause) As BoundNode

            If Not node.HasErrors Then

                '  array type must be known if the node is valid
                Debug.Assert(node.ArrayTypeOpt IsNot Nothing)

                '  build expression returning created (and optionally initialized) array
                Dim valueBeingAssigned As BoundExpression = New BoundArrayCreation(node.Syntax, node.SyntaxTree,
                                                                                   node.Indices, Nothing, node.ArrayTypeOpt)

                Dim temporaries As ArrayBuilder(Of TempLocalSymbol) = Nothing
                Dim assignmentTarget = node.Operand

                If node.IsPreserve Then
                    ' build a call to Microsoft.VisualBasic.CompilerServices.Utils.CopyArray

                    '  use the operand twice
                    temporaries = ArrayBuilder(Of TempLocalSymbol).GetInstance()
                    Dim result As UseTwiceRewriter.Result = UseTwiceRewriter.UseTwice(Me.ContainingMember, assignmentTarget, temporaries)

                    '  the first to be used as an assignment target
                    assignmentTarget = result.First
                    '  the second will be used for accessing the array's current value
                    Dim arrayValueAccess = result.Second

                    '  make an r-value from array value access
                    If arrayValueAccess.Kind = BoundKind.PropertyAccess Then
                        arrayValueAccess = DirectCast(arrayValueAccess, BoundPropertyAccess).SpecifyAccessKind(PropertyAccessKind.Get)
                    Else
                        arrayValueAccess = arrayValueAccess.MakeRValue()
                    End If

                    '  System.Array type
                    Dim systemArray = node.CopyArrayUtilityMethodOpt.Parameters(0).Type

                    '  add conversion
                    arrayValueAccess = New BoundDirectCast(node.Syntax, node.SyntaxTree, arrayValueAccess,
                                                           Conversions.ClassifyDirectCastConversion(arrayValueAccess.Type, systemArray),
                                                           systemArray)

                    '  bind call to CopyArray
                    valueBeingAssigned = New BoundCall(node.Syntax, node.SyntaxTree,
                                                       Nothing, node.CopyArrayUtilityMethodOpt,
                                                       ReadOnlyArray(Of BoundExpression).CreateFrom(arrayValueAccess, valueBeingAssigned),
                                                       Nothing, systemArray)
                End If

                '  add conversion if needed
                valueBeingAssigned = New BoundDirectCast(node.Syntax, node.SyntaxTree, valueBeingAssigned,
                                                         Conversions.ClassifyDirectCastConversion(valueBeingAssigned.Type, assignmentTarget.Type),
                                                         assignmentTarget.Type)

                '  adjust assignment target
                If assignmentTarget.Kind = BoundKind.PropertyAccess Then
                    assignmentTarget = DirectCast(assignmentTarget, BoundPropertyAccess).SpecifyAccessKind(PropertyAccessKind.Set)
                End If

                '  create assignment operator
                Dim assignmentOperator As BoundExpression = New BoundAssignmentOperator(node.Syntax, node.SyntaxTree, assignmentTarget,
                                                                     valueBeingAssigned, True, assignmentTarget.Type)

                '  if there are any temporaries, wrap it in 
                If temporaries IsNot Nothing Then
                    If temporaries.Count > 0 Then
                        assignmentOperator = New BoundSequenceSideEffectsValue(node.Syntax, node.SyntaxTree,
                                                                               ReadOnlyArray(Of LocalSymbol).CreateFrom(temporaries.ToReadOnlyAndFree()),
                                                                               ReadOnlyArray(Of BoundExpression).Empty,
                                                                               assignmentOperator,
                                                                               assignmentOperator.Type)
                    Else
                        temporaries.Free()
                    End If
                End If

                '  create assignment statement
                Return Visit(New BoundExpressionStatement(node.Syntax, node.SyntaxTree, assignmentOperator))
            End If

            '  report as an errorneous statement
            Return New BoundBadStatement(node.Syntax, node.SyntaxTree, ReadOnlyArray(Of BoundNode).CreateFrom(node), True)
        End Function

        Public Overrides Function VisitPropertyAccess(node As BoundPropertyAccess) As BoundNode

            If node.HasErrors Then
                Return node
            End If

            ' Rewrite property access into call to getter.
            Debug.Assert(node.AccessKind = PropertyAccessKind.Get)

            Dim [property] = node.PropertySymbol.GetBaseProperty()
            Dim getMethod = [property].GetMethod
            Debug.Assert(getMethod IsNot Nothing)
            'EDMAURER the following assert assumes that the overriding property successfully
            'overrode its base. That may not be the case if the declarations are in error.
            'Today (10/6/2011) method bodies are lowered when there are declaration errors
            'when GetDiagnostics() is called.
            'Debug.Assert(Not getMethod.IsOverrides)

            Dim receiverOpt = DirectCast(Visit(node.ReceiverOpt), BoundExpression)
            Dim arguments = VisitList(node.Arguments)
            Dim copybackExpressionsOpt = ReadOnlyArray(Of BoundExpression).Empty

            Return GenerateCall(node.Syntax,
                                node.SyntaxTree,
                                getMethod,
                                receiverOpt,
                                arguments,
                                ReadOnlyArray(Of LocalSymbol).Empty,
                                copybackExpressionsOpt,
                                node.ConstantValueOpt,
                                getMethod.ReturnType)
        End Function

        Public Overrides Function VisitAssignmentOperator(node As BoundAssignmentOperator) As BoundNode

            If node.HasErrors Then
                Return node
            End If

            Dim setNode = TryCast(node.Left, BoundPropertyAccess)
            If setNode Is Nothing Then
                Return MyBase.VisitAssignmentOperator(node)
            End If

            ' Rewrite property assignment into call to setter.
            Debug.Assert(setNode.AccessKind = PropertyAccessKind.Set)

            Dim [property] = setNode.PropertySymbol.GetBaseProperty()
            Dim setMethod = [property].SetMethod
            Debug.Assert(setMethod IsNot Nothing)
            Debug.Assert(Not setMethod.IsOverrides)

            Dim receiverOpt = DirectCast(Visit(setNode.ReceiverOpt), BoundExpression)
            Dim arguments = VisitList(setNode.Arguments)
            Dim copybackExpressionsOpt = ReadOnlyArray(Of BoundExpression).Empty
            Dim value = DirectCast(Visit(node.Right), BoundExpression)

            Return GenerateCall(node.Syntax,
                                node.SyntaxTree,
                                setMethod,
                                receiverOpt,
                                arguments.Concat(ReadOnlyArray.Singleton(value)),
                                ReadOnlyArray(Of LocalSymbol).Empty,
                                copybackExpressionsOpt,
                                node.ConstantValueOpt,
                                setMethod.ReturnType)
        End Function

        Private Shared Function GenerateCall(
            syntax As SyntaxNode,
            syntaxTree As SyntaxTree,
            methodSymbol As MethodSymbol,
            receiverOpt As BoundExpression,
            arguments As ReadOnlyArray(Of BoundExpression),
            temporariesOpt As ReadOnlyArray(Of LocalSymbol),
            copybackExpressionsOpt As ReadOnlyArray(Of BoundExpression),
            constantValueOpt As ConstantValue,
            type As TypeSymbol
        ) As BoundExpression
            Dim methodGroupOpt As BoundMethodGroup = Nothing

            If HasSideEffects(copybackExpressionsOpt, temporariesOpt) Then
                Dim value = New BoundCall(syntax,
                                            syntaxTree,
                                            receiverOpt,
                                            methodSymbol,
                                            arguments,
                                            constantValueOpt,
                                            type)
                Return New BoundSequenceValueSideEffects(syntax,
                                           syntaxTree,
                                           temporariesOpt,
                                           value,
                                           copybackExpressionsOpt.NullToEmpty(),
                                           type)
            End If

            Return New BoundCall(syntax,
                                 syntaxTree,
                                 methodSymbol,
                                 receiverOpt,
                                 arguments,
                                 temporariesOpt,
                                 copybackExpressionsOpt,
                                 constantValueOpt,
                                 type)
        End Function

        Private Shared Function HasSideEffects(
                                              copybackExpressionsOpt As ReadOnlyArray(Of BoundExpression),
                                              temporariesOpt As ReadOnlyArray(Of LocalSymbol)) As Boolean
            Debug.Assert(copybackExpressionsOpt.IsNullOrEmpty OrElse
                         Not temporariesOpt.IsNullOrEmpty)

            Return Not (copybackExpressionsOpt.IsNullOrEmpty AndAlso temporariesOpt.IsNullOrEmpty)
        End Function

    End Class

End Namespace
