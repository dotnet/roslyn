' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class SymbolDisplayVisitor

        Protected Overrides Sub AddBitwiseOr()
            AddKeyword(SyntaxKind.OrKeyword)
        End Sub

        Protected Overrides Sub AddExplicitlyCastedLiteralValue(namedType As INamedTypeSymbol, value As Object)
            Debug.Assert(namedType.TypeKind = TypeKind.Enum)

            ' VB doesn't actually need to cast a literal value to get an enum value.  So we just add
            ' the literal value directly.
            AddNonEnumConstantValue(namedType.EnumUnderlyingType, value)
        End Sub

        ''' <summary>
        ''' Append a default argument (i.e. the default argument of an optional parameter) of a non-enum type.
        ''' </summary>
        Protected Overrides Sub AddNonEnumConstantValue(type As ITypeSymbol, value As Object)
            Debug.Assert(type.TypeKind <> TypeKind.Enum)

            SymbolDisplay.AddConstantValue(builder, value, format.ConstantValueOptions)
        End Sub
    End Class
End Namespace
