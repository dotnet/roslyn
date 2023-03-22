' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class OperatorDeclarationHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(OperatorDeclarationHighlighter)
        End Function

        <Fact>
        Public Async Function TestOperatorExample1_1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|Public Shared Operator|]|} +(v As Complex) As Complex
    [|Return|] v
[|End Operator|]
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestOperatorExample1_2() As Task
            Await TestAsync(<Text>
Class C
[|Public Shared Operator|] +(v As Complex) As Complex
    {|Cursor:[|Return|]|} v
[|End Operator|]
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestOperatorExample1_3() As Task
            Await TestAsync(<Text>
Class C
[|Public Shared Operator|] +(v As Complex) As Complex
    [|Return|] v
{|Cursor:[|End Operator|]|}
End Class</Text>)
        End Function
    End Class
End Namespace
