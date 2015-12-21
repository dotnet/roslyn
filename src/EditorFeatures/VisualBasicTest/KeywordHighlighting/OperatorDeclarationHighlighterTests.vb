' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class OperatorDeclarationHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New OperatorDeclarationHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestOperatorExample1_1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|Public Shared Operator|]|} +(v As Complex) As Complex
    [|Return|] v
[|End Operator|]
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestOperatorExample1_2() As Task
            Await TestAsync(<Text>
Class C
[|Public Shared Operator|] +(v As Complex) As Complex
    {|Cursor:[|Return|]|} v
[|End Operator|]
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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
