' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class SyncLockBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(SyncLockBlockHighlighter)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestSyncLockBlock1() As Task
            Await TestAsync(<Text>
Class C
Sub M()
{|Cursor:[|SyncLock|]|} Me
    Count += 1
[|End SyncLock|]
End Sub
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestSyncLockBlock2() As Task
            Await TestAsync(<Text>
Class C
Sub M()
[|SyncLock|] Me
    Count += 1
{|Cursor:[|End SyncLock|]|}
End Sub
End Class</Text>)
        End Function
    End Class
End Namespace
