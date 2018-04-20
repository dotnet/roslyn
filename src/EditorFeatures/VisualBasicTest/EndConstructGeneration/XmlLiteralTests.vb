' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    Public Class XmlLiteralTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlStartElement()
            VerifyXmlElementEndConstructApplied(
                before:="Class C1
    Sub M1()
        Dim x = <xml>
    End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class C1
    Sub M1()
        Dim x = <xml></xml>
    End Sub
End Class",
                afterCaret:={2, 21})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlStartElementSplitAcrossLines()
            VerifyXmlElementEndConstructApplied(
                before:="Class C1
    Sub M1()
        Dim x = <xml
                    >
    End Sub
End Class",
                beforeCaret:={3, -1},
                after:="Class C1
    Sub M1()
        Dim x = <xml
                    ></xml>
    End Sub
End Class",
                afterCaret:={3, 21})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlStartElementWithNamespace()
            VerifyXmlElementEndConstructApplied(
                before:="Class C1
    Sub M1()
        Dim x = <a:b>
    End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class C1
    Sub M1()
        Dim x = <a:b></a:b>
    End Sub
End Class",
                afterCaret:={2, 21})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyInParameterDeclaration1()
            VerifyXmlElementEndConstructNotApplied(
                text:="Class C1
    Sub M1(<xml>)
    End Sub
End Class",
                caret:={1, 16})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyInParameterDeclaration2()
            VerifyXmlElementEndConstructNotApplied(
                text:="Class C1
    Sub M1(i As Integer,
           <xml>)
    End Sub
End Class",
                caret:={2, 16})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyAfterXmlStartElementWithEndElement()
            VerifyXmlElementEndConstructNotApplied(
                text:="Class C1
    Sub M1()
        Dim x = <xml></xml>
    End Sub
End Class",
                caret:={2, 23})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyAfterXmlEndElement()
            VerifyXmlElementEndConstructNotApplied(
                text:="Class C1
    Sub M1()
        Dim x = </xml>
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyAfterSingleXmlTag()
            VerifyXmlElementEndConstructNotApplied(
                text:="Class C1
    Sub M1()
        Dim x = <xml/>
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyAfterProcessingInstruction()
            VerifyXmlElementEndConstructNotApplied(
                text:="Class C1
    Sub M1()
        Dim x = <?xml version=""1.0""?>
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlStartElementWhenPassedAsParameter1()
            VerifyXmlElementEndConstructApplied(
                before:="Class C1
    Sub M1()
        M2(<xml>
    End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class C1
    Sub M1()
        M2(<xml></xml>
    End Sub
End Class",
                afterCaret:={2, 16})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlStartElementWhenPassedAsParameter2()
            VerifyXmlElementEndConstructApplied(
                before:="Class C1
    Sub M1()
        M2(<xml>)
    End Sub
End Class",
                beforeCaret:={2, 16},
                after:="Class C1
    Sub M1()
        M2(<xml></xml>)
    End Sub
End Class",
                afterCaret:={2, 16})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlComment()
            VerifyXmlCommentEndConstructApplied(
                before:="Class C1
    Sub M1()
        Dim x = <!--
    End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class C1
    Sub M1()
        Dim x = <!---->
    End Sub
End Class",
                afterCaret:={2, 20})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlCommentWhenPassedAsParameter1()
            VerifyXmlCommentEndConstructApplied(
                before:="Class C1
    Sub M1()
        M2(<!--
    End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class C1
    Sub M1()
        M2(<!---->
    End Sub
End Class",
                afterCaret:={2, 15})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlCommentWhenPassedAsParameter2()
            VerifyXmlCommentEndConstructApplied(
                before:="Class C1
    Sub M1()
        M2(<!--)
    End Sub
End Class",
                beforeCaret:={2, 15},
                after:="Class C1
    Sub M1()
        M2(<!---->)
    End Sub
End Class",
                afterCaret:={2, 15})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlCData()
            VerifyXmlCDataEndConstructApplied(
                before:="Class C1
    Sub M1()
        Dim x = <![CDATA[
    End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class C1
    Sub M1()
        Dim x = <![CDATA[]]>
    End Sub
End Class",
                afterCaret:={2, 25})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlCData2()
            VerifyXmlCDataEndConstructApplied(
                before:="Class C1
    Sub M1()
        Dim x = <Code><![CDATA[</Code>
    End Sub
End Class",
                beforeCaret:={2, 31},
                after:="Class C1
    Sub M1()
        Dim x = <Code><![CDATA[]]></Code>
    End Sub
End Class",
                afterCaret:={2, 31})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlEmbeddedExpression1()
            VerifyXmlEmbeddedExpressionEndConstructApplied(
                before:="Class C1
    Sub M1()
        Dim x = <%=
    End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class C1
    Sub M1()
        Dim x = <%=  %>
    End Sub
End Class",
                afterCaret:={2, 20})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlEmbeddedExpression2()
            VerifyXmlEmbeddedExpressionEndConstructApplied(
                before:="Class C1
    Sub M1()
        Dim x = <a><%=
    End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class C1
    Sub M1()
        Dim x = <a><%=  %>
    End Sub
End Class",
                afterCaret:={2, 23})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlEmbeddedExpression3()
            VerifyXmlEmbeddedExpressionEndConstructApplied(
                before:="Class C1
    Sub M1()
        Dim x = <a><%=</a>
    End Sub
End Class",
                beforeCaret:={2, 22},
                after:="Class C1
    Sub M1()
        Dim x = <a><%=  %></a>
    End Sub
End Class",
                afterCaret:={2, 23})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlProcessingInstruction()
            VerifyXmlProcessingInstructionEndConstructApplied(
                before:="Class C1
    Sub M1()
        Dim x = <?
    End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class C1
    Sub M1()
        Dim x = <??>
    End Sub
End Class",
                afterCaret:={2, 18})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlProcessingInstructionWhenPassedAsParameter1()
            VerifyXmlProcessingInstructionEndConstructApplied(
                before:="Class C1
    Sub M1()
        M2(<?
    End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class C1
    Sub M1()
        M2(<??>
    End Sub
End Class",
                afterCaret:={2, 13})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterXmlProcessingInstructionWhenPassedAsParameter2()
            VerifyXmlProcessingInstructionEndConstructApplied(
                before:="Class C1
    Sub M1()
        M2(<?)
    End Sub
End Class",
                beforeCaret:={2, 13},
                after:="Class C1
    Sub M1()
        M2(<??>)
    End Sub
End Class",
                afterCaret:={2, 13})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestInsertBlankLineWhenPressingEnterInEmptyXmlTag()
            VerifyStatementEndConstructApplied(
                before:="Class C1
    Sub M1()
        Dim x = <goo></goo>
    End Sub
End Class",
                beforeCaret:={2, 21},
                after:="Class C1
    Sub M1()
        Dim x = <goo>

                </goo>
    End Sub
End Class",
                afterCaret:={3, -1})
        End Sub
    End Class
End Namespace
