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

        ' TODO: missing/omitted arguments
        Public Overrides Function VisitLateInvocation(node As BoundLateInvocation) As BoundNode

            Debug.Assert(node.Member IsNot Nothing, "late invocation always has a member that is invoked")

            If _inExpressionLambda Then
                ' just preserve the node to report an error in ExpressionLambdaRewriter
                Return MyBase.VisitLateInvocation(node)
            End If

            If node.Member.Kind = BoundKind.LateMemberAccess Then
                ' objReceiver.goo(args)         
                Dim member = DirectCast(node.Member, BoundLateMemberAccess)

                ' NOTE: member is not the receiver of the call, it just represents the latebound access. 
                '       actual receiver of the whole invocation is the member's receiver.
                Return RewriteLateBoundMemberInvocation(member,
                                                        member.ReceiverOpt,
                                                        node.ArgumentsOpt,
                                                        node.ArgumentNamesOpt,
                                                        useLateCall:=node.AccessKind = LateBoundAccessKind.Call)
            Else
                ' objExpr(args) 

                ' NOTE: member is the receiver of this call. it must be an array or something indexable.
                Return RewriteLateBoundIndexInvocation(node, node.Member, node.ArgumentsOpt)
            End If
        End Function

        Private Function RewriteLateBoundIndexInvocation(invocation As BoundLateInvocation,
                                                        receiverExpression As BoundExpression,
                                                        argExpressions As ImmutableArray(Of BoundExpression)) As BoundExpression

            Debug.Assert(invocation.AccessKind = LateBoundAccessKind.Get)
            Debug.Assert(receiverExpression.Kind <> BoundKind.LateMemberAccess)

            Dim rewrittenReceiver = VisitExpressionNode(invocation.Member)
            Dim rewrittenArguments = VisitList(argExpressions)

            Return LateIndexGet(invocation, rewrittenReceiver, rewrittenArguments)
        End Function

        Private Function RewriteLateBoundMemberInvocation(memberAccess As BoundLateMemberAccess,
                                                        receiverExpression As BoundExpression,
                                                        argExpressions As ImmutableArray(Of BoundExpression),
                                                        argNames As ImmutableArray(Of String),
                                                        useLateCall As Boolean) As BoundExpression

            Dim temps As ArrayBuilder(Of SynthesizedLocal) = Nothing

            Dim rewrittenReceiver As BoundExpression = VisitExpressionNode(receiverExpression)

            Dim assignmentArguments As ImmutableArray(Of BoundExpression) = Nothing
            LateCaptureArgsComplex(temps, argExpressions, assignmentArguments)

            Dim result As BoundExpression = LateCallOrGet(memberAccess,
                                                          rewrittenReceiver,
                                                          argExpressions,
                                                          assignmentArguments,
                                                          argNames,
                                                          useLateCall)

            Dim tempArray As ImmutableArray(Of SynthesizedLocal) = Nothing
            If temps IsNot Nothing Then
                tempArray = temps.ToImmutableAndFree

                If Not tempArray.IsEmpty Then
                    result = New BoundSequence(memberAccess.Syntax,
                                               StaticCast(Of LocalSymbol).From(tempArray),
                                               ImmutableArray(Of BoundExpression).Empty,
                                               result,
                                               result.Type)

                End If
            End If

            Return result
        End Function
    End Class
End Namespace
