' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class SyncLockBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New SyncLockBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSyncLockBlock1()
            Test(<Text>
Class C
Sub M()
{|Cursor:[|SyncLock|]|} Me
    Count += 1
[|End SyncLock|]
End Sub
End Class</Text>)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub TestSyncLockBlock2()
            Test(<Text>
Class C
Sub M()
[|SyncLock|] Me
    Count += 1
{|Cursor:[|End SyncLock|]|}
End Sub
End Class</Text>)
        End Sub
    End Class
End Namespace
