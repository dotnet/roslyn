' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    Friend Module SymbolExtensions
        <Extension>
        Public Function FindRelatedExplicitlyDeclaredSymbol(symbol As ISymbol, compilation As Compilation) As ISymbol
            ' For example: My.Forms.[|LoginForm|]
            ' LoginForm is a SynthesizedMyGroupCollectionPropertySymbol with no Location. Use the
            ' type of this property, the actual LoginForm type itself, for navigation purposes.

            If symbol.IsKind(SymbolKind.Property) AndAlso symbol.IsImplicitlyDeclared Then
                Dim propertySymbol = DirectCast(symbol, IPropertySymbol)
                If propertySymbol.ContainingType IsNot Nothing AndAlso
                   propertySymbol.ContainingType.Name = "MyForms" AndAlso
                   propertySymbol.ContainingType.ContainingNamespace IsNot Nothing AndAlso
                   propertySymbol.ContainingType.ContainingNamespace.IsMyNamespace(compilation) Then

                    Return propertySymbol.Type
                End If
            End If

            Return symbol
        End Function
    End Module
End Namespace