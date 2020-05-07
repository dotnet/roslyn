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
        Private Function GenerateNamespaceBlockOrCompilationUnit(symbol As INamespaceSymbol) As SyntaxNode
            Dim [imports] = GenerateImportsStatements(CodeGenerator.GetImports(symbol))
            Dim members = GenerateMemberStatements(symbol.GetMembers())

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
    End Module
End Namespace
