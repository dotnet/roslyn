' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class ConditionalPreprocessorHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function GetHighlighterType() As Type
            Return GetType(ConditionalPreprocessorHighlighter)
        End Function

        <Fact>
        Public Async Function TestConditionalPreprocessorSample1_1() As Task
            Await TestAsync(<Text>
#Const Goo1 = 1
#Const Goo2 = 2
{|Cursor:[|#If|]|} Goo1 [|Then|]
[|#ElseIf|] Goo2 [|Then|]
[|#Else|]
[|#End If|]</Text>)
        End Function

        <Fact>
        Public Async Function TestConditionalPreprocessorSample1_2() As Task
            Await TestAsync(<Text>
#Const Goo1 = 1
#Const Goo2 = 2
[|#If|] Goo1 {|Cursor:[|Then|]|}
[|#ElseIf|] Goo2 [|Then|]
[|#Else|]
[|#End If|]</Text>)
        End Function

        <Fact>
        Public Async Function TestConditionalPreprocessorSample1_3() As Task
            Await TestAsync(<Text>
#Const Goo1 = 1
#Const Goo2 = 2
[|#If|] Goo1 [|Then|]
{|Cursor:[|#ElseIf|]|} Goo2 [|Then|]
[|#Else|]
[|#End If|]</Text>)
        End Function

        <Fact>
        Public Async Function TestConditionalPreprocessorSample1_4() As Task
            Await TestAsync(<Text>
#Const Goo1 = 1
#Const Goo2 = 2
[|#If|] Goo1 [|Then|]
[|#ElseIf|] Goo2 {|Cursor:[|Then|]|}
[|#Else|]
[|#End If|]</Text>)
        End Function

        <Fact>
        Public Async Function TestConditionalPreprocessorSample1_5() As Task
            Await TestAsync(<Text>
#Const Goo1 = 1
#Const Goo2 = 2
[|#If|] Goo1 [|Then|]
[|#ElseIf|] Goo2 [|Then|]
{|Cursor:[|#Else|]|}
[|#End If|]</Text>)
        End Function

        <Fact>
        Public Async Function TestConditionalPreprocessorSample1_6() As Task
            Await TestAsync(<Text>
#Const Goo1 = 1
#Const Goo2 = 2
[|#If|] Goo1 [|Then|]
[|#ElseIf|] Goo2 [|Then|]
[|#Else|]
{|Cursor:[|#End If|]|}</Text>)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544469")>
        Public Async Function TestConditionalWithMissingIf1() As Task
            Await TestAsync(<Text>
#Const goo = _
True : #If goo Then
{|Cursor:[|#Else|]|}
[|#End If|]
            ' #If should be the first one in sorted order
            Dim ifDirective = condDirectives.First()
            Contract.Assert(ifDirective.Kind = SyntaxKind.IfDirective)
(ifDirective.Kind == ElseDirective)</Text>)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544469")>
        Public Async Function TestConditionalWithMissingIf2() As Task
            Await TestAsync(<Text>
#Const goo = _
True : #If goo Then
[|#Else|]
{|Cursor:[|#End If|]|}
            ' #If should be the first one in sorted order
            Dim ifDirective = condDirectives.First()
            Contract.Assert(ifDirective.Kind = SyntaxKind.IfDirective)
(ifDirective.Kind == ElseDirective)</Text>)
        End Function
    End Class
End Namespace
