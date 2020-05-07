' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.SourceGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
    Partial Friend Module VisualBasicCodeGenerator
        Private Function GenerateNamedType(symbol As INamedTypeSymbol) As TypeSyntax
            If symbol.SpecialType <> SpecialType.None Then
                Return GenerateSpecialType(symbol)
            End If

            Throw New NotImplementedException()
        End Function

        Private Function GenerateSpecialType(symbol As INamedTypeSymbol) As TypeSyntax
            Select Case symbol.SpecialType
                Case SpecialType.System_Object
                    Return PredefinedType(Token(SyntaxKind.ObjectKeyword))
                Case SpecialType.System_Boolean
                    Return PredefinedType(Token(SyntaxKind.BooleanKeyword))
                Case SpecialType.System_Char
                    Return PredefinedType(Token(SyntaxKind.CharKeyword))
                Case SpecialType.System_SByte
                    Return PredefinedType(Token(SyntaxKind.SByteKeyword))
                Case SpecialType.System_Byte
                    Return PredefinedType(Token(SyntaxKind.ByteKeyword))
                Case SpecialType.System_Int16
                    Return PredefinedType(Token(SyntaxKind.ShortKeyword))
                Case SpecialType.System_UInt16
                    Return PredefinedType(Token(SyntaxKind.UShortKeyword))
                Case SpecialType.System_Int32
                    Return PredefinedType(Token(SyntaxKind.IntegerKeyword))
                Case SpecialType.System_UInt32
                    Return PredefinedType(Token(SyntaxKind.UIntegerKeyword))
                Case SpecialType.System_Int64
                    Return PredefinedType(Token(SyntaxKind.LongKeyword))
                Case SpecialType.System_UInt64
                    Return PredefinedType(Token(SyntaxKind.ULongKeyword))
                Case SpecialType.System_Decimal
                    Return PredefinedType(Token(SyntaxKind.DecimalKeyword))
                Case SpecialType.System_Single
                    Return PredefinedType(Token(SyntaxKind.SingleKeyword))
                Case SpecialType.System_Double
                    Return PredefinedType(Token(SyntaxKind.DoubleKeyword))
                Case SpecialType.System_String
                    Return PredefinedType(Token(SyntaxKind.StringKeyword))
                Case SpecialType.System_DateTime
                    Return PredefinedType(Token(SyntaxKind.DateKeyword))
            End Select

            Throw New NotImplementedException()
        End Function
    End Module
End Namespace
