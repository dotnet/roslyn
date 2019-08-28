' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EditAndContinue

    Friend NotInheritable Class VisualBasicEditAndContinueTestHelpers
        Inherits EditAndContinueTestHelpers

        Private ReadOnly _fxReferences As ImmutableArray(Of PortableExecutableReference)

        Friend Shared ReadOnly Instance As VisualBasicEditAndContinueTestHelpers = New VisualBasicEditAndContinueTestHelpers(
            ImmutableArray.Create(TestReferences.NetFx.v4_0_30319_17626.mscorlib, TestReferences.NetFx.v4_0_30319.System, TestReferences.NetFx.v4_0_30319.System_Core))

        Friend Shared ReadOnly Instance40 As VisualBasicEditAndContinueTestHelpers = New VisualBasicEditAndContinueTestHelpers(
            ImmutableArray.Create(TestReferences.NetFx.v4_0_30319.mscorlib, TestReferences.NetFx.v4_0_30319.System_Core))

        Friend Shared ReadOnly InstanceMinAsync As VisualBasicEditAndContinueTestHelpers = New VisualBasicEditAndContinueTestHelpers(
            ImmutableArray.Create(TestReferences.NetFx.Minimal.mincorlib, TestReferences.NetFx.Minimal.minasync))

        Private Shared ReadOnly s_analyzer As VisualBasicEditAndContinueAnalyzer = New VisualBasicEditAndContinueAnalyzer()

        Sub New(fxReferences As ImmutableArray(Of PortableExecutableReference))
            _fxReferences = fxReferences
        End Sub

        Public Overrides ReadOnly Property Analyzer As AbstractEditAndContinueAnalyzer
            Get
                Return s_analyzer
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

