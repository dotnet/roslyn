' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Public Overrides Function VisitSyncLockStatement(node As BoundSyncLockStatement) As BoundNode
            Dim statements = ArrayBuilder(Of BoundStatement).GetInstance
            Dim syntaxNode = DirectCast(node.Syntax, SyncLockBlockSyntax)

            ' rewrite the lock expression.
            Dim visitedLockExpression = VisitExpressionNode(node.LockExpression)

            Dim objectType = GetSpecialType(SpecialType.System_Object)
            Dim useSiteInfo = GetNewCompoundUseSiteInfo()
            Dim conversionKind = Conversions.ClassifyConversion(visitedLockExpression.Type, objectType, useSiteInfo).Key
            _diagnostics.Add(node, useSiteInfo)

            ' when passing this boundlocal to Monitor.Enter and Monitor.Exit we need to pass a local of type object, because the parameter
            ' are of type object. We also do not want to have this conversion being shown in the semantic model, which is why we add it 
            ' during rewriting. Because only reference types are allowed for synclock, this is always guaranteed to succeed.
            ' This also unboxes a type parameter, so that the same object is passed to both methods.
            If Not Conversions.IsIdentityConversion(conversionKind) Then
                Dim integerOverflow As Boolean
                Dim constantResult = Conversions.TryFoldConstantConversion(
                                        visitedLockExpression,
                                        objectType,
                                        integerOverflow)

                visitedLockExpression = TransformRewrittenConversion(New BoundConversion(node.LockExpression.Syntax,
                                                                                  visitedLockExpression,
                                                                                  conversionKind,
                                                                                  False,
                                                                                  False,
                                                                                  constantResult,
                                                                                  objectType))
            End If

            ' create a new temp local for the lock object
            Dim tempLockObjectLocal As LocalSymbol = New SynthesizedLocal(Me._currentMethodOrLambda, objectType, SynthesizedLocalKind.Lock, syntaxNode.SyncLockStatement)
            Dim boundLockObjectLocal = New BoundLocal(syntaxNode,
                                                      tempLockObjectLocal,
                                                      objectType)

            Dim instrument As Boolean = Me.Instrument(node)

            If instrument Then
                ' create a sequence point that contains the whole SyncLock statement as the first reachable sequence point
                ' of the SyncLock statement. 
                Dim prologue = _instrumenterOpt.CreateSyncLockStatementPrologue(node)
                If prologue IsNot Nothing Then
                    statements.Add(prologue)
                End If
            End If

            ' assign the lock expression / object to it to avoid changes to it
            Dim tempLockObjectAssignment As BoundStatement = New BoundAssignmentOperator(syntaxNode,
                                                                                         boundLockObjectLocal,
                                                                                         visitedLockExpression,
                                                                                         suppressObjectClone:=True,
                                                                                         type:=objectType).ToStatement

            boundLockObjectLocal = boundLockObjectLocal.MakeRValue()

            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                tempLockObjectAssignment = RegisterUnstructuredExceptionHandlingResumeTarget(syntaxNode, tempLockObjectAssignment, canThrow:=True)
            End If

            Dim saveState As UnstructuredExceptionHandlingContext = LeaveUnstructuredExceptionHandlingContext(node)

            If instrument Then
                tempLockObjectAssignment = _instrumenterOpt.InstrumentSyncLockObjectCapture(node, tempLockObjectAssignment)
            End If

            statements.Add(tempLockObjectAssignment)

            ' If the type of the lock object is System.Object we need to call the vb runtime helper 
            ' Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.CheckForSyncLockOnValueType to ensure no value type is 
            ' used. Note that we are checking type on original bound node for LockExpression because rewritten node will
            ' always have System.Object as its type due to conversion added above.
            ' If helper not available on this platform (/vbruntime*), don't call this helper and do not report errors.
            Dim checkForSyncLockOnValueTypeMethod As MethodSymbol = Nothing
            If node.LockExpression.Type.IsObjectType() AndAlso
               TryGetWellknownMember(checkForSyncLockOnValueTypeMethod, WellKnownMember.Microsoft_VisualBasic_CompilerServices_ObjectFlowControl__CheckForSyncLockOnValueType, syntaxNode, isOptional:=True) Then
                Dim boundHelperCall = New BoundCall(syntaxNode,
                                                    checkForSyncLockOnValueTypeMethod,
                                                    Nothing,
                                                    Nothing,
                                                    ImmutableArray.Create(Of BoundExpression)(boundLockObjectLocal),
                                                    Nothing,
                                                    checkForSyncLockOnValueTypeMethod.ReturnType,
                                                    suppressObjectClone:=True)
                Dim boundHelperCallStatement = boundHelperCall.ToStatement
                boundHelperCallStatement.SetWasCompilerGenerated() ' used to not create sequence points
                statements.Add(boundHelperCallStatement)
            End If

            Dim locals As ImmutableArray(Of LocalSymbol)
            Dim boundLockTakenLocal As BoundLocal = Nothing
            Dim tempLockTakenAssignment As BoundStatement = Nothing
            Dim tryStatements As ImmutableArray(Of BoundStatement)

            Dim boundMonitorEnterCallStatement As BoundStatement = GenerateMonitorEnter(node.LockExpression.Syntax, boundLockObjectLocal, boundLockTakenLocal, tempLockTakenAssignment)

            ' the new Monitor.Enter call will be inside the try block, the old is outside
            If boundLockTakenLocal IsNot Nothing Then
                locals = ImmutableArray.Create(Of LocalSymbol)(tempLockObjectLocal, boundLockTakenLocal.LocalSymbol)
                statements.Add(tempLockTakenAssignment)
                tryStatements = ImmutableArray.Create(Of BoundStatement)(boundMonitorEnterCallStatement,
                                                      DirectCast(Visit(node.Body), BoundBlock))
            Else
                locals = ImmutableArray.Create(tempLockObjectLocal)
                statements.Add(boundMonitorEnterCallStatement)
                tryStatements = ImmutableArray.Create(Of BoundStatement)(DirectCast(Visit(node.Body), BoundBlock))
            End If

            ' rewrite the SyncLock body
            Dim tryBody As BoundBlock = New BoundBlock(syntaxNode,
                                                       Nothing,
                                                       ImmutableArray(Of LocalSymbol).Empty,
                                                       tryStatements)

            Dim statementInFinally As BoundStatement = GenerateMonitorExit(syntaxNode, boundLockObjectLocal, boundLockTakenLocal)

            Dim finallyBody As BoundBlock = New BoundBlock(syntaxNode,
                                                           Nothing,
                                                           ImmutableArray(Of LocalSymbol).Empty,
                                                           ImmutableArray.Create(Of BoundStatement)(statementInFinally))

            If instrument Then
                ' Add a sequence point to highlight the "End SyncLock" syntax in case the body has thrown an exception
                finallyBody = DirectCast(Concat(finallyBody, _instrumenterOpt.CreateSyncLockExitDueToExceptionEpilogue(node)), BoundBlock)
            End If

            Dim rewrittenSyncLock = RewriteTryStatement(syntaxNode, tryBody, ImmutableArray(Of BoundCatchBlock).Empty, finallyBody, Nothing)
            statements.Add(rewrittenSyncLock)

            If instrument Then
                ' Add a sequence point to highlight the "End SyncLock" syntax in case the body has been complete executed and
                ' exited normally
                Dim epilogue = _instrumenterOpt.CreateSyncLockExitNormallyEpilogue(node)
                If epilogue IsNot Nothing Then
                    statements.Add(epilogue)
                End If
            End If

            RestoreUnstructuredExceptionHandlingContext(node, saveState)

            Return New BoundBlock(syntaxNode,
                                  Nothing,
                                  locals,
                                  statements.ToImmutableAndFree)
        End Function

        Private Function GenerateMonitorEnter(
            syntaxNode As SyntaxNode,
            boundLockObject As BoundExpression,
            <Out> ByRef boundLockTakenLocal As BoundLocal,
            <Out> ByRef boundLockTakenInitialization As BoundStatement
        ) As BoundStatement
            boundLockTakenLocal = Nothing
            boundLockTakenInitialization = Nothing
            Dim parameters As ImmutableArray(Of BoundExpression)

            ' Figure out what Enter method to call from Monitor. 
            ' In case the "new" Monitor.Enter(Object, ByRef Boolean) method is found, use that one,
            ' otherwise fall back to the Monitor.Enter() method.
            Dim enterMethod As MethodSymbol = Nothing
            If TryGetWellknownMember(enterMethod, WellKnownMember.System_Threading_Monitor__Enter2, syntaxNode, isOptional:=True) Then
                ' create local for the lockTaken boolean and initialize it with "False"
                Dim tempLockTaken As LocalSymbol
                If syntaxNode.Parent.Kind = SyntaxKind.SyncLockStatement Then
                    tempLockTaken = New SynthesizedLocal(Me._currentMethodOrLambda, enterMethod.Parameters(1).Type, SynthesizedLocalKind.LockTaken, DirectCast(syntaxNode.Parent, SyncLockStatementSyntax))
                Else
                    tempLockTaken = New SynthesizedLocal(Me._currentMethodOrLambda, enterMethod.Parameters(1).Type, SynthesizedLocalKind.LoweringTemp)
                End If

                Debug.Assert(tempLockTaken.Type.IsBooleanType())

                boundLockTakenLocal = New BoundLocal(syntaxNode, tempLockTaken, tempLockTaken.Type)

                boundLockTakenInitialization = New BoundAssignmentOperator(syntaxNode,
                                                                          boundLockTakenLocal,
                                                                          New BoundLiteral(syntaxNode, ConstantValue.False, boundLockTakenLocal.Type),
                                                                          suppressObjectClone:=True,
                                                                          type:=boundLockTakenLocal.Type).ToStatement
                boundLockTakenInitialization.SetWasCompilerGenerated() ' used to not create sequence points

                parameters = ImmutableArray.Create(Of BoundExpression)(boundLockObject, boundLockTakenLocal)

                boundLockTakenLocal = boundLockTakenLocal.MakeRValue()
            Else
                TryGetWellknownMember(enterMethod, WellKnownMember.System_Threading_Monitor__Enter, syntaxNode)

                parameters = ImmutableArray.Create(Of BoundExpression)(boundLockObject)
            End If

            If enterMethod IsNot Nothing Then
                ' create a call to void Enter(object)
                Dim boundMonitorEnterCall As BoundExpression
                boundMonitorEnterCall = New BoundCall(syntaxNode,
                                                  enterMethod,
                                                  Nothing,
                                                  Nothing,
                                                  parameters,
                                                  Nothing,
                                                  enterMethod.ReturnType,
                                                  suppressObjectClone:=True)
                Dim boundMonitorEnterCallStatement = boundMonitorEnterCall.ToStatement
                boundMonitorEnterCallStatement.SetWasCompilerGenerated() ' used to not create sequence points

                Return boundMonitorEnterCallStatement
            End If

            Return New BoundBadExpression(syntaxNode, LookupResultKind.NotReferencable, ImmutableArray(Of Symbol).Empty, parameters, ErrorTypeSymbol.UnknownResultType, hasErrors:=True).ToStatement()
        End Function

        Private Function GenerateMonitorExit(
            syntaxNode As SyntaxNode,
            boundLockObject As BoundExpression,
            boundLockTakenLocal As BoundLocal
        ) As BoundStatement
            Dim statementInFinally As BoundStatement

            Dim boundMonitorExitCall As BoundExpression

            Dim exitMethod As MethodSymbol = Nothing
            If TryGetWellknownMember(exitMethod, WellKnownMember.System_Threading_Monitor__Exit, syntaxNode) Then
                ' create a call to void Monitor.Exit(object)
                boundMonitorExitCall = New BoundCall(syntaxNode,
                                                     exitMethod,
                                                     Nothing,
                                                     Nothing,
                                                     ImmutableArray.Create(Of BoundExpression)(boundLockObject),
                                                     Nothing,
                                                     exitMethod.ReturnType,
                                                     suppressObjectClone:=True)
            Else
                boundMonitorExitCall = New BoundBadExpression(syntaxNode, LookupResultKind.NotReferencable, ImmutableArray(Of Symbol).Empty, ImmutableArray.Create(boundLockObject), ErrorTypeSymbol.UnknownResultType, hasErrors:=True)
            End If

            Dim boundMonitorExitCallStatement = boundMonitorExitCall.ToStatement
            boundMonitorExitCallStatement.SetWasCompilerGenerated() ' used to not create sequence points

            If boundLockTakenLocal IsNot Nothing Then
                Debug.Assert(boundLockTakenLocal.Type.IsBooleanType())

                ' if the "new" enter method is used we need to check the temporary boolean to see if the lock was really taken.
                ' (maybe there was an exception after try and before the enter call).
                Dim boundCondition = New BoundBinaryOperator(syntaxNode,
                                                             BinaryOperatorKind.Equals,
                                                             boundLockTakenLocal,
                                                             New BoundLiteral(syntaxNode, ConstantValue.True, boundLockTakenLocal.Type),
                                                             False,
                                                             boundLockTakenLocal.Type)
                statementInFinally = RewriteIfStatement(syntaxNode, boundCondition, boundMonitorExitCallStatement, Nothing, instrumentationTargetOpt:=Nothing)
            Else
                statementInFinally = boundMonitorExitCallStatement
            End If

            Return statementInFinally
        End Function

    End Class
End Namespace
