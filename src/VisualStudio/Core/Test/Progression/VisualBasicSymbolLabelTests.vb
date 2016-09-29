' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.GraphModel
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Public Class VisualBasicSymbolLabelTests
        <Fact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(545008, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545008")>
        Public Async Function TestMethodWithOptionalParameter() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
                                Class C
                                    Sub $$S(Optional i As Integer = 42)
                                    End Sub
                                End Class
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "S([Integer])", "C.S([Integer])")
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(545009, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545009")>
        Public Async Function TestMethodWithByRefParameter() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
                                Class C
                                    Sub $$S(ByRef i As Integer)
                                    End Sub
                                End Class
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "S(ByRef Integer)", "C.S(ByRef Integer)")
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(545017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545017")>
        Public Async Function TestEnumMember() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
                                Enum E
                                    $$M
                                End Enum
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "M", "E.M")
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(608256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608256")>
        Public Async Function TestGenericType() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
                                Class $$C(Of T)
                                End Class
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "C(Of T)", "C(Of T)")
            End Using
        End Function
    End Class
End Namespace
