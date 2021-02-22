﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Differencing

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EditAndContinue

    Friend NotInheritable Class VisualBasicEditAndContinueTestHelpers
        Inherits EditAndContinueTestHelpers

        Private ReadOnly _analyzer As VisualBasicEditAndContinueAnalyzer

        Public Sub New(Optional faultInjector As Action(Of SyntaxNode) = Nothing)
            _analyzer = New VisualBasicEditAndContinueAnalyzer(faultInjector)
        End Sub

        Public Overrides ReadOnly Property Analyzer As AbstractEditAndContinueAnalyzer
            Get
                Return _analyzer
            End Get
        End Property

        Public Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Public Overrides ReadOnly Property TopSyntaxComparer As TreeComparer(Of SyntaxNode)
            Get
                Return CodeAnalysis.VisualBasic.EditAndContinue.TopSyntaxComparer.Instance
            End Get
        End Property

        Public Overrides Function ParseText(source As String) As SyntaxTree
            Return SyntaxFactory.ParseSyntaxTree(source, VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest))
        End Function

        Public Overrides Function FindNode(root As SyntaxNode, span As TextSpan) As SyntaxNode
            Dim result = root.FindToken(span.Start).Parent
            While result.Span <> span
                result = result.Parent
                Assert.NotNull(result)
            End While

            Return result
        End Function

        Public Overrides Function GetDeclarators(method As ISymbol) As ImmutableArray(Of SyntaxNode)
            Assert.True(TypeOf method Is IMethodSymbol, "Only methods should have a syntax map.")
            Return LocalVariableDeclaratorsCollector.GetDeclarators(DirectCast(method, SourceMethodSymbol))
        End Function
    End Class
End Namespace

