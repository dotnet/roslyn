' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.SimplifyThisOrMe

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SimplifyThisOrMe
    Partial Public Class SimplifyThisOrMeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicSimplifyThisOrMeDiagnosticAnalyzer(),
                    New VisualBasicSimplifyThisOrMeCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyThisOrMe)>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/965208")>
        Public Async Function TestSimplifyDiagnosticId() As Task
            Dim source =
        <Code>
Imports System
Class C
    Dim x as Integer
    Sub F()
        Dim z = [|Me.x|]
    End Sub
End Module
</Code>

            Dim parameters3 As New TestParameters()
            Using workspace = CreateWorkspaceFromOptions(source.ToString(), parameters3)
                Dim diagnostics = (Await GetDiagnosticsAsync(workspace, parameters3)).Where(Function(d) d.Id = IDEDiagnosticIds.RemoveThisOrMeQualificationDiagnosticId)
                Assert.Equal(1, diagnostics.Count)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyThisOrMe)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/6682")>
        Public Async Function TestMeWithNoType() As Task
            Await TestInRegularAndScriptAsync(
"Class C
    Dim x = 7
    Sub M()
        [|Me|].x = Nothing
    End Sub
End Class",
"Class C
    Dim x = 7
    Sub M()
        x = Nothing
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyThisOrMe)>
        Public Async Function TestAppropriateDiagnosticOnMissingQualifier() As Task
            Await TestDiagnosticInfoAsync(
                "Class C : Property SomeProperty As Integer : Sub M() : [|Me|].SomeProperty = 1 : End Sub : End Class",
                options:=New OptionsCollection(GetLanguage()) From {{CodeStyleOptions2.QualifyPropertyAccess, False, NotificationOption2.Error}},
                diagnosticId:=IDEDiagnosticIds.RemoveThisOrMeQualificationDiagnosticId,
                diagnosticSeverity:=DiagnosticSeverity.Error)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyThisOrMe)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution_RemoveMe() As Task
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

            Await TestInRegularAndScriptAsync(input, expected)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyThisOrMe)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution_RemoveMemberAccessQualification() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class C
    Property SomeProperty As Integer
    Property OtherProperty As Integer

    Sub M()
        {|FixAllInSolution:Me.SomeProperty|} = 1
        Dim x = Me.OtherProperty
    End Sub
End Class]]>
        </Document>
        <Document><![CDATA[
Imports System

Class D
    Property StringProperty As String
    field As Integer

    Sub N()
        Me.StringProperty = String.Empty
        Me.field = 0 ' ensure qualification isn't removed
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>.ToString()

            Dim expected =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class C
    Property SomeProperty As Integer
    Property OtherProperty As Integer

    Sub M()
        SomeProperty = 1
        Dim x = OtherProperty
    End Sub
End Class]]>
        </Document>
        <Document><![CDATA[
Imports System

Class D
    Property StringProperty As String
    field As Integer

    Sub N()
        StringProperty = String.Empty
        Me.field = 0 ' ensure qualification isn't removed
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>.ToString()

            Dim options = New OptionsCollection(GetLanguage()) From {
                {CodeStyleOptions2.QualifyPropertyAccess, False, NotificationOption2.Suggestion},
                {CodeStyleOptions2.QualifyFieldAccess, True, NotificationOption2.Suggestion}
                }
            Await TestInRegularAndScriptAsync(
                initialMarkup:=input,
                expectedMarkup:=expected,
                options:=options)
        End Function
    End Class
End Namespace
