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
    Friend Module VisualBasicCodeGenerator
        <Extension>
        Public Function GenerateString(
                symbol As ISymbol,
                Optional indentation As String = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation,
                Optional eol As String = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL,
                Optional elasticTrivia As Boolean = False) As String

            Return symbol.GenerateSyntax(indentation, eol, elasticTrivia).ToFullString()
        End Function

        <Extension>
        Public Function GenerateSyntax(
                symbol As ISymbol,
                Optional indentation As String = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation,
                Optional eol As String = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL,
                Optional elasticTrivia As Boolean = False) As SyntaxNode

            Return GenerateSyntaxWorker(symbol).NormalizeWhitespace(indentation, eol, elasticTrivia)
        End Function

        Private Function GenerateSyntaxWorker(symbol As ISymbol) As SyntaxNode
            Select Case symbol.Kind
                Case SymbolKind.Namespace
                    Return GenerateNamespace(DirectCast(symbol, INamespaceSymbol))
            End Select

            Throw New NotImplementedException()
        End Function

        Private Function GenerateNamespace(symbol As INamespaceSymbol) As SyntaxNode
            Dim [imports] = GenerateImports(CodeGenerator.GetImports(symbol))
            Dim members = GenerateMembers(symbol.GetMembers())

            If symbol.IsGlobalNamespace Then
                Return CompilationUnit(options:=Nothing, [imports], attributes:=Nothing, members)
            End If

            If [imports].Count > 0 Then
                Throw New ArgumentException("VisualBasic namespaces cannot contain imports.")
            End If

            Return NamespaceBlock(
                NamespaceStatement(ParseName(symbol.Name)),
                members)
        End Function

        Private Function GenerateMembers(members As IEnumerable(Of INamespaceOrTypeSymbol)) As SyntaxList(Of StatementSyntax)
            Dim builder = ArrayBuilder(Of StatementSyntax).GetInstance()

            For Each member In members
                builder.Add(GenerateMember(member))
            Next

            Return List(builder.ToImmutableAndFree())
        End Function

        Private Function GenerateMember(member As INamespaceOrTypeSymbol) As StatementSyntax
            Return DirectCast(GenerateSyntaxWorker(member), StatementSyntax)
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
    End Module
End Namespace
