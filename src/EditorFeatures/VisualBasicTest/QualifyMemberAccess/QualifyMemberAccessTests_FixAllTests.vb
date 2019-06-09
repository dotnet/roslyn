' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.QualifyMemberAccess
    Partial Public Class QualifyMemberAccessTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

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
                options:=[Option](CodeStyleOptions.QualifyPropertyAccess, True, NotificationOption.Suggestion))
        End Function
    End Class
End Namespace
