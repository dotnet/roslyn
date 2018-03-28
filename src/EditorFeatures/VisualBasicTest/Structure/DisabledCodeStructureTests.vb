' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class DisabledCodeStructureProviderTests
        Inherits AbstractVisualBasicSyntaxTriviaStructureProviderTests

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New DisabledTextTriviaStructureProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
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
