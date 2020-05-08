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
        Private Function GenerateTypeSyntax(symbol As ITypeSymbol, onlyNames As Boolean) As TypeSyntax
            Select Case symbol.Kind
                Case SymbolKind.ArrayType
                    Return GenerateArrayTypeSyntax(DirectCast(symbol, IArrayTypeSymbol), onlyNames)
                Case SymbolKind.DynamicType
                    Return GenerateDynamicTypeSyntax(DirectCast(symbol, IDynamicTypeSymbol))
                Case SymbolKind.NamedType
                    Return GenerateNamedTypeSyntax(DirectCast(symbol, INamedTypeSymbol), onlyNames)
            End Select

            Throw New NotImplementedException()

        End Function
    End Module
End Namespace
