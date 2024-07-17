' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class ExtensionTypeTests
        Inherits BasicTestBase

        <Theory>
        <CombinatorialData>
        Public Sub InstanceMethod_01(asStruct As Boolean)

            Dim csSource =
"
public implicit extension E for C
{
    public void Method()
    {
        this.Increment();
    }
}

public " + If(asStruct, "struct", "class") + " C
{
    public int F;

    public void Increment()
    {
        F++;
    }
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource, parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard)).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = new C()
        System.Console.Write(c.F)
        E.Method(c)
        System.Console.Write(c.F)
        E.Method(c)
        System.Console.Write(c.F)
        c.Method()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)

            comp1.AssertTheseDiagnostics(
<expected>
BC30657: 'Method' has a return type that is not supported or parameter types that are not supported.
        E.Method(c)
          ~~~~~~
BC30657: 'Method' has a return type that is not supported or parameter types that are not supported.
        E.Method(c)
          ~~~~~~
BC30456: 'Method' is not a member of 'C'.
        c.Method()
        ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceMethod_02()

            Dim csSource =
"
public implicit extension E for C
{
    public void Method()
    {
        System.Console.Write(1);
    }

    public static void Method(C c)
    {
        System.Console.Write(2);
    }
}

public class C
{
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource, parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard)).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = new C()
        E.Method(c)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)

            ' PROTOTYPE(roles): Latest C# is able to consume both extension methods, but VB cannot.
            comp1.AssertTheseDiagnostics(
<expected>
BC31429: 'Method' is ambiguous because multiple kinds of members with this name exist in structure 'E'.
        E.Method(c)
          ~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceProperty_01()

            Dim csSource =
"
public implicit extension E for C
{
    public int P1
    {
        get => this.P;
        set => this.P = value; 
    }
}

public class C
{
    public int P { get; set; }
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource, parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard)).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = new C()
        E.P1(c) = 2
        System.Console.WriteLine(E.P1(c))
        c.P1 = 2
        System.Console.WriteLine(c.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)

            comp1.AssertTheseDiagnostics(
<expected><![CDATA[
BC30643: Property 'E.P1(<>4__this As C)' is of an unsupported type.
        E.P1(c) = 2
          ~~
BC30643: Property 'E.P1(<>4__this As C)' is of an unsupported type.
        System.Console.WriteLine(E.P1(c))
                                   ~~
BC30456: 'P1' is not a member of 'C'.
        c.P1 = 2
        ~~~~
BC30456: 'P1' is not a member of 'C'.
        System.Console.WriteLine(c.P1)
                                 ~~~~
]]></expected>)
        End Sub

        <Fact>
        Public Sub InstanceProperty_02()

            Dim csSource =
"
public implicit extension E for C
{
    public int P1
    {
        get => this.P;
        set => this.P = value; 
    }
}

public struct C
{
    public int P { get; set; }
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource, parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard)).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = new C()
        Test1(c)
        System.Console.WriteLine(Test2(c))
        c.P1 = 2
        System.Console.WriteLine(c.P1)
    End Sub

    Shared Sub Test1(ByRef c As C)
        E.P1(c) = 2
    End Sub

    Shared Function Test2(ByRef c As C) As Integer
        Return E.P1(c)
    End Function
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)

            comp1.AssertTheseDiagnostics(
<expected><![CDATA[
BC30456: 'P1' is not a member of 'C'.
        c.P1 = 2
        ~~~~
BC30456: 'P1' is not a member of 'C'.
        System.Console.WriteLine(c.P1)
                                 ~~~~
BC30643: Property 'E.P1(ByRef <>4__this As C)' is of an unsupported type.
        E.P1(c) = 2
          ~~
BC30643: Property 'E.P1(ByRef <>4__this As C)' is of an unsupported type.
        Return E.P1(c)
                 ~~
]]></expected>)

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = new C()
        Test1(c)
        System.Console.WriteLine(Test2(c))
    End Sub

    Shared Sub Test1(ByRef c As C)
        E.set_P1(c, 2)
    End Sub

    Shared Function Test2(ByRef c As C) As Integer
        Return E.get_P1(c)
    End Function
End Class
]]></file>
</compilation>

            Dim comp2 = CreateCompilation(source2, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
BC30456: 'set_P1' is not a member of 'E'.
        E.set_P1(c, 2)
        ~~~~~~~~
BC30456: 'get_P1' is not a member of 'E'.
        Return E.get_P1(c)
               ~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub InstanceIndexer_01()

            Dim csSource =
"
public implicit extension E for C
{
    public int this[int i]
    {
        get => this.P;
        set => this.P = value; 
    }
}

public class C
{
    public int P { get; set; }
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource, parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard)).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = new C()
        E(c, 1) = 2
        System.Console.WriteLine(E(c, 1))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)

            comp1.AssertTheseDiagnostics(
<expected>
BC30110: 'E' is a structure type and cannot be used as an expression.
        E(c, 1) = 2
        ~
BC30110: 'E' is a structure type and cannot be used as an expression.
        System.Console.WriteLine(E(c, 1))
                                 ~
</expected>)

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = new C()
        E.set_Item(c, 1, 2)
        System.Console.WriteLine(E.get_Item(c, 1))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp2 = CreateCompilation(source2, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
BC30456: 'set_Item' is not a member of 'E'.
        E.set_Item(c, 1, 2)
        ~~~~~~~~~~
BC30456: 'get_Item' is not a member of 'E'.
        System.Console.WriteLine(E.get_Item(c, 1))
                                 ~~~~~~~~~~
</expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = new C()
        E.Item(c, 1) = 2
        System.Console.WriteLine(E.Item(c, 1))
        c(1) = 2
        System.Console.WriteLine(c(1))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)
            comp3.AssertTheseDiagnostics(
<expected><![CDATA[
BC30643: Property 'E.Item(<>4__this As C, i As Integer)' is of an unsupported type.
        E.Item(c, 1) = 2
          ~~~~
BC30643: Property 'E.Item(<>4__this As C, i As Integer)' is of an unsupported type.
        System.Console.WriteLine(E.Item(c, 1))
                                   ~~~~
BC30367: Class 'C' cannot be indexed because it has no default property.
        c(1) = 2
        ~
BC30367: Class 'C' cannot be indexed because it has no default property.
        System.Console.WriteLine(c(1))
                                 ~
]]></expected>)
        End Sub

        <Fact>
        Public Sub InstanceIndexer_02()

            Dim csSource =
"
public implicit extension E for C
{
    public int this[int i]
    {
        get => this.P;
        set => this.P = value; 
    }
}

public struct C
{
    public int P { get; set; }
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource, parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard)).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = new C()
        E(c, 1) = 2
        System.Console.WriteLine(E(c, 1))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)

            comp1.AssertTheseDiagnostics(
<expected>
BC30110: 'E' is a structure type and cannot be used as an expression.
        E(c, 1) = 2
        ~
BC30110: 'E' is a structure type and cannot be used as an expression.
        System.Console.WriteLine(E(c, 1))
                                 ~
</expected>)

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = new C()
        E.set_Item(c, 1, 2)
        System.Console.WriteLine(E.get_Item(c, 1))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp2 = CreateCompilation(source2, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
BC30456: 'set_Item' is not a member of 'E'.
        E.set_Item(c, 1, 2)
        ~~~~~~~~~~
BC30456: 'get_Item' is not a member of 'E'.
        System.Console.WriteLine(E.get_Item(c, 1))
                                 ~~~~~~~~~~
</expected>)

            Dim source3 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = new C()
        E.Item(c, 1) = 2
        System.Console.WriteLine(E.Item(c, 1))
        c(1) = 2
        System.Console.WriteLine(c(1))
    End Sub
End Class
]]></file>
</compilation>

            Dim comp3 = CreateCompilation(source3, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)
            comp3.AssertTheseDiagnostics(
<expected><![CDATA[
BC30643: Property 'E.Item(ByRef <>4__this As C, i As Integer)' is of an unsupported type.
        E.Item(c, 1) = 2
          ~~~~
BC30643: Property 'E.Item(ByRef <>4__this As C, i As Integer)' is of an unsupported type.
        System.Console.WriteLine(E.Item(c, 1))
                                   ~~~~
BC30690: Structure 'C' cannot be indexed because it has no default property.
        c(1) = 2
        ~
BC30690: Structure 'C' cannot be indexed because it has no default property.
        System.Console.WriteLine(c(1))
                                 ~
]]></expected>)
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub InstanceEvent_01(asStruct As Boolean)

            Dim csSource =
"
public implicit extension E for C
{
    public event System.Action E1
    {
        add => this.E += value;
        remove => this.E -= value; 
    }
}

public " + If(asStruct, "struct", "class") + " C
{
    public event System.Action E;

    public void Fire() => E();

    public static System.Action M2() => (() => System.Console.Write(2));
    public static System.Action M3() => (() => System.Console.Write(3));
}
"

            Dim csCompilation = CreateCSharpCompilation(csSource, parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.Preview),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard)).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = new C()
        AddHandler E.E1, C.M2()
        c.Fire()
        AddHandler E.E1, C.M3()
        RemoveHandler E.E1, C.M2()
        c.Fire()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)

            comp1.AssertTheseDiagnostics(
<expected><![CDATA[
BC30657: 'E1' has a return type that is not supported or parameter types that are not supported.
        AddHandler E.E1, C.M2()
                   ~~~~
BC30657: 'E1' has a return type that is not supported or parameter types that are not supported.
        AddHandler E.E1, C.M3()
                   ~~~~
BC30657: 'E1' has a return type that is not supported or parameter types that are not supported.
        RemoveHandler E.E1, C.M2()
                      ~~~~
]]></expected>)

            Dim source2 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = new C()
        E.add_E1(c, C.M2())
        c.Fire()
        E.add_E1(c, C.M3())
        E.remove_E1(c, C.M2())
        c.Fire()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp2 = CreateCompilation(source2, targetFramework:=TargetFramework.Standard, references:={csCompilation}, options:=TestOptions.DebugExe)

            comp2.AssertTheseDiagnostics(
<expected>
BC30456: 'add_E1' is not a member of 'E'.
        E.add_E1(c, C.M2())
        ~~~~~~~~
BC30456: 'add_E1' is not a member of 'E'.
        E.add_E1(c, C.M3())
        ~~~~~~~~
BC30456: 'remove_E1' is not a member of 'E'.
        E.remove_E1(c, C.M2())
        ~~~~~~~~~~~
</expected>)
        End Sub

    End Class
End Namespace
