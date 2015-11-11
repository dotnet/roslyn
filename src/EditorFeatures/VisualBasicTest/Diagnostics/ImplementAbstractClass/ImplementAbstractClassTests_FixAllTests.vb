Option Strict Off
' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.ImplementAbstractClass

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.ImplementAbstractClass
    Partial Public Class ImplementAbstractClassTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInDocument() As Task
            Dim fixAllActionId = ImplementAbstractClassCodeFixProvider.GetCodeActionId("Assembly1", "Global.A1")

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Public MustInherit Class A1
    Public MustOverride Sub F1()
End Class

Public Interface I1
    Sub F2()
End Interface

Class {|FixAllInDocument:B1|}
    Inherits A1
    Implements I1

    Private Class C1
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                </Document>
                                <Document><![CDATA[
Class B2
    Inherits A1
    Implements I1

    Private Class C2
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Class B3
    Inherits A1
    Implements I1

    Private Class C3
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Public MustInherit Class A1
    Public MustOverride Sub F1()
End Class

Public Interface I1
    Sub F2()
End Interface

Class B1
    Inherits A1
    Implements I1

    Public Overrides Sub F1()
        Throw New NotImplementedException()
    End Sub

    Private Class C1
        Inherits A1
        Implements I1

        Public Overrides Sub F1()
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Class B2
    Inherits A1
    Implements I1

    Private Class C2
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Class B3
    Inherits A1
    Implements I1

    Private Class C3
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestAsync(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=fixAllActionId)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInProject() As Task
            Dim fixAllActionId = ImplementAbstractClassCodeFixProvider.GetCodeActionId("Assembly1", "Global.A1")

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Public MustInherit Class A1
    Public MustOverride Sub F1()
End Class

Public Interface I1
    Sub F2()
End Interface

Class {|FixAllInProject:B1|}
    Inherits A1
    Implements I1

    Private Class C1
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                </Document>
                                <Document><![CDATA[
Class B2
    Inherits A1
    Implements I1

    Private Class C2
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Class B3
    Inherits A1
    Implements I1

    Private Class C3
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Public MustInherit Class A1
    Public MustOverride Sub F1()
End Class

Public Interface I1
    Sub F2()
End Interface

Class B1
    Inherits A1
    Implements I1

    Public Overrides Sub F1()
        Throw New NotImplementedException()
    End Sub

    Private Class C1
        Inherits A1
        Implements I1

        Public Overrides Sub F1()
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Class B2
    Inherits A1
    Implements I1

    Public Overrides Sub F1()
        Throw New NotImplementedException()
    End Sub

    Private Class C2
        Inherits A1
        Implements I1

        Public Overrides Sub F1()
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
    Inherits A1
    Implements I1

    Private Class C3
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestAsync(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=fixAllActionId)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution() As Task
            Dim fixAllActionId = ImplementAbstractClassCodeFixProvider.GetCodeActionId("Assembly1", "Global.A1")

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Public MustInherit Class A1
    Public MustOverride Sub F1()
End Class

Public Interface I1
    Sub F2()
End Interface

Class {|FixAllInSolution:B1|}
    Inherits A1
    Implements I1

    Private Class C1
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                </Document>
                                <Document><![CDATA[
Class B2
    Inherits A1
    Implements I1

    Private Class C2
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Class B3
    Inherits A1
    Implements I1

    Private Class C3
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Public MustInherit Class A1
    Public MustOverride Sub F1()
End Class

Public Interface I1
    Sub F2()
End Interface

Class B1
    Inherits A1
    Implements I1

    Public Overrides Sub F1()
        Throw New NotImplementedException()
    End Sub

    Private Class C1
        Inherits A1
        Implements I1

        Public Overrides Sub F1()
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Class B2
    Inherits A1
    Implements I1

    Public Overrides Sub F1()
        Throw New NotImplementedException()
    End Sub

    Private Class C2
        Inherits A1
        Implements I1

        Public Overrides Sub F1()
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
    Inherits A1
    Implements I1

    Public Overrides Sub F1()
        Throw New NotImplementedException()
    End Sub

    Private Class C3
        Inherits A1
        Implements I1

        Public Overrides Sub F1()
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestAsync(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=fixAllActionId)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution_DifferentAssemblyWithSameTypeName() As Task
            Dim fixAllActionId = ImplementAbstractClassCodeFixProvider.GetCodeActionId("Assembly1", "Global.A1")

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Public MustInherit Class A1
    Public MustOverride Sub F1()
End Class

Public Interface I1
    Sub F2()
End Interface

Class {|FixAllInSolution:B1|}
    Inherits A1
    Implements I1

    Private Class C1
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                </Document>
                                <Document><![CDATA[
Class B2
    Inherits A1
    Implements I1

    Private Class C2
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <Document><![CDATA[
Public MustInherit Class A1
    Public MustOverride Sub F1()
End Class

Public Interface I1
    Sub F2()
End Interface

Class B3
    Inherits A1
    Implements I1

    Private Class C3
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Public MustInherit Class A1
    Public MustOverride Sub F1()
End Class

Public Interface I1
    Sub F2()
End Interface

Class B1
    Inherits A1
    Implements I1

    Public Overrides Sub F1()
        Throw New NotImplementedException()
    End Sub

    Private Class C1
        Inherits A1
        Implements I1

        Public Overrides Sub F1()
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Class B2
    Inherits A1
    Implements I1

    Public Overrides Sub F1()
        Throw New NotImplementedException()
    End Sub

    Private Class C2
        Inherits A1
        Implements I1

        Public Overrides Sub F1()
            Throw New NotImplementedException()
        End Sub
    End Class
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <Document><![CDATA[
Public MustInherit Class A1
    Public MustOverride Sub F1()
End Class

Public Interface I1
    Sub F2()
End Interface

Class B3
    Inherits A1
    Implements I1

    Private Class C3
        Inherits A1
        Implements I1
    End Class
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestAsync(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=fixAllActionId)
        End Function
    End Class
End Namespace
