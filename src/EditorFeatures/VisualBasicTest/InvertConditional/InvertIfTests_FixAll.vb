' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InvertConditional
    <Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)>
    Partial Public Class InvertConditionalTests
        Inherits AbstractVisualBasicCodeActionTest

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function InvertConditional_FixAllInDocument() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M1(x As Boolean, a As Integer, b As Integer)
        Dim c = {|FixAllInDocument:|}If(x, a, b)
    End Sub

    Sub M2(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class

Class C2
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class",
"Class C
    Sub M1(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
    End Sub

    Sub M2(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
    End Sub
End Class

Class C2
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function InvertConditional_FixAllInProject() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Class C
    Sub M1(x As Boolean, a As Integer, b As Integer)
        Dim c = {|FixAllInProject:|}If(x, a, b)
    End Sub

    Sub M2(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Class C2
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Class C3
    Sub M4(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Class C
    Sub M1(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
    End Sub

    Sub M2(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Class C2
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Class C3
    Sub M4(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function InvertConditional_FixAllInSolution() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Class C
    Sub M1(x As Boolean, a As Integer, b As Integer)
        Dim c = {|FixAllInSolution:|}If(x, a, b)
    End Sub

    Sub M2(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Class C2
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Class C3
    Sub M4(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Class C
    Sub M1(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
    End Sub

    Sub M2(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Class C2
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Class C3
    Sub M4(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function InvertConditional_FixAllInContainingMember() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Sub M1(x As Boolean, a As Integer, b As Integer)
        Dim c = {|FixAllInContainingMember:|}If(x, a, b)
        Dim c2 = If(x, a, b)
    End Sub

    Sub M2(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
        Dim c2 = If(x, a, b)
    End Sub
End Class

Class C2
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
        Dim c2 = If(x, a, b)
    End Sub
End Class",
"Class C
    Sub M1(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
        Dim c2 = If(Not x, b, a)
    End Sub

    Sub M2(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
        Dim c2 = If(x, a, b)
    End Sub
End Class

Class C2
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
        Dim c2 = If(x, a, b)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function InvertConditional_FixAllInContainingType() As Task
            Await TestInRegularAndScriptAsync(
"Partial Class C
    Sub M1(x As Boolean, a As Integer, b As Integer)
        Dim c = {|FixAllInContainingType:|}If(x, a, b)
        Dim c2 = If(x, a, b)
    End Sub

    Sub M2(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
        Dim c2 = If(x, a, b)
    End Sub
End Class

Class C2
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
        Dim c2 = If(x, a, b)
    End Sub
End Class

Partial Class C
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
        Dim c2 = If(x, a, b)
    End Sub
End Class",
"Partial Class C
    Sub M1(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
        Dim c2 = If(Not x, b, a)
    End Sub

    Sub M2(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
        Dim c2 = If(Not x, b, a)
    End Sub
End Class

Class C2
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
        Dim c2 = If(x, a, b)
    End Sub
End Class

Partial Class C
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
        Dim c2 = If(Not x, b, a)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function InvertConditional_FixAllInContainingType_AcrossFiles() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Partial Class C1
    Sub M1(x As Boolean, a As Integer, b As Integer)
        Dim c = {|FixAllInContainingType:|}If(x, a, b)
    End Sub

    Sub M2(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Partial Class C1
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class

Class C2
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Class C3
    Sub M4(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Partial Class C1
    Sub M1(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
    End Sub

    Sub M2(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Partial Class C1
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(Not x, b, a)
    End Sub
End Class

Class C2
    Sub M3(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Class C3
    Sub M4(x As Boolean, a As Integer, b As Integer)
        Dim c = If(x, a, b)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
        End Function
    End Class
End Namespace
