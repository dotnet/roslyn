' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Analyzer.Utilities.Extensions
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Diagnostics.Analyzers

Namespace Roslyn.Diagnostics.VisualBasic.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicSymbolDeclaredEventAnalyzer
        Inherits SymbolDeclaredEventAnalyzer(Of SyntaxKind)

        Private Const SourceModuleTypeFullName As String = "Microsoft.CodeAnalysis.VisualBasic.Symbols.SourceModuleSymbol"

        Protected Overrides Function GetCompilationAnalyzer(compilation As Compilation, symbolType As INamedTypeSymbol) As CompilationAnalyzer
            Dim compilationType = compilation.GetOrCreateTypeByMetadataName(GetType(VisualBasicCompilation).FullName)
            If compilationType Is Nothing Then
                Return Nothing
            End If

            Dim sourceModuleType = compilation.GetOrCreateTypeByMetadataName(SourceModuleTypeFullName)
            If sourceModuleType Is Nothing Then
                Return Nothing
            End If

            Return New BasicCompilationAnalyzer(symbolType, compilationType, sourceModuleType)
        End Function

        Protected Overrides ReadOnly Property InvocationExpressionSyntaxKind As SyntaxKind
            Get
                Return SyntaxKind.InvocationExpression
            End Get
        End Property

        Private NotInheritable Class BasicCompilationAnalyzer
            Inherits CompilationAnalyzer

            Private ReadOnly _sourceModuleType As INamedTypeSymbol

            Private Shared ReadOnly s_symbolTypesWithExpectedSymbolDeclaredEvent As ImmutableHashSet(Of String) =
                ImmutableHashSet.Create("SourceNamespaceSymbol", "SourceNamedTypeSymbol", "SourceEventSymbol", "SourceFieldSymbol", "SourceMethodSymbol", "SourcePropertySymbol")
            Private Const s_atomicSetFlagAndRaiseSymbolDeclaredEventName As String = "AtomicSetFlagAndRaiseSymbolDeclaredEvent"

            Public Sub New(symbolType As INamedTypeSymbol, compilationType As INamedTypeSymbol, sourceModuleSymbol As INamedTypeSymbol)
                MyBase.New(symbolType, compilationType)
                Me._sourceModuleType = sourceModuleSymbol
            End Sub

            Protected Overrides ReadOnly Property SymbolTypesWithExpectedSymbolDeclaredEvent As ImmutableHashSet(Of String)
                Get
                    Return s_symbolTypesWithExpectedSymbolDeclaredEvent
                End Get
            End Property

            Protected Overrides Function GetFirstArgumentOfInvocation(invocation As SyntaxNode) As SyntaxNode
                Dim invocationExpression = DirectCast(invocation, InvocationExpressionSyntax)
                If invocationExpression.ArgumentList IsNot Nothing Then
                    Dim argument = invocationExpression.ArgumentList.Arguments.FirstOrDefault()
                    If argument IsNot Nothing Then
                        Return argument.GetExpression
                    End If
                End If

                Return Nothing
            End Function

            Friend Overrides Sub AnalyzeMethodInvocation(invocationSymbol As IMethodSymbol, context As SyntaxNodeAnalysisContext)
                If invocationSymbol.Name.Equals(s_atomicSetFlagAndRaiseSymbolDeclaredEventName, StringComparison.OrdinalIgnoreCase) AndAlso
                    _sourceModuleType.Equals(invocationSymbol.ContainingType) Then

                    Dim invocationExp = DirectCast(context.Node, InvocationExpressionSyntax)
                    If invocationExp.ArgumentList IsNot Nothing Then
                        For Each argument In invocationExp.ArgumentList.Arguments
                            If AnalyzeSymbolDeclaredEventInvocation(argument.GetExpression, context) Then
                                Exit For
                            End If
                        Next
                    End If
                End If

                MyBase.AnalyzeMethodInvocation(invocationSymbol, context)
            End Sub
        End Class
    End Class
End Namespace
