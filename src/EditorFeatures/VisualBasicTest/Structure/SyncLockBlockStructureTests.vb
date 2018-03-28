' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class SyncLockBlockStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of SyncLockBlockSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New SyncLockBlockStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSyncLockBlock1() As Task
            Const code = "
Class C
    Sub M()
        {|span:SyncLock x $$
        End SyncLock|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "SyncLock x ...", autoCollapse:=False))
        End Function
    End Class
End Namespace
