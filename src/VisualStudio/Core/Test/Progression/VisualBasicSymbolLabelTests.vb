' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.GraphModel
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.Progression)>
    Public Class VisualBasicSymbolLabelTests
        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545008")>
        Public Async Function TestMethodWithOptionalParameter() As Task
            Using testState = ProgressionTestState.Create(
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

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545009")>
        Public Async Function TestMethodWithByRefParameter() As Task
            Using testState = ProgressionTestState.Create(
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

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545017")>
        Public Async Function TestEnumMember() As Task
            Using testState = ProgressionTestState.Create(
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

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608256")>
        Public Async Function TestGenericType() As Task
            Using testState = ProgressionTestState.Create(
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
