' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class OperatorDeclarationHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New OperatorDeclarationHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestOperatorExample1_1()
            Test(<Text>
Class C
{|Cursor:[|Public Shared Operator|]|} +(v As Complex) As Complex
    [|Return|] v
[|End Operator|]
End Class</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestOperatorExample1_2()
            Test(<Text>
Class C
[|Public Shared Operator|] +(v As Complex) As Complex
    {|Cursor:[|Return|]|} v
[|End Operator|]
End Class</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestOperatorExample1_3()
            Test(<Text>
Class C
[|Public Shared Operator|] +(v As Complex) As Complex
    [|Return|] v
{|Cursor:[|End Operator|]|}
End Class</Text>)
        End Sub
    End Class
End Namespace
