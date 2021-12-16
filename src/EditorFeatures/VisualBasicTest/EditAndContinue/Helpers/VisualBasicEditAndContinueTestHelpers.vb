' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EditAndContinue

    Friend NotInheritable Class VisualBasicEditAndContinueTestHelpers
        Inherits EditAndContinueTestHelpers

        Private ReadOnly _analyzer As VisualBasicEditAndContinueAnalyzer = New VisualBasicEditAndContinueAnalyzer()

        Private ReadOnly _fxReferences As ImmutableArray(Of PortableExecutableReference)

        Friend Shared Function CreateInstance() As VisualBasicEditAndContinueTestHelpers
            Return New VisualBasicEditAndContinueTestHelpers(
                ImmutableArray.Create(TestMetadata.Net451.mscorlib, TestMetadata.Net451.System, TestMetadata.Net451.SystemCore))
        End Function

        Friend Shared Function CreateInstance40() As VisualBasicEditAndContinueTestHelpers
            Return New VisualBasicEditAndContinueTestHelpers(
                ImmutableArray.Create(TestMetadata.Net40.mscorlib, TestMetadata.Net40.SystemCore))
        End Function

        Friend Shared Function CreateInstanceMinAsync() As VisualBasicEditAndContinueTestHelpers
            Return New VisualBasicEditAndContinueTestHelpers(
                ImmutableArray.Create(TestReferences.NetFx.Minimal.mincorlib, TestReferences.NetFx.Minimal.minasync))
        End Function

        Public Sub New(fxReferences As ImmutableArray(Of PortableExecutableReference))
            _fxReferences = fxReferences
        End Sub

        Public Overrides ReadOnly Property Analyzer As AbstractEditAndContinueAnalyzer
            Get
                Return _analyzer
            End Get
        End Property

        Public Overrides Function CreateLibraryCompilation(name As String, trees As IEnumerable(Of SyntaxTree)) As Compilation
            Return VisualBasicCompilation.Create("New",
                                                 trees,
                                                 _fxReferences,
                                                 TestOptions.ReleaseDll.WithEmbedVbCoreRuntime(True))
        End Function

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

