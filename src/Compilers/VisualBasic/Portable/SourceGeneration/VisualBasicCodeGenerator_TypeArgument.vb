' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
    Partial Friend Module VisualBasicCodeGenerator
        Private Function GenerateTypeArgumentList(typeArguments As ImmutableArray(Of ITypeSymbol)) As TypeArgumentListSyntax
            Using temp = GetArrayBuilder(Of TypeSyntax)()
                Dim builder = temp.Builder

                For Each arg In typeArguments
                    builder.Add(arg.GenerateTypeSyntax())
                Next

                Return TypeArgumentList(SeparatedList(builder))
            End Using
        End Function
    End Module
End Namespace
