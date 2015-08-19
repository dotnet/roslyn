' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Public Sub XmlEndConstructApplied()
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
        Public Sub XmlEndConstructNotApplied()
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
        Public Sub XmlCommentEndConstructApplied()
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
        Public Sub XmlCommentEndConstructNotApplied()
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
        Public Sub XmlEmbeddedExpressionEndConstructApplied()
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
        Public Sub XmlEmbeddedExpressionEndConstructNotApplied()
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
        Public Sub XmlCDataEndConstructApplied()
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
        Public Sub XmlCDataEndConstructNotApplied()
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
        Public Sub XmlProcessingInstructionEndConstructApplied()
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
        Public Sub XmlProcessingInstructionEndConstructNotApplied()
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
