' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting.Indentation
    <[UseExportProvider]>
    Public Class SmartIndenterTests
        Inherits VisualBasicFormatterTestBase

        Private Shared s_htmlMarkup As String = <text>
&lt;html&gt;
    &lt;body&gt;
        &lt;%{|S1:|}%&gt;
    &lt;/body&gt;
&lt;/html&gt;
</text>.NormalizedValue
        Private Shared s_baseIndentationOfNugget As Integer = 8

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEmptyFile()
            AssertSmartIndent(
                code:="",
                indentationLine:=0,
                expectedIndentation:=0)
        End Sub

        <WpfFact(Skip:="674611")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        <WorkItem(529886, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529886")>
        <WorkItem(674611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674611")>
        Public Sub TestAtBeginningOfSpanInNugget()
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
        {|S1:[|
$$Console.WriteLine()|]|}
#End ExternalSource
    End Sub
End Module
</code>.NormalizedValue

            AssertSmartIndentIndentationInProjection(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        <WorkItem(529886, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529886")>
        Public Sub TestAtEndOfSpanInNugget()
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
        {|S1:[|Console.WriteLine()
$$|]|}
#End ExternalSource
    End Sub
End Module
</code>.NormalizedValue

            AssertSmartIndentIndentationInProjection(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        <WorkItem(529886, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529886")>
        Public Sub TestInsideMiddleOfSpanInNugget()
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
        {|S1:[|Console.Wri
$$teLine()|]|}
#End ExternalSource
    End Sub
End Module
</code>.NormalizedValue

            AssertSmartIndentIndentationInProjection(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        <WorkItem(529886, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529886")>
        Public Sub TestAtContinuationAtStartOfNugget()
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
        {|S1:[|Console.
$$WriteLine()|]|}
#End ExternalSource
    End Sub
End Module
</code>.NormalizedValue

            ' Again, it doesn't matter where Console _is_ in this case - we format based on
            ' where we think it _should_ be.  So the position is one indent level past the base
            ' for the nugget (where we think the statement should be), plus one more since it is
            ' a continuation
            AssertSmartIndentIndentationInProjection(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        <WorkItem(529886, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529886")>
        Public Sub TestAtContinuationInsideOfNugget()
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
        {|S1:[|
            Console.
$$WriteLine()|]|}
#End ExternalSource
    End Sub
End Module
</code>.NormalizedValue

            ' "Console" starts gets indented once from the base indent, and we indent once from it.
            Dim extra = 8
            AssertSmartIndentIndentationInProjection(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + extra)
        End Sub

#Region "Non-line-continued constructs"

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBadLineNumberLabelInFile()
            AssertSmartIndent(
                code:="10:",
                indentationLine:=0,
                expectedIndentation:=0)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestImportStatement()
            Dim code = <Code>Import System
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=1,
                expectedIndentation:=0)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestComments()
            Dim code = <Code>        ' comments
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=1,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestXmlComments()
            Dim code = <Code>        ''' Xml comments
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=1,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestClassStatement()
            Dim code = <Code>Namespace NS
    Class CL
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestClassStatementWithInherits()
            Dim code = <Code>Namespace NS
    Class CL
        Inherits BC
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndClassStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Dim i As Integer
    End Class
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestClassStatementWithInheritsImplementsAndStatementSeparators()
            Dim code = <Code>Namespace NS
    Class CL
        Inherits IGoo : Implements Goo
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestClassStatementWithInheritsImplementsAndStatementSeparators2()
            Dim code = <Code>Namespace NS
    Class CL : Inherits IGoo : Implements Goo
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestInterfaceStatement()
            Dim code = <Code>Namespace NS
    Interface IF
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndInterfaceStatement()
            Dim code = <Code>Namespace NS
    Interface IF
        Sub Goo()
    End Interface
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestInterfaceStatementWithInherits()
            Dim code = <Code>Namespace NS
    Interface IF
        Inherits IGoo
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestInterfaceStatementWithInheritsOnTheSameLine()
            Dim code = <Code>Namespace NS
    Interface IF : Inherits IGoo
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEnumStatement()
            Dim code = <Code>Namespace NS
    Enum Goo
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndEnumStatement()
            Dim code = <Code>Namespace NS
    Enum Goo
        Member1
    End Enum
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEnumMembers()
            Dim code = <Code>Namespace NS
    Enum Goo
        Member1
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestStructureStatement()
            Dim code = <Code>Namespace NS
    Structure SomeStructure
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndStructureStatement()
            Dim code = <Code>Namespace NS
    Structure SomeStructure
        Dim i As Integer
    End Structure
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestNamespaceStatement()
            Dim code = <Code>Namespace NS
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=1,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndNamespaceStatement()
            Dim code = <Code>Namespace NS
    Class C
        Dim i As Integer
    End Class
End Namespace
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=0)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestModuleStatement()
            Dim code = <Code>Namespace NS
    Module Module1
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndModuleStatement()
            Dim code = <Code>Namespace NS
    Module Module1
        Sub Goo()
        End Sub
    End Module
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSubStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSubStatementWithParametersOnDifferentLines()
            Dim code = <Code>Class C
    Sub Method(ByVal p1 As Boolean,
               ByVal p2 As Boolean,
               ByVal p3 As Boolean)

    End Sub
End Class</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSingleLineIfStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method
            If True Then Return
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestIfStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method
            If True Then
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestElseStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method
            If True Then
                Return
            Else
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndIfStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method
            If True Then
                Return
            Else
                Return
            End If
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=8,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestLineContinuedIfStatement()
            Dim code = <Code>Class C
    Sub Method()
        If True OrElse
           False Then
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestDoStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Do
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndDoStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Do
                Goo()
            Loop
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestForStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            For a = 1 To 10
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestForEachStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            For Each a In Group
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndForStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            For i = 1 To 10
                Goo()
            Next
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestOperatorStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Public Shared Operator =(ByVal objVehicle1 as Vehicle, ByVal objVehicle2 as Vehicle) As Boolean
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSelectStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select A
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSelectCaseStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select Case A
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestCaseStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select A
                Case 1
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=20)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestCaseStatementWithCode()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select A
                Case 1
                    goo()
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=20)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestCaseElseStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select A
                Case 1
                    goo()
                Case Else
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=7,
                expectedIndentation:=20)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndSelectStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select A
                Case 1
                    goo()
            End Select
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=7,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSyncLockStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            SyncLock New Object()
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndSyncLockStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            SyncLock New Object()
                Method()
            End SyncLock
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestTryStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Try
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestCatchStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Try
            Catch ex as Exception
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestFinallyStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Try
            Catch ex as Exception
            Finally
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndTryStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Try
            Catch ex as Exception
            Finally
                Method()
            End Try
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=8,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestUsingStatement()
            Dim code = <code>Namespace NS
    Class CL
        Sub Method
            Using resource As new Resource()
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestWhileStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            While True
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndWhileStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            While True
                Goo()
            End While
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Sub


        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestWithStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            With DataStructure
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestEndWithStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            With DataStructure
                .goo = "goo"
            End With
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestPropertyStatementWithParameter()
            Dim code = <Code>Namespace NS
    Class CL
        Property Prop(ByVal index as Integer) As String
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestPropertyStatementWithoutParens()
            Dim code = <Code>Namespace NS
    Class CL
        Property Prop As String
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestPropertyStatementWithParens()
            Dim code = <Code>Namespace NS
    Class CL
        Property Prop() As String
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestPropertyStatementWithGet()
            Dim code = <Code>Namespace NS
    Class CL
        Property Prop() As String
            Get
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestPropertyStatementWithSet()
            Dim code = <Code>Namespace NS
    Class CL
        Property Prop() As String
            Get
            End Get
            Set
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=16)
        End Sub


        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        <WorkItem(536466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536466")>
        Public Sub TestXmlComments2()
            Dim code = <Code>Class C
    '''a
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        <WorkItem(536545, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536545")>
        Public Sub TestXmlComments3()
            Dim code = <Code>Class C
    Sub Bar()
        If True Then 'c
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Sub

#End Region

#Region "Lambdas"
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSingleLineFunctionLambda()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim goo = Function(x) 42
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestMultiLineFunctionLambda()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim goo = Function(x)
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=22)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestMultiLineFunctionLambdaWithComment()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim goo = Function(x) 'Comment
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=22)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSingleLineSubLambda()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim goo = Sub(x) Console.WriteLine("Goo")
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSingleLineSubLambda2()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim goo = Sub(x) Console.WriteLine("Goo") _
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=26)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestMultiLineSubLambda()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim goo = Sub(x)
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=22)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestMultiLineSubLambdaWithComment()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim goo = Sub(x) 'Comment
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=22)
        End Sub

#End Region

#Region "LINQ"

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestQueryExpressionOnSingleLineAmbiguous()
            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In B
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestQueryExpressionOnMultipleLinesAmbiguous()
            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In B
                Where c > 10
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestQueryExpressionOnMultipleLinesAmbiguous2()
            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In B
                Where c > 10
                Select c
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <WorkItem(538933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538933")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestQueryExpressionFollowedByBlankLine()
            ' What if user hits ENTER twice after a query expression? Should 'exit' the query.

            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In B
                Where c > 10
                Select c

</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestQueryExpressionWithNestedQueryExpressionOnNewLine()
            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In
                    From c2 in b
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=20)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestQueryExpressionWithNestedQueryExpressionOnSameLine()
            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In From c2 in b
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=26)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestQueryExpressionWithNestedQueryExpressionWithMultipleLines()
            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In From c2 in b
                          Where c2 > 10
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=26)
        End Sub

        <WorkItem(536762, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536762")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBugFix1417_2()
            Dim code = <Code>Sub Main()
    Dim goo = From x In y
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=14)
        End Sub

        <WpfFact>
        Public Sub TestQueryExpressionExplicitLineContinued()
            ' This should still follow indent of 'From', as in Dev10

            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In From c2 in b _
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=26)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestQueryExpressionExplicitLineContinuedCommentsAfterLineContinuation()
            ' This should still follow indent of 'From', as in Dev10

            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In From c2 in b _ ' Test
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=26)
        End Sub
#End Region

#Region "Implicit line-continuation"
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestImplicitLineContinuationAfterAttributeInNamespace()
            Dim code = "Namespace goo" & vbCrLf &
                       "    <SomeAttribute()>" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestImplicitLineContinuationWithMultipleAttributes()
            Dim code = "Namespace goo" & vbCrLf &
                       "    <SomeAttribute1()>" & vbCrLf &
                       "    <SomeAttribute2()>" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestImplicitLineContinuationAfterAttributeInClass()
            Dim code = "Namespace goo" & vbCrLf &
                       "    Class C" & vbCrLf &
                       "        <SomeAttribute()>" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestImplicitLineContinuationMethodParameters()
            Dim code = <Code>Class C
    Sub Method(ByVal p1 As Boolean,

    End Sub
End Class</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=15)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestImplicitLineContinuationMethodArguments()
            Dim code = <Code>Class C
    Sub Method(ByVal p1 As Boolean, ByVal p2 As Boolean)
        Method(1,

    End Sub
End Class</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=15)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestImplicitLineContinuationExpression()
            Dim code = <Code>Class C
    Sub Method()
        Dim a =
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Sub

        <WorkItem(539456, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539456")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestImplicitLineContinuationExpression1()
            Dim code = <Code>Class C
    Function Goo$(ParamArray arg())
        Dim r$ = "3"
        Goo$ = Goo$(
            r$

    End Function
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=12)
        End Sub

        <WorkItem(540634, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540634")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestImplicitLineContinuationExpression2()
            Dim code = <Code>Module Program
    Sub Main(args As String())
        If True And
False Then
        End If
    End Sub
End Module
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Sub
#End Region

#Region "Explicit line-continuation"
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestExplicitLineContinuationInExpression()
            Dim code = <Code>Class C
    Sub Method()
        Dim q = 1 + _
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestExplicitLineContinuationInExpressionCommentsAfterLineContinuation()
            Dim code = <Code>Class C
    Sub Method()
        Dim q = 1 + _ ' Test
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestMultipleExplicitLineContinuationsInExpression()
            Dim code = <Code>Class C
    Sub Method()
        Dim q = 1 + _
                    2 + _
                        3 + _
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=24)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestMultipleExplicitLineContinuationsInExpressionCommentsAfterLineContinuation()
            Dim code = <Code>Class C
    Sub Method()
        Dim q = 1 + _ ' Test
                    2 + _ ' Test
                        3 + _ ' Test
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=24)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestExplicitLineContinuationInFieldDeclaration()
            Dim code = <Code>Class C
    Dim q _
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestExplicitLineContinuationInFieldDeclarationCommentsAfterLineContinuation()
            Dim code = <Code>Class C
    Dim q _ ' Test
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestExplicitLineContinuationAfterAttributeInNamespace()
            Dim code = "Namespace goo" & vbCrLf &
                       "    <SomeAttribute()> _" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestExplicitLineContinuationAfterAttributeInNamespaceCommentsAfterLineContinuation()
            Dim code = "Namespace goo" & vbCrLf &
                       "    <SomeAttribute()> _ ' Test" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestExplicitLineContinuationWithMultipleAttributes()
            Dim code = "Namespace goo" & vbCrLf &
                       "    <SomeAttribute1()> _" & vbCrLf &
                       "    <SomeAttribute2()> _" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestExplicitLineContinuationWithMultipleAttributesCommentsAfterLineContinuation()
            Dim code = "Namespace goo" & vbCrLf &
                       "    <SomeAttribute1()> _ ' Test" & vbCrLf &
                       "    <SomeAttribute2()> _ ' Test 1" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestExplicitLineContinuationAfterAttributeInClass()
            Dim code = "Namespace goo" & vbCrLf &
                       "    Class C" & vbCrLf &
                       "        <SomeAttribute()> _" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestExplicitLineContinuationAfterAttributeInClassCommentsAfterLineContinuation()
            Dim code = "Namespace goo" & vbCrLf &
                       "    Class C" & vbCrLf &
                       "        <SomeAttribute()> _ ' Test" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub
#End Region

#Region "Statement Separators"

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestMultipleStatementsWithStatementSeparators()
            Dim code = <Code>Namespace Goo
    Class C
        Sub Method()
            Dim r As Integer = 22 : Dim q = 15
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestMultipleStatementsIncludingMultilineLambdaWithStatementSeparators()
            Dim code = <Code>Namespace Goo
    Class C
        Sub Method()
            Dim r As Integer = 22 : Dim s = Sub()
                                                Dim q = 15
                                            End Sub : Dim t = 42
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Sub

#End Region

#Region "Preprocessor directives"

        <WpfFact>
        <WorkItem(538937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538937")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestPreprocessorConstWithoutAssignment()
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#Const goo
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <WorkItem(538937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538937")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestPreprocessorConstWithAssignment()
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#Const goo = 42
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <WorkItem(538937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538937")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestPreprocessorIf()
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#If True Then
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <WorkItem(538937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538937")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestPreprocessorElseIf()
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#If a = True Then
#ElseIf a = False Then
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <WorkItem(538937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538937")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestPreprocessorElse()
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#If True Then
#Else
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <WorkItem(538937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538937")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestPreprocessorEndIf()
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#If True Then
#End If
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=12)
        End Sub

#End Region

#Region "XML Literals"
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestXMLLiteralOpenTag()
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=20)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestXMLLiteralNestOpenTag()
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <inner>" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=24)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestXMLLiteralCloseTag()
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                </xml>" & vbCrLf &
                       "" & vbCrLf &
                       "    End Sub"

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <WorkItem(538938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538938")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestXMLLiteralCloseTagInXML()
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <inner>" & vbCrLf &
                       "                    </inner>" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=20)
        End Sub

        <WpfFact(Skip:="Bug 816976")>
        <WorkItem(816976, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/816976")>
        <WorkItem(538938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538938")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestXMLExpressionHole()
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <%= 2 +" & vbCrLf &
                       "" & vbCrLf &
                       "    End Sub"

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=24)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestXMLExpressionHoleWithMultilineLambda()
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <%= Sub()" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Sub

        <WpfFact>
        <WorkItem(538938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538938")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestXMLExpressionHoleClosed()
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <%= 42 %>" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=20)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestXMLExpressionHoleWithXMLInIt()
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <%= <xml2>" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=28)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestXMLLiteralText()
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    goo" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestXMLIndentOnBlankLine()
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "" & vbCrLf &
                       "                </xml>"

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=20)
        End Sub

        <WorkItem(816976, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/816976")>
        <WpfFact(Skip:="Bug 816976")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestXMLIndentOnLineContinuedXMLExpressionHole()
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <%= Goo(2 _" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=28)
        End Sub
#End Region

#Region "Bugs"

        <WorkItem(538771, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538771")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBugFix4481()
            Dim code = <Code>_
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=1,
                expectedIndentation:=4)
        End Sub

        <WorkItem(538771, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538771")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBugFix4481_2()
            Dim code = <Code>  _
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=1,
                expectedIndentation:=6)
        End Sub

        <WorkItem(539553, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539553")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBug5559()
            Dim code = <Code>Public Class Class1
    Property too(ByVal d As Char)
        Get
        End Get
        Set(ByVal Value)
            Exit _
            exit property
: End Set
    End Property
End Class</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=7,
                expectedIndentation:=8)
        End Sub

        <WorkItem(539575, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539575")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBug5586()
            Dim code = <Code>Module Program
    Sub Main()
        Dim x = &lt;?xml version="1.0"?&gt;

    End Sub
End Module</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=16)
        End Sub

        <WorkItem(539609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539609")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBug5629()
            Dim code = <Code>Module Module1
    Sub Main()
        Dim q = Sub()
                    Dim a = 2

                End Sub
    End Sub
End Module</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=20)
        End Sub

        <WorkItem(539686, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539686")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBug5730()
            Dim code = <Code>Module Program
    Sub Main(args As String())
        Dim y = New List(Of Integer) From

    End Sub
End Module
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Sub

        <WorkItem(539686, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539686")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBug5730_1()
            Dim code = <Code>Module Program
    Sub Main(args As String())
        Dim query = From

    End Sub
End Module</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=20)
        End Sub

        <WorkItem(539639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539639")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBug5666()
            Dim code = <Code>Module Program
    Sub Main(args As String())
        If True Then
#Const goo = 23

        End If
    End Sub
End Module
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WorkItem(539453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539453")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBug5430_1()
            Dim code = My.Resources.XmlLiterals.IndentationTest2

            AssertSmartIndent(
                code,
                indentationLine:=11,
                expectedIndentation:=16)
        End Sub

        <WorkItem(540198, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540198")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBug6374()
            Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        : 'comment
        : Console.WriteLine("TEST")

    End Sub
End Module</text>.Value

            AssertSmartIndent(
                code,
                indentationLine:=8,
                expectedIndentation:=8)
        End Sub

        <WorkItem(542240, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542240")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestMissingEndStatement()
            Dim code = <text>Module Module1
    Sub Main()
        If True Then
            Dim q

    End Sub

End Module</text>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub
#End Region

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndenterConstructorThrows1()
            Assert.Throws(Of ArgumentNullException)(
                Function() New SmartIndent(Nothing))
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestParameter1()
            Dim code = <code>Class CL
    Sub Method(Arg1 As Integer,
Arg2 As Integer)
    End Sub
End Class</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=15)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestArgument()
            Dim code = <code>Class CL
    Sub Method(Arg1 As Integer, Arg2 As Integer)
        Method(1,
2)
    End Sub
End Class</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=15)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestParameter_LineContinuation()
            Dim code = <code>Class CL
    Sub Method(Arg1 _
As Integer, Arg2 As Integer)
    End Sub
End Class</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=15)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestParameter_LineContinuation2()
            Dim code = <code>Class CL
    Sub Method(Arg1 As _
Integer, Arg2 As Integer)
    End Sub
End Class</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=15)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestParameter2()
            Dim code = <code>Class CL
    Sub Method(Arg1 As Integer, Arg2 _
As Integer)
    End Sub
End Class</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=15)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestTypeParameter()
            Dim code = <code>Class CL
    Sub Method(Of
T, T2)()
    End Sub
End Class</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=19)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestTypeParameter2()
            Dim code = <code>Class CL
    Sub Method(Of T,
T2)()
    End Sub
End Class</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=19)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestTypeArgument()
            Dim code = <code>Class CL
    Sub Method(Of T, T2)()
        Method(Of
Integer, Integer)()
    End Sub
End Class</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestTypeArgument2()
            Dim code = <code>Class CL
    Sub Method(Of T, T2)()
        Method(Of Integer,
Integer)()
    End Sub
End Class</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestArgument_ImplicitLineContinuation()
            Dim code = <code>Class CL
    Sub Method()(i as Integer, i2 as Integer)
        Method(
1, 2)
    End Sub
End Class</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestArgument_ImplicitLineContinuation2()
            Dim code = <code>Class CL
    Sub Method()(i as Integer, i2 as Integer)
        Method(1,
2)
    End Sub
End Class</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=15)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestStatementAfterLabel()
            Dim code = <code>Module Module1
    Sub Main(args As String())
x100:

    End Sub
End Module
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestAfterStatementInNugget()
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
            {|S1:[|
        Console.WriteLine()
$$
            |]|}
#End ExternalSource
    End Sub
End Module
</code>.Value

            AssertSmartIndentIndentationInProjection(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestAfterStatementOnFirstLineOfNugget()
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
        {|S1:[|Console.WriteLine()
$$
|]|}
#End ExternalSource
    End Sub
End Module
</code>.Value

            ' TODO: Fix this to indent relative to the previous statement,
            ' instead of relative to the containing scope.  I.e. Format like:
            '     <%Console.WriteLine()
            '       Console.WriteLine() %>
            ' instead of
            '     <%Console.WriteLine()
            '         Console.WriteLine() %>
            ' C# had the desired behavior in Dev12, where VB had the same behavior
            ' as Roslyn has.  The Roslyn formatting engine currently always formats
            ' each statement independently, so let's not change that just for Venus
            AssertSmartIndentIndentationInProjection(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestInQueryInNugget()
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
            {|S1:[|
              Dim query = From
$$
|]|}
#End ExternalSource
    End Sub
End Module
</code>.Value

            ' In this case, we don't look at the base indentation at all - we just line up directly with "From"
            AssertSmartIndentIndentationInProjection(
                markup,
                expectedIndentation:=26)
        End Sub

        <WorkItem(574314, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/574314")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestInQueryOnFirstLineOfNugget()
            Dim markup = <code>Module Module1
    Sub Main(args As String())
ExternalSource ("Default.aspx", 3)
                {|S1:[|Dim query = From
$$
|]|}
#End ExternalSource
    End Sub
End Module
</code>.Value

            AssertSmartIndentIndentationInProjection(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 2 + "Dim query = ".Length)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestInNestedBlockInNugget()
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
            {|S1:[|
        If True Then
$$
            |]|}
#End ExternalSource
    End Sub
End Module
</code>.Value

            AssertSmartIndentIndentationInProjection(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestInNestedBlockStartingOnFirstLineOfNugget()
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
        {|S1:[|If True Then
     $$
|]|}
#End ExternalSource
    End Sub
End Module
</code>.Value

            AssertSmartIndentIndentationInProjection(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 8)
        End Sub

        <WpfFact, WorkItem(646663, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/646663")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestInEmptyNugget()
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
        {|S1:[|
$$|]|}
#End ExternalSource
    End Sub
End Module
</code>.Value

            AssertSmartIndentIndentationInProjection(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 4)
        End Sub

        <WpfFact, WorkItem(1190278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1190278")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub GetNextTokenForFormattingSpanCalculationIncludesZeroWidthToken_VB()
            Dim markup = <code>Option Strict Off
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Web
Imports System.Web.Helpers
Imports System.Web.Mvc
Imports System.Web.Mvc.Ajax
Imports System.Web.Mvc.Html
Imports System.Web.Optimization
Imports System.Web.Routing
Imports System.Web.Security
Imports System.Web.UI
Imports System.Web.WebPages
Imports Szs.IssueTracking.Web
Imports Zyxat.Util.Web.Mvc

Namespace ASP
Public Class _Page_Views_Shared__DeleteModel_vbhtml
Inherits System.Web.Mvc.WebViewPage(Of Zyxat.Util.Web.Mvc.IModelViewModel)
Private Shared __o As Object
Public Sub New()
MyBase.New
End Sub
Protected ReadOnly Property ApplicationInstance() As System.Web.HttpApplication
Get
Return CType(Context.ApplicationInstance,System.Web.HttpApplication)
End Get
End Property
Private Sub __RazorDesignTimeHelpers__()


#ExternalSource("C:\Users\fettinma\OneDrive\Entwicklung\Projekte\Szs.IssueTracking\Szs.IssueTracking.Web\Views\Shared\_DeleteModel.vbhtml",1)
Dim __inheritsHelper As Zyxat.Util.Web.Mvc.IModelViewModel = Nothing


#End ExternalSource

End Sub
Public Overrides Sub Execute()

#ExternalSource("C:\Users\fettinma\OneDrive\Entwicklung\Projekte\Szs.IssueTracking\Szs.IssueTracking.Web\Views\Shared\_DeleteModel.vbhtml",2)
If (Me.Model.ID > 0) Then


#End ExternalSource

#ExternalSource("C:\Users\fettinma\OneDrive\Entwicklung\Projekte\Szs.IssueTracking\Szs.IssueTracking.Web\Views\Shared\_DeleteModel.vbhtml",3)
__o = US.CS("Delete")


#End ExternalSource

#ExternalSource("C:\Users\fettinma\OneDrive\Entwicklung\Projekte\Szs.IssueTracking\Szs.IssueTracking.Web\Views\Shared\_DeleteModel.vbhtml",4)

Else


#End ExternalSource

#ExternalSource("C:\Users\fettinma\OneDrive\Entwicklung\Projekte\Szs.IssueTracking\Szs.IssueTracking.Web\Views\Shared\_DeleteModel.vbhtml",5)
__o = US.CS("Delete")


#End ExternalSource

#ExternalSource("C:\Users\fettinma\OneDrive\Entwicklung\Projekte\Szs.IssueTracking\Szs.IssueTracking.Web\Views\Shared\_DeleteModel.vbhtml",6)

End If

#End ExternalSource

#ExternalSource("C:\Users\fettinma\OneDrive\Entwicklung\Projekte\Szs.IssueTracking\Szs.IssueTracking.Web\Views\Shared\_DeleteModel.vbhtml",7)
     __o = US.CS("Delete")


#End ExternalSource

#ExternalSource("C:\Users\fettinma\OneDrive\Entwicklung\Projekte\Szs.IssueTracking\Szs.IssueTracking.Web\Views\Shared\_DeleteModel.vbhtml",8)
   __o = {|S1:[|US.CS("ReallyDelete)
        @Me.Model.DisplayName
      &lt;/div&gt;
      &lt;div class="modal-footer"&gt;
        &lt;a href="@Url.Action("Delete", New With {.id = Me.Model.ID}$$)|]|}" class="btn btn-primary"&gt;
          @US.CS("OK")
        &lt;/a&gt;
        &lt;button type="button" class="btn btn-Default" data-dismiss="modal"&gt;
          @US.CS("Cancel")
        &lt;/button&gt;
      &lt;/div&gt;
    &lt;/div&gt;&lt;!-- /.modal-content --&gt;
  &lt;/div&gt;&lt;!-- /.modal-dialog --&gt;
&lt;/div&gt;&lt;!-- /.modal --&gt;


#End ExternalSource
End Sub
End Class
End Namespace
</code>.Value

            AssertSmartIndentIndentationInProjection(
                markup,
                expectedIndentation:=15)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBlockIndentation1()
            Dim code = <code>Class C
    Sub Main()
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=4,
                indentStyle:=FormattingOptions.IndentStyle.Block)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestBlockIndentation2()
            Dim code = <code>Class C
    Sub Main()
        Dim x = 3
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8,
                indentStyle:=FormattingOptions.IndentStyle.Block)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestNoIndentation()
            Dim code = <code>Class C
    Sub Main()
        Dim x = 3
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=0,
                indentStyle:=FormattingOptions.IndentStyle.None)
        End Sub

        <WpfFact>
        <WorkItem(809354, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/809354")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestCaseStatement1()
            Dim code = <code>Enum E
    A
    B
    C
End Enum

Module Module1
    Function F(value As E) As Integer
        Select Case value
            Case E.A,

        End Select
        Return 0
    End Function
End Module
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=10,
                expectedIndentation:=17)
        End Sub

        <WpfFact>
        <WorkItem(1082028, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1082028")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestNotLineContinuationIndentation_Empty()
            Dim code = <code>Module Module1
    Sub Main()
        Dim cust2 = New Customer With {
        }
    End Sub
End Module

Public Class Customer
    Public NameFirst As String
    Public NameLast As String
End Class
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <WorkItem(1082028, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1082028")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestLineContinuationIndentation()
            Dim code = <code>Module Module1
    Sub Main()
        Dim cust2 = New Customer With {
            .NameFirst = "First",
        }
    End Sub
End Module

Public Class Customer
    Public NameFirst As String
    Public NameLast As String
End Class
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <WorkItem(1082028, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1082028")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestNotLineContinuationIndentation_ObjectMember()
            Dim code = <code>Module Module1
    Sub Main()
        Dim cust2 = New Customer With {
            .NameFirst = "First"
        }
    End Sub
End Module

Public Class Customer
    Public NameFirst As String
    Public NameLast As String
End Class
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <WorkItem(1082028, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1082028")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestNotLineContinuationIndentation_ObjectCollection()
            Dim code = <code>Module Module1
    Sub Main()
        Dim l2 = New List(Of String) From {
            "First"
        }
    End Sub
End Module
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <WorkItem(1082028, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1082028")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestNotLineContinuationIndentation_Collection()
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() As Char = {
            "s"c
        }
    End Sub
End Module
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=8)
        End Sub

        <WorkItem(2231, "https://github.com/dotnet/roslyn/issues/2231")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndentInsideInterpolatedMultiLineString_0()
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() = $"
            "
    End Sub
End Module
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=0)
        End Sub

        <WorkItem(2231, "https://github.com/dotnet/roslyn/issues/2231")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndentInsideInterpolatedMultiLineString_1()
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() = $"
     {0} what"
    End Sub
End Module
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=0)
        End Sub

        <WorkItem(2231, "https://github.com/dotnet/roslyn/issues/2231")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndentInsideInterpolatedMultiLineString_2()
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() = $"what
            "
    End Sub
End Module
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=0)
        End Sub

        <WorkItem(2231, "https://github.com/dotnet/roslyn/issues/2231")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndentInsideInterpolatedMultiLineString_3()
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() = $"what
            {0}"
    End Sub
End Module
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=0)
        End Sub

        <WorkItem(2231, "https://github.com/dotnet/roslyn/issues/2231")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndentInsideInterpolatedMultiLineString_4()
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() = $"what{0}
            "
    End Sub
End Module
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=0)
        End Sub

        <WorkItem(2231, "https://github.com/dotnet/roslyn/issues/2231")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndentInsideMultiLineString()
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() = $"1
            2"
    End Sub
End Module
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=0)
        End Sub

        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndentAtCaseBlockEnd()
            Dim code = <code>Class Program
    Public Sub M()
        Dim s = 1
        Select Case s
            Case 1
                System.Console.WriteLine(s)

            Case 2
        End Select
    End Sub
End Class
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=16)
        End Sub

        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndentAtCaseBlockEndComment()
            Dim code = <code>Class Program
    Public Sub M()
        Dim s = 1
        Select Case s
            Case 1
                System.Console.WriteLine(s)
                ' This comment belongs to case 1

            Case 2
        End Select
    End Sub
End Class
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=7,
                expectedIndentation:=16)
        End Sub

        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndentAtCaseBlockInbetweenComments()
            Dim code = <code>Class Program
    Public Sub M()
        Dim s = 1
        Select Case s
            Case 1
                System.Console.WriteLine(s)
                ' This comment belongs to case 1

            ' This comment belongs to case 2
            Case 2
        End Select
    End Sub
End Class
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=7,
                expectedIndentation:=16)
        End Sub

        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndentInArgumentLists1()
            Dim code = "
Class C
    Sub M()
        Console.WriteLine(""{0} + {1}"",

    End Sub
End Class"

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=26)
        End Sub

        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndentInArgumentLists2()
            Dim code = "
Class C
    Sub M()
        Console.WriteLine(""{0} + {1}"",
            19,

    End Sub
End Class"

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=12)
        End Sub

        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndentInArgumentLists3()
            Dim code = "
Class C
    Sub M()
        Method(a +
          b, c +
          d,

    End Sub
End Class"

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=13)
        End Sub

        <WorkItem(25323, "https://github.com/dotnet/roslyn/issues/25323")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndentAtCaseBlockEndUntabbedComment()
            Dim code = <code>Class Program
    Public Sub M()
        Dim s = 1
        Select Case s
            Case 1
                System.Console.WriteLine(s)
            ' This comment belongs to case 1

            Case 2
        End Select
    End Sub
End Class
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=7,
                expectedIndentation:=12)
        End Sub

        <WorkItem(38819, "https://github.com/dotnet/roslyn/issues/38819")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub IndentationOfReturnInFileWithTabs1()
            dim code = "
public class Example
	public sub Test(session as object)
		if (session is nothing)
return
	end sub
end class"
            ' Ensure the test code doesn't get switched to spaces
            Assert.Contains(vbTab & vbTab & "if (session is nothing)", code)
            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12,
                useTabs:=True,
                indentStyle:=FormattingOptions.IndentStyle.Smart)
        End sub

        Private Sub AssertSmartIndentIndentationInProjection(
                markup As String,
                expectedIndentation As Integer)
            Using workspace = TestWorkspace.CreateVisualBasic(markup)
                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument = workspace.CreateProjectionBufferDocument(s_htmlMarkup, workspace.Documents)

                Dim factory = TryCast(workspace.Services.GetService(Of IHostDependentFormattingRuleFactoryService)(),
                                    TestFormattingRuleFactoryServiceFactory.Factory)
                If factory IsNot Nothing Then
                    factory.BaseIndentation = s_baseIndentationOfNugget
                    factory.TextSpan = subjectDocument.SelectedSpans.Single()
                End If

                Dim indentationLine = projectedDocument.GetTextBuffer().CurrentSnapshot.GetLineFromPosition(projectedDocument.CursorPosition.Value)
                Dim point = projectedDocument.GetTextView().BufferGraph.MapDownToBuffer(indentationLine.Start, PointTrackingMode.Negative, subjectDocument.GetTextBuffer(), PositionAffinity.Predecessor)

                TestIndentation(
                    point.Value, expectedIndentation, projectedDocument.GetTextView(), subjectDocument)
            End Using
        End Sub

        ''' <param name="indentationLine">0-based. The line number in code to get indentation for.</param>
        Private Sub AssertSmartIndent(
                code As String, indentationLine As Integer,
                expectedIndentation As Integer?,
                Optional indentStyle As FormattingOptions.IndentStyle = FormattingOptions.IndentStyle.Smart)
            AssertSmartIndent(code, indentationLine, expectedIndentation, useTabs:=False, indentStyle)
            AssertSmartIndent(code.Replace("    ", vbTab), indentationLine, expectedIndentation, useTabs:=True, indentStyle)
        End Sub

        ''' <param name="indentationLine">0-based. The line number in code to get indentation for.</param>
        Private Sub AssertSmartIndent(
                code As String, indentationLine As Integer,
                expectedIndentation As Integer?,
                useTabs As Boolean,
                indentStyle As FormattingOptions.IndentStyle)
            Using workspace = TestWorkspace.CreateVisualBasic(code)
                workspace.Options = workspace.Options _
                    .WithChangedOption(FormattingOptions.SmartIndent, LanguageNames.VisualBasic, indentStyle) _
                    .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.VisualBasic, useTabs)

                TestIndentation(workspace, indentationLine, expectedIndentation)
            End Using
        End Sub
    End Class
End Namespace
