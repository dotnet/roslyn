' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class UsingBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New UsingBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestUsingBlock1()
            Test(<Text>
Class C
Sub M()
{|Cursor:[|Using|]|} f = File.Open(name)
    Read(f)
[|End Using|]
End Sub
End Class</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestUsingBlock2()
            Test(<Text>
Class C
Sub M()
[|Using|] f = File.Open(name)
    Read(f)
{|Cursor:[|End Using|]|}
End Sub
End Class</Text>)
        End Sub
    End Class
End Namespace
