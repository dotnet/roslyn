' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports System.Threading.Tasks
Imports ImplementInterfaceCodeAction = Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService.ImplementInterfaceCodeAction

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.ImplementInterface
    Partial Public Class ImplementInterfaceTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInDocument() As Task
            Dim fixAllActionEquivalenceKey = ImplementInterfaceCodeAction.GetCodeActionEquivalenceKey("Assembly1", "Global.I1", explicitly:=False, abstractly:=False, throughMember:=Nothing, codeActionTypeName:=GetType(ImplementInterfaceCodeAction).FullName)

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Public Interface I1
    Sub F1()
End Interface

Public Interface I2
    Sub F1()
End Interface

Class B1
    Implements {|FixAllInDocument:I1|}
    Implements I2

    Private Class C1
        Implements I1
        Implements I2
    End Class
End Class]]>
                                </Document>
                                <Document><![CDATA[
Class B2
    Implements I1
    Implements I2

    Private Class C2
        Implements I1
        Implements I2
    End Class
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Class B3
    Implements I1
    Implements I2

    Private Class C3
        Implements I1
        Implements I2
    End Class
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Public Interface I1
    Sub F1()
End Interface

Public Interface I2
    Sub F1()
End Interface

Class B1
    Implements I1
    Implements I2

    Public Sub F1() Implements I1.F1
        Throw New NotImplementedException()
    End Sub

    Private Class C1
        Implements I1
        Implements I2

        Public Sub F1() Implements I1.F1
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Class B2
    Implements I1
    Implements I2

    Private Class C2
        Implements I1
        Implements I2
    End Class
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Class B3
    Implements I1
    Implements I2

    Private Class C3
        Implements I1
        Implements I2
    End Class
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestAsync(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=fixAllActionEquivalenceKey)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInProject() As Task
            Dim fixAllActionEquivalenceKey = ImplementInterfaceCodeAction.GetCodeActionEquivalenceKey("Assembly1", "Global.I1", explicitly:=False, abstractly:=False, throughMember:=Nothing, codeActionTypeName:=GetType(ImplementInterfaceCodeAction).FullName)

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Public Interface I1
    Sub F1()
End Interface

Public Interface I2
    Sub F1()
End Interface

Class B1
    Implements {|FixAllInProject:I1|}
    Implements I2

    Private Class C1
        Implements I1
        Implements I2
    End Class
End Class]]>
                                </Document>
                                <Document><![CDATA[
Class B2
    Implements I1
    Implements I2

    Private Class C2
        Implements I1
        Implements I2
    End Class
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Class B3
    Implements I1
    Implements I2

    Private Class C3
        Implements I1
        Implements I2
    End Class
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Public Interface I1
    Sub F1()
End Interface

Public Interface I2
    Sub F1()
End Interface

Class B1
    Implements I1
    Implements I2

    Public Sub F1() Implements I1.F1
        Throw New NotImplementedException()
    End Sub

    Private Class C1
        Implements I1
        Implements I2

        Public Sub F1() Implements I1.F1
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Class B2
    Implements I1
    Implements I2

    Public Sub F1() Implements I1.F1
        Throw New NotImplementedException()
    End Sub

    Private Class C2
        Implements I1
        Implements I2

        Public Sub F1() Implements I1.F1
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Class B3
    Implements I1
    Implements I2

    Private Class C3
        Implements I1
        Implements I2
    End Class
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestAsync(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=fixAllActionEquivalenceKey)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution() As Task
            Dim fixAllActionEquivalenceKey = ImplementInterfaceCodeAction.GetCodeActionEquivalenceKey("Assembly1", "Global.I1", explicitly:=False, abstractly:=False, throughMember:=Nothing, codeActionTypeName:=GetType(ImplementInterfaceCodeAction).FullName)

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Public Interface I1
    Sub F1()
End Interface

Public Interface I2
    Sub F1()
End Interface

Class B1
    Implements {|FixAllInSolution:I1|}
    Implements I2

    Private Class C1
        Implements I1
        Implements I2
    End Class
End Class]]>
                                </Document>
                                <Document><![CDATA[
Class B2
    Implements I1
    Implements I2

    Private Class C2
        Implements I1
        Implements I2
    End Class
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Class B3
    Implements I1
    Implements I2

    Private Class C3
        Implements I1
        Implements I2
    End Class
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Public Interface I1
    Sub F1()
End Interface

Public Interface I2
    Sub F1()
End Interface

Class B1
    Implements I1
    Implements I2

    Public Sub F1() Implements I1.F1
        Throw New NotImplementedException()
    End Sub

    Private Class C1
        Implements I1
        Implements I2

        Public Sub F1() Implements I1.F1
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Class B2
    Implements I1
    Implements I2

    Public Sub F1() Implements I1.F1
        Throw New NotImplementedException()
    End Sub

    Private Class C2
        Implements I1
        Implements I2

        Public Sub F1() Implements I1.F1
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Class B3
    Implements I1
    Implements I2

    Public Sub F1() Implements I1.F1
        Throw New NotImplementedException()
    End Sub

    Private Class C3
        Implements I1
        Implements I2

        Public Sub F1() Implements I1.F1
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestAsync(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=fixAllActionEquivalenceKey)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution_DifferentAssemblyWithSameTypeName() As Task
            Dim fixAllActionEquivalenceKey = ImplementInterfaceCodeAction.GetCodeActionEquivalenceKey("Assembly1", "Global.I1", explicitly:=False, abstractly:=False, throughMember:=Nothing, codeActionTypeName:=GetType(ImplementInterfaceCodeAction).FullName)

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Public Interface I1
    Sub F1()
End Interface

Public Interface I2
    Sub F1()
End Interface

Class B1
    Implements {|FixAllInSolution:I1|}
    Implements I2

    Private Class C1
        Implements I1
        Implements I2
    End Class
End Class]]>
                                </Document>
                                <Document><![CDATA[
Class B2
    Implements I1
    Implements I2

    Private Class C2
        Implements I1
        Implements I2
    End Class
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <Document><![CDATA[
Public Interface I1
    Sub F1()
End Interface

Public Interface I2
    Sub F1()
End Interface

Class B3
    Implements I1
    Implements I2

    Private Class C3
        Implements I1
        Implements I2
    End Class
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Public Interface I1
    Sub F1()
End Interface

Public Interface I2
    Sub F1()
End Interface

Class B1
    Implements I1
    Implements I2

    Public Sub F1() Implements I1.F1
        Throw New NotImplementedException()
    End Sub

    Private Class C1
        Implements I1
        Implements I2

        Public Sub F1() Implements I1.F1
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Class B2
    Implements I1
    Implements I2

    Public Sub F1() Implements I1.F1
        Throw New NotImplementedException()
    End Sub

    Private Class C2
        Implements I1
        Implements I2

        Public Sub F1() Implements I1.F1
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <Document><![CDATA[
Public Interface I1
    Sub F1()
End Interface

Public Interface I2
    Sub F1()
End Interface

Class B3
    Implements I1
    Implements I2

    Private Class C3
        Implements I1
        Implements I2
    End Class
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestAsync(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=fixAllActionEquivalenceKey)
        End Function
    End Class
End Namespace
