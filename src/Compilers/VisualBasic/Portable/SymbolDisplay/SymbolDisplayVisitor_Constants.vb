' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class SymbolDisplayVisitor

        Protected Overrides Sub AddBitwiseOr()
            AddKeyword(SyntaxKind.OrKeyword)
        End Sub

        Protected Overrides Sub AddExplicitlyCastedLiteralValue(namedType As INamedTypeSymbol, type As SpecialType, value As Object)
            ' VB doesn't actually need to cast a literal value to get an enum value.  So we just add
            ' the literal value directly.
            AddLiteralValue(type, value)
        End Sub

        Protected Overrides Sub AddLiteralValue(type As SpecialType, value As Object)
            Debug.Assert(value.GetType().GetTypeInfo().IsPrimitive OrElse TypeOf value Is String OrElse TypeOf value Is Decimal OrElse TypeOf value Is DateTime)

            Select Case type
                Case SpecialType.System_String
                    SymbolDisplay.AddSymbolDisplayParts(Builder, DirectCast(value, String))

                Case SpecialType.System_Char
                    SymbolDisplay.AddSymbolDisplayParts(Builder, DirectCast(value, Char))

                Case Else
                    Dim valueString = SymbolDisplay.FormatPrimitive(value, quoteStrings:=True, useHexadecimalNumbers:=False)
                    Me.Builder.Add(CreatePart(SymbolDisplayPartKind.NumericLiteral, Nothing, valueString, False))
            End Select
        End Sub

        ''' <summary> Append a default argument (i.e. the default argument of an optional parameter). 
        ''' Assumed to be non-null. 
        ''' </summary>
        Private Sub AddConstantValue(type As ITypeSymbol, constantValue As Object, Optional preferNumericValueOrExpandedFlagsForEnum As Boolean = False)
            If constantValue IsNot Nothing Then
                AddNonNullConstantValue(type, constantValue, preferNumericValueOrExpandedFlagsForEnum)
            Else
                AddKeyword(SyntaxKind.NothingKeyword)
            End If
        End Sub
    End Class
End Namespace
