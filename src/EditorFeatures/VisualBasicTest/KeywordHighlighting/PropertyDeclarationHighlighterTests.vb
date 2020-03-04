﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class PropertyDeclarationHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New PropertyDeclarationHighlighter()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestAutoProperty1() As Task
            Await TestAsync(<Text>
Class C
{|Cursor:[|Public Property|]|} Goo As Integer [|Implements|] IGoo.Goo
End Class</Text>)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestAutoProperty2() As Task
            Await TestAsync(<Text>
Class C
[|Public Property|] Goo As Integer {|Cursor:[|Implements|]|} IGoo.Goo
End Class</Text>)
        End Function

    End Class
End Namespace
