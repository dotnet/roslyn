' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class NamespaceBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(NamespaceBlockHighlighter)
        End Function

        <Fact>
        Public Async Function TestNamespace1() As Task
            Await TestAsync(<Text>
{|Cursor:[|Namespace|]|} N1
[|End Namespace|]</Text>)
        End Function

        <Fact>
        Public Async Function TestNamespace2() As Task
            Await TestAsync(<Text>
[|Namespace|] N1
{|Cursor:[|End Namespace|]|}</Text>)
        End Function
    End Class
End Namespace
