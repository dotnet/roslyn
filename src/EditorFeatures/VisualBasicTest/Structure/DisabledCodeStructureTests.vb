' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class DisabledCodeStructureProviderTests
        Inherits AbstractVisualBasicSyntaxTriviaStructureProviderTests

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New DisabledTextTriviaStructureProvider()
        End Function

        <Fact>
        Public Async Function TestDisabledIf() As Task
            Const code = "
#If False
{|span:$$Blah
Blah
Blah|}
#End If
"
            Await VerifyBlockSpansAsync(code,
                Region("span", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestDisabledElse() As Task
            Const code = "
#If True
#Else
{|span:$$Blah
Blah
Blah|}
#End If
"
            Await VerifyBlockSpansAsync(code,
                Region("span", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestDisabledElseIf() As Task
            Const code = "
#If True
#ElseIf False
{|span:$$Blah
Blah
Blah|}
#End If
"
            Await VerifyBlockSpansAsync(code,
                Region("span", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Function
    End Class
End Namespace
