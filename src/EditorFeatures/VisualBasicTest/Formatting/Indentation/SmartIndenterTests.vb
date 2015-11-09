' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
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
        Public Sub EmptyFile()
            AssertSmartIndent(
                code:="",
                indentationLine:=0,
                expectedIndentation:=0)
        End Sub

        <WpfFact(Skip:="674611")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        <WorkItem(529886)>
        <WorkItem(674611)>
        Public Sub AtBeginningOfSpanInNugget()
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
        <WorkItem(529886)>
        Public Sub AtEndOfSpanInNugget()
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
        <WorkItem(529886)>
        Public Sub InsideMiddleOfSpanInNugget()
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
        <WorkItem(529886)>
        Public Sub AtContinuationAtStartOfNugget()
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
        <WorkItem(529886)>
        Public Sub AtContinuationInsideOfNugget()
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
        Public Sub BadLineNumberLabelInFile()
            AssertSmartIndent(
                code:="10:",
                indentationLine:=0,
                expectedIndentation:=0)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub ImportStatement()
            Dim code = <Code>Import System
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=1,
                expectedIndentation:=0)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub Comments()
            Dim code = <Code>        ' comments
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=1,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub XmlComments()
            Dim code = <Code>        ''' Xml comments
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=1,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub ClassStatement()
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
        Public Sub ClassStatementWithInherits()
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
        Public Sub EndClassStatement()
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
        Public Sub ClassStatementWithInheritsImplementsAndStatementSeparators()
            Dim code = <Code>Namespace NS
    Class CL
        Inherits IFoo : Implements Foo
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub ClassStatementWithInheritsImplementsAndStatementSeparators2()
            Dim code = <Code>Namespace NS
    Class CL : Inherits IFoo : Implements Foo
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub InterfaceStatement()
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
        Public Sub EndInterfaceStatement()
            Dim code = <Code>Namespace NS
    Interface IF
        Sub Foo()
    End Interface
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub InterfaceStatementWithInherits()
            Dim code = <Code>Namespace NS
    Interface IF
        Inherits IFoo
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub InterfaceStatementWithInheritsOnTheSameLine()
            Dim code = <Code>Namespace NS
    Interface IF : Inherits IFoo
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub EnumStatement()
            Dim code = <Code>Namespace NS
    Enum Foo
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub EndEnumStatement()
            Dim code = <Code>Namespace NS
    Enum Foo
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
        Public Sub EnumMembers()
            Dim code = <Code>Namespace NS
    Enum Foo
        Member1
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub StructureStatement()
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
        Public Sub EndStructureStatement()
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
        Public Sub NamespaceStatement()
            Dim code = <Code>Namespace NS
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=1,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub EndNamespaceStatement()
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
        Public Sub ModuleStatement()
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
        Public Sub EndModuleStatement()
            Dim code = <Code>Namespace NS
    Module Module1
        Sub Foo()
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
        Public Sub SubStatement()
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
        Public Sub SubStatementWithParametersOnDifferentLines()
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
        Public Sub SingleLineIfStatement()
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
        Public Sub IfStatement()
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
        Public Sub ElseStatement()
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
        Public Sub EndIfStatement()
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
        Public Sub LineContinuedIfStatement()
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
        Public Sub DoStatement()
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
        Public Sub EndDoStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Do
                Foo()
            Loop
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub ForStatement()
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
        Public Sub ForEachStatement()
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
        Public Sub EndForStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            For i = 1 To 10
                Foo()
            Next
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub OperatorStatement()
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
        Public Sub SelectStatement()
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
        Public Sub SelectCaseStatement()
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
        Public Sub CaseStatement()
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
        Public Sub CaseStatementWithCode()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select A
                Case 1
                    foo()
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=20)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub CaseElseStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select A
                Case 1
                    foo()
                Case Else
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=7,
                expectedIndentation:=20)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub EndSelectStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Select A
                Case 1
                    foo()
            End Select
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=7,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub SyncLockStatement()
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
        Public Sub EndSyncLockStatement()
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
        Public Sub TryStatement()
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
        Public Sub CatchStatement()
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
        Public Sub FinallyStatement()
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
        Public Sub EndTryStatement()
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
        Public Sub UsingStatement()
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
        Public Sub WhileStatement()
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
        Public Sub EndWhileStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            While True
                Foo()
            End While
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Sub


        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub WithStatement()
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
        Public Sub EndWithStatement()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            With DataStructure
                .foo = "foo"
            End With
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=6,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub PropertyStatementWithParameter()
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
        Public Sub PropertyStatementWithoutParens()
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
        Public Sub PropertyStatementWithParens()
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
        Public Sub PropertyStatementWithGet()
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
        Public Sub PropertyStatementWithSet()
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
        <WorkItem(536466)>
        Public Sub XmlComments2()
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
        <WorkItem(536545)>
        Public Sub XmlComments3()
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
        Public Sub SingleLineFunctionLambda()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Function(x) 42
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub MultiLineFunctionLambda()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Function(x)
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=22)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub MultiLineFunctionLambdaWithComment()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Function(x) 'Comment
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=22)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub SingleLineSubLambda()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Sub(x) Console.WriteLine("Foo")
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub SingleLineSubLambda2()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Sub(x) Console.WriteLine("Foo") _
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=26)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub MultiLineSubLambda()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Sub(x)
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=22)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub MultiLineSubLambdaWithComment()
            Dim code = <Code>Namespace NS
    Class CL
        Sub Method()
            Dim foo = Sub(x) 'Comment
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
        Public Sub QueryExpressionOnSingleLineAmbiguous()
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
        Public Sub QueryExpressionOnMultipleLinesAmbiguous()
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
        Public Sub QueryExpressionOnMultipleLinesAmbiguous2()
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
        <WorkItem(538933)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub QueryExpressionFollowedByBlankLine()
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
        Public Sub QueryExpressionWithNestedQueryExpressionOnNewLine()
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
        Public Sub QueryExpressionWithNestedQueryExpressionOnSameLine()
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
        Public Sub QueryExpressionWithNestedQueryExpressionWithMultipleLines()
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

        <WorkItem(536762)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub BugFix1417_2()
            Dim code = <Code>Sub Main()
    Dim foo = From x In y
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=14)
        End Sub

        Public Sub QueryExpressionExplicitLineContinued()
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

#End Region

#Region "Implicit line-continuation"
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub ImplicitLineContinuationAfterAttributeInNamespace()
            Dim code = "Namespace foo" & vbCrLf &
                       "    <SomeAttribute()>" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub ImplicitLineContinuationWithMultipleAttributes()
            Dim code = "Namespace foo" & vbCrLf &
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
        Public Sub ImplicitLineContinuationAfterAttributeInClass()
            Dim code = "Namespace foo" & vbCrLf &
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
        Public Sub ImplicitLineContinuationMethodParameters()
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
        Public Sub ImplicitLineContinuationMethodArguments()
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
        Public Sub ImplicitLineContinuationExpression()
            Dim code = <Code>Class C
    Sub Method()
        Dim a = 
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=12)
        End Sub

        <WorkItem(539456)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub ImplicitLineContinuationExpression1()
            Dim code = <Code>Class C
    Function Foo$(ParamArray arg())
        Dim r$ = "3"
        Foo$ = Foo$(
            r$

    End Function
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=5,
                expectedIndentation:=12)
        End Sub

        <WorkItem(540634)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub ImplicitLineContinuationExpression2()
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
        Public Sub ExplicitLineContinuationInExpression()
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
        Public Sub MultipleExplicitLineContinuationsInExpression()
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
        Public Sub ExplicitLineContinuationInFieldDeclaration()
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
        Public Sub ExplicitLineContinuationAfterAttributeInNamespace()
            Dim code = "Namespace foo" & vbCrLf &
                       "    <SomeAttribute()> _" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=2,
                expectedIndentation:=4)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub ExplicitLineContinuationWithMultipleAttributes()
            Dim code = "Namespace foo" & vbCrLf &
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
        Public Sub ExplicitLineContinuationAfterAttributeInClass()
            Dim code = "Namespace foo" & vbCrLf &
                       "    Class C" & vbCrLf &
                       "        <SomeAttribute()> _" & vbCrLf &
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
        Public Sub MultipleStatementsWithStatementSeparators()
            Dim code = <Code>Namespace Foo
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
        Public Sub MultipleStatementsIncludingMultilineLambdaWithStatementSeparators()
            Dim code = <Code>Namespace Foo
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
        <WorkItem(538937)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub PreprocessorConstWithoutAssignment()
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#Const foo
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <WorkItem(538937)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub PreprocessorConstWithAssignment()
            Dim code = <Code>Namespace SomeNamespace
    Class C
        Sub Method()
#Const foo = 42
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WpfFact>
        <WorkItem(538937)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub PreprocessorIf()
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
        <WorkItem(538937)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub PreprocessorElseIf()
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
        <WorkItem(538937)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub PreprocessorElse()
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
        <WorkItem(538937)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub PreprocessorEndIf()
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
        Public Sub XMLLiteralOpenTag()
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
        Public Sub XMLLiteralNestOpenTag()
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
        Public Sub XMLLiteralCloseTag()
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
        <WorkItem(538938)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub XMLLiteralCloseTagInXML()
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
        <WorkItem(816976)>
        <WorkItem(538938)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub XMLExpressionHole()
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
        Public Sub XMLExpressionHoleWithMultilineLambda()
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
        <WorkItem(538938)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub XMLExpressionHoleClosed()
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
        Public Sub XMLExpressionHoleWithXMLInIt()
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
        Public Sub XMLLiteralText()
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    foo" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=8)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub XMLIndentOnBlankLine()
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

        <WorkItem(816976)>
        <WpfFact(Skip:="Bug 816976")>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub XMLIndentOnLineContinuedXMLExpressionHole()
            Dim code = "Class C" & vbCrLf &
                       "    Sub Method()" & vbCrLf &
                       "        Dim q = <xml>" & vbCrLf &
                       "                    <%= Foo(2 _" & vbCrLf &
                       ""

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=28)
        End Sub
#End Region

#Region "Bugs"

        <WorkItem(538771)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub BugFix4481()
            Dim code = <Code>_
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=1,
                expectedIndentation:=4)
        End Sub

        <WorkItem(538771)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub BugFix4481_2()
            Dim code = <Code>  _
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=1,
                expectedIndentation:=6)
        End Sub

        <WorkItem(539553)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub Bug5559()
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

        <WorkItem(539575)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub Bug5586()
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

        <WorkItem(539609)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub Bug5629()
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

        <WorkItem(539686)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub Bug5730()
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

        <WorkItem(539686)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub Bug5730_1()
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

        <WorkItem(539639)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub Bug5666()
            Dim code = <Code>Module Program
    Sub Main(args As String())
        If True Then
#Const foo = 23

        End If
    End Sub
End Module
</Code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=4,
                expectedIndentation:=12)
        End Sub

        <WorkItem(539453)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub Bug5430_1()
            Dim code = My.Resources.XmlLiterals.IndentationTest2

            AssertSmartIndent(
                code,
                indentationLine:=11,
                expectedIndentation:=16)
        End Sub

        <WorkItem(540198)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub Bug6374()
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

        <WorkItem(542240)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub MissingEndStatement()
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
        Public Sub SmartIndenterConstructorThrows1()
            AssertEx.Throws(Of ArgumentNullException)(
                Function() New SmartIndent(Nothing),
                allowDerived:=True)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub Parameter1()
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
        Public Sub Argument()
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
        Public Sub Parameter_LineContinuation()
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
        Public Sub Parameter_LineContinuation2()
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
        Public Sub Parameter2()
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
        Public Sub TypeParameter()
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
        Public Sub TypeParameter2()
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
        Public Sub TypeArgument()
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
        Public Sub TypeArgument2()
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
        Public Sub Argument_ImplicitLineContinuation()
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
        Public Sub Argument_ImplicitLineContinuation2()
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
        Public Sub StatementAfterLabel()
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
        Public Sub AfterStatementInNugget()
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
        Public Sub AfterStatementOnFirstLineOfNugget()
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
        Public Sub InQueryInNugget()
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

        <WorkItem(574314)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub InQueryOnFirstLineOfNugget()
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
        Public Sub InNestedBlockInNugget()
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
        Public Sub InNestedBlockStartingOnFirstLineOfNugget()
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

        <WpfFact, WorkItem(646663)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub InEmptyNugget()
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

        <WpfFact, WorkItem(1190278)>
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
        Public Sub BlockIndentation1()
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
        Public Sub BlockIndentation2()
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
        Public Sub NoIndentation()
            Dim code = <code>Class C
    Sub Main()
        Dim x = 3
</code>.Value

            AssertSmartIndent(
                code,
                indentationLine:=3,
                expectedIndentation:=Nothing,
                indentStyle:=FormattingOptions.IndentStyle.None)
        End Sub

        <WpfFact>
        <WorkItem(809354)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub CaseStatement1()
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
        <WorkItem(1082028)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub NotLineContinuationIndentation_Empty()
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
        <WorkItem(1082028)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub LineContinuationIndentation()
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
        <WorkItem(1082028)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub NotLineContinuationIndentation_ObjectMember()
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
        <WorkItem(1082028)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub NotLineContinuationIndentation_ObjectCollection()
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
        <WorkItem(1082028)>
        <Trait(Traits.Feature, Traits.Features.SmartIndent)>
        Public Sub NotLineContinuationIndentation_Collection()
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
        Public Sub SmartIndentInsideInterpolatedMultiLineString_0()
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
        Public Sub SmartIndentInsideInterpolatedMultiLineString_1()
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
        Public Sub SmartIndentInsideInterpolatedMultiLineString_2()
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
        Public Sub SmartIndentInsideInterpolatedMultiLineString_3()
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
        Public Sub SmartIndentInsideInterpolatedMultiLineString_4()
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
        Public Sub SmartIndentInsideMultiLineString()
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
        Public Sub SmartIndentAtCaseBlockEnd()
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
        Public Sub SmartIndentAtCaseBlockEndComment()
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
        Public Sub SmartIndentAtCaseBlockInbetweenComments()
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
        Public Sub SmartIndentAtCaseBlockEndUntabbedComment()
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

        Private Shared Sub AssertSmartIndentIndentationInProjection(markup As String,
                                                                    expectedIndentation As Integer)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines({markup})
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
        End Sub

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
        Private Shared Sub AssertSmartIndent(code As String, indentationLine As Integer, expectedIndentation As Integer?, Optional indentStyle As FormattingOptions.IndentStyle = FormattingOptions.IndentStyle.Smart)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines({code})
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
        End Sub

        Friend Shared Sub SetIndentStyle(buffer As ITextBuffer, indentStyle As FormattingOptions.IndentStyle)
            Dim optionService = buffer.GetWorkspace().Services.GetService(Of IOptionService)()
            Dim optionSet = optionService.GetOptions()
            optionService.SetOptions(optionSet.WithChangedOption(FormattingOptions.SmartIndent, LanguageNames.VisualBasic, indentStyle))
        End Sub
    End Class
End Namespace
