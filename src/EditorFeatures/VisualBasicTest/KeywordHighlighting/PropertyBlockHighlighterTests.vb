' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class PropertyBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(PropertyBlockHighlighter)
        End Function

        <Fact>
        Public Async Function TestPropertySample1_1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|Public Property|]|} Goo As Integer [|Implements|] IGoo.Goo
    Get
        Return 1
    End Get
    Private Set(value As Integer)
        Exit Property
    End Set
[|End Property|]
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestPropertySample1_2() As Task
            Await TestAsync(<Text>
Class C
[|Public Property|] Goo As Integer {|Cursor:[|Implements|]|} IGoo.Goo
    Get
        Return 1
    End Get
    Private Set(value As Integer)
        Exit Property
    End Set
[|End Property|]
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestPropertySample1_3() As Task
            Await TestAsync(<Text>
Class C
[|Public Property|] Goo As Integer [|Implements|] IGoo.Goo
    Get
        Return 1
    End Get
    Private Set(value As Integer)
        Exit Property
    End Set
{|Cursor:[|End Property|]|}
End Class</Text>)
        End Function
    End Class
End Namespace
