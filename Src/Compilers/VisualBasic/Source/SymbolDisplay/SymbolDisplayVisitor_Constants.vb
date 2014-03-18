' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

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
            Debug.Assert(value.GetType().IsPrimitive OrElse TypeOf value Is String OrElse TypeOf value Is Decimal OrElse TypeOf value Is DateTime)

            Select Case type
                Case SpecialType.System_String
                    VbStringDisplay.AddSymbolDisplayParts(builder, DirectCast(value, String))

                Case SpecialType.System_Char
                    VbStringDisplay.AddSymbolDisplayParts(builder, DirectCast(value, Char))

                Case Else
                    Dim valueString = SymbolDisplay.FormatPrimitive(value, quoteStrings:=True, useHexadecimalNumbers:=False)
                    Me.builder.Add(CreatePart(SymbolDisplayPartKind.NumericLiteral, Nothing, valueString, False))
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