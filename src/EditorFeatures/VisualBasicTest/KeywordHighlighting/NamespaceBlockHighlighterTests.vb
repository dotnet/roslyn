' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class NamespaceBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New NamespaceBlockHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestNamespace1() As Task
            Await TestAsync(<Text>
{|Cursor:[|Namespace|]|} N1
[|End Namespace|]</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestNamespace2() As Task
            Await TestAsync(<Text>
[|Namespace|] N1
{|Cursor:[|End Namespace|]|}</Text>)
        End Function
    End Class
End Namespace
