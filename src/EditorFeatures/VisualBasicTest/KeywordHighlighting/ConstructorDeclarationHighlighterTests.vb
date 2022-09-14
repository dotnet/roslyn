' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class ConstructorDeclarationHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(ConstructorDeclarationHighlighter)
        End Function

        <Fact>
        Public Async Function TestConstructorExample1_1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|Public Sub New|]|}()
    [|Exit Sub|]
[|End Sub|]
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestConstructorExample1_2() As Task
            Await TestAsync(<Text>
Class C
[|Public Sub New|]()
    {|Cursor:[|Exit Sub|]|}
[|End Sub|]
End Class</Text>)
        End Function

        <Fact>
        Public Async Function TestConstructorExample1_3() As Task
            Await TestAsync(<Text>
Class C
[|Public Sub New|]()
    [|Exit Sub|]
{|Cursor:[|End Sub|]|}
End Class</Text>)
        End Function
    End Class
End Namespace
