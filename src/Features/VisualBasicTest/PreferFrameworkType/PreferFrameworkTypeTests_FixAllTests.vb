' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.PreferFrameworkTypeTests
    Partial Public Class PreferFrameworkTypeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInDocument_DeclarationContext() As Task

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Integer, y As System.Int16) As Integer
        Dim i1 As {|FixAllInDocument:Integer|} = 0
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

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Int32, y As System.Int16) As Int32
        Dim i1 As Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As Int32 = 0
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

            Await TestInRegularAndScriptAsync(input, expected, options:=FrameworkTypeEverywhere)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInProject_DeclarationContext() As Task

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Integer, y As System.Int16) As Integer
        Dim i1 As {|FixAllInProject:Integer|} = 0
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

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Int32, y As System.Int16) As Int32
        Dim i1 As Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Int32, y As System.Int16) As Int32
        Dim i1 As Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As Int32 = 0
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

            Await TestInRegularAndScriptAsync(input, expected, options:=FrameworkTypeEverywhere)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution_DeclarationContext() As Task

            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Integer, y As System.Int16) As Integer
        Dim i1 As {|FixAllInSolution:Integer|} = 0
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

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Int32, y As System.Int16) As Int32
        Dim i1 As Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Int32, y As System.Int16) As Int32
        Dim i1 As Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As Int32 = 0
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
    Private Shared Function F(x As Int32, y As System.Int16) As Int32
        Dim i1 As Int32 = 0
        Dim s1 As System.Int16 = 0
        Dim i2 As Int32 = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected, options:=FrameworkTypeEverywhere)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution_MemberAccessContext() As Task

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
        {|FixAllInSolution:Integer|}.Parse(i1 + s1 + i2)
        Integer.Parse(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class]]>
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
        Integer.Parse(i1 + s1 + i2)
        Integer.Parse(i1 + s1 + i2)
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
        Integer.Parse(i1 + s1 + i2)
        Integer.Parse(i1 + s1 + i2)
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
    Dim x As Int32 = 0
    Dim y As Int32 = 0
    Dim z As Int32 = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x
        Dim s1 As System.Int16 = Me.y
        Dim i2 As System.Int32 = Me.z
        Int32.Parse(i1 + s1 + i2)
        Int32.Parse(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Class ProgramA2
    Dim x As Int32 = 0
    Dim y As Int32 = 0
    Dim z As Int32 = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x
        Dim s1 As System.Int16 = Me.y
        Dim i2 As System.Int32 = Me.z
        Int32.Parse(i1 + s1 + i2)
        Int32.Parse(i1 + s1 + i2)
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
    Dim x As Int32 = 0
    Dim y As Int32 = 0
    Dim z As Int32 = 0

    Private Function F(p1 As System.Int32, p2 As System.Int16) As System.Int32
        Dim i1 As System.Int32 = Me.x
        Dim s1 As System.Int16 = Me.y
        Dim i2 As System.Int32 = Me.z
        Int32.Parse(i1 + s1 + i2)
        Int32.Parse(i1 + s1 + i2)
        Dim ex As System.Exception = Nothing
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected, options:=FrameworkTypeEverywhere)
        End Function
    End Class
End Namespace
