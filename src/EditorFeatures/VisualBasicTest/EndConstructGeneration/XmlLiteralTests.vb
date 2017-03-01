' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
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
        Public Async Function DontApplyInParameterDeclaration2() As Threading.Tasks.Task
            VerifyXmlElementEndConstructNotApplied(
                text:="Class C1
    Sub M1(i As Integer,
           <xml>)
    End Sub
End Class",
                caret:={2, 16})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyAfterXmlStartElementWithEndElement() As Threading.Tasks.Task
            VerifyXmlElementEndConstructNotApplied(
                text:="Class C1
    Sub M1()
        Dim x = <xml></xml>
    End Sub
End Class",
                caret:={2, 23})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyAfterXmlEndElement() As Threading.Tasks.Task
            VerifyXmlElementEndConstructNotApplied(
                text:="Class C1
    Sub M1()
        Dim x = </xml>
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyAfterSingleXmlTag() As Threading.Tasks.Task
            VerifyXmlElementEndConstructNotApplied(
                text:="Class C1
    Sub M1()
        Dim x = <xml/>
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyAfterProcessingInstruction() As Threading.Tasks.Task
            VerifyXmlElementEndConstructNotApplied(
                text:="Class C1
    Sub M1()
        Dim x = <?xml version=""1.0""?>
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterXmlStartElementWhenPassedAsParameter1() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterXmlStartElementWhenPassedAsParameter2() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterXmlComment() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterXmlCommentWhenPassedAsParameter1() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterXmlCommentWhenPassedAsParameter2() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterXmlCData() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterXmlCData2() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterXmlEmbeddedExpression1() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterXmlEmbeddedExpression2() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterXmlEmbeddedExpression3() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterXmlProcessingInstruction() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterXmlProcessingInstructionWhenPassedAsParameter1() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterXmlProcessingInstructionWhenPassedAsParameter2() As Task
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestInsertBlankLineWhenPressingEnterInEmptyXmlTag() As Task
            VerifyStatementEndConstructApplied(
                before:="Class C1
    Sub M1()
        Dim x = <foo></foo>
    End Sub
End Class",
                beforeCaret:={2, 21},
                after:="Class C1
    Sub M1()
        Dim x = <foo>

                </foo>
    End Sub
End Class",
                afterCaret:={3, -1})
        End Function
    End Class
End Namespace
