' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Formatting.Indentation
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Text.Projection
Imports Moq

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting.Indentation
    Public Class SmartIndenterTests
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
        Public Async Function TestEmptyFile() As Task
            Await AssertSmartIndentAsync(
                code:="",
                indentationLine:=0,
                expectedIndentation:=0)
        End Function

        <WpfFact(Skip:="674611")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        <WorkItem(529886)>
        <WorkItem(674611)>
        Public Async Function TestAtBeginningOfSpanInNugget() As Task
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
        {|S1:[|
$$Console.WriteLine()|]|}
#End ExternalSource
    End Sub
End Module
</code>.NormalizedValue

            Await AssertSmartIndentIndentationInProjectionAsync(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        <WorkItem(529886)>
        Public Async Function TestAtEndOfSpanInNugget() As Task
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
        {|S1:[|Console.WriteLine()
$$|]|}
#End ExternalSource
    End Sub
End Module
</code>.NormalizedValue

            Await AssertSmartIndentIndentationInProjectionAsync(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        <WorkItem(529886)>
        Public Async Function TestInsideMiddleOfSpanInNugget() As Task
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
        {|S1:[|Console.Wri
$$teLine()|]|}
#End ExternalSource
    End Sub
End Module
</code>.NormalizedValue

            Await AssertSmartIndentIndentationInProjectionAsync(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        <WorkItem(529886)>
        Public Async Function TestAtContinuationAtStartOfNugget() As Task
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
            Await AssertSmartIndentIndentationInProjectionAsync(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        <WorkItem(529886)>
        Public Async Function TestAtContinuationInsideOfNugget() As Task
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
            Await AssertSmartIndentIndentationInProjectionAsync(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + extra)
        End Function

#Region "Non-line-continued constructs"

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBadLineNumberLabelInFile() As Task
            Await AssertSmartIndentAsync(
                code:="10:",
                indentationLine:=0,
                expectedIndentation:=0)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestImportStatement() As Task
            Dim code = <Code>Import System
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=1,
                expectedIndentation:=0)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestComments() As Task
            Dim code = <Code>        ' comments
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=1,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestXmlComments() As Task
            Dim code = <Code>        ''' Xml comments
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=1,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestClassStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestClassStatementWithInherits() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Inherits BC
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndClassStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Dim i As Integer
    End Class
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestClassStatementWithInheritsImplementsAndStatementSeparators() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Inherits IFoo : Implements Foo
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestClassStatementWithInheritsImplementsAndStatementSeparators2() As Task
            Dim code = <Code>Namespace NS
    Class CL : Inherits IFoo : Implements Foo
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestInterfaceStatement() As Task
            Dim code = <Code>Namespace NS
    Interface IF
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndInterfaceStatement() As Task
            Dim code = <Code>Namespace NS
    Interface IF
        Sub Foo()
    End Interface
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestInterfaceStatementWithInherits() As Task
            Dim code = <Code>Namespace NS
    Interface IF
        Inherits IFoo
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestInterfaceStatementWithInheritsOnTheSameLine() As Task
            Dim code = <Code>Namespace NS
    Interface IF : Inherits IFoo
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEnumStatement() As Task
            Dim code = <Code>Namespace NS
    Enum Foo
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndEnumStatement() As Task
            Dim code = <Code>Namespace NS
    Enum Foo
        Member1
    End Enum
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEnumMembers() As Task
            Dim code = <Code>Namespace NS
    Enum Foo
        Member1
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestStructureStatement() As Task
            Dim code = <Code>Namespace NS
    Structure SomeStructure
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndStructureStatement() As Task
            Dim code = <Code>Namespace NS
    Structure SomeStructure
        Dim i As Integer
    End Structure
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestNamespaceStatement() As Task
            Dim code = <Code>Namespace NS
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=1,
                expectedIndentation:=4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndNamespaceStatement() As Task
            Dim code = <Code>Namespace NS
    Class C
        Dim i As Integer
    End Class
End Namespace
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=5,
                expectedIndentation:=0)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestModuleStatement() As Task
            Dim code = <Code>Namespace NS
    Module Module1
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndModuleStatement() As Task
            Dim code = <Code>Namespace NS
    Module Module1
        Sub Foo()
        End Sub
    End Module
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=5,
                expectedIndentation:=4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSubStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSubStatementWithParametersOnDifferentLines() As Task
            Dim code = <Code>Class C
    Sub Method(ByVal p1 As Boolean,
               ByVal p2 As Boolean,
               ByVal p3 As Boolean)

    End Sub
End Class</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSingleLineIfStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method
            If True Then Return
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestIfStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method
            If True Then
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestElseStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method
            If True Then
                Return
            Else
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=6,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndIfStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method
            If True Then
                Return
            Else
                Return
            End If
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=8,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestLineContinuedIfStatement() As Task
            Dim code = <Code>Class C
    Sub Method()
        If True OrElse
           False Then
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestDoStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Do
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndDoStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Do
                Foo()
            Loop
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestForStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            For a = 1 To 10
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestForEachStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            For Each a In Group
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndForStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            For i = 1 To 10
                Foo()
            Next
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestOperatorStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Public Shared Operator =(ByVal objVehicle1 as Vehicle, ByVal objVehicle2 as Vehicle) As Boolean
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSelectStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select A
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSelectCaseStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select Case A
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestCaseStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select A
                Case 1
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=5,
                expectedIndentation:=20)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestCaseStatementWithCode() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select A
                Case 1
                    foo()
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=6,
                expectedIndentation:=20)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestCaseElseStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select A
                Case 1
                    foo()
                Case Else
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=7,
                expectedIndentation:=20)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndSelectStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select A
                Case 1
                    foo()
            End Select
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=7,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSyncLockStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            SyncLock New Object()
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndSyncLockStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            SyncLock New Object()
                Method()
            End SyncLock
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestTryStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Try
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestCatchStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Try
            Catch ex as Exception
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=5,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestFinallyStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Try
            Catch ex as Exception
            Finally
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=6,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndTryStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Try
            Catch ex as Exception
            Finally
                Method()
            End Try
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=8,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestUsingStatement() As Task
            Dim code = <code>Namespace NS
    Class CL
        Sub Method
            Using resource As new Resource()
</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestWhileStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            While True
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndWhileStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            While True
                Foo()
            End While
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Function


        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestWithStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            With DataStructure
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestEndWithStatement() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            With DataStructure
                .foo = "foo"
            End With
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestPropertyStatementWithParameter() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Property Prop(ByVal index as Integer) As String
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestPropertyStatementWithoutParens() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Property Prop As String
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestPropertyStatementWithParens() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Property Prop() As String
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestPropertyStatementWithGet() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Property Prop() As String
            Get
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestPropertyStatementWithSet() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Property Prop() As String
            Get
            End Get
            Set
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=6,
                expectedIndentation:=16)
        End Function


        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        <WorkItem(536466)>
        Public Async Function TestXmlComments2() As Task
            Dim code = <Code>Class C
    '''a
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        <WorkItem(536545)>
        Public Async Function TestXmlComments3() As Task
            Dim code = <Code>Class C
    Sub Bar()
        If True Then 'c
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Function

#End Region

#Region "Lambdas"
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSingleLineFunctionLambda() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Function(x) 42
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestMultiLineFunctionLambda() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Function(x)
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=22)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestMultiLineFunctionLambdaWithComment() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Function(x) 'Comment
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=22)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSingleLineSubLambda() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Sub(x) Console.WriteLine("Foo")
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSingleLineSubLambda2() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Sub(x) Console.WriteLine("Foo") _
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=26)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestMultiLineSubLambda() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Sub(x)
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=22)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestMultiLineSubLambdaWithComment() As Task
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Sub(x) 'Comment
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=22)
        End Function

#End Region

#Region "LINQ"

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestQueryExpressionOnSingleLineAmbiguous() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In B
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestQueryExpressionOnMultipleLinesAmbiguous() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In B
                Where c > 10
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestQueryExpressionOnMultipleLinesAmbiguous2() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In B
                Where c > 10
                Select c
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=5,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <WorkItem(538933)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestQueryExpressionFollowedByBlankLine() As Task
            ' What if user hits ENTER twice after a query expression? Should 'exit' the query.

            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In B
                Where c > 10
                Select c

</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=6,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestQueryExpressionWithNestedQueryExpressionOnNewLine() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In
                    From c2 in b
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=20)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestQueryExpressionWithNestedQueryExpressionOnSameLine() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In From c2 in b
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=26)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestQueryExpressionWithNestedQueryExpressionWithMultipleLines() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In From c2 in b
                          Where c2 > 10
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=26)
        End Function

        <WorkItem(536762)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBugFix1417_2() As Task
            Dim code = <Code>Sub Main()
    Dim foo = From x In y
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=14)
        End Function

        Public Async Function TestQueryExpressionExplicitLineContinued() As Task
            ' This should still follow indent of 'From', as in Dev10

            Dim code = <Code>Class C
    Sub Method()
        Dim q = From c In From c2 in b _
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=26)
        End Function

#End Region

#Region "Implicit line-continuation"
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestImplicitLineContinuationAfterAttributeInNamespace() As Task
            Dim code = "Namespace foo" & vbCrLf &
                       "    <SomeAttribute()>" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestImplicitLineContinuationWithMultipleAttributes() As Task
            Dim code = "Namespace foo" & vbCrLf &
                       "    <SomeAttribute1()>" & vbCrLf &
                       "    <SomeAttribute2()>" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestImplicitLineContinuationAfterAttributeInClass() As Task
            Dim code = "Namespace foo" & vbCrLf &
                       "    Class C" & vbCrLf &
                       "        <SomeAttribute()>" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestImplicitLineContinuationMethodParameters() As Task
            Dim code = <Code>Class C
    Sub Method(ByVal p1 As Boolean,

    End Sub
End Class</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=15)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestImplicitLineContinuationMethodArguments() As Task
            Dim code = <Code>Class C
    Sub Method(ByVal p1 As Boolean, ByVal p2 As Boolean)
        Method(1,

    End Sub
End Class</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=15)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestImplicitLineContinuationExpression() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim a = 
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Function

        <WorkItem(539456)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestImplicitLineContinuationExpression1() As Task
            Dim code = <Code>Class C
    Function Foo$(ParamArray arg())
        Dim r$ = "3"
        Foo$ = Foo$(
            r$

    End Function
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=5,
                expectedIndentation:=12)
        End Function

        <WorkItem(540634)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestImplicitLineContinuationExpression2() As Task
            Dim code = <Code>Module Program
    Sub Main(args As String())
        If True And
False Then
        End If
    End Sub
End Module
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Function
#End Region

#Region "Explicit line-continuation"
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestExplicitLineContinuationInExpression() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim q = 1 + _
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestMultipleExplicitLineContinuationsInExpression() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim q = 1 + _
                    2 + _
                        3 + _
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=5,
                expectedIndentation:=24)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestExplicitLineContinuationInFieldDeclaration() As Task
            Dim code = <Code>Class C
    Dim q _
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestExplicitLineContinuationAfterAttributeInNamespace() As Task
            Dim code = "Namespace foo" & vbCrLf &
                       "    <SomeAttribute()> _" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestExplicitLineContinuationWithMultipleAttributes() As Task
            Dim code = "Namespace foo" & vbCrLf &
                       "    <SomeAttribute1()> _" & vbCrLf &
                       "    <SomeAttribute2()> _" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestExplicitLineContinuationAfterAttributeInClass() As Task
            Dim code = "Namespace foo" & vbCrLf &
                       "    Class C" & vbCrLf &
                       "        <SomeAttribute()> _" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Function

#End Region

#Region "Statement Separators"

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestMultipleStatementsWithStatementSeparators() As Task
            Dim code = <Code>Namespace Foo
    Class C
        Sub Method()
            Dim r As Integer = 22 : Dim q = 15
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestMultipleStatementsIncludingMultilineLambdaWithStatementSeparators() As Task
            Dim code = <Code>Namespace Foo
    Class C
        Sub Method()
            Dim r As Integer = 22 : Dim s = Sub()
                                                Dim q = 15
                                            End Sub : Dim t = 42
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Function

#End Region

#Region "Preprocessor directives"

        <WpfFact>
        <WorkItem(538937)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestPreprocessorConstWithoutAssignment() As Task
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#Const foo
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <WorkItem(538937)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestPreprocessorConstWithAssignment() As Task
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#Const foo = 42
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <WorkItem(538937)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestPreprocessorIf() As Task
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#If True Then
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <WorkItem(538937)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestPreprocessorElseIf() As Task
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#If a = True Then
#ElseIf a = False Then
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=5,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <WorkItem(538937)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestPreprocessorElse() As Task
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#If True Then
#Else
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=5,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <WorkItem(538937)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestPreprocessorEndIf() As Task
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#If True Then
#End If
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=5,
                expectedIndentation:=12)
        End Function

#End Region

#Region "XML Literals"
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestXMLLiteralOpenTag() As Task
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=20)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestXMLLiteralNestOpenTag() As Task
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <inner>" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=24)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestXMLLiteralCloseTag() As Task
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                </xml>" & vbCrLf &
                       "" & vbCrLf &
                       "    End Sub"

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <WorkItem(538938)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestXMLLiteralCloseTagInXML() As Task
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <inner>" & vbCrLf &
                       "                    </inner>" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=5,
                expectedIndentation:=20)
        End Function

        <WpfFact(Skip:="Bug 816976")>
        <WorkItem(816976)>
        <WorkItem(538938)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestXMLExpressionHole() As Task
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <%= 2 +" & vbCrLf &
                       "" & vbCrLf &
                       "    End Sub"

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=24)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestXMLExpressionHoleWithMultilineLambda() As Task
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <%= Sub()" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=16)
        End Function

        <WpfFact>
        <WorkItem(538938)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestXMLExpressionHoleClosed() As Task
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <%= 42 %>" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=20)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestXMLExpressionHoleWithXMLInIt() As Task
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <%= <xml2>" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=28)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestXMLLiteralText() As Task
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    foo" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestXMLIndentOnBlankLine() As Task
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "" & vbCrLf &
                       "                </xml>"

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=20)
        End Function

        <WorkItem(816976)>
        <WpfFact(Skip:="Bug 816976")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestXMLIndentOnLineContinuedXMLExpressionHole() As Task
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <%= Foo(2 _" & vbCrLf &
                       ""

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=28)
        End Function
#End Region

#Region "Bugs"

        <WorkItem(538771)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBugFix4481() As Task
            Dim code = <Code>_
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=1,
                expectedIndentation:=4)
        End Function

        <WorkItem(538771)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBugFix4481_2() As Task
            Dim code = <Code>  _
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=1,
                expectedIndentation:=6)
        End Function

        <WorkItem(539553)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBug5559() As Task
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

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=7,
                expectedIndentation:=8)
        End Function

        <WorkItem(539575)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBug5586() As Task
            Dim code = <Code>Module Program
    Sub Main()
        Dim x = &lt;?xml version="1.0"?&gt;

    End Sub
End Module</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=16)
        End Function

        <WorkItem(539609)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBug5629() As Task
            Dim code = <Code>Module Module1
    Sub Main()
        Dim q = Sub()
                    Dim a = 2

                End Sub
    End Sub
End Module</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=20)
        End Function

        <WorkItem(539686)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBug5730() As Task
            Dim code = <Code>Module Program
    Sub Main(args As String())
        Dim y = New List(Of Integer) From

    End Sub
End Module
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Function

        <WorkItem(539686)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBug5730_1() As Task
            Dim code = <Code>Module Program
    Sub Main(args As String())
        Dim query = From

    End Sub
End Module</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=20)
        End Function

        <WorkItem(539639)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBug5666() As Task
            Dim code = <Code>Module Program
    Sub Main(args As String())
        If True Then
#Const foo = 23

        End If
    End Sub
End Module
</Code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Function

        <WorkItem(539453)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBug5430_1() As Task
            Dim code = My.Resources.XmlLiterals.IndentationTest2

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=11,
                expectedIndentation:=16)
        End Function

        <WorkItem(540198)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBug6374() As Task
            Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        : 'comment   
        : Console.WriteLine("TEST")

    End Sub
End Module</text>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=8,
                expectedIndentation:=8)
        End Function

        <WorkItem(542240)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestMissingEndStatement() As Task
            Dim code = <text>Module Module1
    Sub Main()
        If True Then
            Dim q

    End Sub

End Module</text>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Function
#End Region

        <Fact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub TestSmartIndenterConstructorThrows1()
            Assert.Throws(Of ArgumentNullException)(
                Function() New SmartIndent(Nothing))
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestParameter1() As Task
            Dim code = <code>Class CL
    Sub Method(Arg1 As Integer,
Arg2 As Integer)
    End Sub
End Class</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=15)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestArgument() As Task
            Dim code = <code>Class CL
    Sub Method(Arg1 As Integer, Arg2 As Integer)
        Method(1,
2)
    End Sub
End Class</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=15)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestParameter_LineContinuation() As Task
            Dim code = <code>Class CL
    Sub Method(Arg1 _ 
As Integer, Arg2 As Integer)
    End Sub
End Class</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=15)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestParameter_LineContinuation2() As Task
            Dim code = <code>Class CL
    Sub Method(Arg1 As _ 
Integer, Arg2 As Integer)
    End Sub
End Class</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=15)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestParameter2() As Task
            Dim code = <code>Class CL
    Sub Method(Arg1 As Integer, Arg2 _
As Integer)
    End Sub
End Class</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=15)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestTypeParameter() As Task
            Dim code = <code>Class CL
    Sub Method(Of 
T, T2)()
    End Sub
End Class</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=19)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestTypeParameter2() As Task
            Dim code = <code>Class CL
    Sub Method(Of T,
T2)()
    End Sub
End Class</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=19)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestTypeArgument() As Task
            Dim code = <code>Class CL
    Sub Method(Of T, T2)()
        Method(Of
Integer, Integer)()
    End Sub
End Class</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestTypeArgument2() As Task
            Dim code = <code>Class CL
    Sub Method(Of T, T2)()
        Method(Of Integer, 
Integer)()
    End Sub
End Class</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestArgument_ImplicitLineContinuation() As Task
            Dim code = <code>Class CL
    Sub Method()(i as Integer, i2 as Integer)
        Method(
1, 2)
    End Sub
End Class</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestArgument_ImplicitLineContinuation2() As Task
            Dim code = <code>Class CL
    Sub Method()(i as Integer, i2 as Integer)
        Method(1,
2)
    End Sub
End Class</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=15)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestStatementAfterLabel() As Task
            Dim code = <code>Module Module1
    Sub Main(args As String())
x100:

    End Sub
End Module
</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestAfterStatementInNugget() As Task
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

            Await AssertSmartIndentIndentationInProjectionAsync(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestAfterStatementOnFirstLineOfNugget() As Task
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
            Await AssertSmartIndentIndentationInProjectionAsync(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 4)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestInQueryInNugget() As Task
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
            Await AssertSmartIndentIndentationInProjectionAsync(
                markup,
                expectedIndentation:=26)
        End Function

        <WorkItem(574314)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestInQueryOnFirstLineOfNugget() As Task
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

            Await AssertSmartIndentIndentationInProjectionAsync(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 2 + "Dim query = ".Length)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestInNestedBlockInNugget() As Task
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

            Await AssertSmartIndentIndentationInProjectionAsync(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 8)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestInNestedBlockStartingOnFirstLineOfNugget() As Task
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

            Await AssertSmartIndentIndentationInProjectionAsync(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 8)
        End Function

        <WpfFact, WorkItem(646663)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestInEmptyNugget() As Task
            Dim markup = <code>Module Module1
    Sub Main(args As String())
#ExternalSource ("Default.aspx", 3)
        {|S1:[|
$$|]|}
#End ExternalSource
    End Sub
End Module
</code>.Value

            Await AssertSmartIndentIndentationInProjectionAsync(
                markup,
                expectedIndentation:=s_baseIndentationOfNugget + 4)
        End Function

        <WpfFact, WorkItem(1190278)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function GetNextTokenForFormattingSpanCalculationIncludesZeroWidthToken_VB() As Tasks.Task
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

            Await AssertSmartIndentIndentationInProjectionAsync(
                markup,
                expectedIndentation:=15)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBlockIndentation1() As Task
            Dim code = <code>Class C
    Sub Main()
</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=2,
                expectedIndentation:=4,
                indentStyle:=FormattingOptions.IndentStyle.Block)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestBlockIndentation2() As Task
            Dim code = <code>Class C
    Sub Main()
        Dim x = 3
</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=8,
                indentStyle:=FormattingOptions.IndentStyle.Block)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestNoIndentation() As Task
            Dim code = <code>Class C
    Sub Main()
        Dim x = 3
</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=Nothing,
                indentStyle:=FormattingOptions.IndentStyle.None)
        End Function

        <WpfFact>
        <WorkItem(809354)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestCaseStatement1() As Task
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

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=10,
                expectedIndentation:=17)
        End Function

        <WpfFact>
        <WorkItem(1082028)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestNotLineContinuationIndentation_Empty() As Task
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

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <WorkItem(1082028)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestLineContinuationIndentation() As Task
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

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Function

        <WpfFact>
        <WorkItem(1082028)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestNotLineContinuationIndentation_ObjectMember() As Task
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

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <WorkItem(1082028)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestNotLineContinuationIndentation_ObjectCollection() As Task
            Dim code = <code>Module Module1
    Sub Main()
        Dim l2 = New List(Of String) From {
            "First"
        }
    End Sub
End Module
</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=8)
        End Function

        <WpfFact>
        <WorkItem(1082028)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestNotLineContinuationIndentation_Collection() As Task
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() As Char = {
            "s"c
        }
    End Sub
End Module
</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=4,
                expectedIndentation:=8)
        End Function

        <WorkItem(2231, "https://github.com/dotnet/roslyn/issues/2231")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSmartIndentInsideInterpolatedMultiLineString_0() As Task
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() = $"
            "
    End Sub
End Module
</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=0)
        End Function

        <WorkItem(2231, "https://github.com/dotnet/roslyn/issues/2231")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSmartIndentInsideInterpolatedMultiLineString_1() As Task
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() = $"
     {0} what"
    End Sub
End Module
</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=0)
        End Function

        <WorkItem(2231, "https://github.com/dotnet/roslyn/issues/2231")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSmartIndentInsideInterpolatedMultiLineString_2() As Task
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() = $"what
            "
    End Sub
End Module
</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=0)
        End Function

        <WorkItem(2231, "https://github.com/dotnet/roslyn/issues/2231")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSmartIndentInsideInterpolatedMultiLineString_3() As Task
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() = $"what
            {0}"
    End Sub
End Module
</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=0)
        End Function

        <WorkItem(2231, "https://github.com/dotnet/roslyn/issues/2231")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSmartIndentInsideInterpolatedMultiLineString_4() As Task
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() = $"what{0}
            "
    End Sub
End Module
</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=0)
        End Function

        <WorkItem(2231, "https://github.com/dotnet/roslyn/issues/2231")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSmartIndentInsideMultiLineString() As Task
            Dim code = <code>Module Module1
    Sub Main()
        Dim c2() = $"1
            2"
    End Sub
End Module
</code>.Value

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=3,
                expectedIndentation:=0)
        End Function

        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSmartIndentAtCaseBlockEnd() As Task
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

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=6,
                expectedIndentation:=16)
        End Function

        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSmartIndentAtCaseBlockEndComment() As Task
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

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=7,
                expectedIndentation:=16)
        End Function

        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSmartIndentAtCaseBlockInbetweenComments() As Task
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

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=7,
                expectedIndentation:=16)
        End Function

        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Async Function TestSmartIndentAtCaseBlockEndUntabbedComment() As Task
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

            Await AssertSmartIndentAsync(
                code,
                indentationLine:=7,
                expectedIndentation:=12)
        End Function

        Private Shared Async Function AssertSmartIndentIndentationInProjectionAsync(markup As String,
                                                                    expectedIndentation As Integer) As Tasks.Task
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromFileAsync(markup)
                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument = workspace.CreateProjectionBufferDocument(s_htmlMarkup, workspace.Documents, LanguageNames.CSharp)

                Dim factory = TryCast(workspace.Services.GetService(Of IHostDependentFormattingRuleFactoryService)(),
                                    TestFormattingRuleFactoryServiceFactory.Factory)
                If factory IsNot Nothing Then
                    factory.BaseIndentation = s_baseIndentationOfNugget
                    factory.TextSpan = subjectDocument.SelectedSpans.Single()
                End If

                Dim indentationLine = projectedDocument.TextBuffer.CurrentSnapshot.GetLineFromPosition(projectedDocument.CursorPosition.Value)
                Dim point = projectedDocument.GetTextView().BufferGraph.MapDownToBuffer(indentationLine.Start, PointTrackingMode.Negative, subjectDocument.TextBuffer, PositionAffinity.Predecessor)

                TestIndentation(point.Value, expectedIndentation, projectedDocument.GetTextView(), subjectDocument)
            End Using
        End Function

        Friend Shared Sub TestIndentation(point As Integer, expectedIndentation As Integer?, textView As ITextView, subjectDocument As TestHostDocument)
            Dim snapshot = subjectDocument.TextBuffer.CurrentSnapshot
            Dim indentationLineFromBuffer = snapshot.GetLineFromPosition(point)
            Dim lineNumber = indentationLineFromBuffer.LineNumber

            Dim textUndoHistory = New Mock(Of ITextUndoHistoryRegistry)
            Dim editorOperationsFactory = New Mock(Of IEditorOperationsFactoryService)
            Dim editorOperations = New Mock(Of IEditorOperations)
            editorOperationsFactory.Setup(Function(x) x.GetEditorOperations(textView)).Returns(editorOperations.Object)

            Dim commandHandler = New SmartTokenFormatterCommandHandler(textUndoHistory.Object, editorOperationsFactory.Object)
            commandHandler.ExecuteCommandWorker(New ReturnKeyCommandArgs(textView, subjectDocument.TextBuffer), CancellationToken.None)
            Dim newSnapshot = subjectDocument.TextBuffer.CurrentSnapshot

            Dim actualIndentation As Integer?
            If newSnapshot.Version.VersionNumber > snapshot.Version.VersionNumber Then
                actualIndentation = newSnapshot.GetLineFromLineNumber(lineNumber).GetFirstNonWhitespaceOffset()
            Else
                Dim provider = New SmartIndent(textView)
                actualIndentation = provider.GetDesiredIndentation(indentationLineFromBuffer)
            End If

            If actualIndentation Is Nothing Then
                Dim x = 0
            End If

            Assert.Equal(Of Integer)(expectedIndentation.Value, actualIndentation.Value)
        End Sub

        ''' <param name="indentationLine">0-based. The line number in code to get indentation for.</param>
        Private Shared Async Function AssertSmartIndentAsync(code As String, indentationLine As Integer, expectedIndentation As Integer?, Optional indentStyle As FormattingOptions.IndentStyle = FormattingOptions.IndentStyle.Smart) As Task
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromFileAsync(code)
                Dim buffer = workspace.Documents.First().GetTextBuffer()

                SetIndentStyle(buffer, indentStyle)

                Dim bufferGraph = New Mock(Of IBufferGraph)(MockBehavior.Strict)
                bufferGraph.Setup(Function(x) x.MapUpToSnapshot(It.IsAny(Of SnapshotPoint)(),
                                                                It.IsAny(Of PointTrackingMode)(),
                                                                It.IsAny(Of PositionAffinity)(),
                                                                It.IsAny(Of ITextSnapshot))).
                    Returns(Of SnapshotPoint, PointTrackingMode, PositionAffinity, ITextSnapshot)(
                        Function(p, m, a, s)
                            Dim factory = TryCast(workspace.Services.GetService(Of IHostDependentFormattingRuleFactoryService)(),
                                            TestFormattingRuleFactoryServiceFactory.Factory)

                            If factory IsNot Nothing AndAlso factory.BaseIndentation <> 0 AndAlso factory.TextSpan.Contains(p.Position) Then
                                Dim line = p.GetContainingLine()
                                Dim projectedOffset = line.GetFirstNonWhitespaceOffset().Value - factory.BaseIndentation
                                Return New SnapshotPoint(p.Snapshot, p.Position - projectedOffset)
                            End If

                            Return p
                        End Function)

                WpfTestCase.RequireWpfFact("Test helper creates mocks of ITextView")

                Dim textView = New Mock(Of ITextView)(MockBehavior.Strict)
                textView.Setup(Function(x) x.Options).Returns(TestEditorOptions.Instance)
                textView.Setup(Function(x) x.BufferGraph).Returns(bufferGraph.Object)
                textView.SetupGet(Function(x) x.TextSnapshot).Returns(buffer.CurrentSnapshot)

                Using indenter = New SmartIndent(textView.Object)
                    Dim indentationLineFromBuffer = buffer.CurrentSnapshot.GetLineFromLineNumber(indentationLine)
                    Dim actualIndentation = indenter.GetDesiredIndentation(indentationLineFromBuffer)

                    If expectedIndentation.HasValue Then
                        Assert.Equal(Of Integer)(expectedIndentation.Value, actualIndentation.Value)
                    Else
                        Assert.Null(actualIndentation)
                    End If
                End Using
            End Using
        End Function

        Friend Shared Sub SetIndentStyle(buffer As ITextBuffer, indentStyle As FormattingOptions.IndentStyle)
            Dim optionService = buffer.GetWorkspace().Services.GetService(Of IOptionService)()
            Dim optionSet = optionService.GetOptions()
            optionService.SetOptions(optionSet.WithChangedOption(FormattingOptions.SmartIndent, LanguageNames.VisualBasic, indentStyle))
        End Sub
    End Class
End Namespace
