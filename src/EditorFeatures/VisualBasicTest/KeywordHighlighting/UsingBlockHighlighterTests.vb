' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class UsingBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(UsingBlockHighlighter)
        End Function

        <Fact>
        Public Async Function TestUsingBlock1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|Using|]|} f = File.Open(name)
    Read(f)
[|End Using|]
End Sub
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestUsingBlock2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|Using|] f = File.Open(name)
    Read(f)
{|Cursor:[|End Using|]|}
End Sub
End Class</Text>)
        End Function
    End Class
End Namespace
