' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class SyncLockBlockHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New SyncLockBlockHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
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
