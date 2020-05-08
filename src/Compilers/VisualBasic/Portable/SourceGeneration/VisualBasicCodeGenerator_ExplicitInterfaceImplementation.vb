' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
    Partial Friend Module VisualBasicCodeGenerator
        Private Function GenerateImplementsClause(Of TSymbol As ISymbol)(explicitInterfaceImplementations As ImmutableArray(Of TSymbol)) As ImplementsClauseSyntax
            Using temp = GetArrayBuilder(Of QualifiedNameSyntax)()
                Dim members = temp.Builder

                For Each symbol In explicitInterfaceImplementations
                    members.Add(QualifiedName(
                        symbol.ContainingType.GenerateNameSyntax(),
                        IdentifierName(symbol.Name)))
                Next

                Return If(members.Count = 0,
                    Nothing,
                    ImplementsClause(SeparatedList(members)))
            End Using
        End Function
    End Module
End Namespace
