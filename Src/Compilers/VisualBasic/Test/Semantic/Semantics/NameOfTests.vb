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
<compilation name="QueryExpressions">
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
BC30183: Keyword is not valid as an identifier.
        Dim x = NameOf(Integer.MaxValue)
                       ~~~~~~~
BC30183: Keyword is not valid as an identifier.
        Dim y = NameOf(Integer)
                       ~~~~~~~
BC30183: Keyword is not valid as an identifier.
        Dim z = NameOf(Variant)
                       ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_02()
            Dim compilationDef =
<compilation name="QueryExpressions">
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
BC32099: Comma or ')' expected.
        Dim x = NameOf(C2(Of Integer).C3(Of ))
                             ~~~~~~~
BC32088: Type arguments unexpected.
        Dim x = NameOf(C2(Of Integer).C3(Of ))
                                        ~~~~~
BC32088: Type arguments unexpected.
        Dim y = NameOf(C2(Of ).C3(Of Integer))
                                 ~~~~~~~~~~~~
BC32099: Comma or ')' expected.
        Dim z = NameOf(C2(Of Integer).C3(Of Integer))
                             ~~~~~~~
BC32088: Type arguments unexpected.
        Dim z = NameOf(C2(Of Integer).C3(Of Integer))
                                        ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_03()
            Dim compilationDef =
<compilation name="QueryExpressions">
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
BC32099: Comma or ')' expected.
        Dim x = NameOf(C2(Of Integer).C3(Of ).M1)
                             ~~~~~~~
BC32099: Comma or ')' expected.
        Dim y = NameOf(C2(Of ).C3(Of Integer).M1)
                                     ~~~~~~~
BC32099: Comma or ')' expected.
        Dim z = NameOf(C2(Of Integer).C3(Of Integer).M1)
                             ~~~~~~~
BC32099: Comma or ')' expected.
        Dim z = NameOf(C2(Of Integer).C3(Of Integer).M1)
                                            ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestParsing_04()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(Global)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30287: '.' expected.
        Dim x = NameOf(Global)
                             ~
</expected>)
        End Sub

        <Fact>
        Public Sub Namespace_01()
            Dim compilationDef =
<compilation name="QueryExpressions">
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
<compilation name="QueryExpressions">
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2(Of ).C3(Of ).M1))
        System.Console.WriteLine(NameOf(C2(Of ).C3(Of ).m1))
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
<compilation name="QueryExpressions">
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
        Public Sub GenericType_01()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2(Of ).C3))
        System.Console.WriteLine(NameOf(C2(Of ).c3))
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
<compilation name="QueryExpressions">
    <file name="a.vb">
Module Module1
    Sub Main()
        System.Console.WriteLine(NameOf(C2(Of ).CC3))
        System.Console.WriteLine(NameOf(C2(Of ).cc3))
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

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
CC3
cc3
]]>)
        End Sub

        <Fact>
        Public Sub AmbiguousType_02()
            Dim compilationDef =
<compilation name="QueryExpressions">
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

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
CC3
cc3
]]>)
        End Sub

        <Fact>
        Public Sub InacessibleNonGenericType_01()
            Dim compilationDef =
<compilation name="QueryExpressions">
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

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
CC3
]]>)
        End Sub

        <Fact>
        Public Sub Alias_01()
            Dim compilationDef =
<compilation name="QueryExpressions">
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
        Public Sub Inaccessible_01()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of ).C3(Of ).M1)
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
        Dim x = NameOf(C2(Of ).C3(Of ).M1)
                                       ~~
</expected>)
        End Sub

        <Fact>
        Public Sub Missing_01()
            Dim compilationDef =
<compilation name="QueryExpressions">
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x = NameOf(C2(Of ).C3(Of ).Missing)
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
BC30456: 'Missing' is not a member of 'C2(Of T).C3(Of S)'.
        Dim x = NameOf(C2(Of ).C3(Of ).Missing)
                                       ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Missing_02()
            Dim compilationDef =
<compilation name="QueryExpressions">
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
BC31208: Type or namespace 'Missing' is not defined.
        Dim x = NameOf(Missing.M1)
                       ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Missing_03()
            Dim compilationDef =
<compilation name="QueryExpressions">
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
<compilation name="QueryExpressions">
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

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
Ambiguous
]]>)
        End Sub

        <Fact>
        Public Sub AmbiguousMethod_02()
            Dim compilationDef =
<compilation name="QueryExpressions">
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
<compilation name="QueryExpressions">
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
<compilation name="QueryExpressions">
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
<compilation name="QueryExpressions">
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

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
LOCAL
]]>)
        End Sub

        <Fact>
        Public Sub Local_04()
            Dim compilationDef =
<compilation name="QueryExpressions">
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

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            AssertTheseDiagnostics(comp,
<expected>
BC30451: 'LOCAL' is not declared. It may be inaccessible due to its protection level.
        System.Console.WriteLine(NameOf(LOCAL))
                                        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Local_05()
            Dim compilationDef =
<compilation name="QueryExpressions">
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
<compilation name="QueryExpressions">
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
<compilation name="QueryExpressions">
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

            CompileAndVerify(comp, expectedOutput:=
            <![CDATA[
M1
]]>)
        End Sub

    End Class
End Namespace