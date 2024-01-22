' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SimplifyTypeNames
    Public Class SimplifyTypeNamesTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInDocument() As Task
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
    Private Shared Function F(x As Integer, y As Short) As Integer
        Dim i1 As Integer = 0
        Dim s1 As Short = 0
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

            Await TestInRegularAndScriptAsync(input, expected, options:=PreferIntrinsicPredefinedTypeEverywhere())
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInProject() As Task
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
    Private Shared Function F(x As Integer, y As Short) As Integer
        Dim i1 As Integer = 0
        Dim s1 As Short = 0
        Dim i2 As Integer = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Integer, y As Short) As Integer
        Dim i1 As Integer = 0
        Dim s1 As Short = 0
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

            Await TestInRegularAndScriptAsync(input, expected, options:=PreferIntrinsicPredefinedTypeEverywhere())
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution() As Task
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
    Private Shared Function F(x As Integer, y As Short) As Integer
        Dim i1 As Integer = 0
        Dim s1 As Short = 0
        Dim i2 As Integer = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Class Program
    Private Shared Function F(x As Integer, y As Short) As Integer
        Dim i1 As Integer = 0
        Dim s1 As Short = 0
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
    Private Shared Function F(x As Integer, y As Short) As Integer
        Dim i1 As Integer = 0
        Dim s1 As Short = 0
        Dim i2 As Integer = 0
        Return i1 + s1 + i2
    End Function
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected, options:=PreferIntrinsicPredefinedTypeEverywhere())
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution_SimplifyMemberAccess() As Task
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

            Await TestInRegularAndScriptAsync(input, expected)
        End Function
    End Class
End Namespace
