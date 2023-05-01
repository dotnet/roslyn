' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class SynthesizedStringSwitchHashMethod
        Inherits SynthesizedGlobalMethodBase

        ''' <summary>
        ''' Compute the hashcode of a sub string using FNV-1a
        ''' See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        ''' </summary>
        ''' <remarks>
        ''' This method should be kept consistent with MethodBodySynthesizer.ConstructStringSwitchHashFunctionBody
        ''' The control flow in this method mimics lowered "for" loop. It is exactly what we want to emit
        ''' to ensure that JIT can do range check hoisting.
        ''' </remarks>
        Friend Shared Function ComputeStringHash(text As String) As UInteger
            ' Nothing and "" should result in same hashcode 2166136261
            ' that is intentional since Nothing = "" in VB
            Dim hashCode As UInteger = 2166136261

            If text <> Nothing Then
                Dim i As Integer = 0
                GoTo start

again:
                hashCode = CUInt((AscW(text(i)) Xor hashCode) * 16777619)
                i = i + 1

start:
                If (i < text.Length) Then
                    GoTo again
                End If

            End If
            Return hashCode
        End Function

        ''' <summary>
        ''' Construct a body for String Switch Hash Function
        ''' </summary>
        ''' <remarks>
        ''' This method should be kept consistent with ComputeStringHash
        ''' </remarks>
        Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            Dim F = New SyntheticBoundNodeFactory(Me, Me, Me.Syntax, compilationState, diagnostics)
            F.CurrentMethod = Me

            Dim i As LocalSymbol = F.SynthesizedLocal(Me.ContainingAssembly.GetSpecialType(SpecialType.System_Int32))
            Dim hashCode As LocalSymbol = F.SynthesizedLocal(Me.ContainingAssembly.GetSpecialType(SpecialType.System_UInt32))

            Dim again As LabelSymbol = F.GenerateLabel("again")
            Dim start As LabelSymbol = F.GenerateLabel("start")

            Dim text As ParameterSymbol = Me.Parameters(0)

            '  This method should be kept consistent with ComputeStringHash

            '            ' Nothing and "" should result in same hashcode 2166136261
            '            ' that is intentional since Nothing = "" in VB
            '            Dim hashCode As UInteger = 2166136261

            '            If text <> Nothing Then
            '                Dim i As Integer = 0
            '                GoTo start

            'again :
            '                hashCode = CUInt((AscW(text(i)) Xor hashCode) * 16777619)
            '                i = i + 1

            'start :
            '                If (i < text.Length) Then
            '                    GoTo again
            '                End If

            '            End If
            '            Return hashCode

            Dim textI As BoundExpression = F.Call(F.Parameter(text),
                               DirectCast(Me.ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_String__Chars), MethodSymbol),
                               F.Local(i, False))

            textI = F.Convert(i.Type, textI, ConversionKind.WideningNumeric)
            textI = F.Convert(hashCode.Type, textI, ConversionKind.WideningNumeric)

            Dim body As BoundBlock = F.Block(
                    ImmutableArray.Create(Of LocalSymbol)(hashCode, i),
                    F.Assignment(F.Local(hashCode, True), New BoundLiteral(Me.Syntax, ConstantValue.Create(CUInt(2166136261)), hashCode.Type)),
                    F.If(
                        F.Binary(BinaryOperatorKind.IsNot, Me.ContainingAssembly.GetSpecialType(SpecialType.System_Boolean),
                            F.Parameter(text).MakeRValue(),
                            F.Null(text.Type)),
                        F.Block(
                            F.Assignment(F.Local(i, True), New BoundLiteral(Me.Syntax, ConstantValue.Create(0), i.Type)),
                            F.Goto(start),
                            F.Label(again),
                            F.Assignment(
                                F.Local(hashCode, True),
                                F.Binary(BinaryOperatorKind.Multiply, hashCode.Type,
                                    F.Binary(BinaryOperatorKind.Xor, hashCode.Type,
                                        textI,
                                        F.Local(hashCode, False)),
                                    New BoundLiteral(Me.Syntax, ConstantValue.Create(CUInt(16777619)), hashCode.Type))),
                            F.Assignment(
                                F.Local(i, True),
                                F.Binary(BinaryOperatorKind.Add, i.Type,
                                    F.Local(i, False),
                                    New BoundLiteral(Me.Syntax, ConstantValue.Create(1), i.Type))),
                            F.Label(start),
                            F.If(
                                F.Binary(BinaryOperatorKind.LessThan, Me.ContainingAssembly.GetSpecialType(SpecialType.System_Boolean),
                                    F.Local(i, False),
                                    F.Call(F.Parameter(text).MakeRValue(), DirectCast(Me.ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_String__Length), MethodSymbol))),
                                F.Goto(again)))),
                F.Return(F.Local(hashCode, False)))

            ' NOTE: we created this block in its most-lowered form, so analysis is unnecessary
            Return body
        End Function
    End Class

End Namespace
