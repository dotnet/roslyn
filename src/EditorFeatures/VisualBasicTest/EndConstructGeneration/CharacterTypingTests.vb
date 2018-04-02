' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    Public Class CharacterTypingTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestXmlEndConstructApplied()
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
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestXmlEndConstructNotApplied()
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestXmlCommentEndConstructApplied()
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestXmlCommentEndConstructNotApplied()
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestXmlEmbeddedExpressionEndConstructApplied()
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestXmlEmbeddedExpressionEndConstructNotApplied()
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestXmlCDataEndConstructApplied()
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestXmlCDataEndConstructNotApplied()
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestXmlProcessingInstructionEndConstructApplied()
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestXmlProcessingInstructionEndConstructNotApplied()
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
