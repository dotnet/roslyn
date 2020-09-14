' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
    Partial Friend Module VisualBasicCodeGenerator
        Private Function GenerateVariableDeclarator(
                type As ITypeSymbol,
                name As String,
                value As ExpressionSyntax) As VariableDeclaratorSyntax

            Return VariableDeclarator(
                SingletonSeparatedList(ModifiedIdentifier(name)),
                SimpleAsClause(type.GenerateTypeSyntax()),
                If(value Is Nothing, Nothing, EqualsValue(value)))
        End Function
    End Module
End Namespace
