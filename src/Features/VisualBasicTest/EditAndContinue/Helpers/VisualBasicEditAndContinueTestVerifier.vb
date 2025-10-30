' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
Imports Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EditAndContinue

    Friend NotInheritable Class VisualBasicEditAndContinueTestVerifier
        Inherits EditAndContinueTestVerifier

        Public Sub New()
            MyBase.New(faultInjector:=Nothing)
        End Sub

        Public Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Public Overrides ReadOnly Property ProjectFileExtension As String
            Get
                Return ".vbproj"
            End Get
        End Property

        Public Overrides ReadOnly Property TopSyntaxComparer As TreeComparer(Of SyntaxNode)
            Get
                Return SyntaxComparer.TopLevel
            End Get
        End Property

        Public Overrides Function GetDeclarators(method As ISymbol) As ImmutableArray(Of SyntaxNode)
            Assert.True(TypeOf method Is IMethodSymbol, "Only methods should have a syntax map.")
            Return LocalVariableDeclaratorsCollector.GetDeclarators(DirectCast(method, SourceMethodSymbol))
        End Function

        Public Overrides Function TryGetResource(keyword As String) As String
            Return EditingTestBase.TryGetResource(keyword)
        End Function

        Public Overrides ReadOnly Property ParseOptions As ParseOptions
            Get
                Return VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)
            End Get
        End Property
    End Class
End Namespace

