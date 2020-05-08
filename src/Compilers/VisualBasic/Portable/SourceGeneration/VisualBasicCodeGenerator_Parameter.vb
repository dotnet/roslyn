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
        Private Function GenerateParameterList(parameters As ImmutableArray(Of IParameterSymbol)) As ParameterListSyntax
            Using temp = GetArrayBuilder(Of ParameterSyntax)()
                Dim builder = temp.Builder

                For Each param In parameters
                    builder.Add(GenerateParameter(param))
                Next

                Return ParameterList(SeparatedList(builder))
            End Using
        End Function

        Private Function GenerateParameter(param As IParameterSymbol) As ParameterSyntax
            Dim expr = GenerateConstantExpression(param.Type, param.HasExplicitDefaultValue, param.ExplicitDefaultValue)
            Dim init = If(expr Is Nothing, Nothing, EqualsValue(expr))
            Return Parameter(
                GenerateAttributeLists(param.GetAttributes()),
                GenerateModifiers(isType:=False, param.DeclaredAccessibility, param.GetModifiers()),
                ModifiedIdentifier(param.Name),
                SimpleAsClause(param.Type.GenerateTypeSyntax()),
                init)
        End Function
    End Module
End Namespace
