' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.GraphModel
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Public Class VisualBasicSymbolLabelTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(545008)>
        Public Sub MethodWithOptionalParameter()
            Using testState = New ProgressionTestState(
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

                testState.AssertMarkedSymbolLabelIs(GraphCommandDefinition.Contains.Id, "S([Integer])", "C.S([Integer])")
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(545009)>
        Public Sub MethodWithByRefParameter()
            Using testState = New ProgressionTestState(
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

                testState.AssertMarkedSymbolLabelIs(GraphCommandDefinition.Contains.Id, "S(ByRef Integer)", "C.S(ByRef Integer)")
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(545017)>
        Public Sub EnumMember()
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
                                Enum E
                                    $$M
                                End Enum
                            </Document>
                        </Project>
                    </Workspace>)

                testState.AssertMarkedSymbolLabelIs(GraphCommandDefinition.Contains.Id, "M", "E.M")
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(608256)>
        Public Sub GenericType()
            Using testState = New ProgressionTestState(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" FilePath="Z:\Project.vbproj">
                            <Document FilePath="Z:\Project.vb">
                                Class $$C(Of T)
                                End Class
                            </Document>
                        </Project>
                    </Workspace>)

                testState.AssertMarkedSymbolLabelIs(GraphCommandDefinition.Contains.Id, "C(Of T)", "C(Of T)")
            End Using
        End Sub
    End Class
End Namespace
