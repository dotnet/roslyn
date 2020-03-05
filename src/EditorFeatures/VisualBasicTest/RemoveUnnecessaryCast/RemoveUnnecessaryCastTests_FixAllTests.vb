﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnnecessaryCast
    Partial Public Class RemoveUnnecessaryCastTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInDocument() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
Imports System
Class Program
    Private f As Char = CChar("c"C)
    Public Sub F(Optional x As Integer = CInt(0))
        ' unnecessary casts
        Dim y As Integer = {|FixAllInDocument:CInt(0)|}
        Dim z As Boolean = CBool(True)

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = If(z, DirectCast(s1, Object), DirectCast(s2, Object))

        Dim prog = New Program
        Dim x = ((DirectCast(Prog, Program)).F)
        Dim x2 = ((DirectCast(Prog, Program)).F)
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Imports System
Class Program2
    Private f As Char = CChar("c"C)
    Public Sub F(Optional x As Integer = CInt(0))
        ' unnecessary casts
        Dim y As Integer = CInt(0)
        Dim z As Boolean = CBool(True)

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = If(z, DirectCast(s1, Object), DirectCast(s2, Object))

        Dim prog = New Program
        Dim x = ((DirectCast(Prog, Program)).F)
        Dim x2 = ((DirectCast(Prog, Program)).F)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Imports System
Class Program3
    Private f As Char = CChar("c"C)
    Public Sub F(Optional x As Integer = CInt(0))
        ' unnecessary casts
        Dim y As Integer = CInt(0)
        Dim z As Boolean = CBool(True)

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = If(z, DirectCast(s1, Object), DirectCast(s2, Object))

        Dim prog = New Program
        Dim x = ((DirectCast(Prog, Program)).F)
        Dim x2 = ((DirectCast(Prog, Program)).F)
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Imports System
Class Program
    Private f As Char = "c"C
    Public Sub F(Optional x As Integer = 0)
        ' unnecessary casts
        Dim y As Integer = 0
        Dim z As Boolean = True

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = CObj(If(z, s1, s2))

        Dim prog = New Program
        Dim x = ((Prog).F)
        Dim x2 = ((Prog).F)
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Class Program2
    Private f As Char = CChar("c"C)
    Public Sub F(Optional x As Integer = CInt(0))
        ' unnecessary casts
        Dim y As Integer = CInt(0)
        Dim z As Boolean = CBool(True)

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = If(z, DirectCast(s1, Object), DirectCast(s2, Object))

        Dim prog = New Program
        Dim x = ((DirectCast(Prog, Program)).F)
        Dim x2 = ((DirectCast(Prog, Program)).F)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Imports System
Class Program3
    Private f As Char = CChar("c"C)
    Public Sub F(Optional x As Integer = CInt(0))
        ' unnecessary casts
        Dim y As Integer = CInt(0)
        Dim z As Boolean = CBool(True)

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = If(z, DirectCast(s1, Object), DirectCast(s2, Object))

        Dim prog = New Program
        Dim x = ((DirectCast(Prog, Program)).F)
        Dim x2 = ((DirectCast(Prog, Program)).F)
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
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
    Private f As Char = CChar("c"C)
    Public Sub F(Optional x As Integer = CInt(0))
        ' unnecessary casts
        Dim y As Integer = {|FixAllInProject:CInt(0)|}
        Dim z As Boolean = CBool(True)

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = If(z, DirectCast(s1, Object), DirectCast(s2, Object))
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Imports System
Class Program2
    Private f As Char = CChar("c"C)
    Public Sub F(Optional x As Integer = CInt(0))
        ' unnecessary casts
        Dim y As Integer = CInt(0)
        Dim z As Boolean = CBool(True)

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = If(z, DirectCast(s1, Object), DirectCast(s2, Object))
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Imports System
Class Program3
    Private f As Char = CChar("c"C)
    Public Sub F(Optional x As Integer = CInt(0))
        ' unnecessary casts
        Dim y As Integer = CInt(0)
        Dim z As Boolean = CBool(True)

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = If(z, DirectCast(s1, Object), DirectCast(s2, Object))
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Imports System
Class Program
    Private f As Char = "c"C
    Public Sub F(Optional x As Integer = 0)
        ' unnecessary casts
        Dim y As Integer = 0
        Dim z As Boolean = True

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = CObj(If(z, s1, s2))
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Class Program2
    Private f As Char = "c"C
    Public Sub F(Optional x As Integer = 0)
        ' unnecessary casts
        Dim y As Integer = 0
        Dim z As Boolean = True

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = CObj(If(z, s1, s2))
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Imports System
Class Program3
    Private f As Char = CChar("c"C)
    Public Sub F(Optional x As Integer = CInt(0))
        ' unnecessary casts
        Dim y As Integer = CInt(0)
        Dim z As Boolean = CBool(True)

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = If(z, DirectCast(s1, Object), DirectCast(s2, Object))
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
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
    Private f As Char = CChar("c"C)
    Public Sub F(Optional x As Integer = CInt(0))
        ' unnecessary casts
        Dim y As Integer = {|FixAllInSolution:CInt(0)|}
        Dim z As Boolean = CBool(True)

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = If(z, DirectCast(s1, Object), DirectCast(s2, Object))
    End Sub
End Class]]>
                                </Document>
                                <Document><![CDATA[
Imports System
Class Program2
    Private f As Char = CChar("c"C)
    Public Sub F(Optional x As Integer = CInt(0))
        ' unnecessary casts
        Dim y As Integer = CInt(0)
        Dim z As Boolean = CBool(True)

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = If(z, DirectCast(s1, Object), DirectCast(s2, Object))
    End Sub
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Imports System
Class Program3
    Private f As Char = CChar("c"C)
    Public Sub F(Optional x As Integer = CInt(0))
        ' unnecessary casts
        Dim y As Integer = CInt(0)
        Dim z As Boolean = CBool(True)

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = If(z, DirectCast(s1, Object), DirectCast(s2, Object))
    End Sub
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document><![CDATA[
Imports System
Class Program
    Private f As Char = "c"C
    Public Sub F(Optional x As Integer = 0)
        ' unnecessary casts
        Dim y As Integer = 0
        Dim z As Boolean = True

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = CObj(If(z, s1, s2))
    End Sub
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Class Program2
    Private f As Char = "c"C
    Public Sub F(Optional x As Integer = 0)
        ' unnecessary casts
        Dim y As Integer = 0
        Dim z As Boolean = True

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = CObj(If(z, s1, s2))
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Imports System
Class Program3
    Private f As Char = "c"C
    Public Sub F(Optional x As Integer = 0)
        ' unnecessary casts
        Dim y As Integer = 0
        Dim z As Boolean = True

        ' required cast
        Dim l As Long = 1
        Dim ll = CInt(l)

        ' required cast after cast removal in same statement
        Dim s1 As String = Nothing, s2 As String = Nothing
        Dim s3 = CObj(If(z, s1, s2))
    End Sub
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected)
        End Function
    End Class
End Namespace
