' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.RemoveUnnecessaryCast
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics.RemoveUnnecessaryCast

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.RemoveUnnecessaryCast
    Partial Public Class RemoveUnnecessaryCastTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Sub TestFixAllInDocument()
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

            Test(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=Nothing)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Sub TestFixAllInProject()
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

            Test(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=Nothing)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Sub TestFixAllInSolution()
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

            Test(input, expected, compareTokens:=False, fixAllActionEquivalenceKey:=Nothing)
        End Sub
    End Class
End Namespace
