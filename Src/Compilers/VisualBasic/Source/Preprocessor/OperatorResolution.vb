' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

' TODO: this is not in Preprocessor namespace as it may be generally useful.
Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Module OperatorResolution

        Private Enum TableKind
            Addition
            SubtractionMultiplicationModulo
            Division
            Power
            IntegerDivision
            Shift
            Logical
            Bitwise
            Relational
            ConcatenationLike
        End Enum

        ' TODO: need to fix the tables and remove the mapping. 
        ' It only exists because tables and enum have different order
        Private Function TypeCodeToIndex(tc As TypeCode) As Integer
            Select Case tc
                Case TypeCode.Empty
                    Return 0
                Case TypeCode.Boolean
                    Return 1
                Case TypeCode.SByte
                    Return 2
                Case TypeCode.Byte
                    Return 3
                Case TypeCode.Int16
                    Return 4
                Case TypeCode.UInt16
                    Return 5
                Case TypeCode.Int32
                    Return 6
                Case TypeCode.UInt32
                    Return 7
                Case TypeCode.Int64
                    Return 8
                Case TypeCode.UInt64
                    Return 9
                Case TypeCode.Decimal
                    Return 10
                Case TypeCode.Single
                    Return 11
                Case TypeCode.Double
                    Return 12
                Case TypeCode.DateTime
                    Return 13
                Case TypeCode.Char
                    Return 14
                Case TypeCode.String
                    Return 15
                Case TypeCode.Object
                    Return 16
            End Select
            Throw ExceptionUtilities.UnexpectedValue(tc)
        End Function

        ' PERF: Using Byte instead of TypeCode because we want the compiler to use array literal initialization.
        '       The most natural type choice, Enum arrays, are not blittable due to a CLR limitation.
        Private ReadOnly Table(,,) As Byte

        Sub New()
            ' */
            ' /****************************************************************************************
            ' BEGIN intrinsic operator tables
            ' ****************************************************************************************/

            Const t_r4 As Byte = CType(TypeCode.Single, Byte)
            Const t_r8 As Byte = CType(TypeCode.Double, Byte)
            Const t_dec As Byte = CType(TypeCode.Decimal, Byte)
            Const t_str As Byte = CType(TypeCode.String, Byte)

            Const t_bad As Byte = CType(TypeCode.Empty, Byte)
            Const t_i1 As Byte = CType(TypeCode.SByte, Byte)
            Const t_i2 As Byte = CType(TypeCode.Int16, Byte)
            Const t_i4 As Byte = CType(TypeCode.Int32, Byte)
            Const t_i8 As Byte = CType(TypeCode.Int64, Byte)
            Const t_ui1 As Byte = CType(TypeCode.Byte, Byte)
            Const t_ui2 As Byte = CType(TypeCode.UInt16, Byte)
            Const t_ui4 As Byte = CType(TypeCode.UInt32, Byte)
            Const t_ui8 As Byte = CType(TypeCode.UInt64, Byte)

            Const t_ref As Byte = CType(TypeCode.Object, Byte)
            Const t_bool As Byte = CType(TypeCode.Boolean, Byte)
            Const t_date As Byte = CType(TypeCode.DateTime, Byte)
            Const t_char As Byte = CType(TypeCode.Char, Byte)

            Const TYPE_NUM As Integer = 17
            Const NUM_OPERATORS As Integer = 10

            '// RHS->    bad     bool    i1      ui1     i2      ui2     i4      ui4     i8      ui8     dec     r4      r8      date    char    str     ref
            Table = New Byte(NUM_OPERATORS - 1, TYPE_NUM - 1, TYPE_NUM - 1) _
            {
                {  ' Addition
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_i2, t_i1, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i1, t_i1, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i2, t_i2, t_ui1, t_i2, t_ui2, t_i4, t_ui4, t_i8, t_ui8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i2, t_i2, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i4, t_i4, t_ui2, t_i4, t_ui2, t_i4, t_ui4, t_i8, t_ui8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i8, t_i8, t_ui4, t_i8, t_ui4, t_i8, t_ui4, t_i8, t_ui8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_dec, t_dec, t_ui8, t_dec, t_ui8, t_dec, t_ui8, t_dec, t_ui8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_str, t_bad, t_str, t_ref},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_str, t_str, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_str, t_str, t_str, t_ref},
                   {t_bad, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref}
               },
               { ' Subtraction, Multiplication, and Modulo. Special Note:  Date - Date is actually TimeSpan, but that cannot be encoded in this table.
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_i2, t_i1, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i1, t_i1, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i2, t_i2, t_ui1, t_i2, t_ui2, t_i4, t_ui4, t_i8, t_ui8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i2, t_i2, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i4, t_i4, t_ui2, t_i4, t_ui2, t_i4, t_ui4, t_i8, t_ui8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i8, t_i8, t_ui4, t_i8, t_ui4, t_i8, t_ui4, t_i8, t_ui8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_dec, t_dec, t_ui8, t_dec, t_ui8, t_dec, t_ui8, t_dec, t_ui8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_bad, t_bad, t_ref, t_ref}
               },
               { ' Division
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_bad, t_bad, t_ref, t_ref}
               },
               { ' Power
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_bad, t_bad, t_ref, t_ref}
               },
               { 'Integer Division
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_i2, t_i1, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i1, t_i1, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i2, t_i2, t_ui1, t_i2, t_ui2, t_i4, t_ui4, t_i8, t_ui8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i2, t_i2, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i4, t_i4, t_ui2, t_i4, t_ui2, t_i4, t_ui4, t_i8, t_ui8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_ui4, t_i8, t_ui4, t_i8, t_ui4, t_i8, t_ui8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_ui8, t_i8, t_ui8, t_i8, t_ui8, t_i8, t_ui8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_bad, t_bad, t_ref, t_ref}
               },
               { ' Shift. Note: The right operand serves little purpose in this table, however a table is utilized nonetheless to make the most use of already existing code which analyzes binary operators.
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_ref},
                   {t_bad, t_i1, t_i1, t_i1, t_i1, t_i1, t_i1, t_i1, t_i1, t_i1, t_i1, t_i1, t_i1, t_i1, t_i1, t_i1, t_ref},
                   {t_bad, t_ui1, t_ui1, t_ui1, t_ui1, t_ui1, t_ui1, t_ui1, t_ui1, t_ui1, t_ui1, t_ui1, t_ui1, t_ui1, t_ui1, t_ui1, t_ref},
                   {t_bad, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_i2, t_ref},
                   {t_bad, t_ui2, t_ui2, t_ui2, t_ui2, t_ui2, t_ui2, t_ui2, t_ui2, t_ui2, t_ui2, t_ui2, t_ui2, t_ui2, t_ui2, t_ui2, t_ref},
                   {t_bad, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_ref},
                   {t_bad, t_ui4, t_ui4, t_ui4, t_ui4, t_ui4, t_ui4, t_ui4, t_ui4, t_ui4, t_ui4, t_ui4, t_ui4, t_ui4, t_ui4, t_ui4, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_ref},
                   {t_bad, t_ui8, t_ui8, t_ui8, t_ui8, t_ui8, t_ui8, t_ui8, t_ui8, t_ui8, t_ui8, t_ui8, t_ui8, t_ui8, t_ui8, t_ui8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_ref},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_ref},
                   {t_bad, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref}
               },
               { 'Logical Operators
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bool, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_bad, t_bad, t_ref, t_ref}
               },
               { ' Bitwise Operators
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_bool, t_i1, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_i1, t_i1, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i2, t_i2, t_ui1, t_i2, t_ui2, t_i4, t_ui4, t_i8, t_ui8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i2, t_i2, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i4, t_i4, t_ui2, t_i4, t_ui2, t_i4, t_ui4, t_i8, t_ui8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_ui4, t_i8, t_ui4, t_i8, t_ui4, t_i8, t_ui8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_ui8, t_i8, t_ui8, t_i8, t_ui8, t_i8, t_ui8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_bool, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_bad, t_bad, t_i8, t_ref},
                   {t_bad, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_bad, t_bad, t_ref, t_ref}
               },
               { ' Relational Operators -- This one is a little unusual because it lists the type of the relational operation, even though the result type is always Boolean
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_bool, t_i1, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_bool, t_ref},
                   {t_bad, t_i1, t_i1, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i2, t_i2, t_ui1, t_i2, t_ui2, t_i4, t_ui4, t_i8, t_ui8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i2, t_i2, t_i2, t_i2, t_i4, t_i4, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i4, t_i4, t_ui2, t_i4, t_ui2, t_i4, t_ui4, t_i8, t_ui8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i4, t_i4, t_i4, t_i4, t_i4, t_i4, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i8, t_i8, t_ui4, t_i8, t_ui4, t_i8, t_ui4, t_i8, t_ui8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_i8, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_dec, t_dec, t_ui8, t_dec, t_ui8, t_dec, t_ui8, t_dec, t_ui8, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_dec, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r4, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_bad, t_bad, t_r8, t_ref},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_date, t_bad, t_date, t_ref},
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_char, t_str, t_ref},
                   {t_bad, t_bool, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_r8, t_date, t_str, t_str, t_ref},
                   {t_bad, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref}
               },
               { ' Concatenation and Like
                   {t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad, t_bad},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_str, t_ref},
                   {t_bad, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref, t_ref}
               }
            }

        End Sub

        Friend Function LookupInOperatorTables(
            Opcode As SyntaxKind,
            Left As TypeCode,
            Right As TypeCode
        ) As TypeCode
            Dim whichTable As TableKind

            Select Case (Opcode)

                Case SyntaxKind.AddExpression
                    whichTable = TableKind.Addition

                Case _
                    SyntaxKind.SubtractExpression,
                    SyntaxKind.MultiplyExpression,
                    SyntaxKind.ModuloExpression

                    whichTable = TableKind.SubtractionMultiplicationModulo

                Case _
                    SyntaxKind.DivideExpression

                    whichTable = TableKind.Division
                Case _
                    SyntaxKind.IntegerDivideExpression

                    whichTable = TableKind.IntegerDivision
                Case _
                    SyntaxKind.ExponentiateExpression

                    whichTable = TableKind.Power
                Case _
                    SyntaxKind.LeftShiftExpression,
                    SyntaxKind.RightShiftExpression

                    whichTable = TableKind.Shift
                Case _
                    SyntaxKind.OrElseExpression,
                    SyntaxKind.AndAlsoExpression

                    whichTable = TableKind.Logical
                Case _
                    SyntaxKind.ConcatenateExpression,
                    SyntaxKind.LikeExpression

                    whichTable = TableKind.ConcatenationLike
                Case _
                    SyntaxKind.EqualsExpression,
                    SyntaxKind.NotEqualsExpression,
                    SyntaxKind.LessThanOrEqualExpression,
                    SyntaxKind.GreaterThanOrEqualExpression,
                    SyntaxKind.LessThanExpression,
                    SyntaxKind.GreaterThanExpression

                    whichTable = TableKind.Relational
                Case _
                    SyntaxKind.OrExpression,
                    SyntaxKind.ExclusiveOrExpression,
                    SyntaxKind.AndExpression

                    whichTable = TableKind.Bitwise
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(Opcode)
            End Select

            Return CType(Table(whichTable, TypeCodeToIndex(Left), TypeCodeToIndex(Right)), TypeCode)
        End Function
    End Module

End Namespace