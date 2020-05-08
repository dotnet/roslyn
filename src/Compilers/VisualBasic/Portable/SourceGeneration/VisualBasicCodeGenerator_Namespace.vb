' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.SourceGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
    Partial Friend Module VisualBasicCodeGenerator
        Private Function GenerateNamespaceBlockOrCompilationUnit(symbol As INamespaceSymbol) As SyntaxNode
            Dim [imports] = GenerateImportsStatements(CodeGenerator.GetImports(symbol))
            Dim members = GenerateMemberStatements(DirectCast(symbol.GetMembers(), IEnumerable(Of ISymbol)).ToImmutableArray())

            If symbol.IsGlobalNamespace Then
                Return CompilationUnit(options:=Nothing, [imports], attributes:=Nothing, members)
            End If

            If [imports].Count > 0 Then
                Throw New ArgumentException("VisualBasic Namespaces cannot contain Imports.")
            End If

            Return NamespaceBlock(
                NamespaceStatement(ParseName(symbol.Name)),
                members)
        End Function

        Private Function GenerateNameSyntax(symbol As INamespaceSymbol) As NameSyntax
            If symbol.IsGlobalNamespace Then
                Return SyntaxFactory.IdentifierName(SyntaxFacts.GetText(SyntaxKind.GlobalKeyword))
            End If

            Dim nameSyntax = IdentifierName(symbol.Name)
            If symbol.ContainingNamespace Is Nothing Then
                Return nameSyntax
            End If

            Dim containingNamespace = symbol.ContainingNamespace.GenerateNameSyntax()
            Return QualifiedName(containingNamespace, nameSyntax)
        End Function
    End Module
End Namespace
