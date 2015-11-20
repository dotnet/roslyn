' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class DisabledCodeOutlinerTests
        Inherits AbstractVisualBasicSyntaxTriviaOutlinerTests

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New DisabledTextTriviaOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDisabledIf()
            Const code = "
#If False
{|span:$$Blah
Blah
Blah|}
#End If
"
            Regions(code,
                Region("span", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDisabledElse()
            Const code = "
#If True
#Else
{|span:$$Blah
Blah
Blah|}
#End If
"
            Regions(code,
                Region("span", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDisabledElseIf()
            Const code = "
#If True
#ElseIf False
{|span:$$Blah
Blah
Blah|}
#End If
"
            Regions(code,
                Region("span", VisualBasicOutliningHelpers.Ellipsis, autoCollapse:=True))
        End Sub

    End Class
End Namespace
