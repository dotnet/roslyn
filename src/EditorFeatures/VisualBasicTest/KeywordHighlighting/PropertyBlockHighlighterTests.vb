' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class PropertyBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New PropertyBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestPropertySample1_1()
            Test(<Text>
Class C
{|Cursor:[|Public Property|]|} Foo As Integer [|Implements|] IFoo.Foo
    Get
        Return 1
    End Get
    Private Set(value As Integer)
        Exit Property
    End Set
[|End Property|]
End Class</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestPropertySample1_2()
            Test(<Text>
Class C
[|Public Property|] Foo As Integer {|Cursor:[|Implements|]|} IFoo.Foo
    Get
        Return 1
    End Get
    Private Set(value As Integer)
        Exit Property
    End Set
[|End Property|]
End Class</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestPropertySample1_3()
            Test(<Text>
Class C
[|Public Property|] Foo As Integer [|Implements|] IFoo.Foo
    Get
        Return 1
    End Get
    Private Set(value As Integer)
        Exit Property
    End Set
{|Cursor:[|End Property|]|}
End Class</Text>)
        End Sub
    End Class
End Namespace
