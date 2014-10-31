' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class NameOfTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TestParsing_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(Integer.MaxValue)
        Dim y = NameOf(Integer)
        Dim z = NameOf(Variant)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC37244: This expression does not have a name.
        Dim y = NameOf(Integer)
                       ~~~~~~~
BC30804: 'Variant' is no longer a supported type; use the 'Object' type instead.
        Dim z = NameOf(Variant)
                       ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of ))
        Dim y = NameOf(C2(Of ).C3(Of Integer))
        Dim z = NameOf(C2(Of Integer).C3(Of Integer))
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)

    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30182: Type expected.
        Dim x = NameOf(C2(Of Integer).C3(Of ))
                                            ~
BC30182: Type expected.
        Dim y = NameOf(C2(Of ).C3(Of Integer))
                             ~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of ).M1)
        Dim y = NameOf(C2(Of ).C3(Of Integer).M1)
        Dim z = NameOf(C2(Of Integer).C3(Of Integer).M1)
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
        Sub M1()
        End Sub
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30182: Type expected.
        Dim x = NameOf(C2(Of Integer).C3(Of ).M1)
                                            ~
BC30182: Type expected.
        Dim y = NameOf(C2(Of ).C3(Of Integer).M1)
                             ~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_04()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(Global)
        Dim y = NameOf(Global.System)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC36000: 'Global' must be followed by '.' and an identifier.
        Dim x = NameOf(Global)
                       ~~~~~~
BC37244: This expression does not have a name.
        Dim x = NameOf(Global)
                       ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_05()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
    End Sub
End Module

Class CTest
    Sub Test1()
        Dim x = NameOf(MyClass)
        Dim y = NameOf(MyClass.Test1)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC32028: 'MyClass' must be followed by '.' and an identifier.
        Dim x = NameOf(MyClass)
                       ~~~~~~~
BC37244: This expression does not have a name.
        Dim x = NameOf(MyClass)
                       ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_06()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
    End Sub
End Module

Class CTest
    Sub Test1()
        Dim x = NameOf(MyBase)
        Dim y = NameOf(MyBase.GetHashCode)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC32027: 'MyBase' must be followed by '.' and an identifier.
        Dim x = NameOf(MyBase)
                       ~~~~~~
BC37244: This expression does not have a name.
        Dim x = NameOf(MyBase)
                       ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_07()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
    End Sub
End Module

Class CTest
    Sub Test1()
        Dim x = NameOf(Me)
        Dim y = NameOf(Me.GetHashCode)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC37244: This expression does not have a name.
        Dim x = NameOf(Me)
                       ~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_08()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(Integer?)
        Dim y = NameOf(Integer?.GetValueOrDefault)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC37244: This expression does not have a name.
        Dim x = NameOf(Integer?)
                       ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_09()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As Integer? = Nothing
        Dim y = NameOf(x.GetValueOrDefault)
        Dim z = NameOf((x).GetValueOrDefault)
        Dim u = NameOf(New Integer?().GetValueOrDefault)
        Dim v = NameOf(GetVal().GetValueOrDefault)
        Dim w = NameOf(GetVal.GetValueOrDefault)
    End Sub

    Function GetVal() As Integer?
        Return Nothing
    End Function
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC37245: This sub-expression cannot be used inside NameOf argument.
        Dim z = NameOf((x).GetValueOrDefault)
                       ~~~
BC37245: This sub-expression cannot be used inside NameOf argument.
        Dim u = NameOf(New Integer?().GetValueOrDefault)
                       ~~~~~~~~~~~~~~
BC37245: This sub-expression cannot be used inside NameOf argument.
        Dim v = NameOf(GetVal().GetValueOrDefault)
                       ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_10()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As Integer? = Nothing
        NameOf(x.GetValueOrDefault)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30035: Syntax error.
        NameOf(x.GetValueOrDefault)
        ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Namespace_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(Global.System))
        System.Console.WriteLine(NameOf(Global.system))
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
System
system
]]>)
        End Sub

        <Fact>
        Public Sub Method_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2(Of Integer).C3(Of Short).M1))
        System.Console.WriteLine(NameOf(C2(Of Integer).C3(Of Short).m1))
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
        Sub M1()
        End Sub
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
m1
]]>)
        End Sub

        <Fact>
        Public Sub Method_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C1.M1))
        System.Console.WriteLine(NameOf(C1.m1))
    End Sub
End Module

Class C1
    Sub M1(Of T)()
    End Sub

    Sub M1(x as Integer)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
m1
]]>)
        End Sub

        <Fact>
        Public Sub Method_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C1.M1))
    End Sub
End Module

Class C1
    Sub M1(Of T)()
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
]]>)
        End Sub

        <Fact>
        Public Sub Method_04()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C1.M1(Of Integer)))
    End Sub
End Module

Class C1
    Sub M1(Of T)()
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC37246: Method type arguments unexpected.
        System.Console.WriteLine(NameOf(C1.M1(Of Integer)))
                                             ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub GenericType_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2(Of Integer).C3(Of Short)))
        System.Console.WriteLine(NameOf(C2(Of Integer).c3(Of Short)))
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
C3
c3
]]>)
        End Sub

        <Fact>
        Public Sub AmbiguousType_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2(Of Integer).CC3))
        System.Console.WriteLine(NameOf(C2(Of Integer).cc3))
    End Sub
End Module

Class C2(Of T)
    Class Cc3(Of S)
    End Class

    Class cC3(Of U, V)
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC32042: Too few type arguments to 'C2(Of Integer).Cc3(Of S)'.
        System.Console.WriteLine(NameOf(C2(Of Integer).CC3))
                                        ~~~~~~~~~~~~~~~~~~
BC32042: Too few type arguments to 'C2(Of Integer).Cc3(Of S)'.
        System.Console.WriteLine(NameOf(C2(Of Integer).cc3))
                                        ~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub AmbiguousType_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.CC3))
        System.Console.WriteLine(NameOf(C2.cc3))
    End Sub
End Module

Class C1
    Class Cc3(Of S)
    End Class
End Class

Class C2
    Inherits C1

    Class cC3(Of U, V)
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC32042: Too few type arguments to 'C2.cC3(Of U, V)'.
        System.Console.WriteLine(NameOf(C2.CC3))
                                        ~~~~~~
BC32042: Too few type arguments to 'C2.cC3(Of U, V)'.
        System.Console.WriteLine(NameOf(C2.cc3))
                                        ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InacessibleNonGenericType_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.CC3))
    End Sub
End Module

Class C2
    protected Class Cc3
    End Class

    Class cC3(Of U, V)
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30389: 'C2.Cc3' is not accessible in this context because it is 'Protected'.
        System.Console.WriteLine(NameOf(C2.CC3))
                                        ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Alias_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports [alias] = System

Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf([alias]))
        System.Console.WriteLine(NameOf([ALIAS]))
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
alias
ALIAS
]]>)
        End Sub

        <Fact>
        Public Sub InaccessibleMethod_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of Short).M1)
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
        Protected Sub M1()
        End Sub
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30390: 'C3.Protected Sub M1()' is not accessible in this context because it is 'Protected'.
        Dim x = NameOf(C2(Of Integer).C3(Of Short).M1)
                                                   ~~
</expected>)
        End Sub

        <Fact>
        Public Sub InaccessibleProperty_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of Short).P1)
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
        Protected Property P1 As Integer
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30389: 'C2(Of Integer).C3(Of Short).P1' is not accessible in this context because it is 'Protected'.
        Dim x = NameOf(C2(Of Integer).C3(Of Short).P1)
                                                   ~~
</expected>)
        End Sub

        <Fact>
        Public Sub InaccessibleField_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of Short).F1)
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
        Protected F1 As Integer
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30389: 'C2(Of Integer).C3(Of Short).F1' is not accessible in this context because it is 'Protected'.
        Dim x = NameOf(C2(Of Integer).C3(Of Short).F1)
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InaccessibleEvent_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of Short).E1)
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
        Protected Event E1 As System.Action
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30389: 'C2(Of Integer).C3(Of Short).E1' is not accessible in this context because it is 'Protected'.
        Dim x = NameOf(C2(Of Integer).C3(Of Short).E1)
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Missing_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of Integer).C3(Of Short).Missing)
    End Sub
End Module

Class C2(Of T)
    Class C3(Of S)
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30456: 'Missing' is not a member of 'C2(Of Integer).C3(Of Short)'.
        Dim x = NameOf(C2(Of Integer).C3(Of Short).Missing)
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Missing_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(Missing.M1)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30451: 'Missing' is not declared. It may be inaccessible due to its protection level.
        Dim x = NameOf(Missing.M1)
                       ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Missing_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(Missing)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30451: 'Missing' is not declared. It may be inaccessible due to its protection level.
        Dim x = NameOf(Missing)
                       ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub AmbiguousMethod_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(Ambiguous))
    End Sub
End Module

Module Module2
    Sub Ambiguous()
    End Sub
End Module

Module Module3
    Sub Ambiguous()
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30562: 'Ambiguous' is ambiguous between declarations in Modules 'Module2, Module3'.
        System.Console.WriteLine(NameOf(Ambiguous))
                                        ~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub AmbiguousMethod_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(I3.Ambiguous))
    End Sub
End Module

Interface I1
    Sub Ambiguous()
End Interface

Interface I2
    Sub Ambiguous(x as Integer)
End Interface

Interface I3
    Inherits I1, I2
End Interface
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
Ambiguous
]]>)
        End Sub

        <Fact>
        Public Sub Local_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim local As Integer = 0
        System.Console.WriteLine(NameOf(LOCAL))
        System.Console.WriteLine(NameOf(loCal))
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
LOCAL
loCal
]]>)
        End Sub

        <Fact>
        Public Sub Local_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(LOCAL))
        Dim local As Integer = 0
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC32000: Local variable 'local' cannot be referred to before it is declared.
        System.Console.WriteLine(NameOf(LOCAL))
                                        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Local_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim local = NameOf(LOCAL)
        System.Console.WriteLine(local)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30980: Type of 'local' cannot be inferred from an expression containing 'local'.
        Dim local = NameOf(LOCAL)
                           ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Local_04()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Option Explicit Off

Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(LOCAL))
        local = 0
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
LOCAL
]]>)
        End Sub

        <Fact>
        Public Sub Local_05()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Option Explicit Off

Module Module1
    Sub Main()
        local = 3
        System.Console.WriteLine(NameOf(LOCAL))
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
LOCAL
]]>)
        End Sub

        <Fact>
        Public Sub Local_06()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Option Explicit Off

Module Module1
    Sub Main()
        local = NameOf(LOCAL)
        System.Console.WriteLine(local)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
LOCAL
]]>)
        End Sub

        <Fact>
        Public Sub TypeParameterAsQualifier_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Option Explicit Off

Module Module1
    Sub Main()
        C3(Of C2).Test()
    End Sub
End Module

Class C2
    Sub M1()
    End Sub
End Class

Class C3(Of T As C2)
    Shared Sub Test()
        System.Console.WriteLine(NameOf(T.M1))
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC32098: Type parameters cannot be used as qualifiers.
        System.Console.WriteLine(NameOf(T.M1))
                                        ~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.F1))
    End Sub
End Module

Class C2
    Public F1 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
F1
]]>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.F1.F2))
    End Sub
End Module

Class C2
    Public F1 As C3
End Class

Class C3
    Public F2 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
        System.Console.WriteLine(NameOf(C2.F1.F2))
                                        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.P1))
    End Sub
End Module

Class C2
    Public Property P1 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
P1
]]>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_04()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.P1.P2))
    End Sub
End Module

Class C2
    Public Property P1 As C3
End Class

Class C3
    Public Property P2 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
        System.Console.WriteLine(NameOf(C2.P1.P2))
                                        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_05()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.M1))
    End Sub
End Module

Class C2
    Public Function M1() As Integer
        Return Nothing
    End Function
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
]]>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_06()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.M1.M2))
    End Sub
End Module

Class C2
    Public Function M1() As C3
        Return Nothing
    End Function
End Class

Class C3
    Public Sub M2()
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
        System.Console.WriteLine(NameOf(C2.M1.M2))
                                        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_07()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.M1))
    End Sub

    <System.Runtime.CompilerServices.Extension>
    Public Function M1(this As C2) As Integer
        Return Nothing
    End Function
End Module

Class C2
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(compilationDef, {SystemCoreRef}, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
]]>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_08()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.M1.M2))
    End Sub

    <System.Runtime.CompilerServices.Extension>
    Public Function M1(this As C2) As C3
        Return Nothing
    End Function
End Module

Class C2
End Class

Class C3
    Public Sub M2()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(compilationDef, {SystemCoreRef}, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
        System.Console.WriteLine(NameOf(C2.M1.M2))
                                        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_09()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.E1))
    End Sub
End Module

Class C2
    Public Event E1 As System.Action
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
E1
]]>)
        End Sub

        <Fact>
        Public Sub InstanceOfType_10()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2.E1.Invoke))
    End Sub
End Module

Class C2
    Public Event E1 As System.Action
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
        System.Console.WriteLine(NameOf(C2.E1.Invoke))
                                        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub SharedOfValue_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New C2()
        System.Console.WriteLine(NameOf(x.F1))
        System.Console.WriteLine(NameOf(x.F1.F2))
    End Sub
End Module

Class C2
    Shared Public F1 As C3
End Class

Class C3
    Shared Public F2 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.F1))
                                        ~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.F1.F2))
                                        ~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.F1.F2))
                                        ~~~~~~~
</expected>)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
F1
F2
]]>)
        End Sub

        <Fact>
        Public Sub SharedOfValue_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New C2()
        System.Console.WriteLine(NameOf(x.P1))
        System.Console.WriteLine(NameOf(x.P1.P2))
    End Sub
End Module

Class C2
    Shared Public Property P1 As C3
End Class

Class C3
    Shared Public Property P2 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.P1.P2))
                                        ~~~~
</expected>)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
P1
P2
]]>)
        End Sub

        <Fact>
        Public Sub SharedOfValue_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New C2()
        System.Console.WriteLine(NameOf(x.M1))
        System.Console.WriteLine(NameOf(x.M1.M2))
    End Sub
End Module

Class C2
    Shared Public Function M1() As C3
        Return Nothing
    End Function
End Class

Class C3
    Shared Public Sub M2()
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.M1.M2))
                                        ~~~~
</expected>)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
M2
]]>)
        End Sub

        <Fact>
        Public Sub SharedOfValue_04()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New C2()
        System.Console.WriteLine(NameOf(x.E1))
    End Sub
End Module

Class C2
    Shared Public Event E1 As System.Action
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.E1))
                                        ~~~~
</expected>)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
E1
]]>)
        End Sub

        <Fact>
        Public Sub SharedOfValue_05()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New C2()
        System.Console.WriteLine(NameOf(x.T1))
        System.Console.WriteLine(NameOf(x.P1.T2))
    End Sub
End Module

Class C2
    Shared Public Property P1 As C3

    Public Class T1
    End Class
End Class

Class C3
    Public Class T2
    End Class
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            AssertTheseDiagnostics(comp,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.T1))
                                        ~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.P1.T2))
                                        ~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        System.Console.WriteLine(NameOf(x.P1.T2))
                                        ~~~~~~~
</expected>)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
T1
T2
]]>)
        End Sub

        <Fact>
        Public Sub DataFlow_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As C2
        System.Console.WriteLine(NameOf(x.F1))

        Dim y As C2

        Return 
        System.Console.WriteLine(y.F1)
    End Sub
End Module

Class C2
    Public F1 As Integer
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
F1
]]>).VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Attribute_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
<System.Diagnostics.DebuggerDisplay("={" + NameOf(Test.MTest) + "()}")>
Class Test

    Shared Sub Main()
        System.Console.WriteLine(DirectCast(GetType(Test).GetCustomAttributes(GetType(System.Diagnostics.DebuggerDisplayAttribute), False)(0), System.Diagnostics.DebuggerDisplayAttribute).Value)
    End Sub

    Function MTest() As String
        Return ""
    End Function
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.DebugExe)

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
={MTest()}
]]>).VerifyDiagnostics()
        End Sub

    End Class
End Namespace