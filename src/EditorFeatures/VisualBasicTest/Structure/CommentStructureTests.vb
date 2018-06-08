' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Structure
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class CommentTests
        Inherits AbstractSyntaxTriviaStructureProviderTests
        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New CommentTriviaStructureProvider()
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSimpleComment1() As Task
            Const code = "
{|span:' $$Hello|}
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSimpleComment2() As Task
            Const code = "
{|span:' $$Hello
'
' VB!|}
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSimpleComment3() As Task
            Const code = "
{|span:' $$Hello

' VB!|}
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSingleLineCommentGroupFollowedByDocumentationComment() As Task
            Const code = "
{|span:' $$Hello

' VB!|}
''' <summary></summary>
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Function

    End Class
End Namespace
