' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.ExtensionMethods

    Public Class LookupAndInvocation : Inherits BasicTestBase

        <Fact>
        Public Sub MethodProximity1()
            Dim compilationDef =
<compilation name="MethodProximity1">
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
                Dim x As Integer = 0
                x.Test1()
            End Sub

            &lt;Extension()&gt;
            Sub Test1(this As Integer)
                WriteLine("Module1.Test1")
            End Sub
        End Module

        Module Module2
            &lt;Extension()&gt;
            Sub Test1(this As Integer)
                WriteLine("Module2.Test1")
            End Sub
        End Module
    End Namespace

    Module Module3
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module3.Test1")
        End Sub
    End Module
End Namespace

Module Module4
    &lt;Extension()&gt;
    Sub Test1(this As Integer)
        WriteLine("Module4.Test1")
    End Sub
End Module

Namespace NS3
    Module Module5
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module5.Test1")
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module6.Test1")
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module7.Test1")
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module8.Test1")
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module9.Test1")
        End Sub
    End Module
End Namespace




Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})),
                             expectedOutput:=
            <![CDATA[
Module1.Test1
]]>)
        End Sub

        <Fact>
        Public Sub MethodProximity2()
            Dim compilationDef =
<compilation name="MethodProximity2">
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
                Dim x As Integer = 0
                x.Test1()
            End Sub

            Sub Test1(this As Integer)
                WriteLine("Module1.Test1")
            End Sub
        End Module

        Module Module2
            &lt;Extension()&gt;
            Sub Test1(this As Integer)
                WriteLine("Module2.Test1")
            End Sub
        End Module
    End Namespace

    Module Module3
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module3.Test1")
        End Sub
    End Module
End Namespace

Module Module4
    &lt;Extension()&gt;
    Sub Test1(this As Integer)
        WriteLine("Module4.Test1")
    End Sub
End Module

Namespace NS3
    Module Module5
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module5.Test1")
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module6.Test1")
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module7.Test1")
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module8.Test1")
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module9.Test1")
        End Sub
    End Module
End Namespace




Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})),
                             expectedOutput:=
            <![CDATA[
Module2.Test1
]]>)
        End Sub

        <Fact>
        Public Sub MethodProximity3()
            Dim compilationDef =
<compilation name="MethodProximity3">
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
                Dim x As Integer = 0
                x.Test1()
            End Sub

            Sub Test1(this As Integer)
                WriteLine("Module1.Test1")
            End Sub
        End Module

        Module Module2
            Sub Test1(this As Integer)
                WriteLine("Module2.Test1")
            End Sub
        End Module
    End Namespace

    Module Module3
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module3.Test1")
        End Sub
    End Module
End Namespace

Module Module4
    &lt;Extension()&gt;
    Sub Test1(this As Integer)
        WriteLine("Module4.Test1")
    End Sub
End Module

Namespace NS3
    Module Module5
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module5.Test1")
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module6.Test1")
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module7.Test1")
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module8.Test1")
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module9.Test1")
        End Sub
    End Module
End Namespace




Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})),
                             expectedOutput:=
            <![CDATA[
Module3.Test1
]]>)
        End Sub

        <Fact>
        Public Sub MethodProximity4()
            Dim compilationDef =
<compilation name="MethodProximity4">
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
                Dim x As Integer = 0
                x.Test1()
            End Sub

            Sub Test1(this As Integer)
                WriteLine("Module1.Test1")
            End Sub
        End Module

        Module Module2
            Sub Test1(this As Integer)
                WriteLine("Module2.Test1")
            End Sub
        End Module
    End Namespace

    Module Module3
        Sub Test1(this As Integer)
            WriteLine("Module3.Test1")
        End Sub
    End Module
End Namespace

Module Module4
    &lt;Extension()&gt;
    Sub Test1(this As Integer)
        WriteLine("Module4.Test1")
    End Sub
End Module

Namespace NS3
    Module Module5
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module5.Test1")
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module6.Test1")
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module7.Test1")
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module8.Test1")
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module9.Test1")
        End Sub
    End Module
End Namespace




Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})),
                             expectedOutput:=
            <![CDATA[
Module4.Test1
]]>)
        End Sub

        <Fact>
        Public Sub MethodProximity5()
            Dim compilationDef =
<compilation name="MethodProximity5">
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
                Dim x As Integer = 0
                x.Test1()
            End Sub

            Sub Test1(this As Integer)
                WriteLine("Module1.Test1")
            End Sub
        End Module

        Module Module2
            Sub Test1(this As Integer)
                WriteLine("Module2.Test1")
            End Sub
        End Module
    End Namespace

    Module Module3
        Sub Test1(this As Integer)
            WriteLine("Module3.Test1")
        End Sub
    End Module
End Namespace

Module Module4
    Sub Test1(this As Integer)
        WriteLine("Module4.Test1")
    End Sub
End Module

Namespace NS3
    Module Module5
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module5.Test1")
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module6.Test1")
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module7.Test1")
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module8.Test1")
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module9.Test1")
        End Sub
    End Module
End Namespace




Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})),
                             expectedOutput:=
            <![CDATA[
Module5.Test1
]]>)
        End Sub

        <Fact>
        Public Sub MethodProximity6()
            Dim compilationDef =
<compilation name="MethodProximity6">
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
                Dim x As Integer = 0
                x.Test1()
            End Sub

            Sub Test1(this As Integer)
                WriteLine("Module1.Test1")
            End Sub
        End Module

        Module Module2
            Sub Test1(this As Integer)
                WriteLine("Module2.Test1")
            End Sub
        End Module
    End Namespace

    Module Module3
        Sub Test1(this As Integer)
            WriteLine("Module3.Test1")
        End Sub
    End Module
End Namespace

Module Module4
    Sub Test1(this As Integer)
        WriteLine("Module4.Test1")
    End Sub
End Module

Namespace NS3
    Module Module5
        Sub Test1(this As Integer)
            WriteLine("Module5.Test1")
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module6.Test1")
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module7.Test1")
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module8.Test1")
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module9.Test1")
        End Sub
    End Module
End Namespace




Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})),
                             expectedOutput:=
            <![CDATA[
Module6.Test1
]]>)
        End Sub

        <Fact>
        Public Sub MethodProximity7()
            Dim compilationDef =
<compilation name="MethodProximity7">
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
                Dim x As Integer = 0
                x.Test1()
            End Sub

            Sub Test1(this As Integer)
                WriteLine("Module1.Test1")
            End Sub
        End Module

        Module Module2
            Sub Test1(this As Integer)
                WriteLine("Module2.Test1")
            End Sub
        End Module
    End Namespace

    Module Module3
        Sub Test1(this As Integer)
            WriteLine("Module3.Test1")
        End Sub
    End Module
End Namespace

Module Module4
    Sub Test1(this As Integer)
        WriteLine("Module4.Test1")
    End Sub
End Module

Namespace NS3
    Module Module5
        Sub Test1(this As Integer)
            WriteLine("Module5.Test1")
        End Sub
    End Module

    Module Module6
        Sub Test1(this As Integer)
            WriteLine("Module6.Test1")
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module7.Test1")
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module8.Test1")
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module9.Test1")
        End Sub
    End Module
End Namespace




Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})),
                             expectedOutput:=
            <![CDATA[
Module7.Test1
]]>)
        End Sub

        <Fact>
        Public Sub MethodProximity8()
            Dim compilationDef =
<compilation name="MethodProximity8">
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
                Dim x As Integer = 0
                x.Test1()
            End Sub

            Sub Test1(this As Integer)
                WriteLine("Module1.Test1")
            End Sub
        End Module

        Module Module2
            Sub Test1(this As Integer)
                WriteLine("Module2.Test1")
            End Sub
        End Module
    End Namespace

    Module Module3
        Sub Test1(this As Integer)
            WriteLine("Module3.Test1")
        End Sub
    End Module
End Namespace

Module Module4
    Sub Test1(this As Integer)
        WriteLine("Module4.Test1")
    End Sub
End Module

Namespace NS3
    Module Module5
        Sub Test1(this As Integer)
            WriteLine("Module5.Test1")
        End Sub
    End Module

    Module Module6
        Sub Test1(this As Integer)
            WriteLine("Module6.Test1")
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        Sub Test1(this As Integer)
            WriteLine("Module7.Test1")
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module8.Test1")
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module9.Test1")
        End Sub
    End Module
End Namespace




Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})),
                             expectedOutput:=
            <![CDATA[
Module8.Test1
]]>)
        End Sub

        <Fact>
        Public Sub MethodProximity9()
            Dim compilationDef =
<compilation name="MethodProximity9">
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
                Dim x As Integer = 0
                x.Test1()
            End Sub

            Sub Test1(this As Integer)
                WriteLine("Module1.Test1")
            End Sub
        End Module

        Module Module2
            Sub Test1(this As Integer)
                WriteLine("Module2.Test1")
            End Sub
        End Module
    End Namespace

    Module Module3
        Sub Test1(this As Integer)
            WriteLine("Module3.Test1")
        End Sub
    End Module
End Namespace

Module Module4
    Sub Test1(this As Integer)
        WriteLine("Module4.Test1")
    End Sub
End Module

Namespace NS3
    Module Module5
        Sub Test1(this As Integer)
            WriteLine("Module5.Test1")
        End Sub
    End Module

    Module Module6
        Sub Test1(this As Integer)
            WriteLine("Module6.Test1")
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        Sub Test1(this As Integer)
            WriteLine("Module7.Test1")
        End Sub
    End Module

    Module Module8
        Sub Test1(this As Integer)
            WriteLine("Module8.Test1")
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test1(this As Integer)
            WriteLine("Module9.Test1")
        End Sub
    End Module
End Namespace




Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef,
                                                                             TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30456: 'Test1' is not a member of 'Integer'.
                x.Test1()
                ~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub MethodProximity10()
            Dim compilationDef =
<compilation name="MethodProximity10">
    <file name="a.vb">
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As New C1()
        x.Test1()
    End Sub

End Module

Module Module2
    &lt;Extension()&gt;
    Sub Test1(this As C1)
    End Sub
End Module

Module Module3
    &lt;Extension()&gt;
    Sub Test1(this As C1)
    End Sub
End Module

Class C1
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Test1' is most specific for these arguments:
    Extension method 'Public Sub Test1()' defined in 'Module2': Not most specific.
    Extension method 'Public Sub Test1()' defined in 'Module3': Not most specific.
        x.Test1()
          ~~~~~
</expected>)
        End Sub


        <Fact>
        Public Sub InstanceVsExtension1()
            Dim compilationDef =
<compilation name="InstanceVsExtension1">
    <file name="a.vb">
Option Strict Off        

Imports System.Console
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As New C1()
        x.Test1()
        x.Test2(1)
        x.Test3()

        Dim y as Long = Integer.MaxValue
        x.Test4(y)
        WriteLine(x.Test5)
    End Sub

    &lt;Extension()&gt;
    Sub Test1(this As C1)
        WriteLine("Module1.Test1")
    End Sub

    &lt;Extension()&gt;
    Sub Test2(this As C1, x As Integer)
        WriteLine("Module1.Test2")
    End Sub

    &lt;Extension()&gt;
    Sub Test3(this As C1)
        WriteLine("Module1.Test3")
    End Sub

    &lt;Extension()&gt;
    Function Test5(this As C1) As Integer
        Return 1
    End Function

End Module

Class C1

    Sub Test1()
        WriteLine("C1.Test1")
    End Sub

    Sub Test2()
        WriteLine("C1.Test2")
    End Sub

    Protected Sub Test3()
        WriteLine("C1.Test3")
    End Sub

    Sub Test4(x as Integer)
        WriteLine("C1.Test4")
    End Sub

    Protected Test5 As Integer

End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
C1.Test1
Module1.Test2
Module1.Test3
C1.Test4
1
]]>)
        End Sub

        <Fact>
        Public Sub InstanceVsExtension2()
            Dim compilationDef =
<compilation name="InstanceVsExtension2">
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As I1 = New C1()
        x.Test1()
        x.Test2(1)
        x.Test3()

        'Call CType(x, Object).Test3()
    End Sub

    &lt;Extension()&gt;
    Sub Test1(this As I1)
        WriteLine("Module1.Test1")
    End Sub

    &lt;Extension()&gt;
    Sub Test2(this As I1, x As Integer)
        WriteLine("Module1.Test2")
    End Sub

    &lt;Extension()&gt;
    Sub Test3(this As Object)
        WriteLine("Module1.Test3")
    End Sub

End Module

Interface I1
    Sub Test1()
    Sub Test2()
End Interface

Class C1
    Implements I1

    Sub Test1() Implements I1.Test1
        WriteLine("C1.Test1")
    End Sub

    Sub Test2() Implements I1.Test2
        WriteLine("C1.Test2")
    End Sub

End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
C1.Test1
Module1.Test2
Module1.Test3
]]>)
        End Sub

        <Fact>
        Public Sub InstanceVsExtension3()
            Dim compilationDef =
<compilation name="InstanceVsExtension3">
    <file name="a.vb">
Option Strict Off

Imports System
Imports System.Runtime.CompilerServices


Module Module1

    Sub Main()
        Dim x As New C1()

        x.Test(AddressOf Target)
    End Sub

    Sub Target(x As Byte)
    End Sub

    &lt;Extension()&gt;
    Sub Test(this As C1, x As Action(Of Byte))
        System.Console.WriteLine(123)
    End Sub
End Module

Class C1
    Sub Test(x As Action(Of Integer))
        System.Console.WriteLine("!!!")
    End Sub
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
123
]]>)
        End Sub

        <Fact>
        Public Sub InstanceVsExtension4a()
            Dim compilationDef =
<compilation name="InstanceVsExtension4a">
    <file name="a.vb">
Option Strict Off

Imports System.Runtime.CompilerServices
Imports System

Module Module1

    Sub Main()
    End Sub

    &lt;Extension()&gt;
    Sub Test1(this As C1, x As Integer)
    End Sub

    &lt;Extension()&gt;
    Sub Test6(this As C1, x As Object)
    End Sub
End Module

Class C1
    ReadOnly f As Integer

    Sub New()
        Dim d1 As Action = Sub() Test1(f)
    End Sub

    Sub Test1(ByRef x As Integer)
    End Sub

    Sub Test6(Of T)(x As IComparable(Of T))
    End Sub
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
        Dim d1 As Action = Sub() Test1(f)
                                       ~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceVsExtension4b()
            Dim compilationDef =
<compilation name="InstanceVsExtension4b">
    <file name="a.vb">
Option Strict Off

Imports System.Runtime.CompilerServices
Imports System

Module Module1

    Sub Main()
        dim o as new C1
    End Sub

    &lt;Extension()&gt;
    Sub Test1(this As C1, x As Integer)
    End Sub

    &lt;Extension()&gt;
    Sub Test6(this As C1, x As Object)
    End Sub
End Module

Class C1
    ReadOnly f As Integer

    Sub New()
        Test6(CObj(1))
    End Sub

    Sub Test1(ByRef x As Integer)
    End Sub

    Sub Test6(Of T)(x As IComparable(Of T))
        System.Console.Writeline("comparable")
    End Sub
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(compilationDef, {SystemCoreRef}, TestOptions.ReleaseExe)
            AssertTheseDiagnostics(compilation,
<expected>
BC36908: Late-bound extension methods are not supported.
        Test6(CObj(1))
        ~~~~~
</expected>)

        End Sub


        <Fact>
        Public Sub InstanceVsExtension5()
            Dim compilationDef =
<compilation name="InstanceVsExtension5">
    <file name="a.vb">
Imports System
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As New C1()
    End Sub

    Sub Target(x As Byte)
    End Sub

    &lt;Extension()&gt;
    Sub Test1(this As C1, x As Integer)
        System.Console.WriteLine("Module1.Test1")
    End Sub

    &lt;Extension()&gt;
    Sub Test2(this As C1, x As Integer, y As Integer)
        System.Console.WriteLine("Module1.Test2")
    End Sub

    &lt;Extension()&gt;
    Sub Test3(this As C1, x As Integer, y As Integer)
        System.Console.WriteLine("Module1.Test3")
    End Sub

    &lt;Extension()&gt;
    Sub Test4(this As C1, x As Integer)
        System.Console.WriteLine("Module1.Test4")
    End Sub

    &lt;Extension()&gt;
    Sub Test5(this As C1, x As Object)
        System.Console.WriteLine("Module1.Test5")
    End Sub

    &lt;Extension()&gt;
    Sub Test6(this As C1, x As Object)
        System.Console.WriteLine("Module1.Test6")
    End Sub
End Module

Class C1
    ReadOnly f As Integer

    Sub New()
        Test1(f)
        Test2(f, f)
        Test3(f, f)
        Test4(f)
        Test5(CObj(Nothing))
        Test5(Nothing)
        Test5(1)
        Test5(Integer.MaxValue)

        Dim d2 As Action = Sub() Test2(f, f)
        d2()
        Dim d3 As Action = Sub() Test3(f, f)
        d3()
        Dim d4 As Action = Sub() Test4(f)
        d4()

        Test6(CObj(1))
    End Sub

    Sub Test1(ByRef x As Integer)
        System.Console.WriteLine("C1.Test1")
    End Sub

    Sub Test2(ByRef x As Integer, y As Byte)
        System.Console.WriteLine("C1.Test2")
    End Sub

    Sub Test3(x As Byte, ByRef y As Integer)
        System.Console.WriteLine("C1.Test3")
    End Sub

    Sub Test4(ByRef x As Byte)
        System.Console.WriteLine("C1.Test4")
    End Sub

    Sub Test5(ParamArray x() As Byte)
        System.Console.WriteLine("C1.Test5")
    End Sub
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Off),
                             expectedOutput:=
            <![CDATA[
C1.Test1
Module1.Test2
Module1.Test3
Module1.Test4
C1.Test5
C1.Test5
C1.Test5
Module1.Test5
Module1.Test2
Module1.Test3
Module1.Test4
Module1.Test6
]]>)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Byte()'.
        Test5(CObj(Nothing))
              ~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceVsExtension6()
            Dim compilationDef =
<compilation name="InstanceVsExtension6">
    <file name="a.vb">
Imports System
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As New C1()
    End Sub

    Sub Target(x As Byte)
    End Sub

    &lt;Extension()&gt;
    Sub Test1(this As C1, x As Integer)
        System.Console.WriteLine("Module1.Test1")
    End Sub

    &lt;Extension()&gt;
    Sub Test2(this As C1, x As Integer, y As Integer)
        System.Console.WriteLine("Module1.Test2")
    End Sub

    &lt;Extension()&gt;
    Sub Test3(this As C1, x As Integer, y As Integer)
        System.Console.WriteLine("Module1.Test3")
    End Sub

    &lt;Extension()&gt;
    Sub Test4(this As C1, x As Integer)
        System.Console.WriteLine("Module1.Test4")
    End Sub

    &lt;Extension()&gt;
    Sub Test5(this As C1, x As Object)
        System.Console.WriteLine("Module1.Test5")
    End Sub

    &lt;Extension()&gt;
    Sub Test6(this As C1, x As Object)
        System.Console.WriteLine("Module1.Test6")
    End Sub
End Module

Class C1
    ReadOnly f As Integer

    Sub New()
        Test1(f)
        Test2(f, f)
        Test3(f, f)
        Test4(f)
        Test5(Nothing)
        Test5(1)
        Test5(Integer.MaxValue)

        Dim d2 As Action = Sub() Test2(f, f)
        d2()
        Dim d3 As Action = Sub() Test3(f, f)
        d3()
        Dim d4 As Action = Sub() Test4(f)
        d4()

        Test6(CObj(1))
    End Sub

    Sub Test1(ByRef x As Integer)
        System.Console.WriteLine("C1.Test1")
    End Sub

    Sub Test2(ByRef x As Integer, y As Byte)
        System.Console.WriteLine("C1.Test2")
    End Sub

    Sub Test3(x As Byte, ByRef y As Integer)
        System.Console.WriteLine("C1.Test3")
    End Sub

    Sub Test4(ByRef x As Byte)
        System.Console.WriteLine("C1.Test4")
    End Sub

    Sub Test5(ParamArray x() As Byte)
        System.Console.WriteLine("C1.Test5")
    End Sub
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On),
                             expectedOutput:=
            <![CDATA[
C1.Test1
Module1.Test2
Module1.Test3
Module1.Test4
C1.Test5
C1.Test5
Module1.Test5
Module1.Test2
Module1.Test3
Module1.Test4
Module1.Test6
]]>)

        End Sub

        <Fact>
        Public Sub ExtendingObject1()
            Dim compilationDef =
<compilation name="ExtendingObject1">
    <file name="a.vb">
Option Strict On

Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As I1 = Nothing
        Call CType(x, Object).Test3()
    End Sub

    &lt;Extension()&gt;
    Sub Test3(this As Object)
    End Sub

End Module

Interface I1
End Interface

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30574: Option Strict On disallows late binding.
        Call CType(x, Object).Test3()
             ~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub


        <Fact>
        Public Sub AccessingThroughTypeOrNamespace()
            Dim compilationDef =
<compilation name="AccessingThroughTypeOrNamespace">
    <file name="a.vb">
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        C1.Test1()
        NS1.Test1()
    End Sub

    &lt;Extension()&gt;
    Sub Test1(this As C1)
    End Sub

End Module

Class C1
    Shared Sub Test2()
        Test1()
    End Sub
End Class

Namespace NS1
    Module Module2
    End Module
End Namespace

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30469: Reference to a non-shared member requires an object reference.
        C1.Test1()
        ~~~~~~~~
BC30456: 'Test1' is not a member of 'NS1'.
        NS1.Test1()
        ~~~~~~~~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
        Test1()
        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ImplicitMe()
            Dim compilationDef =
<compilation name="ImplicitMe">
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As New C1()
        x.Test2()
    End Sub

    &lt;Extension()&gt;
    Sub Test1(this As C1)
        System.Console.WriteLine("Test1")
        System.Console.WriteLine(this)
    End Sub

End Module

Class C1
    Sub Test2()
        System.Console.WriteLine("Test2")
        Test1()
    End Sub
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef, expectedOutput:=
            <![CDATA[
Test2
Test1
C1
]]>)
        End Sub

        <Fact>
        Public Sub ByRefReceiver1()
            Dim compilationDef =
<compilation name="ByRefReceiver1">
    <file name="a.vb">
Option Strict Off

Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As New Base()
        Dim y As New Derived()
        Dim z As New Derived()

        System.Console.WriteLine(x Is y)
        x.Test1(y)
        System.Console.WriteLine(x Is y)

        System.Console.WriteLine(z Is y)
        z.Test1(y)
        System.Console.WriteLine(z Is y)

    End Sub

    &lt;Extension()&gt;
    Sub Test1(ByRef this As Base, x As Base)
        this = x
    End Sub

End Module

Class Base
End Class

Class Derived
    Inherits Base
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef, expectedOutput:=
            <![CDATA[
False
True
False
True
]]>)
        End Sub

        <Fact>
        Public Sub ByRefReceiver2()
            Dim compilationDef =
<compilation name="ByRefReceiver2">
    <file name="a.vb">
Option Strict On

Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Call Nothing.Test1(1)
    End Sub

    Sub Test0(x As Derived)
        x.Test1(1)
    End Sub

    &lt;Extension()&gt;
    Function Test1(ByRef this As Base, x As Integer) As Integer()
        Return Nothing
    End Function

    &lt;Extension()&gt;
    Function Test1(this As Base, x As String) As Integer()
        Return Nothing
    End Function

End Module

Class Base
End Class

Class Derived
    Inherits Base
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30574: Option Strict On disallows late binding.
        Call Nothing.Test1(1)
             ~~~~~~~~~~~~~
BC32029: Option Strict On disallows narrowing from type 'Base' to type 'Derived' in copying the value of 'ByRef' parameter 'this' back to the matching argument.
        x.Test1(1)
        ~
</expected>)
        End Sub

        <Fact>
        Public Sub Construction1()
            Dim compilationDef =
<compilation name="Construction1">
    <file name="a.vb">
Option Strict Off

Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As Integer = 0
        x.Test1()
        x.Test2(1L)
        x.Test2(Of Double)(1L)
        x.Test3(2US)
        x.Test3(Of Byte)(2US)
        x.Test4(2S)
        x.Test4(Of SByte)(2S)
        x.Test5(2UI, "")
        x.Test5(Of Single, Date)(2UI, Nothing)
    End Sub

    &lt;Extension()&gt;
    Sub Test1(Of T)(this As T)
        System.Console.WriteLine("Test1")
        System.Console.WriteLine(this.GetType())
    End Sub

    &lt;Extension()&gt;
    Sub Test2(Of T)(this As Integer, y As T)
        System.Console.WriteLine("Test2")
        System.Console.WriteLine(this.GetType())
        System.Console.WriteLine(y.GetType())
    End Sub

    &lt;Extension()&gt;
    Sub Test3(Of T, S)(this As T, y As S)
        System.Console.WriteLine("Test3")
        System.Console.WriteLine(this.GetType())
        System.Console.WriteLine(y.GetType())
    End Sub

    &lt;Extension()&gt;
    Sub Test4(Of S, T)(this As T, y As S)
        System.Console.WriteLine("Test4")
        System.Console.WriteLine(this.GetType())
        System.Console.WriteLine(y.GetType())
    End Sub

    &lt;Extension()&gt;
    Sub Test5(Of S, T, Q)(this As T, y As S, z As Q)
        System.Console.WriteLine("Test5")
        System.Console.WriteLine(this.GetType())
        System.Console.WriteLine(y.GetType())
        System.Console.WriteLine(z.GetType())
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef, expectedOutput:=
            <![CDATA[
Test1
System.Int32
Test2
System.Int32
System.Int64
Test2
System.Int32
System.Double
Test3
System.Int32
System.UInt16
Test3
System.Int32
System.Byte
Test4
System.Int32
System.Int16
Test4
System.Int32
System.SByte
Test5
System.Int32
System.UInt32
System.String
Test5
System.Int32
System.Single
System.DateTime
]]>)
        End Sub

        <Fact>
        Public Sub Construction2()
            Dim compilationDef =
<compilation name="Construction2">
    <file name="a.vb">
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As Integer = 0
        x.Test1(Of Integer)()
        x.Test2()
        x.Test3(Of Integer, Byte)(2US)
        x.Test4(Of SByte, Integer)(2S)
        x.Test5(Of Single, Integer, Date)(2UI, Nothing)
        x.Test5(Of Single)(2UI, Nothing)
    End Sub

    &lt;Extension()&gt;
    Sub Test1(Of T)(this As T)
    End Sub

    &lt;Extension()&gt;
    Function Test2(Of T)(this As Integer) As T
        Return Nothing
    End Function

    &lt;Extension()&gt;
    Sub Test3(Of T, S)(this As T, y As S)
    End Sub

    &lt;Extension()&gt;
    Sub Test4(Of S, T)(this As T, y As S)
    End Sub

    &lt;Extension()&gt;
    Sub Test5(Of S, T, Q)(this As T, y As S, z As Q)
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36907: Extension method 'Public Sub Test1()' defined in 'Module1' is not generic (or has no free type parameters) and so cannot have type arguments.
        x.Test1(Of Integer)()
               ~~~~~~~~~~~~
BC36589: Type parameter 'T' for extension method 'Public Function Test2(Of T)() As T' defined in 'Module1' cannot be inferred.
        x.Test2()
          ~~~~~
BC36591: Too many type arguments to extension method 'Public Sub Test3(Of S)(y As S)' defined in 'Module1'.
        x.Test3(Of Integer, Byte)(2US)
               ~~~~~~~~~~~~~~~~~~
BC36591: Too many type arguments to extension method 'Public Sub Test4(Of S)(y As S)' defined in 'Module1'.
        x.Test4(Of SByte, Integer)(2S)
               ~~~~~~~~~~~~~~~~~~~
BC36591: Too many type arguments to extension method 'Public Sub Test5(Of S, Q)(y As S, z As Q)' defined in 'Module1'.
        x.Test5(Of Single, Integer, Date)(2UI, Nothing)
               ~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36590: Too few type arguments to extension method 'Public Sub Test5(Of S, Q)(y As S, z As Q)' defined in 'Module1'.
        x.Test5(Of Single)(2UI, Nothing)
               ~~~~~~~~~~~
</expected>)
        End Sub


        <Fact>
        Public Sub NestedClasses1()
            Dim compilationDef =
<compilation name="NestedClasses1">
    <file name="a.vb">
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
    End Sub

    &lt;Extension()&gt;
    Sub Test3(x As C2)
    End Sub

End Module

Class C2

    Sub Test()
        Test3() ' C2
    End Sub

    Class C3
        Sub Test()
            Test3() 'C3
        End Sub

    End Class
End Class


Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30455: Argument not specified for parameter 'x' of 'Public Sub Test3(x As C2)'.
            Test3() 'C3
            ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Accessibility1()
            Dim compilationDef =
<compilation name="Accessibility1">
    <file name="a.vb">
Imports System.Runtime.CompilerServices
Imports NS1.Module2

Module Module1

    Sub Main()
    End Sub

End Module

Class C1
    Sub Test()
        Test3()
    End Sub
End Class


Namespace NS1
    Module Module2
    &lt;Extension()&gt;
        Private Sub Test3(x As C1)
        End Sub
    End Module
End Namespace


Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30390: 'Module2.Private Sub Test3()' is not accessible in this context because it is 'Private'.
        Test3()
        ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub SubOrFunction()
            Dim compilationDef =
<compilation name="SubOrFunction">
    <file name="a.vb">
Imports System.Runtime.CompilerServices

Module Module1

    Sub Main()
        Dim x As New C1()

        x.F1()
    End Sub

End Module


Module M1
    &lt;Extension()&gt;
    Sub F1(this As C1)
    End Sub
End Module

Module M2
    &lt;Extension()&gt;
    Function F1(this As C1) As Integer
        Return 0
    End Function
End Module

Class C1
End Class
Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'F1' is most specific for these arguments:
    Extension method 'Public Sub F1()' defined in 'M1': Not most specific.
    Extension method 'Public Function F1() As Integer' defined in 'M2': Not most specific.
        x.F1()
          ~~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultPropertyTransformation1()
            Dim compilationDef =
<compilation name="DefaultPropertyTransformation">
    <file name="a.vb">
Option Strict Off

Imports System.Runtime.CompilerServices

Module Module1
    Sub Main()
        Dim x As New C1()
        System.Console.WriteLine(x.F1(0))
    End Sub
End Module

Module Module2
    &lt;Extension()&gt;
    Function F1(this As C1) As Integer()
        Return New Integer() {123}
    End Function
End Module

Class C1
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef, expectedOutput:=
            <![CDATA[
123
]]>)
        End Sub

        <Fact>
        Public Sub DefaultPropertyTransformation2()
            Dim compilationDef =
<compilation name="DefaultPropertyTransformation">
    <file name="a.vb">
Option Strict Off

Imports System.Runtime.CompilerServices

Module Module1
    Sub Main()
        Dim x As New C1()
        System.Console.WriteLine(x.F1(0))
    End Sub

    &lt;Extension()&gt;
    Function F1(this As C1) As Integer()
        Return New Integer() {456}
    End Function
End Module

Module Module2
    &lt;Extension()&gt;
    Function F1(this As C1) As Integer()
        Return New Integer() {123}
    End Function
End Module

Class C1
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30516: Overload resolution failed because no accessible 'F1' accepts this number of arguments.
        System.Console.WriteLine(x.F1(0))
                                   ~~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultPropertyTransformation3()
            Dim compilationDef =
<compilation name="DefaultPropertyTransformation">
    <file name="a.vb">
Option Strict Off

Imports System.Runtime.CompilerServices

Module Module1
    Sub Main()
        Dim x As New C1()
        System.Console.WriteLine(x.F1(0))
    End Sub

    &lt;Extension()&gt;
    Function F1(this As C1) As Integer()
        Return New Integer() {456}
    End Function
End Module

Class C1
    Function F1() As Integer()
        Return New Integer() {123}
    End Function
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30516: Overload resolution failed because no accessible 'F1' accepts this number of arguments.
        System.Console.WriteLine(x.F1(0))
                                   ~~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultPropertyTransformation4()
            Dim compilationDef =
<compilation name="DefaultPropertyTransformation">
    <file name="a.vb">
Option Strict Off

Imports System.Runtime.CompilerServices

Module Module1
    Sub Main()
        Dim x As New C1()
        System.Console.WriteLine(x.F1(0))
    End Sub

    &lt;Extension()&gt;
    Function F1(Of T)(this As C1) As Integer()
        Return New Integer() {456}
    End Function
End Module

Class C1
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36582: Too many arguments to extension method 'Public Function F1(Of T)() As Integer()' defined in 'Module1'.
        System.Console.WriteLine(x.F1(0))
                                      ~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultPropertyTransformation5()
            Dim compilationDef =
<compilation name="DefaultPropertyTransformation">
    <file name="a.vb">
Option Strict Off

Imports System.Runtime.CompilerServices

Module Module1
    Sub Main()
        Dim x As New C1()
        System.Console.WriteLine(x.F1(Of Integer)(0))
    End Sub

    &lt;Extension()&gt;
    Function F1(Of T)(this As C1) As Integer()
        Return New Integer() {456}
    End Function
End Module

Class C1
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36582: Too many arguments to extension method 'Public Function F1(Of Integer)() As Integer()' defined in 'Module1'.
        System.Console.WriteLine(x.F1(Of Integer)(0))
                                                  ~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultPropertyTransformation6()
            Dim compilationDef =
<compilation name="DefaultPropertyTransformation">
    <file name="a.vb">
Option Strict Off

Imports System.Runtime.CompilerServices

Module Module1
    Sub Main()
        Dim x As New C1()
        System.Console.WriteLine(x.F1(0))
    End Sub

    &lt;Extension()&gt;
    Sub F1(this As C1)
    End Sub
End Module

Class C1
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36582: Too many arguments to extension method 'Public Sub F1()' defined in 'Module1'.
        System.Console.WriteLine(x.F1(0))
                                      ~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultPropertyTransformation7()
            Dim compilationDef =
<compilation name="DefaultPropertyTransformation">
    <file name="a.vb">
Option Strict On

Imports System.Runtime.CompilerServices

Module Module1
    Sub Main()
        Dim x As New Derived
        System.Console.WriteLine(x.F1(0))
        System.Console.WriteLine((x).F1(0))
    End Sub

    &lt;Extension()&gt;
    Function F1(ByRef this As Base) As Integer()
        Return New Integer() {456}
    End Function
End Module

Class Base
End Class

Class Derived
    Inherits Base
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32029: Option Strict On disallows narrowing from type 'Base' to type 'Derived' in copying the value of 'ByRef' parameter 'this' back to the matching argument.
        System.Console.WriteLine(x.F1(0))
                                 ~
</expected>)
        End Sub

        <Fact>
        Public Sub DefaultPropertyTransformation8()
            Dim compilationDef =
<compilation name="DefaultPropertyTransformation">
    <file name="a.vb">
Imports System.Runtime.CompilerServices

Module Module1
    Sub Main()
        Dim x As New Derived
        System.Console.WriteLine(x.F1(0))
    End Sub

    &lt;Extension()&gt;
    Function F1(ByRef this As Base) As Integer()
        Return New Integer() {456}
    End Function
End Module

Class Base
End Class

Class Derived
    Inherits Base
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC41999: Implicit conversion from 'Base' to 'Derived' in copying the value of 'ByRef' parameter 'this' back to the matching argument.
        System.Console.WriteLine(x.F1(0))
                                 ~
</expected>)

            CompileAndVerify(compilation, expectedOutput:="456")
        End Sub

        <Fact>
        Public Sub Diagnostics1()
            Dim compilationDef =
<compilation name="Diagnostics1">
    <file name="a.vb">
Option Strict Off

Imports System.Runtime.CompilerServices

Module Module1
    Sub Main()
        System.Console.WriteLine(1.F1())
        System.Console.WriteLine(2.F2(, x:=0))
        System.Console.WriteLine(3.F2(1, y:=0, y:=1))
        System.Console.WriteLine(4.F2(1, y:=0, z:=1))

        Dim d1 As System.Action(Of Long) = AddressOf 5.F4
        Dim d2 As System.Action(Of Long, Long) = AddressOf 6.F5
    End Sub

    &lt;Extension()&gt;
    Sub F1(ByRef this As Integer, x As Integer)
    End Sub

    &lt;Extension()&gt;
    Sub F2(ByRef this As Integer, x As Integer, y As Integer)
    End Sub

    &lt;Extension()&gt;
    Sub F4(this As Integer, x As Byte)
    End Sub

    &lt;Extension()&gt;
    Sub F4(this As Integer, x As Integer)
    End Sub

    &lt;Extension()&gt;
    Sub F5(this As Integer, x As Byte, y As Integer)
    End Sub

    &lt;Extension()&gt;
    Sub F5(this As Integer, x As Integer, y As Short)
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36586: Argument not specified for parameter 'x' of extension method 'Public Sub F1(x As Integer)' defined in 'Module1'.
        System.Console.WriteLine(1.F1())
                                   ~~
BC36586: Argument not specified for parameter 'y' of extension method 'Public Sub F2(x As Integer, y As Integer)' defined in 'Module1'.
        System.Console.WriteLine(2.F2(, x:=0))
                                   ~~
BC36583: Parameter 'x' in extension method 'Public Sub F2(x As Integer, y As Integer)' defined in 'Module1' already has a matching omitted argument.
        System.Console.WriteLine(2.F2(, x:=0))
                                        ~
BC36584: Parameter 'y' of extension method 'Public Sub F2(x As Integer, y As Integer)' defined in 'Module1' already has a matching argument.
        System.Console.WriteLine(3.F2(1, y:=0, y:=1))
                                               ~
BC36585: 'z' is not a parameter of extension method 'Public Sub F2(x As Integer, y As Integer)' defined in 'Module1'.
        System.Console.WriteLine(4.F2(1, y:=0, z:=1))
                                               ~
BC30950: No accessible method 'F4' has a signature compatible with delegate 'Delegate Sub Action(Of Long)(obj As Long)':
    Extension method 'Public Sub F4(x As Byte)' defined in 'Module1': Argument matching parameter 'x' narrows from 'Long' to 'Byte'.
    Extension method 'Public Sub F4(x As Integer)' defined in 'Module1': Argument matching parameter 'x' narrows from 'Long' to 'Integer'.
        Dim d1 As System.Action(Of Long) = AddressOf 5.F4
                                                     ~~~~
BC30950: No accessible method 'F5' has a signature compatible with delegate 'Delegate Sub Action(Of Long, Long)(arg1 As Long, arg2 As Long)':
    Extension method 'Public Sub F5(x As Byte, y As Integer)' defined in 'Module1': Method does not have a signature compatible with the delegate.
    Extension method 'Public Sub F5(x As Integer, y As Short)' defined in 'Module1': Method does not have a signature compatible with the delegate.
        Dim d2 As System.Action(Of Long, Long) = AddressOf 6.F5
                                                           ~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub BC36646ERR_TypeInferenceFailure3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="TypeInferenceFailure3">
        <file name="a.vb">
        Imports System

        Module Module1
            Sub Main()
                Dim classInstance As ClassExample=nothing
                classInstance.GenericExtensionMethod("Hello", "World")
            End Sub
            &lt;System.Runtime.CompilerServices.Extension()&gt; _
            Sub GenericExtensionMethod(Of T)(ByVal classEx As ClassExample, _
                                             ByVal x As String, ByVal y As  _
                                             InterfaceExample(Of T))
            End Sub
        End Module
        Interface InterfaceExample(Of T)
        End Interface
        Class ClassExample
        End Class

Namespace System.Runtime.CompilerServices
    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class
End Namespace

    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36646: Data type(s) of the type parameter(s) in extension method 'Public Sub GenericExtensionMethod(Of T)(x As String, y As InterfaceExample(Of T))' defined in 'Module1' cannot be inferred from these arguments. Specifying the data type(s) explicitly might correct this error.
                classInstance.GenericExtensionMethod("Hello", "World")
                              ~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub BC36652ERR_TypeInferenceFailureAmbiguous3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="TypeInferenceFailureAmbiguous3">
        <file name="a.vb">
        Option Strict Off
        Imports System.Runtime.CompilerServices
        Module Module1
            Sub Main()
                Dim caller As New Class1
                caller.targetExtension(1, "2")
            End Sub
            &lt;Extension()&gt; _
            Sub targetExtension(Of T)(ByVal p0 As Class1, ByVal p1 As T, ByVal p2 As T)
            End Sub
            Class Class1
            End Class
        End Module

Namespace System.Runtime.CompilerServices
    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class
End Namespace
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36652: Data type(s) of the type parameter(s) in extension method 'Public Sub targetExtension(Of T)(p1 As T, p2 As T)' defined in 'Module1' cannot be inferred from these arguments because more than one type is possible. Specifying the data type(s) explicitly might correct this error.
                caller.targetExtension(1, "2")
                       ~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub BC36658ERR_TypeInferenceFailureNoBest3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="TypeInferenceFailureNoBest3">
        <file name="a.vb">
        Option Strict Off
        Module Module1
            Sub Main()
                Dim c1 As New Class1
                c1.targetMethod(19, #3/4/2007#)
            End Sub
            &lt;System.Runtime.CompilerServices.Extension()&gt; _
            Sub targetMethod(Of T)(ByVal p0 As Class1, ByVal p1 As T, ByVal p2 As T)
            End Sub
            Class Class1
            End Class
        End Module

Namespace System.Runtime.CompilerServices
    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class
End Namespace
    </file>
    </compilation>)
            CompilationUtils.AssertTheseDiagnostics(compilation,
    <expected>
BC36658: Data type(s) of the type parameter(s) in extension method 'Public Sub targetMethod(Of T)(p1 As T, p2 As T)' defined in 'Module1' cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
                c1.targetMethod(19, #3/4/2007#)
                   ~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(542538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542538")>
        <Fact>
        Public Sub Bug8945()
            Dim compilationDef =
<compilation name="DefaultPropertyTransformation">
    <file name="a.vb">
Imports System.Runtime.CompilerServices
Imports X

Namespace X
    Module M
        &lt;Extension()&gt;
        Function Foo(ByVal x As String) As String
            Return x
        End Function
    End Module

    Namespace Y
        Module N
            &lt;Extension()&gt;
            Sub Foo(ByVal x As String)
                System.Console.WriteLine(x)
            End Sub
        End Module

        Module P
            Sub Main()
                Dim x = "test".Foo
            End Sub
        End Module
    End Namespace
End Namespace

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompilationUtils.AssertTheseDiagnostics(CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef),
<expected>
BC30491: Expression does not produce a value.
                Dim x = "test".Foo
                        ~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(542011, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542011")>
        <Fact>
        Public Sub Bug8968()
            Dim compilationDef =
<compilation name="Bug8968">
    <file name="a.vb">
Option Strict Off        

Imports System
Imports System.Console
Imports System.Runtime.CompilerServices


Interface IA(Of T)
End Interface

Interface IB
End Interface

Class C
    Implements IA(Of Integer), IB

    Sub Baz()
        Dim x1 as Action(Of Integer) = AddressOf Bar
        Bar(1)
    End Sub
End Class

Module M
    &lt;Extension()&gt;
    Sub Bar(Of T)(ByVal x As IA(Of T), ByVal y As Integer)
        System.Console.WriteLine(4)
    End Sub

    &lt;Extension()&gt;
    Sub Bar(ByVal x As IB, ByVal y As Integer)
        System.Console.WriteLine(5)
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompilationUtils.AssertTheseDiagnostics(CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef),
<expected>
BC30794: No accessible 'Bar' is most specific: 
    Extension method 'Public Sub Bar(y As Integer)' defined in 'M'.
    Extension method 'Public Sub Bar(y As Integer)' defined in 'M'.
        Dim x1 as Action(Of Integer) = AddressOf Bar
                                                 ~~~
BC30521: Overload resolution failed because no accessible 'Bar' is most specific for these arguments:
    Extension method 'Public Sub Bar(y As Integer)' defined in 'M': Not most specific.
    Extension method 'Public Sub Bar(y As Integer)' defined in 'M': Not most specific.
        Bar(1)
        ~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ReceiverTypeGenericity()
            Dim compilationDef =
<compilation name="ReceiverTypeGenericity">
    <file name="a.vb">
Option Strict Off        

Imports System
Imports System.Console
Imports System.Runtime.CompilerServices


Class C1
End Class

Module Ext1
    &lt;Extension()&gt;
	Sub M1(c As C1, x As Integer)
        System.Console.WriteLine("Ext1")
	End Sub
End Module

Module Ext2
    &lt;Extension()&gt;
	Sub M1(Of T)(x As T, y As Integer)
        System.Console.WriteLine("Ext2")
	End Sub
End Module

Module Test
	Sub Main()
		Dim c As New C1()

		' Calls Ext1.M1 since Ext2.M1 target type is more generic.
		c.M1(10)
	End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef, expectedOutput:=
            <![CDATA[
Ext1
]]>)
        End Sub

        <WorkItem(542169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542169")>
        <Fact>
        Public Sub Bug9301()
            Dim compilationDef =
<compilation name="Bug9301">
    <file name="a.vb">
Option Strict Off

Imports System
Imports System.Runtime.CompilerServices


Module Module1
    Sub Main()
        Call 1.Foo()
    End Sub
End Module

Public Module M
    &lt;extension()&gt;
    Sub Foo(ByVal x As Integer)
        System.Console.WriteLine("Foo")
    End Sub
End Module

Namespace System.runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class extensionattribute : Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
Foo
]]>)


            Dim compilationDef2 =
<compilation name="Bug9301_1">
    <file name="a.vb">
Module Module1
    Sub Main()
        Call 1.Foo()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(compilationDef2,
                                                                                                      {MetadataReference.CreateFromImage(verifier.EmittedAssemblyData)},
                                                                                                      TestOptions.ReleaseExe)

            CompilationUtils.AssertNoErrors(compilation)

        End Sub

        <WorkItem(542160, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542160")>
        <Fact>
        Public Sub Bug9290()
            Dim compilationDef =
<compilation name="Bug9290">
    <file name="a.vb">
Option Strict Off

Imports System
Imports System.Runtime.CompilerServices


Module Module1
    Sub Main()
        Call 1.Foo()
    End Sub
End Module

Module M
    &lt;Extension()&gt;
    Sub Foo(ByVal x As Integer)
        System.Console.WriteLine("Foo")
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.All)&gt;
    Class ExtensionAttribute : Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
Foo
]]>)
        End Sub

        <WorkItem(528882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528882")>
        <Fact()>
        Public Sub Bug10184()
            Dim compilationDef1 =
<compilation name="Bug1">
    <file name="a.vb">
Option Strict Off        

Imports System
Imports System.Console
Imports System.Runtime.CompilerServices

Friend Module M
    &lt;Extension()&gt;
    Sub Test(this As Integer)
    End Sub
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>

            Dim compilation1 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef1)

            Dim compilationDef2 =
<compilation name="Bug2">
    <file name="a.vb">
Option Strict Off        

Imports System
Imports System.Console
Imports System.Runtime.CompilerServices

Module M
    Sub Main()
        Call 1.Test()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation2 = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(compilationDef2,
                                                                {New VisualBasicCompilationReference(compilation1)},
                                                                TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation2,
<expected>
BC30456: 'Test' is not a member of 'Integer'.
        Call 1.Test()
             ~~~~~~
</expected>)
        End Sub

        <WorkItem(543743, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543743")>
        <Fact()>
        Public Sub PassPropertyByRefToExtension()
            Dim compilationDef =
<compilation name="PassPropertyByRef">
    <file name="a.vb"><![CDATA[
Option Strict Off
Imports System
Imports System.Runtime.CompilerServices

Module Program
    Sub Main(args As String())
        RefValueProp = "Before"
        RefValueProp.SetRef("After")
        Console.WriteLine(RefValueProp)
    End Sub

    Dim refValuePropVal As String
    Public Property RefValueProp() As String
        Get
            Return refValuePropVal
        End Get
        Set(ByVal value As String)
            refValuePropVal = value
        End Set
    End Property
End Module

Module Extension
    <Extension()> _
    Sub SetRef(ByRef p1 As String, ByVal p2 As String)
        p1 = p2
    End Sub
End Module
]]>
    </file>
</compilation>


            CompileAndVerify(compilationDef, expectedOutput:="After").VerifyIL(
                "Program.Main", <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (String V_0)
  IL_0000:  ldstr      "Before"
  IL_0005:  call       "Sub Program.set_RefValueProp(String)"
  IL_000a:  call       "Function Program.get_RefValueProp() As String"
  IL_000f:  stloc.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  ldstr      "After"
  IL_0017:  call       "Sub Extension.SetRef(ByRef String, String)"
  IL_001c:  ldloc.0
  IL_001d:  call       "Sub Program.set_RefValueProp(String)"
  IL_0022:  call       "Function Program.get_RefValueProp() As String"
  IL_0027:  call       "Sub System.Console.WriteLine(String)"
  IL_002c:  ret
}
]]>)

        End Sub

        <WorkItem(543743, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543743")>
        <Fact()>
        Public Sub PassPropertyByRefToExtension_2()
            Dim compilationDef =
<compilation name="PassPropertyByRef">
    <file name="a.vb"><![CDATA[
Option Strict Off
Imports System
Imports System.Runtime.CompilerServices

Module Program
    Sub Main()
        RefValueProp = "Before"

        With RefValueProp
            .SetRef("After")
        End With

        Console.WriteLine(RefValueProp)
    End Sub

    Dim refValuePropVal As String
    Public Property RefValueProp() As String
        Get
            Return refValuePropVal
        End Get
        Set(ByVal value As String)
            refValuePropVal = value
        End Set
    End Property
End Module

Module Extension
    <Extension()> _
    Sub SetRef(ByRef p1 As String, ByVal p2 As String)
        p1 = p2
    End Sub
End Module
]]>
    </file>
</compilation>

            CompileAndVerify(compilationDef, expectedOutput:="Before")
        End Sub

        <WorkItem(543743, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543743")>
        <Fact()>
        Public Sub PassPropertyByRefToExtension_3()
            Dim compilationDef =
<compilation name="PassPropertyByRef">
    <file name="a.vb"><![CDATA[
Option Strict Off
Imports System
Imports System.Runtime.CompilerServices

Module Program
    Sub Main()
        RefValueProp = "Before"
        Dim d As System.Action(Of String) = AddressOf RefValueProp.SetRef
        d("After")
        Console.WriteLine(RefValueProp)
    End Sub

    Dim refValuePropVal As String
    Public Property RefValueProp() As String
        Get
            Return refValuePropVal
        End Get
        Set(ByVal value As String)
            refValuePropVal = value
        End Set
    End Property
End Module

Module Extension
    <Extension()> _
    Sub SetRef(ByRef p1 As String, ByVal p2 As String)
        p1 = p2
    End Sub
End Module
]]>
    </file>
</compilation>

            CompileAndVerify(compilationDef, expectedOutput:="Before")
        End Sub

    End Class

End Namespace
