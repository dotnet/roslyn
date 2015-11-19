' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class ConstructorDeclarationHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New ConstructorDeclarationHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestConstructorExample1_1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|Public Sub New|]|}()
    [|Exit Sub|]
[|End Sub|]
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestConstructorExample1_2() As Task
            Await TestAsync(<Text>
Class C
[|Public Sub New|]()
    {|Cursor:[|Exit Sub|]|}
[|End Sub|]
End Class</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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
