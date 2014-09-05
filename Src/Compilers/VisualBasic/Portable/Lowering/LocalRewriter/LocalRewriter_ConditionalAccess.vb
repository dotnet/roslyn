' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter

        Public Overrides Function VisitConditionalAccess(node As BoundConditionalAccess) As BoundNode
            Debug.Assert(node.Type IsNot Nothing)

            Dim rewrittenReceiver As BoundExpression = VisitExpressionNode(node.Receiver)

            ' What the placeholder should be replaced with
            Dim temporaries = ArrayBuilder(Of SynthesizedLocal).GetInstance()
            Dim placeholderResult As UseTwiceRewriter.Result = UseTwiceRewriter.UseTwice(Me.currentMethodOrLambda, rewrittenReceiver, temporaries)

            Dim factory = New SyntheticBoundNodeFactory(topMethod, currentMethodOrLambda, node.Syntax, compilationState, diagnostics)
            Dim condition As BoundExpression

            If rewrittenReceiver.Type.IsNullableType() Then
                ' if( receiver.HasValue, receiver.GetValueOrDefault(). ... -> to Nullable, Nothing) 
                condition = NullableHasValue(placeholderResult.First)
                AddPlaceholderReplacement(node.Placeholder, NullableValueOrDefault(placeholderResult.Second))

            ElseIf rewrittenReceiver.Type.IsReferenceType Then
                ' if( receiver IsNot Nothing, receiver. ... -> to Nullable, Nothing) 
                condition = factory.ReferenceIsNotNothing(placeholderResult.First.MakeRValue())
                AddPlaceholderReplacement(node.Placeholder, placeholderResult.Second)

            Else
                ' if( receiver IsNot Nothing, receiver. ... -> to Nullable, Nothing) 
                Debug.Assert(Not rewrittenReceiver.Type.IsValueType)
                Debug.Assert(rewrittenReceiver.Type.IsTypeParameter())
                condition = factory.ReferenceIsNotNothing(factory.DirectCast(placeholderResult.First.MakeRValue(), factory.SpecialType(SpecialType.System_Object)))
                AddPlaceholderReplacement(node.Placeholder, placeholderResult.Second)
            End If


            Dim whenTrue As BoundExpression = VisitExpressionNode(node.AccessExpression)

            RemovePlaceholderReplacement(node.Placeholder)

            Dim whenFalse As BoundExpression

            If node.Type.IsVoidType() Then
                whenFalse = New BoundSequence(node.Syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray(Of BoundExpression).Empty, Nothing, node.Type)

                If Not whenTrue.Type.IsVoidType() Then
                    whenTrue = New BoundSequence(whenTrue.Syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(whenTrue), Nothing, node.Type)
                End If
            Else
                If Not whenTrue.Type.IsNullableType() AndAlso whenTrue.Type.IsValueType Then
                    whenTrue = WrapInNullable(whenTrue, node.Type)
                End If

                whenFalse = If(whenTrue.Type.IsNullableType(), NullableNull(node.Syntax, whenTrue.Type), factory.Null(whenTrue.Type))
            End If


            Dim result As BoundExpression = TransformRewrittenTernaryConditionalExpression(factory.TernaryConditionalExpression(condition, whenTrue, whenFalse))

            If temporaries.Count > 0 Then
                If result.Type.IsVoidType() Then
                    result = New BoundSequence(node.Syntax, StaticCast(Of LocalSymbol).From(temporaries.ToImmutable()), ImmutableArray.Create(result), Nothing, result.Type)
                Else
                    result = New BoundSequence(node.Syntax, StaticCast(Of LocalSymbol).From(temporaries.ToImmutable()), ImmutableArray(Of BoundExpression).Empty, result, result.Type)
                End If
            End If

            Return result
        End Function

    End Class
End Namespace

