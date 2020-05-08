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
        Private Function GenerateEnumDeclaration(symbol As INamedTypeSymbol) As EnumBlockSyntax
            Using temp = GetArrayBuilder(Of StatementSyntax)()
                Dim builder = temp.Builder

                For Each member In symbol.GetMembers()
                    If Not member.IsImplicitlyDeclared AndAlso TypeOf member Is IFieldSymbol Then
                        builder.Add(GenerateEnumMemberDeclaration(DirectCast(member, IFieldSymbol)))
                    End If
                Next

                Return EnumBlock(EnumStatement(
                    GenerateAttributeLists(symbol.GetAttributes()),
                    GenerateModifiers(isType:=True, symbol.DeclaredAccessibility, symbol.GetModifiers()),
                    Identifier(symbol.Name),
                    If(symbol.BaseType Is Nothing, Nothing, SimpleAsClause(symbol.BaseType.GenerateTypeSyntax()))),
                    List(builder))
            End Using
        End Function

        Private Function GenerateEnumMemberDeclaration(member As IFieldSymbol) As EnumMemberDeclarationSyntax
            Dim expression = GenerateConstantExpression(member.Type, member.HasConstantValue, member.ConstantValue)
            Dim intializer = If(expression Is Nothing, Nothing, EqualsValue(expression))
            Return EnumMemberDeclaration(
                GenerateAttributeLists(member.GetAttributes()),
                Identifier(member.Name),
                intializer)
        End Function
    End Module
End Namespace
