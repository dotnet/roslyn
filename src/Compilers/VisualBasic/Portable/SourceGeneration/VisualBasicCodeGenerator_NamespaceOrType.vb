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
        Private Function GenerateMember(member As INamespaceOrTypeSymbol) As StatementSyntax
            Return DirectCast(GenerateSyntax(member), StatementSyntax)
        End Function

        Private Function GenerateImports([imports] As ImmutableArray(Of INamespaceOrTypeSymbol)) As SyntaxList(Of ImportsStatementSyntax)
            Dim builder = ArrayBuilder(Of ImportsStatementSyntax).GetInstance()

            For Each import In [imports]
                builder.Add(
                    ImportsStatement(
                        SingletonSeparatedList(Of ImportsClauseSyntax)(SimpleImportsClause(GenerateName(import)))))
            Next

            Return List(builder.ToImmutableAndFree())
        End Function

        Private Function GenerateName(import As INamespaceOrTypeSymbol) As NameSyntax
            Throw New NotImplementedException()
        End Function

        Private Function GenerateMembers(members As IEnumerable(Of INamespaceOrTypeSymbol)) As SyntaxList(Of StatementSyntax)
            Dim builder = ArrayBuilder(Of StatementSyntax).GetInstance()

            For Each member In members
                builder.Add(GenerateMember(member))
            Next

            Return List(builder.ToImmutableAndFree())
        End Function
    End Module
End Namespace
