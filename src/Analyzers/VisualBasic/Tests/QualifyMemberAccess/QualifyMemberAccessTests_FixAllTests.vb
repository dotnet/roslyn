' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.QualifyMemberAccess
    Partial Public Class QualifyMemberAccessTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution_QualifyMemberAccess() As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
        <Document><![CDATA[
Imports System

Class C
    Property SomeProperty As Integer
    Property OtherProperty As Integer

    Sub M()
        {|FixAllInSolution:SomeProperty|} = 1
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
        field = 0 ' ensure this doesn't get qualified
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
        Me.SomeProperty = 1
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
        field = 0 ' ensure this doesn't get qualified
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>.ToString()

            Await TestInRegularAndScriptAsync(
                initialMarkup:=input,
                expectedMarkup:=expected,
                options:=[Option](CodeStyleOptions2.QualifyPropertyAccess, True, NotificationOption2.Suggestion))
        End Function
    End Class
End Namespace
