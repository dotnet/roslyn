' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
'Imports Microsoft.CodeAnalysis.VisualBasic.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Xunit
Imports Microsoft.Cci
Imports System
Imports System.Xml.Linq

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class NameLengthTests : Inherits BasicTestBase
        ' Longest legal symbol name.
        Private Shared ReadOnly LongSymbolName As New String("A"c, PeWriter.NameLengthLimit)
        ' Longest legal path name.
        Private Shared ReadOnly LongPathName As New String("A"c, PeWriter.PathLengthLimit)
        ' Longest legal local name.
        Private Shared ReadOnly LongLocalName As New String("A"c, PeWriter.PdbLengthLimit)

        <Fact>
        Public Sub UnmangledMemberNames()
            Dim sourceTemplate = <![CDATA[
Imports System

Class Fields
    Dim {0} As Integer   ' Fine
    Dim {0}1 As Integer  ' Too long
End Class

Class FieldLikeEvents
    Event {0} as Action     ' Fine (except accessors)
    Event {0}1 as Action    ' Fine (except accessors)
End Class

Class CustomEvents
    Custom Event {0} As Action          ' Fine (except accessors)
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
    Custom Event {0}1 As Action         ' Too long
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Class AutoProperties
    Property {0} As Integer     ' Fine (except accessors And backing field)
    Property {0}1 As Integer    ' Too long
End Class

Class CustomProperties
    Property {0} As Integer     ' Fine (except accessors)
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Property {0}1 As Integer    ' Too long
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class

Class Methods
    Sub {0}()   ' Fine
    End Sub
    Sub {0}1()  ' Too long
    End Sub
End Class
]]>

            Dim _longSquiggle_ As New String("~"c, LongSymbolName.Length)
            Dim source = Format(sourceTemplate, LongSymbolName)
            Dim comp = CreateCompilationWithMscorlib(source)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Dim <%= LongSymbolName %>1 As Integer  ' Too long
        <%= _longSquiggle_ %>~
BC37220: Name '<%= LongSymbolName %>Event' exceeds the maximum length allowed in metadata.
    Event <%= LongSymbolName %> as Action     ' Fine (except accessors)
          <%= _longSquiggle_ %>
BC37220: Name 'add_<%= LongSymbolName %>' exceeds the maximum length allowed in metadata.
    Event <%= LongSymbolName %> as Action     ' Fine (except accessors)
          <%= _longSquiggle_ %>
BC37220: Name 'remove_<%= LongSymbolName %>' exceeds the maximum length allowed in metadata.
    Event <%= LongSymbolName %> as Action     ' Fine (except accessors)
          <%= _longSquiggle_ %>
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Event <%= LongSymbolName %>1 as Action    ' Fine (except accessors)
          <%= _longSquiggle_ %>~
BC37220: Name '<%= LongSymbolName %>1Event' exceeds the maximum length allowed in metadata.
    Event <%= LongSymbolName %>1 as Action    ' Fine (except accessors)
          <%= _longSquiggle_ %>~
BC37220: Name 'add_<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Event <%= LongSymbolName %>1 as Action    ' Fine (except accessors)
          <%= _longSquiggle_ %>~
BC37220: Name 'remove_<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Event <%= LongSymbolName %>1 as Action    ' Fine (except accessors)
          <%= _longSquiggle_ %>~
BC37220: Name 'add_<%= LongSymbolName %>' exceeds the maximum length allowed in metadata.
        AddHandler(value As Action)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37220: Name 'remove_<%= LongSymbolName %>' exceeds the maximum length allowed in metadata.
        RemoveHandler(value As Action)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37220: Name 'raise_<%= LongSymbolName %>' exceeds the maximum length allowed in metadata.
        RaiseEvent()
        ~~~~~~~~~~~~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Custom Event <%= LongSymbolName %>1 As Action         ' Too long
                 <%= _longSquiggle_ %>~
BC37220: Name 'add_<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
        AddHandler(value As Action)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37220: Name 'remove_<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
        RemoveHandler(value As Action)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37220: Name 'raise_<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
        RaiseEvent()
        ~~~~~~~~~~~~
BC37220: Name '_<%= LongSymbolName %>' exceeds the maximum length allowed in metadata.
    Property <%= LongSymbolName %> As Integer     ' Fine (except accessors And backing field)
             <%= _longSquiggle_ %>
BC37220: Name 'get_<%= LongSymbolName %>' exceeds the maximum length allowed in metadata.
    Property <%= LongSymbolName %> As Integer     ' Fine (except accessors And backing field)
             <%= _longSquiggle_ %>
BC37220: Name 'set_<%= LongSymbolName %>' exceeds the maximum length allowed in metadata.
    Property <%= LongSymbolName %> As Integer     ' Fine (except accessors And backing field)
             <%= _longSquiggle_ %>
BC37220: Name '_<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Property <%= LongSymbolName %>1 As Integer    ' Too long
             <%= _longSquiggle_ %>~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Property <%= LongSymbolName %>1 As Integer    ' Too long
             <%= _longSquiggle_ %>~
BC37220: Name 'get_<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Property <%= LongSymbolName %>1 As Integer    ' Too long
             <%= _longSquiggle_ %>~
BC37220: Name 'set_<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Property <%= LongSymbolName %>1 As Integer    ' Too long
             <%= _longSquiggle_ %>~
BC37220: Name 'get_<%= LongSymbolName %>' exceeds the maximum length allowed in metadata.
        Get
        ~~~
BC37220: Name 'set_<%= LongSymbolName %>' exceeds the maximum length allowed in metadata.
        Set(value As Integer)
        ~~~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Property <%= LongSymbolName %>1 As Integer    ' Too long
             <%= _longSquiggle_ %>~
BC37220: Name 'get_<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
        Get
        ~~~
BC37220: Name 'set_<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
        Set(value As Integer)
        ~~~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Sub <%= LongSymbolName %>1()  ' Too long
        <%= _longSquiggle_ %>~
</errors>)
        End Sub

        <Fact>
        Public Sub EmptyNamespaces()
            Dim sourceTemplate = <![CDATA[
Namespace {0}   ' Fine.
End Namespace

Namespace {0}1  ' Too long, but not checked.
End Namespace
]]>

            Dim source = Format(sourceTemplate, LongSymbolName)
            Dim comp = CreateCompilationWithMscorlib(source)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors/>)
        End Sub

        <Fact>
        Public Sub NonGeneratedTypeNames()
            ' {n} == LongSymbolName.Substring(n)
            Dim sourceTemplate = <![CDATA[
Class {0}   ' Fine
End Class

Class {0}1  ' Too long
End Class

Namespace N
    Structure {2}   ' Fine
    End Structure

    Structure {2}1  ' Too long after prepending 'N.'
    End Structure
End Namespace

Class Outer
    Enum {0}    ' Fine, since outer class is not prepended
        A
    End Enum

    Enum {0}1   ' Too long
        A
    End Enum
End Class

Interface {2}(Of T)     ' Fine
End Interface

Interface {2}1(Of T)    ' Too long after appending '`1'
End Interface
]]>

            Dim substring0 = LongSymbolName
            Dim substring1 = LongSymbolName.Substring(1)
            Dim substring2 = LongSymbolName.Substring(2)
            Dim _squiggle2 As New String("~"c, substring2.Length)
            Dim source = Format(sourceTemplate, substring0, substring1, substring2)
            Dim comp = CreateCompilationWithMscorlib(source)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC37220: Name '<%= substring2 %>AA1' exceeds the maximum length allowed in metadata.
Class <%= substring2 %>AA1  ' Too long
      <%= _squiggle2 %>~~~
BC37220: Name 'N.<%= substring2 %>1' exceeds the maximum length allowed in metadata.
    Structure <%= substring2 %>1  ' Too long after prepending 'N.'
              <%= _squiggle2 %>~
BC37220: Name '<%= substring2 %>AA1' exceeds the maximum length allowed in metadata.
    Enum <%= substring2 %>AA1   ' Too long
         <%= _squiggle2 %>~~~
BC37220: Name '<%= substring2 %>1`1' exceeds the maximum length allowed in metadata.
Interface <%= substring2 %>1(Of T)    ' Too long after appending '`1'
          <%= _squiggle2 %>~
                                            </errors>)
        End Sub

        <Fact>
        Public Sub ExplicitInterfaceImplementation()
            Dim sourceTemplate = <![CDATA[
Interface I
    Sub {0}()
    Sub {0}1()
End Interface

Namespace N
    Interface J(Of T)
        Sub {0}()
        Sub {0}1()
    End Interface
End Namespace

Class C : Implements I
    Sub {0}() Implements I.{0}
    End Sub

    Sub {0}1() Implements I.{0}1
    End Sub
End Class

Class D : Implements N.J(Of C)
    Sub {0}() Implements N.J(Of C).{0}
    End Sub

    Sub {0}1() Implements N.J(Of C).{0}1
    End Sub
End Class
]]>

            ' Unlike in C#, explicit interface implementation members don't have mangled names.
            Dim source = Format(sourceTemplate, LongSymbolName)
            Dim comp = CreateCompilationWithMscorlib(source)
            Dim _longSquiggle_ As New String("~"c, LongSymbolName.Length)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Sub <%= LongSymbolName %>1()
        <%= _longSquiggle_ %>~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
        Sub <%= LongSymbolName %>1()
            <%= _longSquiggle_ %>~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Sub <%= LongSymbolName %>1() Implements I.<%= LongSymbolName %>1
        <%= _longSquiggle_ %>~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Sub <%= LongSymbolName %>1() Implements N.J(Of C).<%= LongSymbolName %>1
        <%= _longSquiggle_ %>~
                                            </errors>)
        End Sub

        <Fact>
        Public Sub DllImport()
            Dim sourceTemplate = <![CDATA[
Imports System.Runtime.InteropServices

Class C1
    <DllImport("foo.dll", EntryPoint:="Short1")>
    Shared Sub {0}()  ' Name is fine, entrypoint is fine.
    End Sub
    <DllImport("foo.dll", EntryPoint:="Short2")>
    Shared Sub {0}1() ' Name is too Long, entrypoint is fine.
    End Sub
End Class

Class C2
    <DllImport("foo.dll", EntryPoint:="{0}")>
    Shared Sub Short1()   ' Name is fine, entrypoint is fine.
    End Sub
    <DllImport("foo.dll", EntryPoint:="{0}1")>
    Shared Sub Short2()   ' Name is fine, entrypoint is too Long.
    End Sub
End Class

Class C3
    <DllImport("foo.dll")>
    Shared Sub {0}()  ' Name is fine, entrypoint is unspecified.
    End Sub
    <DllImport("foo.dll")>
    Shared Sub {0}1() ' Name is too Long, entrypoint is unspecified.
    End Sub
End Class
]]>

            Dim source = Format(sourceTemplate, LongSymbolName)
            Dim comp = CreateCompilationWithMscorlib(source)
            Dim _longSquiggle_ As New String("~"c, LongSymbolName.Length)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Shared Sub <%= LongSymbolName %>1() ' Name is too Long, entrypoint is fine.
               <%= _longSquiggle_ %>~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Shared Sub Short2()   ' Name is fine, entrypoint is too Long.
               ~~~~~~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Shared Sub <%= LongSymbolName %>1() ' Name is too Long, entrypoint is unspecified.
               <%= _longSquiggle_ %>~
</errors>)
        End Sub

        <Fact>
        Public Sub Parameters()
            Dim sourceTemplate = <![CDATA[
Class C
    Sub M({0} As Short)
    End Sub
    Sub M({0}1 As Long)
    End Sub

    ReadOnly Property P({0} As Short) As Integer
        Get
            Return 0
        End Get
    End Property
    ReadOnly Property P({0}1 As Long) As Integer
        Get
            Return 0
        End Get
    End Property

    Delegate Sub D1({0} As Short)
    Delegate Sub D2({0}1 As Long)
End Class
]]>

            Dim source = Format(sourceTemplate, LongSymbolName)
            Dim comp = CreateCompilationWithMscorlib(source)
            Dim _longSquiggle_ As New String("~"c, LongSymbolName.Length)
            comp.AssertNoDiagnostics()
            ' Second report is for Invoke method.  Not ideal, but not urgent.
            comp.AssertTheseEmitDiagnostics(<errors>
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Sub M(<%= LongSymbolName %>1 As Long)
          <%= _longSquiggle_ %>~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    ReadOnly Property P(<%= LongSymbolName %>1 As Long) As Integer
                        <%= _longSquiggle_ %>~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Delegate Sub D2(<%= LongSymbolName %>1 As Long)
                    <%= _longSquiggle_ %>~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Delegate Sub D2(<%= LongSymbolName %>1 As Long)
                    <%= _longSquiggle_ %>~
</errors>)
        End Sub

        <Fact>
        Public Sub TypeParameters()
            Dim sourceTemplate = <![CDATA[
Class C(Of {0}, {0}1)
End Class

Delegate Sub D(Of {0}, {0}1)()

Class E
    Sub M(Of {0}, {0}1)()
    End Sub
End Class
]]>

            Dim source = Format(sourceTemplate, LongSymbolName)
            Dim comp = CreateCompilationWithMscorlib(source)
            Dim __longSpace___ As New String(" "c, LongSymbolName.Length)
            Dim _longSquiggle_ As New String("~"c, LongSymbolName.Length)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
Class C(Of <%= LongSymbolName %>, <%= LongSymbolName %>1)
             <%= __longSpace___ %><%= _longSquiggle_ %>~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
Delegate Sub D(Of <%= LongSymbolName %>, <%= LongSymbolName %>1)()
                    <%= __longSpace___ %><%= _longSquiggle_ %>~
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
    Sub M(Of <%= LongSymbolName %>, <%= LongSymbolName %>1)()
               <%= __longSpace___ %><%= _longSquiggle_ %>~
</errors>)
        End Sub

        <Fact>
        Public Sub Locals()
            Dim sourceTemplate = <![CDATA[
Class C
    Function M() As Long
        Dim {0} As Short = 1
        Dim {0}1 As Long = 1
        Return {0} + {0}1
    End Function
End Class
]]>

            Dim source = Format(sourceTemplate, LongLocalName)
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib({source}, {}, compOptions:=TestOptions.DebugDll)
            Dim _longSquiggle_ As New String("~"c, LongLocalName.Length)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC42373: Local name '<%= LongLocalName %>1' is too long for PDB.  Consider shortening or compiling without /debug.
        Dim <%= LongLocalName %>1 As Long = 1
            <%= _longSquiggle_ %>~
</errors>)
        End Sub

        <Fact>
        Public Sub ConstantLocals()
            Dim sourceTemplate = <![CDATA[
Class C
    Function M() As Long
        Const {0} As Short = 1
        Const {0}1 As Long = 1
        Return {0} + {0}1
    End Function
End Class
]]>

            Dim source = Format(sourceTemplate, LongLocalName)
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib({source}, {}, compOptions:=TestOptions.DebugDll)
            Dim _longSquiggle_ As New String("~"c, LongLocalName.Length)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC42373: Local name '<%= LongLocalName %>1' is too long for PDB.  Consider shortening or compiling without /debug.
        Const <%= LongLocalName %>1 As Long = 1
              <%= _longSquiggle_ %>~
</errors>)
        End Sub

        <Fact>
        Public Sub TestStateMachineMethods()
            Dim sourceTemplate = <![CDATA[
Imports System.Collections.Generic
Imports System.Threading.Tasks

Class Iterators
    Iterator Function {0}() As IEnumerable(Of Integer)
        Yield 1
    End Function

    Iterator Function {0}1() As IEnumerable(Of Integer)
        Yield 1
    End Function
End Class

Class Async
    Async Function {0}() As Task
        Await {0}()
    End Function

    Async Function {0}1() As Task
        Await {0}1()
    End Function
End Class
]]>
            Dim padding = GeneratedNames.MakeStateMachineTypeName(1, "A").Length - 1
            Dim longName = LongSymbolName.Substring(padding)
            Dim longSquiggle As New String("~"c, longName.Length)
            Dim source = Format(sourceTemplate, longName)
            Dim comp = CreateCompilationWithMscorlib45(source)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC37220: Name 'VB$StateMachine_1_<%= longName %>1' exceeds the maximum length allowed in metadata.
BC37220: Name 'VB$StateMachine_2_<%= longName %>1' exceeds the maximum length allowed in metadata.
                                            </errors>)
        End Sub

        <Fact>
        Public Sub TestResources()
            Dim comp = CreateCompilationWithMscorlib("Class C : End Class")
            Dim dataProvider As Func(Of Stream) = Function() New System.IO.MemoryStream()
            Dim resources =
            {
                New ResourceDescription("name1", "path1", dataProvider, False),   'fine
                New ResourceDescription(LongSymbolName, "path2", dataProvider, False), 'fine
                New ResourceDescription("name2", LongPathName, dataProvider, False), 'fine
                New ResourceDescription(LongSymbolName & 1, "path3", dataProvider, False), 'name error
                New ResourceDescription("name3", LongPathName & 2, dataProvider, False), 'path error
                New ResourceDescription(LongSymbolName & 3, LongPathName & 4, dataProvider, False) 'name And path errors
            }
            Using assemblyStream As New System.IO.MemoryStream()
                Using pdbStream As New System.IO.MemoryStream()
                    Dim diagnostics = comp.Emit(assemblyStream, pdbStream:=pdbStream, manifestResources:=resources).Diagnostics
                    AssertTheseDiagnostics(diagnostics, <errors>
BC37220: Name '<%= LongPathName %>2' exceeds the maximum length allowed in metadata.
BC37220: Name '<%= LongPathName %>4' exceeds the maximum length allowed in metadata.
BC37220: Name '<%= LongSymbolName %>1' exceeds the maximum length allowed in metadata.
BC37220: Name '<%= LongSymbolName %>3' exceeds the maximum length allowed in metadata.
</errors>)
                End Using
            End Using
        End Sub

        Private Shared Function Format(sourceTemplate As XCData, ParamArray args As Object()) As String
            Return String.Format(sourceTemplate.Value.Replace(vbCr, vbCrLf), args)
        End Function

        Private Function CreateCompilationWithMscorlib(source As String) As VisualBasicCompilation
            Return CompilationUtils.CreateCompilationWithMscorlib({source}, {}, TestOptions.ReleaseDll)
        End Function

        Private Function CreateCompilationWithMscorlib45(source As String) As VisualBasicCompilation
            Return VisualBasicCompilation.Create(GetUniqueName(),
                                                 {VisualBasicSyntaxTree.ParseText(source)},
                                                 {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929},
                                                 TestOptions.ReleaseDll)
        End Function
    End Class
End Namespace