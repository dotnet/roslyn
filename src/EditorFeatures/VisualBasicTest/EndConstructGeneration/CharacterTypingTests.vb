' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class CharacterTypingTests
        <WpfFact>
        Public Async Function TestXmlEndConstructApplied() As Task
            VerifyEndConstructAppliedAfterChar(
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

        <WpfFact>
        Public Async Function TestXmlEndConstructNotApplied() As Task
            VerifyEndConstructNotAppliedAfterChar(
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

        <WpfFact>
        Public Async Function TestXmlCommentEndConstructApplied() As Task
            VerifyEndConstructAppliedAfterChar(
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

        <WpfFact>
        Public Async Function TestXmlCommentEndConstructNotApplied() As Task
            VerifyEndConstructNotAppliedAfterChar(
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

        <WpfFact>
        Public Async Function TestXmlEmbeddedExpressionEndConstructApplied() As Task
            VerifyEndConstructAppliedAfterChar(
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

        <WpfFact>
        Public Async Function TestXmlEmbeddedExpressionEndConstructNotApplied() As Task
            VerifyEndConstructNotAppliedAfterChar(
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

        <WpfFact>
        Public Async Function TestXmlCDataEndConstructApplied() As Task
            VerifyEndConstructAppliedAfterChar(
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

        <WpfFact>
        Public Async Function TestXmlCDataEndConstructNotApplied() As Task
            VerifyEndConstructNotAppliedAfterChar(
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

        <WpfFact>
        Public Async Function TestXmlProcessingInstructionEndConstructApplied() As Task
            VerifyEndConstructAppliedAfterChar(
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

        <WpfFact>
        Public Async Function TestXmlProcessingInstructionEndConstructNotApplied() As Task
            VerifyEndConstructNotAppliedAfterChar(
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
        End Sub

    End Class
End Namespace
