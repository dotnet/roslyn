' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    ''' <summary>
    ''' NYI: PEVerify currently fails for netmodules with error: "The module X was expected to contain an assembly manifest".
    ''' Verification was disabled for net modules for now. Add it back once module support has been added.
    ''' See tests having verify: !outputKind.IsNetModule()
    ''' https://github.com/dotnet/roslyn/issues/23475
    ''' TODO: Add tests for assembly attributes emitted into netmodules which suppress synthesized CompilationRelaxationsAttribute/RuntimeCompatibilityAttribute
    ''' </summary>
    Public Class AttributeTests_Synthesized
        Inherits BasicTestBase

        Public Shared Function OptimizationLevelTheoryData() As IEnumerable(Of Object())
            Dim result = New List(Of Object())
            For Each level As OptimizationLevel In [Enum].GetValues(GetType(OptimizationLevel))
                result.Add(New Object() {level})
            Next
            Return result
        End Function

        Public Shared Function OutputKindTheoryData() As IEnumerable(Of Object())
            Dim result = New List(Of Object())
            For Each kind As OutputKind In [Enum].GetValues(GetType(OutputKind))
                result.Add(New Object() {kind})
            Next
            Return result
        End Function

        Public Shared Function FullMatrixTheoryData() As IEnumerable(Of Object())
            Dim result = New List(Of Object())
            For Each kind As OutputKind In [Enum].GetValues(GetType(OutputKind))
                For Each level As OptimizationLevel In [Enum].GetValues(GetType(OptimizationLevel))
                    result.Add(New Object() {kind, level})
                Next
            Next
            Return result
        End Function

#Region "CompilerGenerated, DebuggerBrowsable, DebuggerDisplay"

        <Fact, WorkItem(546956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546956")>
        Public Sub PrivateImplementationDetails()
            Dim source =
<compilation>
    <file>
Class C
    Dim a As Integer() = {1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3,
                          4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 6, 7, 8, 9}
End Class
</file>
</compilation>

            Dim reference = CreateCompilationWithMscorlib40AndVBRuntime(source).EmitToImageReference()
            Dim comp = VisualBasicCompilation.Create("Name", references:={reference}, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal))

            Dim pid = DirectCast(comp.GlobalNamespace.GetMembers().Single(Function(s) s.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal)), NamedTypeSymbol)
            Dim expectedAttrs = {"CompilerGeneratedAttribute"}
            Dim actualAttrs = GetAttributeNames(pid.GetAttributes())
            AssertEx.SetEqual(expectedAttrs, actualAttrs)
        End Sub

        <Theory>
        <MemberData(NameOf(OptimizationLevelTheoryData))>
        Public Sub BackingFields(optimizationLevel As OptimizationLevel)
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Runtime.InteropServices

Public Class C
    Property P As Integer
    Event E As Action
    WithEvents WE As C
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source, options:=
                                                     New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).
                                                     WithOptimizationLevel(optimizationLevel).
                                                     WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(comp, symbolValidator:=Sub(m As ModuleSymbol)
                                                        Dim c = m.GlobalNamespace.GetTypeMember("C")
                                                        Dim p = c.GetField("_P")
                                                        Dim e = c.GetField("EEvent")
                                                        Dim we = c.GetField("_WE")

                                                        ' Dev11 only emits DebuggerBrowsableAttribute and CompilerGeneratedAttribute for auto-property.
                                                        ' Roslyn emits these attributes for all backing fields.
                                                        Dim expected = If(optimizationLevel = OptimizationLevel.Debug,
                                                                            {"CompilerGeneratedAttribute", "DebuggerBrowsableAttribute"},
                                                                            {"CompilerGeneratedAttribute"})

                                                        Dim attrs = p.GetAttributes()
                                                        AssertEx.SetEqual(expected, GetAttributeNames(attrs))
                                                        If optimizationLevel = OptimizationLevel.Debug Then
                                                            Assert.Equal(DebuggerBrowsableState.Never, GetDebuggerBrowsableState(attrs))
                                                        End If

                                                        attrs = e.GetAttributes()
                                                        AssertEx.SetEqual(expected, GetAttributeNames(attrs))
                                                        If optimizationLevel = OptimizationLevel.Debug Then
                                                            Assert.Equal(DebuggerBrowsableState.Never, GetDebuggerBrowsableState(attrs))
                                                        End If

                                                        attrs = we.GetAttributes()
                                                        AssertEx.SetEqual(expected.Concat("AccessedThroughPropertyAttribute"), GetAttributeNames(attrs))
                                                        If optimizationLevel = OptimizationLevel.Debug Then
                                                            Assert.Equal(DebuggerBrowsableState.Never, GetDebuggerBrowsableState(attrs))
                                                        End If
                                                    End Sub)
        End Sub

        <Theory>
        <MemberData(NameOf(OptimizationLevelTheoryData))>
        <WorkItem(546899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546899")>
        Public Sub Accessors(optimizationLevel As OptimizationLevel)
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Runtime.InteropServices

Public MustInherit Class C
    Property P As Integer
    MustOverride Property Q As Integer

    Event E As Action
    WithEvents WE As C
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source, options:=
                                                     New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).
                                                     WithOptimizationLevel(optimizationLevel).
                                                     WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(comp, symbolValidator:=Sub(m As ModuleSymbol)
                                                        Dim c = m.GlobalNamespace.GetTypeMember("C")
                                                        Dim p = c.GetMember(Of PropertySymbol)("P")
                                                        Dim q = c.GetMember(Of PropertySymbol)("Q")
                                                        Dim e = c.GetMember(Of EventSymbol)("E")
                                                        Dim we = c.GetMember(Of PropertySymbol)("WE")

                                                        ' Unlike Dev11 we don't emit DebuggerNonUserCode since the accessors have no debug info 
                                                        ' and they don't invoke any user code method that could throw an exception whose stack trace
                                                        ' should have the accessor frame hidden.
                                                        Dim expected = {"CompilerGeneratedAttribute"}

                                                        Dim attrs = p.GetMethod.GetAttributes()
                                                        AssertEx.SetEqual(expected, GetAttributeNames(attrs))

                                                        attrs = p.SetMethod.GetAttributes()
                                                        AssertEx.SetEqual(expected, GetAttributeNames(attrs))

                                                        attrs = q.GetMethod.GetAttributes()
                                                        AssertEx.SetEqual(New String() {}, GetAttributeNames(attrs))

                                                        attrs = q.SetMethod.GetAttributes()
                                                        AssertEx.SetEqual(New String() {}, GetAttributeNames(attrs))

                                                        attrs = we.GetMethod.GetAttributes()
                                                        AssertEx.SetEqual(expected, GetAttributeNames(attrs))

                                                        attrs = we.SetMethod.GetAttributes()
                                                        AssertEx.SetEqual(expected, GetAttributeNames(attrs))

                                                        attrs = e.AddMethod.GetAttributes()
                                                        AssertEx.SetEqual(expected, GetAttributeNames(attrs))

                                                        attrs = e.RemoveMethod.GetAttributes()
                                                        AssertEx.SetEqual(expected, GetAttributeNames(attrs))
                                                    End Sub)
        End Sub

        <Theory>
        <MemberData(NameOf(OptimizationLevelTheoryData))>
        <WorkItem(543254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543254")>
        Public Sub Constructors(optimizationLevel As OptimizationLevel)
            Dim source =
<compilation>
    <file>
Imports System
Imports System.Diagnostics

Public Class A
End Class

Public Class B
    Public x As Object = 123
End Class

Public Class C
    Sub New
        MyBase.New()
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source, options:=
                                                     New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).
                                                     WithOptimizationLevel(optimizationLevel).
                                                     WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(comp, symbolValidator:=Sub(m As ModuleSymbol)
                                                        Dim a = m.ContainingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("A")
                                                        Dim b = m.ContainingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("B")
                                                        Dim c = m.ContainingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")

                                                        Dim aAttrs = GetAttributeNames(a.InstanceConstructors.Single().GetAttributes())
                                                        Dim bAttrs = GetAttributeNames(b.InstanceConstructors.Single().GetAttributes())
                                                        Dim cAttrs = GetAttributeNames(c.InstanceConstructors.Single().GetAttributes())

                                                        ' constructors that doesn't contain user code 
                                                        Assert.Empty(aAttrs)

                                                        ' constructors that contain user code 
                                                        Assert.Empty(bAttrs)
                                                        Assert.Empty(cAttrs)
                                                    End Sub)
        End Sub

        <Theory>
        <MemberData(NameOf(OptimizationLevelTheoryData))>
        Public Sub Lambdas(optimizationLevel As OptimizationLevel)
            Dim source =
<compilation>
    <file>
Imports System

Class C
    Sub Goo()
        Dim a = 1, b = 2
        Dim d As Func(Of Integer, Integer, Integer) = Function(x, y) a*x+b*y 
    End Sub
End Class

    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source, options:=
                                                     New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).
                                                     WithOptimizationLevel(optimizationLevel))

            CompileAndVerify(comp, symbolValidator:=Sub(m As ModuleSymbol)
                                                        Dim displayClass = m.ContainingAssembly.GetTypeByMetadataName("C+_Closure$__1-0")

                                                        Dim actual = GetAttributeNames(displayClass.GetAttributes())
                                                        AssertEx.SetEqual({"CompilerGeneratedAttribute"}, actual)

                                                        Dim expected As String()
                                                        For Each member In displayClass.GetMembers()
                                                            actual = GetAttributeNames(member.GetAttributes())

                                                            Select Case member.Name
                                                                Case ".ctor"
                                                                    ' Dev11 emits DebuggerNonUserCodeAttribute, we don't
                                                                    expected = New String() {}

                                                                Case "$VB$Local_a", "$VB$Local_b", "_Lambda$__1"
                                                                    ' Dev11 emits CompilerGenerated attribute on the lambda, 
                                                                    ' Roslyn doesn't since the containing class is already marked by this attribute
                                                                    expected = New String() {}

                                                                Case Else
                                                                    Throw TestExceptionUtilities.UnexpectedValue(member.Name)

                                                            End Select
                                                            AssertEx.SetEqual(expected, actual)
                                                        Next
                                                    End Sub)
        End Sub

        <Theory>
        <MemberData(NameOf(OptimizationLevelTheoryData))>
        Public Sub AnonymousDelegate(optimizationLevel As OptimizationLevel)
            Dim source =
<compilation>
    <file>
Imports System

Class C
    Function Goo() As MultiCastDelegate
        Dim a = 1, b = 2
        Return Function(x As Integer, y As Integer) a*x + b*x
    End Function
End Class   
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source, options:=
                                                     New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).
                                                     WithOptimizationLevel(optimizationLevel).
                                                     WithMetadataImportOptions(MetadataImportOptions.All))

            CompileAndVerify(comp, symbolValidator:=Sub(m As ModuleSymbol)
                                                        Dim anonDelegate = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousDelegate_0`3")

                                                        Dim actual = GetAttributeNames(anonDelegate.GetAttributes())
                                                        AssertEx.SetEqual({"CompilerGeneratedAttribute", "DebuggerDisplayAttribute"}, actual)

                                                        Dim expected As String()
                                                        For Each member In anonDelegate.GetMembers()
                                                            actual = GetAttributeNames(member.GetAttributes())

                                                            Select Case member.Name
                                                                Case ".ctor", "BeginInvoke", "EndInvoke", "Invoke"
                                                                    ' Dev11 emits DebuggerNonUserCode, we don't
                                                                    expected = New String() {}
                                                                Case Else
                                                                    Throw TestExceptionUtilities.UnexpectedValue(member.Name)
                                                            End Select

                                                            AssertEx.SetEqual(expected, actual)
                                                        Next
                                                    End Sub)
        End Sub

        <Theory>
        <MemberData(NameOf(OptimizationLevelTheoryData))>
        Public Sub RelaxationStub1(optimizationLevel As OptimizationLevel)
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Linq

Class C
    Event e As Action(Of Integer)

    Sub Goo() Handles Me.e
        Dim f = Function() As Long
                    Return 9
                End Function

        Dim q = From a In {1, 2, 3, 4}
                Where a < 2
                Select a + 1
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndReferences(source,
                                                                  references:={SystemCoreRef},
                                                                  options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=optimizationLevel))

            ' Dev11 emits DebuggerStepThrough, we emit DebuggerHidden and only in /debug:full mode
            Dim expected = If(optimizationLevel = OptimizationLevel.Debug,
                               "[System.Runtime.CompilerServices.CompilerGeneratedAttribute()] [System.Diagnostics.DebuggerHiddenAttribute()]",
                               "[System.Runtime.CompilerServices.CompilerGeneratedAttribute()]")

            CompileAndVerify(comp, expectedSignatures:=
                {
                    Signature("C", "_Lambda$__R0-1", ".method " + expected + " private specialname instance System.Void _Lambda$__R0-1(System.Int32 a0) cil managed")
                })
        End Sub

        <Theory>
        <MemberData(NameOf(OptimizationLevelTheoryData))>
        Public Sub RelaxationStub2(optimizationLevel As OptimizationLevel)
            Dim source =
<compilation>
    <file><![CDATA[
Imports System

Class C
    Property P() As Func(Of String, Integer) = Function(y As Integer) y.ToString()
End Class
]]>
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source,
                                                                  references:={SystemCoreRef},
                                                                  options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=optimizationLevel))

            ' Dev11 emits DebuggerStepThrough, we emit DebuggerHidden and only in /debug:full mode
            Dim expected = If(optimizationLevel = OptimizationLevel.Debug,
                               "[System.Runtime.CompilerServices.CompilerGeneratedAttribute()] [System.Diagnostics.DebuggerHiddenAttribute()]",
                               "[System.Runtime.CompilerServices.CompilerGeneratedAttribute()]")

            CompileAndVerify(comp)
        End Sub

        <Theory>
        <MemberData(NameOf(OptimizationLevelTheoryData))>
        Public Sub AnonymousTypes(optimizationLevel As OptimizationLevel)
            Dim source =
<compilation>
    <file>
Class C
    Sub Goo()
        Dim x = New With { .X = 1, .Y = 2 }
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40(source,
                                                     options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=optimizationLevel))

            CompileAndVerify(comp, symbolValidator:=
                    Sub(m)
                        Dim anon = m.ContainingAssembly.GetTypeByMetadataName("VB$AnonymousType_0`2")

                        ' VB emits DebuggerDisplay regardless of /debug settings
                        AssertEx.SetEqual({"DebuggerDisplayAttribute", "CompilerGeneratedAttribute"}, GetAttributeNames(anon.GetAttributes()))

                        For Each member In anon.GetMembers()
                            Dim actual = GetAttributeNames(member.GetAttributes())
                            Dim expected As String()

                            Select Case member.Name
                                Case "$X", "$Y"
                                    ' Dev11 doesn't emit this attribute
                                    If optimizationLevel = OptimizationLevel.Debug Then
                                        expected = {"DebuggerBrowsableAttribute"}
                                    Else
                                        expected = New String() {}
                                    End If

                                Case "X", "Y", "get_X", "get_Y", "set_X", "set_Y"
                                    ' Dev11 marks accessors with DebuggerNonUserCodeAttribute
                                    expected = New String() {}

                                Case ".ctor", "ToString"
                                    ' Dev11 marks methods with DebuggerNonUserCodeAttribute
                                    If optimizationLevel = OptimizationLevel.Debug Then
                                        expected = {"DebuggerHiddenAttribute"}
                                    Else
                                        expected = New String() {}
                                    End If

                                Case Else
                                    Throw TestExceptionUtilities.UnexpectedValue(member.Name)
                            End Select

                            AssertEx.SetEqual(expected, actual)
                        Next
                    End Sub)
        End Sub

        <Theory>
        <MemberData(NameOf(OptimizationLevelTheoryData))>
        Public Sub AnonymousTypes_DebuggerDisplay(optimizationLevel As OptimizationLevel)
            Dim source =
<compilation>
    <file>
Public Class C
    Public Sub Goo()
        Dim _1 = New With {.X0 = 1}
        Dim _2 = New With {.X0 = 1, .X1 = 1}
        Dim _3 = New With {.X0 = 1, .X1 = 1, .X2 = 1}
        Dim _4 = New With {.X0 = 1, .X1 = 1, .X2 = 1, .X3 = 1}
        Dim _5 = New With {.X0 = 1, .X1 = 1, .X2 = 1, .X3 = 1, .X4 = 1}
        Dim _6 = New With {.X0 = 1, .X1 = 1, .X2 = 1, .X3 = 1, .X4 = 1, .X5 = 1}
        Dim _7 = New With {.X0 = 1, .X1 = 1, .X2 = 1, .X3 = 1, .X4 = 1, .X5 = 1, .X6 = 1}
        Dim _8 = New With {.X0 = 1, .X1 = 1, .X2 = 1, .X3 = 1, .X4 = 1, .X5 = 1, .X6 = 1, .X7 = 1}
        Dim _10 = New With {.X0 = 1, .X1 = 1, .X2 = 1, .X3 = 1, .X4 = 1, .X5 = 1, .X6 = 1, .X7 = 1, .X8 = 1}
        Dim _11 = New With {.X0 = 1, .X1 = 1, .X2 = 1, .X3 = 1, .X4 = 1, .X5 = 1, .X6 = 1, .X7 = 1, .X8 = 1, .X9 = 1}
        Dim _12 = New With {.X0 = 1, .X1 = 1, .X2 = 1, .X3 = 1, .X4 = 1, .X5 = 1, .X6 = 1, .X7 = 1, .X8 = 1, .X9 = 1, .X10 = 1}
        Dim _13 = New With {.X10 = 1, .X11 = 1, .X12 = 1, .X13 = 1, .X14 = 1, .X15 = 1, .X16 = 1, .X17 = 1, .X20 = 1, .X21 = 1, .X22 = 1, .X23 = 1, .X24 = 1, .X25 = 1, .X26 = 1, .X27 = 1, .X30 = 1, .X31 = 1, .X32 = 1, .X33 = 1, .X34 = 1, .X35 = 1, .X36 = 1, .X37 = 1, .X40 = 1, .X41 = 1, .X42 = 1, .X43 = 1, .X44 = 1, .X45 = 1, .X46 = 1, .X47 = 1, .X50 = 1, .X51 = 1, .X52 = 1, .X53 = 1, .X54 = 1, .X55 = 1, .X56 = 1, .X57 = 1, .X60 = 1, .X61 = 1, .X62 = 1, .X63 = 1, .X64 = 1, .X65 = 1, .X66 = 1, .X67 = 1}
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=optimizationLevel))
            CompileAndVerify(comp, symbolValidator:=
                Sub(m)
                    Dim assembly = m.ContainingAssembly
                    Assert.Equal("X0={X0}", GetDebuggerDisplayString(assembly, 0, 1))
                    Assert.Equal("X0={X0}, X1={X1}", GetDebuggerDisplayString(assembly, 1, 2))
                    Assert.Equal("X0={X0}, X1={X1}, X2={X2}", GetDebuggerDisplayString(assembly, 2, 3))
                    Assert.Equal("X0={X0}, X1={X1}, X2={X2}, X3={X3}", GetDebuggerDisplayString(assembly, 3, 4))
                    Assert.Equal("X0={X0}, X1={X1}, X2={X2}, X3={X3}, ...", GetDebuggerDisplayString(assembly, 4, 5))
                    Assert.Equal("X0={X0}, X1={X1}, X2={X2}, X3={X3}, ...", GetDebuggerDisplayString(assembly, 5, 6))
                    Assert.Equal("X0={X0}, X1={X1}, X2={X2}, X3={X3}, ...", GetDebuggerDisplayString(assembly, 6, 7))
                    Assert.Equal("X0={X0}, X1={X1}, X2={X2}, X3={X3}, ...", GetDebuggerDisplayString(assembly, 7, 8))
                    Assert.Equal("X0={X0}, X1={X1}, X2={X2}, X3={X3}, ...", GetDebuggerDisplayString(assembly, 8, 9))
                    Assert.Equal("X0={X0}, X1={X1}, X2={X2}, X3={X3}, ...", GetDebuggerDisplayString(assembly, 9, 10))
                    Assert.Equal("X0={X0}, X1={X1}, X2={X2}, X3={X3}, ...", GetDebuggerDisplayString(assembly, 10, 11))
                    Assert.Equal("X10={X10}, X11={X11}, X12={X12}, X13={X13}, ...", GetDebuggerDisplayString(assembly, 11, 48))
                End Sub)
        End Sub

        Private Shared Function GetDebuggerDisplayString(assembly As AssemblySymbol, ordinal As Integer, fieldCount As Integer) As String
            Dim anon = assembly.GetTypeByMetadataName("VB$AnonymousType_" & ordinal & "`" & fieldCount)
            Dim dd = anon.GetAttributes().Where(Function(a) a.AttributeClass.Name = "DebuggerDisplayAttribute").Single()
            Return DirectCast(dd.ConstructorArguments.Single().Value, String)
        End Function

        <Fact>
        Public Sub WRN_DebuggerHiddenIgnoredOnProperties()
            Dim source =
<compilation>
    <file><![CDATA[
Imports System
Imports System.Diagnostics

Public MustInherit Class C
    <DebuggerHidden> ' P1
    Property P1 As Integer
        Get
            Return 0
        End Get
        Set
        End Set
    End Property

    <DebuggerHidden>
    Property P2 As Integer
        <DebuggerHidden>
        Get
            Return 0
        End Get
        Set
        End Set
    End Property

    <DebuggerHidden>
    Property P3 As Integer
        Get
            Return 0
        End Get
        <DebuggerHidden>
        Set
        End Set
    End Property

    <DebuggerHidden> ' P4
    ReadOnly Property P4 As Integer
        Get
            Return 0
        End Get
    End Property

    <DebuggerHidden>
    ReadOnly Property P5 As Integer
        <DebuggerHidden>
        Get
            Return 0
        End Get
    End Property

    <DebuggerHidden> ' P6
    WriteOnly Property P6 As Integer
        Set 
        End Set
    End Property

    <DebuggerHidden>
    WriteOnly Property P7 As Integer
        <DebuggerHidden>
        Set
        End Set
    End Property

    <DebuggerHidden> ' AbstractProp
    MustOverride Property AbstractProp As Integer

    <DebuggerHidden> ' AutoProp
    Property AutoProp As Integer

    <DebuggerHidden> ' WE
    WithEvents WE As C   
End Class
]]>
    </file>
</compilation>

            CreateCompilationWithMscorlib40(source).AssertTheseDiagnostics(<![CDATA[
BC40051: System.Diagnostics.DebuggerHiddenAttribute does not affect 'Get' or 'Set' when applied to the Property definition.  Apply the attribute directly to the 'Get' and 'Set' procedures as appropriate.
    <DebuggerHidden> ' P1
     ~~~~~~~~~~~~~~
BC40051: System.Diagnostics.DebuggerHiddenAttribute does not affect 'Get' or 'Set' when applied to the Property definition.  Apply the attribute directly to the 'Get' and 'Set' procedures as appropriate.
    <DebuggerHidden> ' P4
     ~~~~~~~~~~~~~~
BC40051: System.Diagnostics.DebuggerHiddenAttribute does not affect 'Get' or 'Set' when applied to the Property definition.  Apply the attribute directly to the 'Get' and 'Set' procedures as appropriate.
    <DebuggerHidden> ' P6
     ~~~~~~~~~~~~~~
BC40051: System.Diagnostics.DebuggerHiddenAttribute does not affect 'Get' or 'Set' when applied to the Property definition.  Apply the attribute directly to the 'Get' and 'Set' procedures as appropriate.
    <DebuggerHidden> ' AbstractProp
     ~~~~~~~~~~~~~~
BC40051: System.Diagnostics.DebuggerHiddenAttribute does not affect 'Get' or 'Set' when applied to the Property definition.  Apply the attribute directly to the 'Get' and 'Set' procedures as appropriate.
    <DebuggerHidden> ' AutoProp
     ~~~~~~~~~~~~~~
BC30662: Attribute 'DebuggerHiddenAttribute' cannot be applied to 'WE' because the attribute is not valid on this declaration type.
    <DebuggerHidden> ' WE
     ~~~~~~~~~~~~~~
]]>)
        End Sub

#End Region

#Region "CompilationRelaxationsAttribute, RuntimeCompatibilityAttribute"

        Private Sub VerifyCompilationRelaxationsAttribute(attribute As VisualBasicAttributeData, isSynthesized As Boolean)
            Assert.Equal("System.Runtime.CompilerServices.CompilationRelaxationsAttribute", attribute.AttributeClass.ToTestDisplayString())

            Dim expectedArgValue As Integer = If(isSynthesized, CInt(CompilationRelaxations.NoStringInterning), 0)
            Assert.Equal(1, attribute.CommonConstructorArguments.Length)
            attribute.VerifyValue(Of Integer)(0, TypedConstantKind.Primitive, expectedArgValue)

            Assert.Empty(attribute.CommonNamedArguments)
        End Sub

        Private Sub VerifyRuntimeCompatibilityAttribute(attribute As VisualBasicAttributeData, isSynthesized As Boolean)
            Assert.Equal("System.Runtime.CompilerServices.RuntimeCompatibilityAttribute", attribute.AttributeClass.ToTestDisplayString())
            Assert.Empty(attribute.CommonConstructorArguments)

            If isSynthesized Then
                Assert.Equal(1, attribute.CommonNamedArguments.Length)
                attribute.VerifyNamedArgumentValue(Of Boolean)(0, "WrapNonExceptionThrows", TypedConstantKind.Primitive, True)
            Else
                Assert.Empty(attribute.CommonNamedArguments)
            End If
        End Sub

        <Theory>
        <MemberData(NameOf(OutputKindTheoryData))>
        Public Sub TestSynthesizedAssemblyAttributes_01(outputKind As OutputKind)
            ' Verify Synthesized CompilationRelaxationsAttribute
            ' Verify Synthesized RuntimeCompatibilityAttribute

            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Public Class Test
	Public Shared Sub Main()
	End Sub
End Class
]]>
                             </file>
                         </compilation>

            CompileAndVerify(
                CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(outputKind, optimizationLevel:=OptimizationLevel.Release)),
                verify:=If(outputKind.IsNetModule(), Verification.Skipped, Verification.Passes),
                symbolValidator:=Sub(m As ModuleSymbol)
                                     Dim attributes = m.ContainingAssembly.GetAttributes()

                                     If outputKind <> OutputKind.NetModule Then
                                         ' Verify synthesized CompilationRelaxationsAttribute and RuntimeCompatibilityAttribute
                                         Assert.Equal(3, attributes.Length)
                                         VerifyCompilationRelaxationsAttribute(attributes(0), isSynthesized:=True)
                                         VerifyRuntimeCompatibilityAttribute(attributes(1), isSynthesized:=True)
                                         VerifyDebuggableAttribute(attributes(2), DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)
                                     Else
                                         Assert.Empty(attributes)
                                     End If
                                 End Sub)
        End Sub

        <Theory>
        <MemberData(NameOf(OutputKindTheoryData))>
        Public Sub TestSynthesizedAssemblyAttributes_02(outputKind As OutputKind)
            ' Verify Applied CompilationRelaxationsAttribute
            ' Verify Synthesized RuntimeCompatibilityAttribute

            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.CompilerServices

<Assembly: CompilationRelaxationsAttribute(0)>

Public Class Test
	Public Shared Sub Main()
	End Sub
End Class
]]>
                             </file>
                         </compilation>

            CompileAndVerify(
                CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(outputKind, optimizationLevel:=OptimizationLevel.Release)),
                verify:=If(outputKind.IsNetModule(), Verification.Skipped, Verification.Passes),
                sourceSymbolValidator:=Sub(m As ModuleSymbol)
                                           Dim attributes = m.ContainingAssembly.GetAttributes()
                                           VerifyCompilationRelaxationsAttribute(attributes.Single(), isSynthesized:=False)
                                       End Sub,
                symbolValidator:=Sub(m As ModuleSymbol)
                                     ' Verify synthesized RuntimeCompatibilityAttribute
                                     Dim attributes = m.ContainingAssembly.GetAttributes()
                                     If outputKind.IsNetModule() Then
                                         Assert.Empty(attributes)
                                     Else
                                         Assert.Equal(3, attributes.Length)
                                         VerifyRuntimeCompatibilityAttribute(attributes(0), isSynthesized:=True)
                                         VerifyDebuggableAttribute(attributes(1), DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)
                                         VerifyCompilationRelaxationsAttribute(attributes(2), isSynthesized:=False)
                                     End If
                                 End Sub)
        End Sub

        <Theory>
        <MemberData(NameOf(OutputKindTheoryData))>
        Public Sub TestSynthesizedAssemblyAttributes_03(outputKind As OutputKind)
            ' Verify Synthesized CompilationRelaxationsAttribute
            ' Verify Applied RuntimeCompatibilityAttribute

            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.CompilerServices

<Assembly: RuntimeCompatibilityAttribute>

Public Class Test
	Public Shared Sub Main()
	End Sub
End Class
]]>
                             </file>
                         </compilation>

            CompileAndVerify(
                CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(outputKind, optimizationLevel:=OptimizationLevel.Release)),
                verify:=If(outputKind.IsNetModule(), Verification.Skipped, Verification.Passes),
                sourceSymbolValidator:=Sub(m As ModuleSymbol)
                                           Dim attributes = m.ContainingAssembly.GetAttributes()
                                           VerifyRuntimeCompatibilityAttribute(attributes.Single(), isSynthesized:=False)
                                       End Sub,
                symbolValidator:=Sub(m As ModuleSymbol)
                                     ' Verify synthesized RuntimeCompatibilityAttribute
                                     Dim attributes = m.ContainingAssembly.GetAttributes()
                                     If outputKind.IsNetModule() Then
                                         Assert.Empty(attributes)
                                     Else
                                         Assert.Equal(3, attributes.Length)
                                         VerifyCompilationRelaxationsAttribute(attributes(0), isSynthesized:=True)
                                         VerifyDebuggableAttribute(attributes(1), DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)
                                         VerifyRuntimeCompatibilityAttribute(attributes(2), isSynthesized:=False)
                                     End If
                                 End Sub)
        End Sub

        <Theory>
        <MemberData(NameOf(OutputKindTheoryData))>
        Public Sub TestSynthesizedAssemblyAttributes_04(outputKind As OutputKind)
            ' Verify Applied CompilationRelaxationsAttribute
            ' Verify Applied RuntimeCompatibilityAttribute

            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.CompilerServices

<Assembly: CompilationRelaxationsAttribute(0)>
<Assembly: RuntimeCompatibilityAttribute>

Public Class Test
	Public Shared Sub Main()
	End Sub
End Class
]]>
                             </file>
                         </compilation>

            CompileAndVerify(
                CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(outputKind, optimizationLevel:=OptimizationLevel.Release)),
                verify:=If(outputKind.IsNetModule(), Verification.Skipped, Verification.Passes),
                sourceSymbolValidator:=Sub(m As ModuleSymbol)
                                           ' Verify applied CompilationRelaxationsAttribute and RuntimeCompatibilityAttribute
                                           Dim attributes = m.ContainingAssembly.GetAttributes()
                                           Assert.Equal(2, attributes.Length)
                                           VerifyCompilationRelaxationsAttribute(attributes(0), isSynthesized:=False)
                                           VerifyRuntimeCompatibilityAttribute(attributes(1), isSynthesized:=False)
                                       End Sub,
                symbolValidator:=Sub(m As ModuleSymbol)
                                     ' Verify no synthesized attributes
                                     Dim attributes = m.ContainingAssembly.GetAttributes()
                                     If outputKind.IsNetModule() Then
                                         Assert.Empty(attributes)
                                     Else
                                         Assert.Equal(3, attributes.Length)
                                         VerifyDebuggableAttribute(attributes(0), DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)
                                         VerifyCompilationRelaxationsAttribute(attributes(1), isSynthesized:=False)
                                         VerifyRuntimeCompatibilityAttribute(attributes(2), isSynthesized:=False)
                                     End If
                                 End Sub)
        End Sub

        <Fact>
        Public Sub RuntimeCompatibilityAttributeCannotBeAppliedToModule()
            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.CompilerServices

<Module: CompilationRelaxationsAttribute(0)>
<Module: RuntimeCompatibilityAttribute>

Public Class Test
	Public Shared Sub Main()
	End Sub
End Class
]]>
                             </file>
                         </compilation>

            CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseDll).VerifyDiagnostics(
                 Diagnostic(ERRID.ERR_InvalidModuleAttribute1, "RuntimeCompatibilityAttribute").WithArguments("RuntimeCompatibilityAttribute").WithLocation(4, 10))
        End Sub

        <Theory>
        <MemberData(NameOf(OutputKindTheoryData))>
        Public Sub TestSynthesizedAssemblyAttributes_05(outputKind As OutputKind)
            ' Verify module attributes don't suppress synthesized assembly attributes:

            ' Synthesized CompilationRelaxationsAttribute
            ' Synthesized RuntimeCompatibilityAttribute

            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.CompilerServices

<Module: CompilationRelaxationsAttribute(0)>

Public Class Test
	Public Shared Sub Main()
	End Sub
End Class
]]>
                             </file>
                         </compilation>

            CompileAndVerify(
                CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(outputKind, optimizationLevel:=OptimizationLevel.Release)),
                verify:=If(outputKind.IsNetModule(), Verification.Skipped, Verification.Passes),
                sourceSymbolValidator:=Sub(m As ModuleSymbol)
                                           ' Verify no applied assembly attributes
                                           Assert.Empty(m.ContainingAssembly.GetAttributes())

                                           ' Verify applied module attributes
                                           Dim moduleAttributes = m.GetAttributes()
                                           VerifyCompilationRelaxationsAttribute(moduleAttributes.Single(), isSynthesized:=False)
                                       End Sub,
                symbolValidator:=Sub(m As ModuleSymbol)
                                     ' Verify applied module attributes
                                     Dim moduleAttributes = m.GetAttributes()
                                     VerifyCompilationRelaxationsAttribute(moduleAttributes.Single(), isSynthesized:=False)

                                     ' Verify synthesized assembly attributes
                                     Dim assemblyAttributes = m.ContainingAssembly.GetAttributes()
                                     If outputKind.IsNetModule() Then
                                         Assert.Empty(assemblyAttributes)
                                     Else
                                         Assert.Equal(3, assemblyAttributes.Length)
                                         VerifyCompilationRelaxationsAttribute(assemblyAttributes(0), isSynthesized:=True)
                                         VerifyRuntimeCompatibilityAttribute(assemblyAttributes(1), isSynthesized:=True)
                                         VerifyDebuggableAttribute(assemblyAttributes(2), DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)
                                     End If
                                 End Sub)
        End Sub

        <Theory>
        <MemberData(NameOf(OutputKindTheoryData))>
        Public Sub TestSynthesizedAssemblyAttributes_06(outputKind As OutputKind)
            ' Verify missing well-known attribute members generate diagnostics and suppress synthesizing CompilationRelaxationsAttribute and RuntimeCompatibilityAttribute.

            Dim source = <compilation>
                             <file name="a.vb">
                                 <![CDATA[
Imports System.Runtime.CompilerServices

Namespace System.Runtime.CompilerServices
	Public NotInheritable Class CompilationRelaxationsAttribute
		Inherits System.Attribute
	End Class

	Public NotInheritable Class RuntimeCompatibilityAttribute
		Inherits System.Attribute
		Public Sub New(dummy As Integer)
		End Sub
	End Class
End Namespace

Public Class Test
	Public Shared Sub Main()
	End Sub
End Class
]]>
                             </file>
                         </compilation>

            Dim compilation = CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(outputKind))

            If outputKind <> OutputKind.NetModule Then
                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC35000: Requested operation is not available because the runtime library function 'System.Runtime.CompilerServices.CompilationRelaxationsAttribute..ctor' is not defined.
BC35000: Requested operation is not available because the runtime library function 'System.Runtime.CompilerServices.RuntimeCompatibilityAttribute..ctor' is not defined.
BC35000: Requested operation is not available because the runtime library function 'System.Runtime.CompilerServices.RuntimeCompatibilityAttribute.WrapNonExceptionThrows' is not defined.
</expected>)
            Else

                CompileAndVerify(
                    CreateCompilationWithMscorlib40(source, options:=New VisualBasicCompilationOptions(outputKind, optimizationLevel:=OptimizationLevel.Release)),
                    verify:=If(outputKind.IsNetModule(), Verification.Skipped, Verification.Passes),
                    symbolValidator:=Sub(m As ModuleSymbol)
                                         ' Verify no synthesized assembly 
                                         Dim attributes = m.ContainingAssembly.GetAttributes()
                                         Assert.Empty(attributes)
                                     End Sub)
            End If
        End Sub

#End Region

#Region "DebuggableAttribute"
        Private Sub VerifyDebuggableAttribute(attribute As VisualBasicAttributeData, expectedDebuggingMode As DebuggableAttribute.DebuggingModes)
            Assert.Equal("System.Diagnostics.DebuggableAttribute", attribute.AttributeClass.ToTestDisplayString())
            Assert.Equal(1, attribute.ConstructorArguments.Count)
            attribute.VerifyValue(0, TypedConstantKind.Enum, CInt(expectedDebuggingMode))

            Assert.Empty(attribute.NamedArguments)
        End Sub

        Private Sub VerifySynthesizedDebuggableAttribute(attribute As VisualBasicAttributeData, optimizations As OptimizationLevel)
            Dim expectedDebuggingMode = DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints

            If optimizations = OptimizationLevel.Debug Then
                expectedDebuggingMode = expectedDebuggingMode Or
                                        DebuggableAttribute.DebuggingModes.Default Or
                                        DebuggableAttribute.DebuggingModes.DisableOptimizations Or
                                        DebuggableAttribute.DebuggingModes.EnableEditAndContinue
            End If

            VerifyDebuggableAttribute(attribute, expectedDebuggingMode)
        End Sub

        <Theory>
        <MemberData(NameOf(FullMatrixTheoryData))>
        Public Sub TestDebuggableAttribute_01(outputKind As OutputKind, optimizationLevel As OptimizationLevel)
            ' Verify Synthesized DebuggableAttribute

            Dim source =
            <![CDATA[
Public Class Test
	Public Shared Sub Main()
	End Sub
End Class
]]>

            Dim validator =
                Sub(m As ModuleSymbol)
                    Dim attributes = m.ContainingAssembly.GetAttributes()

                    If Not outputKind.IsNetModule() Then
                        ' Verify synthesized DebuggableAttribute based on compilation options.

                        Assert.Equal(3, attributes.Length)
                        VerifyCompilationRelaxationsAttribute(attributes(0), isSynthesized:=True)
                        VerifyRuntimeCompatibilityAttribute(attributes(1), isSynthesized:=True)
                        VerifySynthesizedDebuggableAttribute(attributes(2), optimizationLevel)
                    Else
                        Assert.Empty(attributes)
                    End If
                End Sub

            Dim compOptions = New VisualBasicCompilationOptions(outputKind, optimizationLevel:=optimizationLevel, moduleName:="comp")
            Dim comp = VisualBasicCompilation.Create("comp", {Parse(source)}, {MscorlibRef}, compOptions)
            CompileAndVerify(comp, verify:=If(outputKind <> OutputKind.NetModule, Verification.Passes, Verification.Skipped), symbolValidator:=validator)
        End Sub

        <Theory>
        <MemberData(NameOf(FullMatrixTheoryData))>
        Public Sub TestDebuggableAttribute_02(outputKind As OutputKind, optimizationLevel As OptimizationLevel)
            ' Verify applied assembly DebuggableAttribute suppresses synthesized DebuggableAttribute

            Dim source =
            <![CDATA[
Imports System.Diagnostics

<Assembly: DebuggableAttribute(DebuggableAttribute.DebuggingModes.Default)>

Public Class Test
	Public Shared Sub Main()
	End Sub
End Class
]]>

            Dim validator =
                Sub(m As ModuleSymbol)
                    Dim attributes = m.ContainingAssembly.GetAttributes()

                    If Not outputKind.IsNetModule() Then
                        ' Verify no synthesized DebuggableAttribute.

                        Assert.Equal(3, attributes.Length)
                        VerifyCompilationRelaxationsAttribute(attributes(0), isSynthesized:=True)
                        VerifyRuntimeCompatibilityAttribute(attributes(1), isSynthesized:=True)
                        VerifyDebuggableAttribute(attributes(2), DebuggableAttribute.DebuggingModes.Default)
                    Else
                        Assert.Empty(attributes)
                    End If
                End Sub

            Dim compOptions = New VisualBasicCompilationOptions(outputKind, optimizationLevel:=optimizationLevel, moduleName:="comp")
            Dim comp = VisualBasicCompilation.Create("comp", {Parse(source)}, {MscorlibRef}, compOptions)
            CompileAndVerify(comp, verify:=If(outputKind <> OutputKind.NetModule, Verification.Passes, Verification.Skipped), symbolValidator:=validator)
        End Sub

        <Theory>
        <MemberData(NameOf(FullMatrixTheoryData))>
        Public Sub TestDebuggableAttribute_03(outputKind As OutputKind, optimizationLevel As OptimizationLevel)
            ' Verify applied module DebuggableAttribute suppresses synthesized DebuggableAttribute

            Dim source =
            <![CDATA[
Imports System.Diagnostics

<Module: DebuggableAttribute(DebuggableAttribute.DebuggingModes.Default)>

Public Class Test
	Public Shared Sub Main()
	End Sub
End Class
]]>

            Dim validator =
                Sub(m As ModuleSymbol)
                    Dim attributes = m.ContainingAssembly.GetAttributes()

                    If Not outputKind.IsNetModule() Then
                        ' Verify no synthesized DebuggableAttribute.

                        Assert.Equal(2, attributes.Length)
                        VerifyCompilationRelaxationsAttribute(attributes(0), isSynthesized:=True)
                        VerifyRuntimeCompatibilityAttribute(attributes(1), isSynthesized:=True)
                    Else
                        Assert.Empty(attributes)
                    End If
                End Sub

            Dim compOptions = New VisualBasicCompilationOptions(outputKind, optimizationLevel:=optimizationLevel, moduleName:="comp")
            Dim comp = VisualBasicCompilation.Create("comp", {Parse(source)}, {MscorlibRef}, compOptions)
            CompileAndVerify(comp, verify:=If(outputKind <> OutputKind.NetModule, Verification.Passes, Verification.Skipped), symbolValidator:=validator)
        End Sub

        <Theory>
        <MemberData(NameOf(FullMatrixTheoryData))>
        Public Sub TestDebuggableAttribute_04(outputKind As OutputKind, optimizationLevel As OptimizationLevel)
            ' Applied <Module: DebuggableAttribute()> and <Assembly: DebuggableAttribute()>
            ' Verify no synthesized assembly DebuggableAttribute.

            Dim source =
            <![CDATA[
Imports System.Diagnostics

<Module: DebuggableAttribute(DebuggableAttribute.DebuggingModes.Default)>
<Assembly: DebuggableAttribute(DebuggableAttribute.DebuggingModes.None)>

Public Class Test
    Public Shared Sub Main()
    End Sub
End Class
        ]]>

            Dim validator =
                Sub(m As ModuleSymbol)
                    Dim assemblyAttributes = m.ContainingAssembly.GetAttributes()

                    If Not outputKind.IsNetModule() Then
                        ' Verify no synthesized DebuggableAttribute.

                        Assert.Equal(3, assemblyAttributes.Length)
                        VerifyCompilationRelaxationsAttribute(assemblyAttributes(0), isSynthesized:=True)
                        VerifyRuntimeCompatibilityAttribute(assemblyAttributes(1), isSynthesized:=True)
                        VerifyDebuggableAttribute(assemblyAttributes(2), DebuggableAttribute.DebuggingModes.None)
                    Else
                        Assert.Empty(assemblyAttributes)
                    End If

                    ' Verify applied module Debuggable attribute
                    Dim moduleAttributes = m.GetAttributes()
                    VerifyDebuggableAttribute(moduleAttributes.Single(), DebuggableAttribute.DebuggingModes.Default)
                End Sub

            Dim compOptions = New VisualBasicCompilationOptions(outputKind, optimizationLevel:=optimizationLevel, moduleName:="comp")
            Dim comp = VisualBasicCompilation.Create("comp", {Parse(source)}, {MscorlibRef}, compOptions)
            CompileAndVerify(comp, verify:=If(outputKind <> OutputKind.NetModule, Verification.Passes, Verification.Skipped), symbolValidator:=validator)
        End Sub

        <Theory>
        <MemberData(NameOf(FullMatrixTheoryData))>
        Public Sub TestDebuggableAttribute_MissingWellKnownTypeOrMember_01(outputKind As OutputKind, optimizationLevel As OptimizationLevel)
            ' Missing Well-known type DebuggableAttribute generates no diagnostics and
            ' silently suppresses synthesized DebuggableAttribute.

            Dim source =
            <![CDATA[
Public Class Test
    Public Shared Sub Main()
    End Sub
End Class
        ]]>

            Dim compOptions = New VisualBasicCompilationOptions(outputKind, optimizationLevel:=optimizationLevel, moduleName:="comp")
            Dim comp = VisualBasicCompilation.Create("comp", {Parse(source)}, options:=compOptions)

            CompilationUtils.AssertTheseDiagnostics(comp,
<expected>
BC30002: Type 'System.Void' is not defined.
Public Class Test
~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'comp' failed.
Public Class Test
             ~~~~
BC30002: Type 'System.Void' is not defined.
    Public Shared Sub Main()
    ~~~~~~~~~~~~~~~~~~~~~~~~~
        </expected>)
        End Sub

        <Theory>
        <MemberData(NameOf(FullMatrixTheoryData))>
        Public Sub TestDebuggableAttribute_MissingWellKnownTypeOrMember_02(outputKind As OutputKind, optimizationLevel As OptimizationLevel)
            ' Missing Well-known type DebuggableAttribute.DebuggingModes generates no diagnostics and
            ' silently suppresses synthesized DebuggableAttribute.

            Dim source =
            <![CDATA[
        Imports System.Diagnostics

        Namespace System.Diagnostics
        	Public NotInheritable Class DebuggableAttribute
        		Inherits Attribute
        		Public Sub New(isJITTrackingEnabled As Boolean, isJITOptimizerDisabled As Boolean)
        		End Sub
        	End Class
        End Namespace

        Public Class Test
        	Public Shared Sub Main()
        	End Sub
        End Class
        ]]>

            Dim validator =
                Sub(m As ModuleSymbol)
                    Dim attributes = m.ContainingAssembly.GetAttributes()

                    If Not outputKind.IsNetModule() Then
                        ' Verify no synthesized DebuggableAttribute.

                        Assert.Equal(2, attributes.Length)
                        VerifyCompilationRelaxationsAttribute(attributes(0), isSynthesized:=True)
                        VerifyRuntimeCompatibilityAttribute(attributes(1), isSynthesized:=True)
                    Else
                        Assert.Empty(attributes)
                    End If
                End Sub

            Dim compOptions = New VisualBasicCompilationOptions(outputKind, optimizationLevel:=optimizationLevel, moduleName:="comp")
            Dim comp = VisualBasicCompilation.Create("comp", {Parse(source)}, {MscorlibRef}, compOptions)
            CompileAndVerify(comp, verify:=If(outputKind <> OutputKind.NetModule, Verification.Passes, Verification.Skipped), symbolValidator:=validator)
        End Sub

        <Theory>
        <MemberData(NameOf(FullMatrixTheoryData))>
        Public Sub TestDebuggableAttribute_MissingWellKnownTypeOrMember_03(outputKind As OutputKind, optimizationLevel As OptimizationLevel)
            ' Inaccessible Well-known type DebuggableAttribute.DebuggingModes generates no diagnostics and
            ' silently suppresses synthesized DebuggableAttribute.

            Dim source =
            <![CDATA[
        Imports System.Diagnostics

        Namespace System.Diagnostics
        	Public NotInheritable Class DebuggableAttribute
        		Inherits Attribute
        		Public Sub New(isJITTrackingEnabled As Boolean, isJITOptimizerDisabled As Boolean)
        		End Sub

        		Private Enum DebuggingModes
        			None = 0
        			[Default] = 1
        			IgnoreSymbolStoreSequencePoints = 2
        			EnableEditAndContinue = 4
        			DisableOptimizations = 256
        		End Enum
        	End Class
        End Namespace

        Public Class Test
        	Public Shared Sub Main()
        	End Sub
        End Class
        ]]>

            Dim validator =
                Sub(m As ModuleSymbol)
                    Dim attributes = m.ContainingAssembly.GetAttributes()

                    If Not outputKind.IsNetModule() Then
                        ' Verify no synthesized DebuggableAttribute.

                        Assert.Equal(2, attributes.Length)
                        VerifyCompilationRelaxationsAttribute(attributes(0), isSynthesized:=True)
                        VerifyRuntimeCompatibilityAttribute(attributes(1), isSynthesized:=True)
                    Else
                        Assert.Empty(attributes)
                    End If
                End Sub

            Dim compOptions = New VisualBasicCompilationOptions(outputKind, optimizationLevel:=optimizationLevel, moduleName:="comp")
            Dim comp = VisualBasicCompilation.Create("comp", {Parse(source)}, {MscorlibRef}, compOptions)
            CompileAndVerify(comp, verify:=If(outputKind <> OutputKind.NetModule, Verification.Passes, Verification.Skipped), symbolValidator:=validator)
        End Sub

        <Theory>
        <MemberData(NameOf(FullMatrixTheoryData))>
        Public Sub TestDebuggableAttribute_MissingWellKnownTypeOrMember_04(outputKind As OutputKind, optimizationLevel As OptimizationLevel)
            ' Struct Well-known type DebuggableAttribute.DebuggingModes (instead of enum) generates no diagnostics and
            ' silently suppresses synthesized DebuggableAttribute.

            Dim source =
            <![CDATA[
        Imports System.Diagnostics

        Namespace System.Diagnostics
        	Public NotInheritable Class DebuggableAttribute
        		Inherits Attribute
        		Public Sub New(isJITTrackingEnabled As Boolean, isJITOptimizerDisabled As Boolean)
        		End Sub

        		Public Structure DebuggingModes
        		End Structure
        	End Class
        End Namespace

        Public Class Test
        	Public Shared Sub Main()
        	End Sub
        End Class
        ]]>

            Dim validator =
                Sub(m As ModuleSymbol)
                    Dim attributes = m.ContainingAssembly.GetAttributes()

                    If Not outputKind.IsNetModule() Then
                        ' Verify no synthesized DebuggableAttribute.

                        Assert.Equal(2, attributes.Length)
                        VerifyCompilationRelaxationsAttribute(attributes(0), isSynthesized:=True)
                        VerifyRuntimeCompatibilityAttribute(attributes(1), isSynthesized:=True)
                    Else
                        Assert.Empty(attributes)
                    End If
                End Sub

            Dim compOptions = New VisualBasicCompilationOptions(outputKind, optimizationLevel:=optimizationLevel, moduleName:="comp")
            Dim comp = VisualBasicCompilation.Create("comp", {Parse(source)}, {MscorlibRef}, compOptions)
            CompileAndVerify(comp, verify:=If(outputKind <> OutputKind.NetModule, Verification.Passes, Verification.Skipped), symbolValidator:=validator)
        End Sub

        <Theory>
        <MemberData(NameOf(FullMatrixTheoryData))>
        Public Sub TestDebuggableAttribute_MissingWellKnownTypeOrMember_05(outputKind As OutputKind, optimizationLevel As OptimizationLevel)
            ' Missing DebuggableAttribute constructor generates no diagnostics and
            ' silently suppresses synthesized DebuggableAttribute.

            Dim source =
            <![CDATA[
        Imports System.Diagnostics

        Namespace System.Diagnostics
        	Public NotInheritable Class DebuggableAttribute
        		Inherits Attribute
        		Public Enum DebuggingModes
        			None = 0
        			[Default] = 1
        			IgnoreSymbolStoreSequencePoints = 2
        			EnableEditAndContinue = 4
        			DisableOptimizations = 256
        		End Enum
        	End Class
        End Namespace

        Public Class Test
        	Public Shared Sub Main()
        	End Sub
        End Class
        ]]>

            Dim validator =
                Sub(m As ModuleSymbol)
                    Dim attributes = m.ContainingAssembly.GetAttributes()

                    If Not outputKind.IsNetModule() Then
                        ' Verify no synthesized DebuggableAttribute.

                        Assert.Equal(2, attributes.Length)
                        VerifyCompilationRelaxationsAttribute(attributes(0), isSynthesized:=True)
                        VerifyRuntimeCompatibilityAttribute(attributes(1), isSynthesized:=True)
                    Else
                        Assert.Empty(attributes)
                    End If
                End Sub

            Dim compOptions = New VisualBasicCompilationOptions(outputKind, optimizationLevel:=optimizationLevel, moduleName:="comp")
            Dim comp = VisualBasicCompilation.Create("comp", {Parse(source)}, {MscorlibRef}, compOptions)
            CompileAndVerify(comp, verify:=If(outputKind <> OutputKind.NetModule, Verification.Passes, Verification.Skipped), symbolValidator:=validator)
        End Sub

        Private Shared Function GetDebuggerBrowsableState(attributes As ImmutableArray(Of VisualBasicAttributeData)) As DebuggerBrowsableState
            Return DirectCast(attributes.Single(Function(a) a.AttributeClass.Name = "DebuggerBrowsableAttribute").ConstructorArguments(0).Value(), DebuggerBrowsableState)
        End Function
#End Region

#Region "AsyncStateMachineAttribute"

        <Fact>
        Public Sub AsyncStateMachineAttribute_Method()
            Dim source =
<compilation>
    <file name="a.vb">
        Imports System.Threading.Tasks

        Class Test
            Async Sub F()
                Await Task.Delay(0)
            End Sub
        End Class
            </file>
</compilation>
            Dim reference = CreateCompilationWithMscorlib45AndVBRuntime(source).EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(<compilation/>, {reference}, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Dim stateMachine = comp.GetMember(Of NamedTypeSymbol)("Test.VB$StateMachine_1_F")
            Dim asyncMethod = comp.GetMember(Of MethodSymbol)("Test.F")

            Dim asyncMethodAttributes = asyncMethod.GetAttributes()
            AssertEx.SetEqual({"AsyncStateMachineAttribute"}, GetAttributeNames(asyncMethodAttributes))

            Dim attributeArg = DirectCast(asyncMethodAttributes.Single().ConstructorArguments.Single().Value, NamedTypeSymbol)
            Assert.Equal(attributeArg, stateMachine)
        End Sub

        <Fact>
        Public Sub AsyncStateMachineAttribute_Method_Debug()
            Dim source =
<compilation>
    <file name="a.vb">
        Imports System.Threading.Tasks

        Class Test
            Async Sub F()
                Await Task.Delay(0)
            End Sub
        End Class
            </file>
</compilation>
            Dim reference = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.DebugDll).EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(<compilation/>, {reference}, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Dim stateMachine = comp.GetMember(Of NamedTypeSymbol)("Test.VB$StateMachine_1_F")
            Dim asyncMethod = comp.GetMember(Of MethodSymbol)("Test.F")

            Dim asyncMethodAttributes = asyncMethod.GetAttributes()
            AssertEx.SetEqual({"AsyncStateMachineAttribute", "DebuggerStepThroughAttribute"}, GetAttributeNames(asyncMethodAttributes))

            Dim attributeArg = DirectCast(asyncMethodAttributes.First().ConstructorArguments.Single().Value, NamedTypeSymbol)
            Assert.Equal(attributeArg, stateMachine)
        End Sub

        <Fact>
        Public Sub AsyncStateMachineAttribute_Lambda()
            Dim source =
<compilation>
    <file name="a.vb">
        Imports System
        Imports System.Threading.Tasks

        Class Test
            Sub F()
                Dim f As Action = Async Sub()
                                      Await Task.Delay(0)
                                  End Sub
            End Sub
        End Class
            </file>
</compilation>
            Dim reference = CreateCompilationWithMscorlib45AndVBRuntime(source).EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(<compilation/>, {reference}, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Dim stateMachine = comp.GetMember(Of NamedTypeSymbol)("Test._Closure$__.VB$StateMachine___Lambda$__1-0")
            Dim asyncMethod = comp.GetMember(Of MethodSymbol)("Test._Closure$__._Lambda$__1-0")

            Dim asyncMethodAttributes = asyncMethod.GetAttributes()
            AssertEx.SetEqual({"AsyncStateMachineAttribute"}, GetAttributeNames(asyncMethodAttributes))

            Dim attributeArg = DirectCast(asyncMethodAttributes.Single().ConstructorArguments.First().Value, NamedTypeSymbol)
            Assert.Equal(attributeArg, stateMachine)
        End Sub

        <Fact>
        Public Sub AsyncStateMachineAttribute_Lambda_Debug()
            Dim source =
<compilation>
    <file name="a.vb">
        Imports System
        Imports System.Threading.Tasks

        Class Test
            Sub F()
                Dim f As Action = Async Sub()
                                      Await Task.Delay(0)
                                  End Sub
            End Sub
        End Class
            </file>
</compilation>
            Dim reference = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.DebugDll).EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(<compilation/>, {reference}, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Dim stateMachine = comp.GetMember(Of NamedTypeSymbol)("Test._Closure$__.VB$StateMachine___Lambda$__1-0")
            Dim asyncMethod = comp.GetMember(Of MethodSymbol)("Test._Closure$__._Lambda$__1-0")

            Dim asyncMethodAttributes = asyncMethod.GetAttributes()
            AssertEx.SetEqual({"AsyncStateMachineAttribute", "DebuggerStepThroughAttribute"}, GetAttributeNames(asyncMethodAttributes))

            Dim attributeArg = DirectCast(asyncMethodAttributes.First().ConstructorArguments.First().Value, NamedTypeSymbol)
            Assert.Equal(attributeArg, stateMachine)
        End Sub

        <Fact>
        Public Sub AsyncStateMachineAttribute_GenericStateMachineClass()
            Dim source =
<compilation>
    <file name="a.vb">
        Imports System.Threading.Tasks

        Class Test(Of T)
            Async Sub F(Of U As Test(Of Integer))(arg As U) 
                Await Task.Delay(0)
            End Sub
        End Class
            </file>
</compilation>

            Dim reference = CreateCompilationWithMscorlib45AndVBRuntime(source).EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(<compilation/>, {reference}, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Dim stateMachine = comp.GetMember(Of NamedTypeSymbol)("Test.VB$StateMachine_1_F")
            Dim asyncMethod = comp.GetMember(Of MethodSymbol)("Test.F")

            Dim asyncMethodAttributes = asyncMethod.GetAttributes()
            AssertEx.SetEqual({"AsyncStateMachineAttribute"}, GetAttributeNames(asyncMethodAttributes))

            Dim attributeStateMachineClass = DirectCast(asyncMethodAttributes.Single().ConstructorArguments.Single().Value, NamedTypeSymbol)
            Assert.Equal(attributeStateMachineClass, stateMachine.ConstructUnboundGenericType())
        End Sub

        <Fact>
        Public Sub AsyncStateMachineAttribute_MetadataOnly()
            Dim source =
<compilation>
    <file name="a.vb">
        Imports System.Threading.Tasks

        Public Class Test
            Public Async Sub F()
                Await Task.Delay(0)
            End Sub
        End Class
            </file>
</compilation>

            Dim reference = CreateCompilationWithMscorlib45AndVBRuntime(source).EmitToImageReference(New EmitOptions(metadataOnly:=True))
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(<compilation/>, {reference}, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Assert.Empty(comp.GetMember(Of NamedTypeSymbol)("Test").GetMembers("VB$StateMachine_0_F"))
            Dim asyncMethod = comp.GetMember(Of MethodSymbol)("Test.F")

            Dim asyncMethodAttributes = asyncMethod.GetAttributes()
            Assert.Empty(GetAttributeNames(asyncMethodAttributes))
        End Sub

        <Fact>
        Public Sub AsyncStateMachineAttribute_MetadataOnly_Debug()
            Dim source =
<compilation>
    <file name="a.vb">
        Imports System.Threading.Tasks

        Public Class Test
            Public Async Sub F()
                Await Task.Delay(0)
            End Sub
        End Class
            </file>
</compilation>

            Dim reference = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.DebugDll).EmitToImageReference(New EmitOptions(metadataOnly:=True))
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(<compilation/>, {reference}, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Assert.Empty(comp.GetMember(Of NamedTypeSymbol)("Test").GetMembers("VB$StateMachine_0_F"))
            Dim asyncMethod = comp.GetMember(Of MethodSymbol)("Test.F")

            Dim asyncMethodAttributes = asyncMethod.GetAttributes()
            AssertEx.SetEqual({"DebuggerStepThroughAttribute"}, GetAttributeNames(asyncMethodAttributes))
        End Sub
#End Region

#Region "IteratorStateMachineAttribute"

        <Theory>
        <MemberData(NameOf(OptimizationLevelTheoryData))>
        Public Sub IteratorStateMachineAttribute_Method(optimizationLevel As OptimizationLevel)
            Dim source =
<compilation>
    <file name="a.vb">
        Imports System.Collections.Generic

        Class Test
            Iterator Function F() As IEnumerable(Of Integer)
                Yield 0
            End Function
        End Class
            </file>
</compilation>

            Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=optimizationLevel)
            Dim reference = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=options).EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(<compilation/>, {reference}, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Dim stateMachine = comp.GetMember(Of NamedTypeSymbol)("Test.VB$StateMachine_1_F")
            Dim iteratorMethod = comp.GetMember(Of MethodSymbol)("Test.F")

            Dim iteratorMethodAttributes = iteratorMethod.GetAttributes()
            AssertEx.SetEqual({"IteratorStateMachineAttribute"}, GetAttributeNames(iteratorMethodAttributes))

            Dim attributeArg = DirectCast(iteratorMethodAttributes.Single().ConstructorArguments.Single().Value, NamedTypeSymbol)
            Assert.Equal(attributeArg, stateMachine)
        End Sub

        <Theory>
        <MemberData(NameOf(OptimizationLevelTheoryData))>
        Public Sub IteratorStateMachineAttribute_Lambda(optimizationLevel As OptimizationLevel)
            Dim source =
<compilation>
    <file name="a.vb">
        Imports System
        Imports System.Collections.Generic

        Class Test
            Sub F()
                Dim f As Func(Of IEnumerable(Of Integer)) = 
                    Iterator Function() As IEnumerable(Of Integer)
                        Yield 0
                    End Function
            End Sub
        End Class
            </file>
</compilation>

            Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=optimizationLevel)
            Dim reference = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=options).EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(<compilation/>, {reference}, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Dim stateMachine = comp.GetMember(Of NamedTypeSymbol)("Test._Closure$__.VB$StateMachine___Lambda$__1-0")
            Dim iteratorMethod = comp.GetMember(Of MethodSymbol)("Test._Closure$__._Lambda$__1-0")

            Dim iteratorMethodAttributes = iteratorMethod.GetAttributes()
            AssertEx.SetEqual({"IteratorStateMachineAttribute"}, GetAttributeNames(iteratorMethodAttributes))

            Dim smAttribute = iteratorMethodAttributes.Single(Function(a) a.AttributeClass.Name = "IteratorStateMachineAttribute")
            Dim attributeArg = DirectCast(smAttribute.ConstructorArguments.First().Value, NamedTypeSymbol)
            Assert.Equal(attributeArg, stateMachine)
        End Sub

        <Fact>
        Public Sub IteratorStateMachineAttribute_GenericStateMachineClass()
            Dim source =
<compilation>
    <file name="a.vb">
        Imports System
        Imports System.Collections.Generic

        Class Test(Of T)
            Iterator Function F(Of U As Test(Of Integer))(arg As U) As IEnumerable(Of Integer)
                Yield 0
            End Function
        End Class
            </file>
</compilation>

            Dim reference = CreateCompilationWithMscorlib45AndVBRuntime(source).EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(<compilation/>, {reference}, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Dim stateMachine = comp.GetMember(Of NamedTypeSymbol)("Test.VB$StateMachine_1_F")
            Dim iteratorMethod = comp.GetMember(Of MethodSymbol)("Test.F")

            Dim iteratorMethodAttributes = iteratorMethod.GetAttributes()
            AssertEx.SetEqual({"IteratorStateMachineAttribute"}, GetAttributeNames(iteratorMethodAttributes))

            Dim attributeStateMachineClass = DirectCast(iteratorMethodAttributes.Single().ConstructorArguments.Single().Value, NamedTypeSymbol)
            Assert.Equal(attributeStateMachineClass, stateMachine.ConstructUnboundGenericType())
        End Sub

        <Theory>
        <MemberData(NameOf(OptimizationLevelTheoryData))>
        Public Sub IteratorStateMachineAttribute_MetadataOnly(optimizationLevel As OptimizationLevel)
            Dim source =
<compilation>
    <file name="a.vb">
        Imports System
        Imports System.Collections.Generic

        Public Class Test
            Public Iterator Function F() As IEnumerable(Of Integer)
                Yield 0
            End Function
        End Class
            </file>
</compilation>

            Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=optimizationLevel)
            Dim reference = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=options).EmitToImageReference(New EmitOptions(metadataOnly:=True))
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(<compilation/>, {reference}, options:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Assert.Empty(comp.GetMember(Of NamedTypeSymbol)("Test").GetMembers("VB$StateMachine_1_F"))
            Dim iteratorMethod = comp.GetMember(Of MethodSymbol)("Test.F")

            Dim iteratorMethodAttributes = iteratorMethod.GetAttributes()
            Assert.Empty(GetAttributeNames(iteratorMethodAttributes))
        End Sub
#End Region

        <Fact, WorkItem(7809, "https://github.com/dotnet/roslyn/issues/7809")>
        Public Sub SynthesizeAttributeWithUseSiteErrorFails()
#Region "mslib"
            Dim mslibNoString = "
        Namespace System
            Public Class [Object]
            End Class

            Public Class Int32
            End Class

            Public Class ValueType
            End Class

            Public Class Attribute
            End Class

            Public Class Void
            End Class
        End Namespace
        "
            Dim mslib = mslibNoString & "
        Namespace System
            Public Class [String]
            End Class
        End Namespace
        "
#End Region
            ' Build an mscorlib including String
            Dim mslibComp = CreateEmptyCompilation({Parse(mslib)}).VerifyDiagnostics()
            Dim mslibRef = mslibComp.EmitToImageReference()

            ' Build an mscorlib without String
            Dim mslibNoStringComp = CreateEmptyCompilation({Parse(mslibNoString)}).VerifyDiagnostics()
            Dim mslibNoStringRef = mslibNoStringComp.EmitToImageReference()

            Dim diagLibSource = "
        Namespace System.Diagnostics
            Public Class DebuggerDisplayAttribute
                    Inherits System.Attribute

                Public Sub New(s As System.String)
                End Sub

                Public Property Type as System.String
            End Class
        End Namespace

        Namespace System.Runtime.CompilerServices
            Public Class CompilerGeneratedAttribute
            End Class
        End Namespace
        "
            ' Build Diagnostics referencing mscorlib with String
            Dim diagLibComp = CreateEmptyCompilation({Parse(diagLibSource)}, references:={mslibRef}).VerifyDiagnostics()
            Dim diagLibRef = diagLibComp.EmitToImageReference()

            ' Create compilation using Diagnostics but referencing mscorlib without String
            Dim comp = CreateEmptyCompilation({Parse("")}, references:={diagLibRef, mslibNoStringRef})

            ' Attribute cannot be synthesized because ctor has a use-site error (String type missing)
            Dim attribute = comp.TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__ctor)
            Assert.Equal(Nothing, attribute)

            ' Attribute cannot be synthesized because type in named argument has use-site error (String type missing)
            Dim attribute2 = comp.TrySynthesizeAttribute(
                                           WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor,
                                           namedArguments:=ImmutableArray.Create(New KeyValuePair(Of WellKnownMember, TypedConstant)(
                                                                WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__Type,
                                                                New TypedConstant(comp.GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, "unused"))))
            Assert.Equal(Nothing, attribute2)
        End Sub

    End Class
End Namespace
