' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class EnumBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(EnumBlockHighlighter)
        End Function

        <Fact>
        Public Async Function TestEnum1() As Task
            Await TestAsync(<Text>
{|Cursor:[|Enum|]|} E1
[|End Enum|]</Text>)
        End Function

        <Fact>
        Public Async Function TestEnum2() As Task
            Await TestAsync(<Text>
[|Enum|] E1
{|Cursor:[|End Enum|]|}</Text>)
        End Function
    End Class
End Namespace
