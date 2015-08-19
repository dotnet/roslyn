' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.SimplifyTypeNames
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.SimplifyTypeNames
    Public Class SimplifyTypeNamesTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Sub TestFixAllInDocument()
            Dim fixAllActionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyNamesDiagnosticId, "System.Int32")

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As System.Int32, y As System.Int16) As System.Int32
        Dim i1 As {|FixAllInDocument:System.Int32|} = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As System.Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                </Document>
                                <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As System.Int32, y As System.Int16) As System.Int32
        Dim i1 As System.Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As System.Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As System.Int32, y As System.Int16) As System.Int32
        Dim i1 As System.Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As System.Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Integer, y As System.Int16) As Integer
        Dim i1 As Integer = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As Integer = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As System.Int32, y As System.Int16) As System.Int32
        Dim i1 As System.Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As System.Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As System.Int32, y As System.Int16) As System.Int32
        Dim i1 As System.Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As System.Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Test(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=fixAllActionId)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Sub TestFixAllInProject()
            Dim fixAllActionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyNamesDiagnosticId, "System.Int32")

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As System.Int32, y As System.Int16) As System.Int32
        Dim i1 As {|FixAllInProject:System.Int32|} = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As System.Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                </Document>
                                <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As System.Int32, y As System.Int16) As System.Int32
        Dim i1 As System.Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As System.Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As System.Int32, y As System.Int16) As System.Int32
        Dim i1 As System.Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As System.Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Integer, y As System.Int16) As Integer
        Dim i1 As Integer = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As Integer = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Integer, y As System.Int16) As Integer
        Dim i1 As Integer = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As Integer = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As System.Int32, y As System.Int16) As System.Int32
        Dim i1 As System.Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As System.Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Test(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=fixAllActionId)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Sub TestFixAllInSolution()
            Dim fixAllActionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyNamesDiagnosticId, "System.Int32")

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As System.Int32, y As System.Int16) As System.Int32
        Dim i1 As {|FixAllInSolution:System.Int32|} = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As System.Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                </Document>
                                <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As System.Int32, y As System.Int16) As System.Int32
        Dim i1 As System.Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As System.Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As System.Int32, y As System.Int16) As System.Int32
        Dim i1 As System.Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As System.Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Integer, y As System.Int16) As Integer
        Dim i1 As Integer = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As Integer = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Integer, y As System.Int16) As Integer
        Dim i1 As Integer = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As Integer = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Integer, y As System.Int16) As Integer
        Dim i1 As Integer = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As Integer = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Test(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=fixAllActionId)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Sub TestFixAllInSolution_RemoveMe()
            Dim fixAllActionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyThisOrMeDiagnosticId, Nothing)

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Imports System
Class ProgramA
    Dim x As Integer = 0
    Dim y As Integer = 0
    Dim z As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = {|FixAllInSolution:Me.x|}
        Dim s1 As System.Int16 = Me.y
        Dim i2 As System.Int32 = Me.z
        System.Console.WriteLine(i1 + s1 + i2)
        Return i1 + s1 + i2
    End Function
End Class

Class ProgramB
    Dim x2 As Integer = 0
    Dim y2 As Integer = 0
    Dim z2 As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x2
        Dim s1 As System.Int16 = Me.y2
        Dim i2 As System.Int32 = Me.z2
        System.Console.WriteLine(i1 + s1 + i2)
        Return i1 + s1 + i2
    End Function
End Class
]]>
                                </Document>
                                <Document><![CDATA[
Imports System
Class ProgramA2
    Dim x As Integer = 0
    Dim y As Integer = 0
    Dim z As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x
        Dim s1 As System.Int16 = Me.y
        Dim i2 As System.Int32 = Me.z
        System.Console.WriteLine(i1 + s1 + i2)
        Return i1 + s1 + i2
    End Function
End Class

Class ProgramB2
    Dim x2 As Integer = 0
    Dim y2 As Integer = 0
    Dim z2 As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x2
        Dim s1 As System.Int16 = Me.y2
        Dim i2 As System.Int32 = Me.z2
        System.Console.WriteLine(i1 + s1 + i2)
        Return i1 + s1 + i2
    End Function
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Imports System
Class ProgramA3
    Dim x As Integer = 0
    Dim y As Integer = 0
    Dim z As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x
        Dim s1 As System.Int16 = Me.y
        Dim i2 As System.Int32 = Me.z
        System.Console.WriteLine(i1 + s1 + i2)
        Return i1 + s1 + i2
    End Function
End Class

Class ProgramB3
    Dim x2 As Integer = 0
    Dim y2 As Integer = 0
    Dim z2 As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x2
        Dim s1 As System.Int16 = Me.y2
        Dim i2 As System.Int32 = Me.z2
        System.Console.WriteLine(i1 + s1 + i2)
        Return i1 + s1 + i2
    End Function
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Imports System
Class ProgramA
    Dim x As Integer = 0
    Dim y As Integer = 0
    Dim z As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = x
        Dim s1 As System.Int16 = y
        Dim i2 As System.Int32 = z
        System.Console.WriteLine(i1 + s1 + i2)
        Return i1 + s1 + i2
    End Function
End Class

Class ProgramB
    Dim x2 As Integer = 0
    Dim y2 As Integer = 0
    Dim z2 As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = x2
        Dim s1 As System.Int16 = y2
        Dim i2 As System.Int32 = z2
        System.Console.WriteLine(i1 + s1 + i2)
        Return i1 + s1 + i2
    End Function
End Class
]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Class ProgramA2
    Dim x As Integer = 0
    Dim y As Integer = 0
    Dim z As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = x
        Dim s1 As System.Int16 = y
        Dim i2 As System.Int32 = z
        System.Console.WriteLine(i1 + s1 + i2)
        Return i1 + s1 + i2
    End Function
End Class

Class ProgramB2
    Dim x2 As Integer = 0
    Dim y2 As Integer = 0
    Dim z2 As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = x2
        Dim s1 As System.Int16 = y2
        Dim i2 As System.Int32 = z2
        System.Console.WriteLine(i1 + s1 + i2)
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Imports System
Class ProgramA3
    Dim x As Integer = 0
    Dim y As Integer = 0
    Dim z As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = x
        Dim s1 As System.Int16 = y
        Dim i2 As System.Int32 = z
        System.Console.WriteLine(i1 + s1 + i2)
        Return i1 + s1 + i2
    End Function
End Class

Class ProgramB3
    Dim x2 As Integer = 0
    Dim y2 As Integer = 0
    Dim z2 As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = x2
        Dim s1 As System.Int16 = y2
        Dim i2 As System.Int32 = z2
        System.Console.WriteLine(i1 + s1 + i2)
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Test(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=fixAllActionId)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Sub TestFixAllInSolution_SimplifyMemberAccess()
            Dim fixAllActionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId, "System.Console")

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Imports System
Class ProgramA
    Dim x As Integer = 0
    Dim y As Integer = 0
    Dim z As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x
        Dim s1 As System.Int16 = Me.y
        Dim i2 As System.Int32 = Me.z
        {|FixAllInSolution:System.Console.Write|}(i1 + s1 + i2)
        System.Console.WriteLine(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class

Class ProgramB
    Dim x2 As Integer = 0
    Dim y2 As Integer = 0
    Dim z2 As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x2
        Dim s1 As System.Int16 = Me.y2
        Dim i2 As System.Int32 = Me.z2
        System.Console.Write(i1 + s1 + i2)
        System.Console.WriteLine(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class
]]>
                                </Document>
                                <Document><![CDATA[
Imports System
Class ProgramA2
    Dim x As Integer = 0
    Dim y As Integer = 0
    Dim z As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x
        Dim s1 As System.Int16 = Me.y
        Dim i2 As System.Int32 = Me.z
        System.Console.Write(i1 + s1 + i2)
        System.Console.WriteLine(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class

Class ProgramB2
    Dim x2 As Integer = 0
    Dim y2 As Integer = 0
    Dim z2 As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x2
        Dim s1 As System.Int16 = Me.y2
        Dim i2 As System.Int32 = Me.z2
        System.Console.Write(i1 + s1 + i2)
        System.Console.WriteLine(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Imports System
Class ProgramA3
    Dim x As Integer = 0
    Dim y As Integer = 0
    Dim z As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x
        Dim s1 As System.Int16 = Me.y
        Dim i2 As System.Int32 = Me.z
        System.Console.Write(i1 + s1 + i2)
        System.Console.WriteLine(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class

Class ProgramB3
    Dim x2 As Integer = 0
    Dim y2 As Integer = 0
    Dim z2 As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x2
        Dim s1 As System.Int16 = Me.y2
        Dim i2 As System.Int32 = Me.z2
        System.Console.Write(i1 + s1 + i2)
        System.Console.WriteLine(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Imports System
Class ProgramA
    Dim x As Integer = 0
    Dim y As Integer = 0
    Dim z As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x
        Dim s1 As System.Int16 = Me.y
        Dim i2 As System.Int32 = Me.z
        Console.Write(i1 + s1 + i2)
        Console.WriteLine(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class

Class ProgramB
    Dim x2 As Integer = 0
    Dim y2 As Integer = 0
    Dim z2 As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x2
        Dim s1 As System.Int16 = Me.y2
        Dim i2 As System.Int32 = Me.z2
        Console.Write(i1 + s1 + i2)
        Console.WriteLine(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class
]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Class ProgramA2
    Dim x As Integer = 0
    Dim y As Integer = 0
    Dim z As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x
        Dim s1 As System.Int16 = Me.y
        Dim i2 As System.Int32 = Me.z
        Console.Write(i1 + s1 + i2)
        Console.WriteLine(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class

Class ProgramB2
    Dim x2 As Integer = 0
    Dim y2 As Integer = 0
    Dim z2 As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x2
        Dim s1 As System.Int16 = Me.y2
        Dim i2 As System.Int32 = Me.z2
        Console.Write(i1 + s1 + i2)
        Console.WriteLine(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Imports System
Class ProgramA3
    Dim x As Integer = 0
    Dim y As Integer = 0
    Dim z As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x
        Dim s1 As System.Int16 = Me.y
        Dim i2 As System.Int32 = Me.z
        Console.Write(i1 + s1 + i2)
        Console.WriteLine(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class

Class ProgramB3
    Dim x2 As Integer = 0
    Dim y2 As Integer = 0
    Dim z2 As Integer = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x2
        Dim s1 As System.Int16 = Me.y2
        Dim i2 As System.Int32 = Me.z2
        Console.Write(i1 + s1 + i2)
        Console.WriteLine(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Test(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=fixAllActionId)
        End Sub
    End Class
End Namespace
