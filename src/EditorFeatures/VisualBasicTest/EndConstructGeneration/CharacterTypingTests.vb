' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.EditorUtilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class CharacterTypingTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestXmlEndConstructApplied() As Task
            Await VerifyEndConstructAppliedAfterCharAsync(
                before:=<Code>
                            <![CDATA[
Class C1
    Sub M1()
        Dim x = <xml$$
    End Sub
End Class]]>
                        </Code>.NormalizedValue,
                after:=<Code>
                           <![CDATA[
Class C1
    Sub M1()
        Dim x = <xml></xml>
    End Sub
End Class]]>
                       </Code>.NormalizedValue,
                typedChar:=">"c,
                endCaretPos:={3, 21})
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestXmlEndConstructNotApplied() As Task
            Await VerifyEndConstructNotAppliedAfterCharAsync(
                before:=<Code>
                            <![CDATA[
Class C1
    Sub M1()
        If x $$
    End Sub
End Class]]>
                        </Code>.NormalizedValue,
                after:=<Code>
                           <![CDATA[
Class C1
    Sub M1()
        If x >
    End Sub
End Class]]>
                       </Code>.NormalizedValue,
                typedChar:=">"c,
                endCaretPos:={3, 14})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestXmlCommentEndConstructApplied() As Task
            Await VerifyEndConstructAppliedAfterCharAsync(
                before:=<Code>
                            <![CDATA[
Class C1
    Sub M1()
        Dim x = <xml><!-$$</xml>
    End Sub
End Class]]>
                        </Code>.NormalizedValue,
                after:=<Code>
                           <![CDATA[
Class C1
    Sub M1()
        Dim x = <xml><!----></xml>
    End Sub
End Class]]>
                       </Code>.NormalizedValue,
                typedChar:="-"c,
                endCaretPos:={3, 25})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestXmlCommentEndConstructNotApplied() As Task
            Await VerifyEndConstructNotAppliedAfterCharAsync(
                before:=<Code>
                            <![CDATA[
Class C1
    Sub M1()
        Dim x = 1 $$
    End Sub
End Class]]>
                        </Code>.NormalizedValue,
                after:=<Code>
                           <![CDATA[
Class C1
    Sub M1()
        Dim x = 1 -
    End Sub
End Class]]>
                       </Code>.NormalizedValue,
                typedChar:="-"c,
                endCaretPos:={3, 19})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestXmlEmbeddedExpressionEndConstructApplied() As Task
            Await VerifyEndConstructAppliedAfterCharAsync(
                before:=<Code>
                            <![CDATA[
Class C1
    Sub M1()
        Dim x = <xml attr=<%$$></xml>
    End Sub
End Class]]>
                        </Code>.NormalizedValue,
                after:=<Code>
                           <![CDATA[
Class C1
    Sub M1()
        Dim x = <xml attr=<%=  %>></xml>
    End Sub
End Class]]>
                       </Code>.NormalizedValue,
                typedChar:="="c,
                endCaretPos:={3, 30})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestXmlEmbeddedExpressionEndConstructNotApplied() As Task
            Await VerifyEndConstructNotAppliedAfterCharAsync(
                before:=<Code>
                            <![CDATA[
Class C1
    Sub M1()
        Dim x $$
    End Sub
End Class]]>
                        </Code>.NormalizedValue,
                after:=<Code>
                           <![CDATA[
Class C1
    Sub M1()
        Dim x =
    End Sub
End Class]]>
                       </Code>.NormalizedValue,
                typedChar:="="c,
                endCaretPos:={3, 15})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestXmlCDataEndConstructApplied() As Task
            Await VerifyEndConstructAppliedAfterCharAsync(
                before:=<Code>
                            <![CDATA[
Class C1
    Sub M1()
        Dim x = <xml><![CDATA$$</xml>
    End Sub
End Class]]>
                        </Code>.NormalizedValue,
                after:=<Code>
                           <![CDATA[
Class C1
    Sub M1()
        Dim x = <xml><![CDATA[]]>]]&gt;<![CDATA[</xml>
    End Sub
End Class]]>
                       </Code>.NormalizedValue,
                typedChar:="["c,
                endCaretPos:={3, 30})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestXmlCDataEndConstructNotApplied() As Task
            Await VerifyEndConstructNotAppliedAfterCharAsync(
                before:=<Code>
                            <![CDATA[
Class C1
    Sub M1()
        Dim x = "$$"
    End Sub
End Class]]>
                        </Code>.NormalizedValue,
                after:=<Code>
                           <![CDATA[
Class C1
    Sub M1()
        Dim x = "["
    End Sub
End Class]]>
                       </Code>.NormalizedValue,
                typedChar:="["c,
                endCaretPos:={3, 18})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestXmlProcessingInstructionEndConstructApplied() As Task
            Await VerifyEndConstructAppliedAfterCharAsync(
                before:=<Code>
                            <![CDATA[
Class C1
    Sub M1()
        Dim x = <$$
    End Sub
End Class]]>
                        </Code>.NormalizedValue,
                after:=<Code>
                           <![CDATA[
Class C1
    Sub M1()
        Dim x = <??>
    End Sub
End Class]]>
                       </Code>.NormalizedValue,
                typedChar:="?"c,
                endCaretPos:={3, 18})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestXmlProcessingInstructionEndConstructNotApplied() As Task
            Await VerifyEndConstructNotAppliedAfterCharAsync(
                before:=<Code>
                            <![CDATA[
Class C1
    Sub M1()
        Dim x = "$$"
    End Sub
End Class]]>
                        </Code>.NormalizedValue,
                after:=<Code>
                           <![CDATA[
Class C1
    Sub M1()
        Dim x = "?"
    End Sub
End Class]]>
                       </Code>.NormalizedValue,
                typedChar:="?"c,
                endCaretPos:={3, 18})
        End Function

    End Class
End Namespace
