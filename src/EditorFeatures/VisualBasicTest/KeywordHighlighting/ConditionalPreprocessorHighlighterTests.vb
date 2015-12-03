' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.KeywordHighlighting
    Public Class ConditionalPreprocessorHighlighterTests
        Inherits AbstractVisualBasicKeywordHighlighterTests

        Friend Overrides Function CreateHighlighter() As IHighlighter
            Return New ConditionalPreprocessorHighlighter()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestConditionalPreprocessorSample1_1() As Task
            Await TestAsync(<Text>
#Const Foo1 = 1
#Const Foo2 = 2
{|Cursor:[|#If|]|} Foo1 [|Then|]
[|#ElseIf|] Foo2 [|Then|]
[|#Else|]
[|#End If|]</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestConditionalPreprocessorSample1_2() As Task
            Await TestAsync(<Text>
#Const Foo1 = 1
#Const Foo2 = 2
[|#If|] Foo1 {|Cursor:[|Then|]|}
[|#ElseIf|] Foo2 [|Then|]
[|#Else|]
[|#End If|]</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestConditionalPreprocessorSample1_3() As Task
            Await TestAsync(<Text>
#Const Foo1 = 1
#Const Foo2 = 2
[|#If|] Foo1 [|Then|]
{|Cursor:[|#ElseIf|]|} Foo2 [|Then|]
[|#Else|]
[|#End If|]</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestConditionalPreprocessorSample1_4() As Task
            Await TestAsync(<Text>
#Const Foo1 = 1
#Const Foo2 = 2
[|#If|] Foo1 [|Then|]
[|#ElseIf|] Foo2 {|Cursor:[|Then|]|}
[|#Else|]
[|#End If|]</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestConditionalPreprocessorSample1_5() As Task
            Await TestAsync(<Text>
#Const Foo1 = 1
#Const Foo2 = 2
[|#If|] Foo1 [|Then|]
[|#ElseIf|] Foo2 [|Then|]
{|Cursor:[|#Else|]|}
[|#End If|]</Text>)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestConditionalPreprocessorSample1_6() As Task
            Await TestAsync(<Text>
#Const Foo1 = 1
#Const Foo2 = 2
[|#If|] Foo1 [|Then|]
[|#ElseIf|] Foo2 [|Then|]
[|#Else|]
{|Cursor:[|#End If|]|}</Text>)
        End Function

        <WorkItem(544469)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestConditionalWithMissingIf1() As Task
            Await TestAsync(<Text>
#Const foo = _
True : #If foo Then
{|Cursor:[|#Else|]|}
[|#End If|]
            ' #If should be the first one in sorted order
            Dim ifDirective = condDirectives.First()
            Contract.Assert(ifDirective.Kind = SyntaxKind.IfDirective)
(ifDirective.Kind == ElseDirective)</Text>)
        End Function

        <WorkItem(544469)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestConditionalWithMissingIf2() As Task
            Await TestAsync(<Text>
#Const foo = _
True : #If foo Then
[|#Else|]
{|Cursor:[|#End If|]|}
            ' #If should be the first one in sorted order
            Dim ifDirective = condDirectives.First()
            Contract.Assert(ifDirective.Kind = SyntaxKind.IfDirective)
(ifDirective.Kind == ElseDirective)</Text>)
        End Function
    End Class
End Namespace
