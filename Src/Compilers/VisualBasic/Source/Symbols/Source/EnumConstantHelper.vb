Imports System.Diagnostics
Imports Roslyn.Compilers.Internal
Imports Roslyn.Utilities

Namespace Roslyn.Compilers.VisualBasic

    Friend Enum EnumOverflowKind
        NoOverflow
        OverflowReport
        OverflowIgnore
    End Enum

    Friend Module EnumConstantHelper
        ''' <summary>
        ''' Generate a ConstantValue of the same integer type as the argument
        ''' and offset by the given non-negative amount. Return ConstantValue.Bad
        ''' if the generated constant would be outside the valid range of the type.
        ''' </summary>
        Friend Function OffsetValue(constantValue As ConstantValue, offset As UInteger, ByRef value As ConstantValue) As EnumOverflowKind
            Contract.ThrowIfTrue(constantValue.IsBad)
            Contract.ThrowIfFalse(offset > 0)
            value = constantValue.Bad
            Dim overflowKind As EnumOverflowKind
            Select Case constantValue.Discriminator
                Case ConstantValueTypeDiscriminator.[SByte]
                    Dim previous As Long = constantValue.SByteValue
                    overflowKind = CheckOverflow(System.[SByte].MaxValue, previous, offset)
                    If overflowKind = EnumOverflowKind.NoOverflow Then
                        value = constantValue.Create(CType((previous + offset), SByte))
                    End If

                Case ConstantValueTypeDiscriminator.[Byte]
                    Dim previous As ULong = constantValue.ByteValue
                    overflowKind = CheckOverflow(System.[Byte].MaxValue, previous, offset)
                    If overflowKind = EnumOverflowKind.NoOverflow Then
                        value = constantValue.Create(CType((previous + offset), Byte))
                    End If

                Case ConstantValueTypeDiscriminator.Int16
                    Dim previous As Long = constantValue.Int16Value
                    overflowKind = CheckOverflow(System.Int16.MaxValue, previous, offset)
                    If overflowKind = EnumOverflowKind.NoOverflow Then
                        value = constantValue.Create(CType((previous + offset), Short))
                    End If

                Case ConstantValueTypeDiscriminator.UInt16
                    Dim previous As ULong = constantValue.UInt16Value
                    overflowKind = CheckOverflow(System.UInt16.MaxValue, previous, offset)
                    If overflowKind = EnumOverflowKind.NoOverflow Then
                        value = constantValue.Create(CType((previous + offset), UShort))
                    End If

                Case ConstantValueTypeDiscriminator.Int32
                    Dim previous As Long = constantValue.Int32Value
                    overflowKind = CheckOverflow(System.Int32.MaxValue, previous, offset)
                    If overflowKind = EnumOverflowKind.NoOverflow Then
                        value = constantValue.Create(CType((previous + offset), Integer))
                    End If

                Case ConstantValueTypeDiscriminator.UInt32
                    Dim previous As ULong = constantValue.UInt32Value
                    overflowKind = CheckOverflow(System.UInt32.MaxValue, previous, offset)
                    If overflowKind = EnumOverflowKind.NoOverflow Then
                        value = constantValue.Create(CType((previous + offset), UInteger))
                    End If

                Case ConstantValueTypeDiscriminator.Int64
                    Dim previous As Long = constantValue.Int64Value
                    overflowKind = CheckOverflow(System.Int64.MaxValue, previous, offset)
                    If overflowKind = EnumOverflowKind.NoOverflow Then
                        value = constantValue.Create(CType((previous + offset), Long))
                    End If

                Case ConstantValueTypeDiscriminator.UInt64
                    Dim previous As ULong = constantValue.UInt64Value
                    overflowKind = CheckOverflow(System.UInt64.MaxValue, previous, offset)
                    If overflowKind = EnumOverflowKind.NoOverflow Then
                        value = constantValue.Create(CType((previous + offset), ULong))
                    End If

                Case Else
                    Throw Contract.Unreachable
            End Select

            Return overflowKind
        End Function

        Private Function CheckOverflow(maxOffset As Long, previous As Long, offset As UInteger) As EnumOverflowKind
            Debug.Assert(maxOffset >= previous)
            Return CheckOverflow(CType((maxOffset - previous), ULong), offset)
        End Function

        Private Function CheckOverflow(maxOffset As ULong, previous As ULong, offset As UInteger) As EnumOverflowKind
            Debug.Assert(maxOffset >= previous)
            Return CheckOverflow(maxOffset - previous, offset)
        End Function

        Private Function CheckOverflow(maxOffset As ULong, offset As UInteger) As EnumOverflowKind
            Return If((offset <= maxOffset), EnumOverflowKind.NoOverflow, (If(((offset - 1) = maxOffset), EnumOverflowKind.OverflowReport, EnumOverflowKind.OverflowIgnore)))
        End Function
    End Module
End Namespace

