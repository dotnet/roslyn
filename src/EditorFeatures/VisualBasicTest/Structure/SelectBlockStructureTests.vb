' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class SelectBlockStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of SelectBlockSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New SelectBlockStructureProvider(IncludeAdditionalInternalSpans:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSelectBlock1() As Task
            Const code = "
Class C
    Sub M()
        {|span:Select (goo) $$
        End Select|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Select (goo) ...", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSelectBlock2() As Task
            Const code = "
Class C
    Sub M()
        Dim value = 10
{|span0:Select Case value $$
    {|span1:Case Is < 0
                '
                Console.WriteLine(""Negative"")
                '|}
    {|span2:Case Is = 0
                '
                Console.WriteLine(""Zero"")
                '|}
    {|span3:Case 1 
                '
                Console.WriteLine(""1"")
                '|}
    {|span4:Case 2, 3
                '
                Console.WriteLine(""Two or Three"")
                '|}
    {|span5:Case 4 To 10
                '
                Console.WriteLine(""Four To Ten"")
                '|}
    {|span6:Case Else
                '
                Console.WriteLine(""Somethingelse"")
                '|}
        End Select|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
            Region("span0", "Select Case value ...", autoCollapse:=False),
            Region("span1", "Case Is < 0", autoCollapse:=False),
            Region("span2", "Case Is = 0", autoCollapse:=False),
            Region("span3", "Case 1", autoCollapse:=False),
            Region("span4", "Case 2, ...", autoCollapse:=False),
            Region("span5", "Case 4 To 10", autoCollapse:=False),
            Region("span6", "Case Else", autoCollapse:=False))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSelectBlock3() As Task
            Const code = "
Class C
    Sub M()
       Dim value = 10
{|span0:Select Case value $$
    {|span1:Case Is < 0
                Console.WriteLine(""Negative"")|}
    {|span2:Case Is = 0
                Console.WriteLine(""Zero"")|}
    {|span3:Case 1
                Console.WriteLine(""1"")|}
    {|span4:Case 2, 3
                Console.WriteLine(""Two or Three"")|}
    {|span5:Case 4 To 10
                Console.WriteLine(""Four To Ten"")|}
    {|span6:Case Else
                Console.WriteLine(""Somethingelse"")|}
        End Select|}
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
            Region("span0", "Select Case value ...", autoCollapse:=False),
            Region("span1", "Case Is < 0", autoCollapse:=False),
            Region("span2", "Case Is = 0", autoCollapse:=False),
            Region("span3", "Case 1", autoCollapse:=False),
            Region("span4", "Case 2, ...", autoCollapse:=False),
            Region("span5", "Case 4 To 10", autoCollapse:=False),
            Region("span6", "Case Else", autoCollapse:=False))
        End Function
    End Class
End Namespace
