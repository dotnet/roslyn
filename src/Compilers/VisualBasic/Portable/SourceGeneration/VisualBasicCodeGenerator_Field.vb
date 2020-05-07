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
        Private Function GenerateFieldDeclaration(symbol As IFieldSymbol) As FieldDeclarationSyntax
            Dim modifiers = GenerateModifiers(isType:=False, symbol.DeclaredAccessibility, symbol.GetModifiers())
            If modifiers.Count = 0 Then
                modifiers = TokenList(Token(SyntaxKind.DimKeyword))
            End If

            Return FieldDeclaration(
                GenerateAttributeLists(symbol.GetAttributes()),
                modifiers,
                SingletonSeparatedList(
                    GenerateVariableDeclarator(symbol.Type, symbol.Name,
                        GenerateConstantExpression(symbol.Type, symbol.HasConstantValue, symbol.ConstantValue))))
        End Function
    End Module
End Namespace
