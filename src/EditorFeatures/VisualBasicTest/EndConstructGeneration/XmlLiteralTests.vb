' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class XmlLiteralTests
        <WpfFact>
        Public Async Function TestApplyAfterXmlStartElement() As Task
            Await VerifyXmlElementEndConstructAppliedAsync(
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
        End Function

        <WpfFact>
        Public Async Function TestApplyAfterXmlStartElementSplitAcrossLines() As Task
            Await VerifyXmlElementEndConstructAppliedAsync(
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
        End Function

        <WpfFact>
        Public Async Function TestApplyAfterXmlStartElementWithNamespace() As Task
            Await VerifyXmlElementEndConstructAppliedAsync(
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
        End Function

        <WpfFact>
        Public Async Function DoNotApplyInParameterDeclaration1() As Task
            Await VerifyXmlElementEndConstructNotAppliedAsync(
                text:="Class C1
    Sub M1(<xml>)
    End Sub
End Class",
                caret:={1, 16})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyInParameterDeclaration2() As Task
            Await VerifyXmlElementEndConstructNotAppliedAsync(
                text:="Class C1
    Sub M1(i As Integer,
           <xml>)
    End Sub
End Class",
                caret:={2, 16})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyAfterXmlStartElementWithEndElement() As Task
            Await VerifyXmlElementEndConstructNotAppliedAsync(
                text:="Class C1
    Sub M1()
        Dim x = <xml></xml>
    End Sub
End Class",
                caret:={2, 23})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyAfterXmlEndElement() As Task
            Await VerifyXmlElementEndConstructNotAppliedAsync(
                text:="Class C1
    Sub M1()
        Dim x = </xml>
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyAfterSingleXmlTag() As Task
            Await VerifyXmlElementEndConstructNotAppliedAsync(
                text:="Class C1
    Sub M1()
        Dim x = <xml/>
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyAfterProcessingInstruction() As Task
            Await VerifyXmlElementEndConstructNotAppliedAsync(
                text:="Class C1
    Sub M1()
        Dim x = <?xml version=""1.0""?>
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyAfterXmlStartElementWhenPassedAsParameter1() As Task
            Await VerifyXmlElementEndConstructAppliedAsync(
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

        <WpfFact>
        Public Async Function TestApplyAfterXmlStartElementWhenPassedAsParameter2() As Task
            Await VerifyXmlElementEndConstructAppliedAsync(
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

        <WpfFact>
        Public Async Function TestApplyAfterXmlComment() As Task
            Await VerifyXmlCommentEndConstructAppliedAsync(
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

        <WpfFact>
        Public Async Function TestApplyAfterXmlCommentWhenPassedAsParameter1() As Task
            Await VerifyXmlCommentEndConstructAppliedAsync(
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

        <WpfFact>
        Public Async Function TestApplyAfterXmlCommentWhenPassedAsParameter2() As Task
            Await VerifyXmlCommentEndConstructAppliedAsync(
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

        <WpfFact>
        Public Async Function TestApplyAfterXmlCData() As Task
            Await VerifyXmlCDataEndConstructAppliedAsync(
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

        <WpfFact>
        Public Async Function TestApplyAfterXmlCData2() As Task
            Await VerifyXmlCDataEndConstructAppliedAsync(
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

        <WpfFact>
        Public Async Function TestApplyAfterXmlEmbeddedExpression1() As Task
            Await VerifyXmlEmbeddedExpressionEndConstructAppliedAsync(
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

        <WpfFact>
        Public Async Function TestApplyAfterXmlEmbeddedExpression2() As Task
            Await VerifyXmlEmbeddedExpressionEndConstructAppliedAsync(
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

        <WpfFact>
        Public Async Function TestApplyAfterXmlEmbeddedExpression3() As Task
            Await VerifyXmlEmbeddedExpressionEndConstructAppliedAsync(
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

        <WpfFact>
        Public Async Function TestApplyAfterXmlProcessingInstruction() As Task
            Await VerifyXmlProcessingInstructionEndConstructAppliedAsync(
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

        <WpfFact>
        Public Async Function TestApplyAfterXmlProcessingInstructionWhenPassedAsParameter1() As Task
            Await VerifyXmlProcessingInstructionEndConstructAppliedAsync(
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

        <WpfFact>
        Public Async Function TestApplyAfterXmlProcessingInstructionWhenPassedAsParameter2() As Task
            Await VerifyXmlProcessingInstructionEndConstructAppliedAsync(
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

        <WpfFact>
        Public Async Function TestInsertBlankLineWhenPressingEnterInEmptyXmlTag() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function
    End Class
End Namespace
