' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        Private Shared ReadOnly s_longSymbolName As New String("A"c, MetadataWriter.NameLengthLimit)
        ' Longest legal path name.
        Private Shared ReadOnly s_longPathName As New String("A"c, MetadataWriter.PathLengthLimit)
        ' Longest legal local name.
        Private Shared ReadOnly s_longLocalName As New String("A"c, MetadataWriter.PdbLengthLimit)

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

            Dim _longSquiggle_ As New String("~"c, s_longSymbolName.Length)
            Dim source = Format(sourceTemplate, s_longSymbolName)
            Dim comp = CreateCompilationWithMscorlib(source)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Dim <%= s_longSymbolName %>1 As Integer  ' Too long
        <%= _longSquiggle_ %>~
BC37220: Name '<%= s_longSymbolName %>Event' exceeds the maximum length allowed in metadata.
    Event <%= s_longSymbolName %> as Action     ' Fine (except accessors)
          <%= _longSquiggle_ %>
BC37220: Name 'add_<%= s_longSymbolName %>' exceeds the maximum length allowed in metadata.
    Event <%= s_longSymbolName %> as Action     ' Fine (except accessors)
          <%= _longSquiggle_ %>
BC37220: Name 'remove_<%= s_longSymbolName %>' exceeds the maximum length allowed in metadata.
    Event <%= s_longSymbolName %> as Action     ' Fine (except accessors)
          <%= _longSquiggle_ %>
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Event <%= s_longSymbolName %>1 as Action    ' Fine (except accessors)
          <%= _longSquiggle_ %>~
BC37220: Name '<%= s_longSymbolName %>1Event' exceeds the maximum length allowed in metadata.
    Event <%= s_longSymbolName %>1 as Action    ' Fine (except accessors)
          <%= _longSquiggle_ %>~
BC37220: Name 'add_<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Event <%= s_longSymbolName %>1 as Action    ' Fine (except accessors)
          <%= _longSquiggle_ %>~
BC37220: Name 'remove_<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Event <%= s_longSymbolName %>1 as Action    ' Fine (except accessors)
          <%= _longSquiggle_ %>~
BC37220: Name 'add_<%= s_longSymbolName %>' exceeds the maximum length allowed in metadata.
        AddHandler(value As Action)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37220: Name 'remove_<%= s_longSymbolName %>' exceeds the maximum length allowed in metadata.
        RemoveHandler(value As Action)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37220: Name 'raise_<%= s_longSymbolName %>' exceeds the maximum length allowed in metadata.
        RaiseEvent()
        ~~~~~~~~~~~~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Custom Event <%= s_longSymbolName %>1 As Action         ' Too long
                 <%= _longSquiggle_ %>~
BC37220: Name 'add_<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
        AddHandler(value As Action)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37220: Name 'remove_<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
        RemoveHandler(value As Action)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC37220: Name 'raise_<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
        RaiseEvent()
        ~~~~~~~~~~~~
BC37220: Name '_<%= s_longSymbolName %>' exceeds the maximum length allowed in metadata.
    Property <%= s_longSymbolName %> As Integer     ' Fine (except accessors And backing field)
             <%= _longSquiggle_ %>
BC37220: Name 'get_<%= s_longSymbolName %>' exceeds the maximum length allowed in metadata.
    Property <%= s_longSymbolName %> As Integer     ' Fine (except accessors And backing field)
             <%= _longSquiggle_ %>
BC37220: Name 'set_<%= s_longSymbolName %>' exceeds the maximum length allowed in metadata.
    Property <%= s_longSymbolName %> As Integer     ' Fine (except accessors And backing field)
             <%= _longSquiggle_ %>
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Property <%= s_longSymbolName %>1 As Integer    ' Too long
             <%= _longSquiggle_ %>~
BC37220: Name '_<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Property <%= s_longSymbolName %>1 As Integer    ' Too long
             <%= _longSquiggle_ %>~
BC37220: Name 'get_<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Property <%= s_longSymbolName %>1 As Integer    ' Too long
             <%= _longSquiggle_ %>~
BC37220: Name 'set_<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Property <%= s_longSymbolName %>1 As Integer    ' Too long
             <%= _longSquiggle_ %>~
BC37220: Name 'get_<%= s_longSymbolName %>' exceeds the maximum length allowed in metadata.
        Get
        ~~~
BC37220: Name 'set_<%= s_longSymbolName %>' exceeds the maximum length allowed in metadata.
        Set(value As Integer)
        ~~~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Property <%= s_longSymbolName %>1 As Integer    ' Too long
             <%= _longSquiggle_ %>~
BC37220: Name 'get_<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
        Get
        ~~~
BC37220: Name 'set_<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
        Set(value As Integer)
        ~~~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Sub <%= s_longSymbolName %>1()  ' Too long
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

            Dim source = Format(sourceTemplate, s_longSymbolName)
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

            Dim substring0 = s_longSymbolName
            Dim substring1 = s_longSymbolName.Substring(1)
            Dim substring2 = s_longSymbolName.Substring(2)
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
            Dim source = Format(sourceTemplate, s_longSymbolName)
            Dim comp = CreateCompilationWithMscorlib(source)
            Dim _longSquiggle_ As New String("~"c, s_longSymbolName.Length)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Sub <%= s_longSymbolName %>1()
        <%= _longSquiggle_ %>~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
        Sub <%= s_longSymbolName %>1()
            <%= _longSquiggle_ %>~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
        Sub <%= s_longSymbolName %>1()
            <%= _longSquiggle_ %>~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Sub <%= s_longSymbolName %>1() Implements I.<%= s_longSymbolName %>1
        <%= _longSquiggle_ %>~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Sub <%= s_longSymbolName %>1() Implements N.J(Of C).<%= s_longSymbolName %>1
        <%= _longSquiggle_ %>~
                                            </errors>)
        End Sub

        <Fact>
        Public Sub DllImport()
            Dim sourceTemplate = <![CDATA[
Imports System.Runtime.InteropServices

Class C1
    <DllImport("goo.dll", EntryPoint:="Short1")>
    Shared Sub {0}()  ' Name is fine, entrypoint is fine.
    End Sub
    <DllImport("goo.dll", EntryPoint:="Short2")>
    Shared Sub {0}1() ' Name is too Long, entrypoint is fine.
    End Sub
End Class

Class C2
    <DllImport("goo.dll", EntryPoint:="{0}")>
    Shared Sub Short1()   ' Name is fine, entrypoint is fine.
    End Sub
    <DllImport("goo.dll", EntryPoint:="{0}1")>
    Shared Sub Short2()   ' Name is fine, entrypoint is too Long.
    End Sub
End Class

Class C3
    <DllImport("goo.dll")>
    Shared Sub {0}()  ' Name is fine, entrypoint is unspecified.
    End Sub
    <DllImport("goo.dll")>
    Shared Sub {0}1() ' Name is too Long, entrypoint is unspecified.
    End Sub
End Class
]]>

            Dim source = Format(sourceTemplate, s_longSymbolName)
            Dim comp = CreateCompilationWithMscorlib(source)
            Dim _longSquiggle_ As New String("~"c, s_longSymbolName.Length)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Shared Sub <%= s_longSymbolName %>1() ' Name is too Long, entrypoint is fine.
               <%= _longSquiggle_ %>~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Shared Sub Short2()   ' Name is fine, entrypoint is too Long.
               ~~~~~~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Shared Sub <%= s_longSymbolName %>1() ' Name is too Long, entrypoint is unspecified.
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

            Dim source = Format(sourceTemplate, s_longSymbolName)
            Dim comp = CreateCompilationWithMscorlib(source)
            Dim _longSquiggle_ As New String("~"c, s_longSymbolName.Length)
            comp.AssertNoDiagnostics()
            ' Second report is for Invoke method.  Not ideal, but not urgent.
            comp.AssertTheseEmitDiagnostics(<errors>
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Sub M(<%= s_longSymbolName %>1 As Long)
          <%= _longSquiggle_ %>~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    ReadOnly Property P(<%= s_longSymbolName %>1 As Long) As Integer
                        <%= _longSquiggle_ %>~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Delegate Sub D2(<%= s_longSymbolName %>1 As Long)
                    <%= _longSquiggle_ %>~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Delegate Sub D2(<%= s_longSymbolName %>1 As Long)
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

            Dim source = Format(sourceTemplate, s_longSymbolName)
            Dim comp = CreateCompilationWithMscorlib(source)
            Dim __longSpace___ As New String(" "c, s_longSymbolName.Length)
            Dim _longSquiggle_ As New String("~"c, s_longSymbolName.Length)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
Class C(Of <%= s_longSymbolName %>, <%= s_longSymbolName %>1)
             <%= __longSpace___ %><%= _longSquiggle_ %>~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
Delegate Sub D(Of <%= s_longSymbolName %>, <%= s_longSymbolName %>1)()
                    <%= __longSpace___ %><%= _longSquiggle_ %>~
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
    Sub M(Of <%= s_longSymbolName %>, <%= s_longSymbolName %>1)()
               <%= __longSpace___ %><%= _longSquiggle_ %>~
</errors>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

            Dim source = Format(sourceTemplate, s_longLocalName)
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40({source}, {}, options:=TestOptions.DebugDll)
            Dim _longSquiggle_ As New String("~"c, s_longLocalName.Length)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC42373: Local name '<%= s_longLocalName %>1' is too long for PDB.  Consider shortening or compiling without /debug.
        Dim <%= s_longLocalName %>1 As Long = 1
            <%= _longSquiggle_ %>~
</errors>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
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

            Dim source = Format(sourceTemplate, s_longLocalName)
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40({source}, {}, options:=TestOptions.DebugDll)
            Dim _longSquiggle_ As New String("~"c, s_longLocalName.Length)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC42373: Local name '<%= s_longLocalName %>1' is too long for PDB.  Consider shortening or compiling without /debug.
        Const <%= s_longLocalName %>1 As Long = 1
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
            Dim padding = GeneratedNames.MakeStateMachineTypeName("A", 1, 0).Length - 1
            Dim longName = s_longSymbolName.Substring(padding)
            Dim longSquiggle As New String("~"c, longName.Length)
            Dim source = Format(sourceTemplate, longName)
            Dim comp = CreateCompilationWithMscorlib45(source)
            comp.AssertNoDiagnostics()
            comp.AssertTheseEmitDiagnostics(<errors>
BC37220: Name 'VB$StateMachine_2_<%= longName %>1' exceeds the maximum length allowed in metadata.
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
                New ResourceDescription(s_longSymbolName, "path2", dataProvider, False), 'fine
                New ResourceDescription("name2", s_longPathName, dataProvider, False), 'fine
                New ResourceDescription(s_longSymbolName & 1, "path3", dataProvider, False), 'name error
                New ResourceDescription("name3", s_longPathName & 2, dataProvider, False), 'path error
                New ResourceDescription(s_longSymbolName & 3, s_longPathName & 4, dataProvider, False) 'name And path errors
            }
            Using assemblyStream As New System.IO.MemoryStream()
                Using pdbStream As New System.IO.MemoryStream()
                    Dim diagnostics = comp.Emit(assemblyStream, pdbStream:=pdbStream, manifestResources:=resources).Diagnostics
                    AssertTheseDiagnostics(diagnostics, <errors>
BC37220: Name '<%= s_longPathName %>2' exceeds the maximum length allowed in metadata.
BC37220: Name '<%= s_longPathName %>4' exceeds the maximum length allowed in metadata.
BC37220: Name '<%= s_longSymbolName %>1' exceeds the maximum length allowed in metadata.
BC37220: Name '<%= s_longSymbolName %>3' exceeds the maximum length allowed in metadata.
</errors>)
                End Using
            End Using
        End Sub

        Private Shared Function Format(sourceTemplate As XCData, ParamArray args As Object()) As String
            Return String.Format(sourceTemplate.Value.Replace(vbCr, vbCrLf), args)
        End Function

        Private Function CreateCompilationWithMscorlib(source As String) As VisualBasicCompilation
            Return CompilationUtils.CreateCompilationWithMscorlib40({source}, {}, TestOptions.ReleaseDll)
        End Function

        Private Function CreateCompilationWithMscorlib45(source As String) As VisualBasicCompilation
            Return VisualBasicCompilation.Create(GetUniqueName(),
                                                 {VisualBasicSyntaxTree.ParseText(source)},
                                                 {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929},
                                                 TestOptions.ReleaseDll)
        End Function
    End Class
End Namespace
