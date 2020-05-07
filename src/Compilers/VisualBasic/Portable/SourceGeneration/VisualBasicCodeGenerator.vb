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
        <Extension>
        Public Function GenerateString(
                symbol As ISymbol,
                Optional indentation As String = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation,
                Optional eol As String = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL,
                Optional elasticTrivia As Boolean = False) As String

            Return symbol.GenerateSyntax().NormalizeWhitespace(indentation, eol, elasticTrivia).ToFullString()
        End Function

        <Extension>
        Public Function GenerateNameString(
                symbol As INamespaceOrTypeSymbol,
                Optional indentation As String = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation,
                Optional eol As String = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL,
                Optional elasticTrivia As Boolean = False) As String

            Return symbol.GenerateNameSyntax().NormalizeWhitespace(indentation, eol, elasticTrivia).ToFullString()
        End Function

        <Extension>
        Public Function GenerateTypeString(
                symbol As INamespaceOrTypeSymbol,
                Optional indentation As String = CodeAnalysis.SyntaxNodeExtensions.DefaultIndentation,
                Optional eol As String = CodeAnalysis.SyntaxNodeExtensions.DefaultEOL,
                Optional elasticTrivia As Boolean = False) As String

            Return symbol.GenerateTypeSyntax().NormalizeWhitespace(indentation, eol, elasticTrivia).ToFullString()
        End Function

        <Extension>
        Public Function GenerateSyntax(symbol As ISymbol) As SyntaxNode
            Select Case symbol.Kind
                Case SymbolKind.Label
                    Return GenerateLabelIdentifierName(DirectCast(symbol, ILabelSymbol))
                Case SymbolKind.Namespace
                    Return GenerateNamespaceBlockOrCompilationUnit(DirectCast(symbol, INamespaceSymbol))
            End Select

            Throw New NotImplementedException()
        End Function

        Private Function GetArrayBuilder(Of T)() As BuilderDisposer(Of T)
            Return New BuilderDisposer(Of T)(ArrayBuilder(Of T).GetInstance())
        End Function

        Private Structure BuilderDisposer(Of T)
            Implements IDisposable

            Public ReadOnly Builder As ArrayBuilder(Of T)

            Public Sub New(arrayBuilder As ArrayBuilder(Of T))
                Builder = arrayBuilder
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                Builder.Free()
            End Sub
        End Structure
    End Module
End Namespace
