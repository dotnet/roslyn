' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Module ITypeSymbolExtensions
        <Extension>
        Public Function GenerateExpressionSyntax(typeSymbol As ITypeSymbol) As ExpressionSyntax
            Return typeSymbol.Accept(New ExpressionSyntaxGeneratorVisitor(addGlobal:=True)).WithAdditionalAnnotations(Simplifier.Annotation)
        End Function

        <Extension()>
        Public Function GetPredefinedCastKeyword(specialType As SpecialType) As SyntaxKind
            Select Case specialType
                Case specialType.System_Boolean
                    Return SyntaxKind.CBoolKeyword
                Case specialType.System_Byte
                    Return SyntaxKind.CByteKeyword
                Case specialType.System_Char
                    Return SyntaxKind.CCharKeyword
                Case specialType.System_DateTime
                    Return SyntaxKind.CDateKeyword
                Case specialType.System_Decimal
                    Return SyntaxKind.CDecKeyword
                Case specialType.System_Double
                    Return SyntaxKind.CDblKeyword
                Case specialType.System_Int32
                    Return SyntaxKind.CIntKeyword
                Case specialType.System_Int64
                    Return SyntaxKind.CLngKeyword
                Case specialType.System_Object
                    Return SyntaxKind.CObjKeyword
                Case specialType.System_SByte
                    Return SyntaxKind.CSByteKeyword
                Case specialType.System_Single
                    Return SyntaxKind.CSngKeyword
                Case specialType.System_Int16
                    Return SyntaxKind.CShortKeyword
                Case SpecialType.System_String
                    Return SyntaxKind.CStrKeyword
                Case specialType.System_UInt32
                    Return SyntaxKind.CUIntKeyword
                Case specialType.System_UInt64
                    Return SyntaxKind.CULngKeyword
                Case specialType.System_UInt16
                    Return SyntaxKind.CUShortKeyword
                Case Else
                    Return SyntaxKind.None
            End Select
        End Function

        <Extension()>
        Public Function GetTypeFromPredefinedCastKeyword(compilation As Compilation, castKeyword As SyntaxKind) As ITypeSymbol
            Dim specialType As SpecialType
            Select Case castKeyword
                Case SyntaxKind.CBoolKeyword
                    specialType = specialType.System_Boolean
                Case SyntaxKind.CByteKeyword
                    specialType = specialType.System_Byte
                Case SyntaxKind.CCharKeyword
                    specialType = specialType.System_Char
                Case SyntaxKind.CDateKeyword
                    specialType = specialType.System_DateTime
                Case SyntaxKind.CDecKeyword
                    specialType = specialType.System_Decimal
                Case SyntaxKind.CDblKeyword
                    specialType = specialType.System_Double
                Case SyntaxKind.CIntKeyword
                    specialType = specialType.System_Int32
                Case SyntaxKind.CLngKeyword
                    specialType = specialType.System_Int64
                Case SyntaxKind.CObjKeyword
                    specialType = specialType.System_Object
                Case SyntaxKind.CSByteKeyword
                    specialType = specialType.System_SByte
                Case SyntaxKind.CSngKeyword
                    specialType = specialType.System_Single
                Case SyntaxKind.CStrKeyword
                    specialType = specialType.System_String
                Case SyntaxKind.CShortKeyword
                    specialType = specialType.System_Int16
                Case SyntaxKind.CUIntKeyword
                    specialType = specialType.System_UInt32
                Case SyntaxKind.CULngKeyword
                    specialType = specialType.System_UInt64
                Case SyntaxKind.CUShortKeyword
                    specialType = specialType.System_UInt16
                Case Else
                    Return Nothing
            End Select

            Return compilation.GetSpecialType(specialType)
        End Function

        <Extension>
        Public Function IsIntrinsicType(this As ITypeSymbol) As Boolean
            Select Case this.SpecialType
                Case SpecialType.System_Boolean,
                     SpecialType.System_Byte,
                     SpecialType.System_SByte,
                     SpecialType.System_Int16,
                     SpecialType.System_UInt16,
                     SpecialType.System_Int32,
                     SpecialType.System_UInt32,
                     SpecialType.System_Int64,
                     SpecialType.System_UInt64,
                     SpecialType.System_Single,
                     SpecialType.System_Double,
                     SpecialType.System_Decimal,
                     SpecialType.System_DateTime,
                     SpecialType.System_Char,
                     SpecialType.System_String
                    Return True
                Case Else
                    Return False
            End Select
        End Function
    End Module
End Namespace
