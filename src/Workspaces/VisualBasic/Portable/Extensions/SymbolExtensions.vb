' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module SymbolExtensions

        <Extension>
        Public Function IsMyNamespace(symbol As ISymbol, compilation As Compilation) As Boolean
            If symbol.Kind <> SymbolKind.Namespace OrElse symbol.Name <> "My" Then
                Return False
            End If

            Dim containingNamespace = symbol.ContainingNamespace

            Return containingNamespace IsNot Nothing AndAlso
                  (containingNamespace.IsGlobalNamespace OrElse Object.Equals(containingNamespace, compilation.RootNamespace))
        End Function

        <Extension>
        Public Function IsMyFormsProperty(symbol As ISymbol, compilation As Compilation) As Boolean
            If symbol.Kind <> SymbolKind.Property OrElse symbol.Name <> "Forms" Then
                Return False
            End If

            Dim type = DirectCast(symbol, IPropertySymbol).Type
            If type Is Nothing OrElse
               type.Name <> "MyForms" Then

                Return False
            End If

            Dim containingType = symbol.ContainingType
            If containingType Is Nothing OrElse
               containingType.ContainingType IsNot Nothing OrElse
               containingType.Name <> "MyProject" Then

                Return False
            End If

            Dim containingNamespace = containingType.ContainingNamespace

            Return containingNamespace.IsMyNamespace(compilation)
        End Function

    End Module
End Namespace
