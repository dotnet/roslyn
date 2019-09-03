' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    <CompilerTrait(CompilerFeature.DefaultInterfaceImplementation)>
    Public Class DefaultInterfaceImplementationTests
        Inherits BasicTestBase

        Private Function GetCSharpCompilation(
            csSource As String,
            Optional additionalReferences As MetadataReference() = Nothing,
            Optional targetFramework As TargetFramework = TargetFramework.NetStandardLatest,
            Optional compilationOptions As CSharp.CSharpCompilationOptions = Nothing
        ) As CSharp.CSharpCompilation
            Return CreateCSharpCompilation(csSource,
                                           parseOptions:=CSharp.CSharpParseOptions.Default.WithLanguageVersion(CSharp.LanguageVersion.CSharp8),
                                           referencedAssemblies:=TargetFrameworkUtil.GetReferences(targetFramework, additionalReferences),
                                           compilationOptions:=compilationOptions)
        End Function

        Private Shared ReadOnly Property VerifyOnMonoOrCoreClr As Verification
            Get
                Return If(ExecutionConditionUtil.IsMonoOrCoreClr, Verification.Passes, Verification.Skipped)
            End Get
        End Property

        <Fact>
        <WorkItem(35820, "https://github.com/dotnet/roslyn/issues/35820")>
        Public Sub MethodImplementation_01()

            Dim csSource =
"
public interface I1
{
    void M1() 
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'Sub M1()' for interface 'I1'.
    Implements I1
               ~~
</errors>
            )
        End Sub

        <Fact>
        <WorkItem(35821, "https://github.com/dotnet/roslyn/issues/35821")>
        Public Sub MethodImplementation_02()

            Dim csSource =
"
public interface I1
{
    void M1() 
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Sub M1() Implements I1.M1
        System.Console.WriteLine("C.M1")
    End Sub

    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub MethodImplementation_03()

            Dim csSource =
"
public interface I1
{
    void M1();
}

public interface I2 : I1
{
    void I1.M1()
    {}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'Sub M1()' for interface 'I1'.
    Implements I2
               ~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub MethodImplementation_04()

            Dim csSource =
"
public interface I1
{
    void M1();
}

public interface I2 : I1
{
    void I1.M1()
    {}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2

    Sub M1() Implements I1.M1
        System.Console.WriteLine("C.M1")
    End Sub

    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35823, "https://github.com/dotnet/roslyn/issues/35823")>
        Public Sub MethodImplementation_05()

            Dim csSource =
"
public interface I1
{
    void M1();
}

public interface I2 : I1
{
    abstract void I1.M1();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'Sub M1()' for interface 'I1'.
    Implements I2
               ~~
</errors>
            )
        End Sub

        <Fact>
        <WorkItem(35823, "https://github.com/dotnet/roslyn/issues/35823")>
        Public Sub MethodImplementation_06()

            Dim csSource =
"
public interface I1
{
    void M1();
}

public interface I2 : I1
{
    abstract void I1.M1();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2

    Sub M1() Implements I1.M1
        System.Console.WriteLine("C.M1")
    End Sub

    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub MethodImplementation_07()

            Dim csSource =
"
public interface I1
{
    internal void M1();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Sub M1() Implements I1.M1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})

            comp1.AssertTheseDiagnostics(
<errors>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Friend'.
    Sub M1() Implements I1.M1
                        ~~~~~
</errors>
            )
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub MethodImplementation_08()

            Dim csSource =
"
public interface I1
{
    protected void M1();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.M1()
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Sub M1() Implements I1.M1
        System.Console.WriteLine("C.M1")
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub


        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub MethodImplementation_09()

            Dim csSource =
"
public interface I1
{
    protected internal void M1();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.M1()
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Sub M1() Implements I1.M1
        System.Console.WriteLine("C.M1")
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub MethodImplementation_10()

            Dim csSource =
"
public interface I1
{
    private protected void M1();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Sub M1() Implements I1.M1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})

            comp1.AssertTheseDiagnostics(
<errors>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Private Protected'.
    Sub M1() Implements I1.M1
                        ~~~~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub MethodImplementation_11()

            Dim csSource =
"
public interface I1
{
    static string M1()
    {
        return ""I1.M1"";
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        System.Console.WriteLine(I1.M1())
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="I1.M1")
        End Sub

        <Fact>
        Public Sub MethodImplementation_12()

            Dim csSource =
"
public interface I1
{
    internal static void M1()
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'M1' is not a member of 'I1'.
        I1.M1()
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Friend'.
        I1.M1()
        ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub MethodImplementation_13()

            Dim csSource =
"
public interface I1
{
    protected static void M1()
    {
        System.Console.WriteLine(""I1.M1"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub MethodImplementation_14()

            Dim csSource =
"
public interface I1
{
    protected internal static void M1()
    {
        System.Console.WriteLine(""I1.M1"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub MethodImplementation_15()

            Dim csSource =
"
public interface I1
{
    private protected static void M1()
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Private Protected'.
        I1.M1()
        ~~~~~
</error>)
            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Private Protected'.
        I1.M1()
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub MethodImplementation_16()

            Dim csSource =
"
public interface I1
{
    protected static void M1()
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Protected'.
        I1.M1()
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub MethodImplementation_17()

            Dim csSource =
"
public interface I1
{
    protected internal static void M1()
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Protected Friend'.
        I1.M1()
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub MethodImplementation_18()

            Dim csSource =
"
public interface I1
{
    private static void M1()
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'M1' is not a member of 'I1'.
        I1.M1()
        ~~~~~
</error>)
            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Private'.
        I1.M1()
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub MethodImplementation_19()

            Dim csSource =
"
public interface I1
{
    sealed void M1()
    {
        System.Console.WriteLine(""I1.M1"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 as I1 = New Test()
        i1.M1()
    End Sub
End Class

Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub MethodImplementation_20()

            Dim csSource =
"
public interface I1
{
    internal sealed void M1()
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 as I1 = New Test()
        i1.M1()
    End Sub
End Class

Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'M1' is not a member of 'I1'.
        i1.M1()
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Friend'.
        i1.M1()
        ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub MethodImplementation_21()

            Dim csSource =
"
public interface I1
{
    protected sealed void M1()
    {
        System.Console.WriteLine(""I1.M1"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            i2.M1()
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub MethodImplementation_22()

            Dim csSource =
"
public interface I1
{
    protected internal sealed void M1()
    {
        System.Console.WriteLine(""I1.M1"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            i2.M1()
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub MethodImplementation_23()

            Dim csSource =
"
public interface I1
{
    private protected sealed void M1()
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 as I1 = New C()
        i1.M1()
    End Sub
End Class

Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Private Protected'.
        i1.M1()
        ~~~~~
</error>)
            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Private Protected'.
        i1.M1()
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub MethodImplementation_24()

            Dim csSource =
"
public interface I1
{
    protected sealed void M1()
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 as I1 = New Test()
        i1.M1()
    End Sub
End Class

Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Protected'.
        i1.M1()
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub MethodImplementation_25()

            Dim csSource =
"
public interface I1
{
    protected sealed void M1()
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 as I1 = New C()
        i1.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Protected'.
        i1.M1()
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub MethodImplementation_26()

            Dim csSource =
"
public interface I1
{
    protected internal sealed void M1()
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 as I1 = New Test()
        i1.M1()
    End Sub
End Class

Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Protected Friend'.
        i1.M1()
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub MethodImplementation_27()

            Dim csSource =
"
public interface I1
{
    protected internal sealed void M1()
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 as I1 = New C()
        i1.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Protected Friend'.
        i1.M1()
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub MethodImplementation_28()

            Dim csSource =
"
public interface I1
{
    private void M1()
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 as I1 = New Test()
        i1.M1()
    End Sub
End Class

Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'M1' is not a member of 'I1'.
        i1.M1()
        ~~~~~
</error>)
            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30390: 'I1.Sub M1()' is not accessible in this context because it is 'Private'.
        i1.M1()
        ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub MethodImplementation_29()

            Dim csSource =
"
public interface I1
{
    protected static string M1() => throw null;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        System.Console.WriteLine(I1.M1())
        Dim d1 as System.Func(Of String) = AddressOf I1.M1
        Dim d2 = New System.Func(Of String)(AddressOf I1.M1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(I1.M1())
                                 ~~~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        Dim d1 as System.Func(Of String) = AddressOf I1.M1
                                                     ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        Dim d2 = New System.Func(Of String)(AddressOf I1.M1)
                                                      ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub MethodImplementation_30()

            Dim csSource =
"
public interface I1
{
    protected internal static string M1()
    {
        return ""I1.M1"";
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        System.Console.WriteLine(I1.M1())
        Dim d1 as System.Func(Of String) = AddressOf I1.M1
        Dim d2 = New System.Func(Of String)(AddressOf I1.M1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(I1.M1())
                                 ~~~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        Dim d1 as System.Func(Of String) = AddressOf I1.M1
                                                     ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        Dim d2 = New System.Func(Of String)(AddressOf I1.M1)
                                                      ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35885, "https://github.com/dotnet/roslyn/issues/35885")>
        Public Sub MethodImplementation_31()

            Dim csSource =
"
public interface I1
{
    sealed string M1() => ""I1.M1"";
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 as I1 = New Test()
        i1.M1()
        Dim d1 as System.Func(Of String) = AddressOf i1.M1
        Dim d2 = New System.Func(Of String)(AddressOf i1.M1)
    End Sub
End Class

Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37309: Target runtime doesn't support default interface implementation.
        i1.M1()
        ~~~~~~~
BC37309: Target runtime doesn't support default interface implementation.
        Dim d1 as System.Func(Of String) = AddressOf i1.M1
                                                     ~~~~~
BC37309: Target runtime doesn't support default interface implementation.
        Dim d2 = New System.Func(Of String)(AddressOf i1.M1)
                                                      ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub MethodImplementation_32()

            Dim csSource =
"
public interface I1
{
    protected void M1();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            i2.M1()
            Dim d1 as System.Action = AddressOf i2.M1
            Dim d2 = New System.Action(AddressOf i2.M1)
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Sub M1() Implements I1.M1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            i2.M1()
            ~~~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            Dim d1 as System.Action = AddressOf i2.M1
                                                ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            Dim d2 = New System.Action(AddressOf i2.M1)
                                                 ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub MethodImplementation_33()

            Dim csSource =
"
public interface I1
{
    protected internal void M1();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1

        Shared Sub Main()
            Dim i2 as I2 = New C()
            i2.M1()
            Dim d1 as System.Action = AddressOf i2.M1
            Dim d2 = New System.Action(AddressOf i2.M1)
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Sub M1() Implements I1.M1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            i2.M1()
            ~~~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            Dim d1 as System.Action = AddressOf i2.M1
                                                ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            Dim d2 = New System.Action(AddressOf i2.M1)
                                                 ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Field_01()

            Dim csSource =
"
public interface I1
{
    static string M1 = ""I1.M1"";
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        System.Console.WriteLine(I1.M1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub Field_02()

            Dim csSource =
"
public interface I1
{
    internal static string M1 = ""I1.M1"";
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        System.Console.WriteLine(I1.M1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'M1' is not a member of 'I1'.
        System.Console.WriteLine(I1.M1)
                                 ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.M1' is not accessible in this context because it is 'Friend'.
        System.Console.WriteLine(I1.M1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub Field_03()

            Dim csSource =
"
public interface I1
{
    protected static string M1 = ""I1.M1"";
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        System.Console.WriteLine(I1.M1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub Field_04()

            Dim csSource =
"
public interface I1
{
    protected internal static string M1 = ""I1.M1"";
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        System.Console.WriteLine(I1.M1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub Field_05()

            Dim csSource =
"
public interface I1
{
    private protected static string M1 = ""I1.M1"";
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        System.Console.WriteLine(I1.M1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.M1' is not accessible in this context because it is 'Private Protected'.
        System.Console.WriteLine(I1.M1)
                                 ~~~~~
</error>)
            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.M1' is not accessible in this context because it is 'Private Protected'.
        System.Console.WriteLine(I1.M1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub Field_06()

            Dim csSource =
"
public interface I1
{
    protected static string M1 = ""I1.M1"";
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        System.Console.WriteLine(I1.M1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.M1' is not accessible in this context because it is 'Protected'.
        System.Console.WriteLine(I1.M1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub Field_07()

            Dim csSource =
"
public interface I1
{
    protected internal static string M1 = ""I1.M1"";
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        System.Console.WriteLine(I1.M1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.M1' is not accessible in this context because it is 'Protected Friend'.
        System.Console.WriteLine(I1.M1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub Field_08()

            Dim csSource =
"
public interface I1
{
    private static string M1 = ""I1.M1"";
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        System.Console.WriteLine(I1.M1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'M1' is not a member of 'I1'.
        System.Console.WriteLine(I1.M1)
                                 ~~~~~
</error>)
            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.M1' is not accessible in this context because it is 'Private'.
        System.Console.WriteLine(I1.M1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub Field_09()

            Dim csSource =
"
public interface I1
{
    protected static string M1 = ""I1.M1"";
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        System.Console.WriteLine(I1.M1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(I1.M1)
                                 ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub Field_10()

            Dim csSource =
"
public interface I1
{
    protected internal static string M1 = ""I1.M1"";
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        System.Console.WriteLine(I1.M1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(I1.M1)
                                 ~~~~~
</expected>)
        End Sub

        Private Const NoPiaAttributes As String = "
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
    public sealed class GuidAttribute : Attribute
    {
        public GuidAttribute(string guid){}
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public sealed class PrimaryInteropAssemblyAttribute : Attribute
    {
        public PrimaryInteropAssemblyAttribute(int major, int minor){}
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    public sealed class ComImportAttribute : Attribute
    {
        public ComImportAttribute(){}
    }

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
    public sealed class TypeIdentifierAttribute : Attribute
    {
        public TypeIdentifierAttribute(){}
        public TypeIdentifierAttribute(string scope, string identifier){}
    }
}
"

        <Fact>
        <WorkItem(35852, "https://github.com/dotnet/roslyn/issues/35852")>
        Public Sub NoPia_01()
            Dim attributesRef = GetCSharpCompilation(NoPiaAttributes).EmitToImageReference()

            Dim csSource =
"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    void M1(){}
}
"

            Dim csCompilation = GetCSharpCompilation(csSource, {attributesRef}).EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
class UsePia7 
    Implements ITest33
    Sub M1() Implements ITest33.M1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={attributesRef, csCompilation})
            comp1.AssertTheseEmitDiagnostics(
<expected>
BC37307: Type 'ITest33' cannot be embedded because it has a non-abstract member. Consider setting the 'Embed Interop Types' property to false.
    Implements ITest33
               ~~~~~~~
BC37307: Type 'ITest33' cannot be embedded because it has a non-abstract member. Consider setting the 'Embed Interop Types' property to false.
    Sub M1() Implements ITest33.M1
                        ~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35852, "https://github.com/dotnet/roslyn/issues/35852")>
        Public Sub NoPia_02()
            Dim attributesRef = GetCSharpCompilation(NoPiaAttributes).EmitToImageReference()

            Dim csSource =
"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    void M1(){}
}
"

            Dim csCompilation = GetCSharpCompilation(csSource, {attributesRef}).EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
class UsePia 
    Sub Main(x as ITest33)
        x.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={attributesRef, csCompilation})
            comp1.AssertTheseEmitDiagnostics(
<expected>
BC37307: Type 'ITest33' cannot be embedded because it has a non-abstract member. Consider setting the 'Embed Interop Types' property to false.
    Sub Main(x as ITest33)
                  ~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35852, "https://github.com/dotnet/roslyn/issues/35852")>
        Public Sub NoPia_03()
            Dim attributesRef = GetCSharpCompilation(NoPiaAttributes).EmitToImageReference()

            Dim csSource =
"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    void M1(){}
    void M2();
}
"

            Dim csCompilation = GetCSharpCompilation(csSource, {attributesRef}).EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
class UsePia 
    Sub Main(x as ITest33)
        x.M2()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={attributesRef, csCompilation})
            comp1.AssertTheseEmitDiagnostics(
<expected>
BC37307: Type 'ITest33' cannot be embedded because it has a non-abstract member. Consider setting the 'Embed Interop Types' property to false.
    Sub Main(x as ITest33)
                  ~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35852, "https://github.com/dotnet/roslyn/issues/35852")>
        Public Sub NoPia_04()
            Dim attributesRef = GetCSharpCompilation(NoPiaAttributes).EmitToImageReference()

            Dim csSource =
"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    sealed void M1(){}
}
"

            Dim csCompilation = GetCSharpCompilation(csSource, {attributesRef}).EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
class UsePia 
    Sub Main(x as ITest33)
        x.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={attributesRef, csCompilation})
            comp1.AssertTheseEmitDiagnostics(
<expected>
BC37307: Type 'ITest33' cannot be embedded because it has a non-abstract member. Consider setting the 'Embed Interop Types' property to false.
    Sub Main(x as ITest33)
                  ~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35852, "https://github.com/dotnet/roslyn/issues/35852")>
        Public Sub NoPia_05()
            Dim attributesRef = GetCSharpCompilation(NoPiaAttributes).EmitToImageReference()

            Dim csSource =
"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    static void M1(){}
}
"

            Dim csCompilation = GetCSharpCompilation(csSource, {attributesRef}).EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
class UsePia 
    Sub Main()
        ITest33.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={attributesRef, csCompilation})
            comp1.AssertTheseEmitDiagnostics(
<expected>
BC37307: Type 'ITest33' cannot be embedded because it has a non-abstract member. Consider setting the 'Embed Interop Types' property to false.
        ITest33.M1()
        ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub NoPia_06()
            Dim attributesRef = GetCSharpCompilation(NoPiaAttributes).EmitToImageReference()

            Dim csSource =
"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    [ComImport()]
    [Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
    public interface I1
    {
    }
}
"

            Dim csCompilation = GetCSharpCompilation(csSource, {attributesRef}).EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
class UsePia 
    Sub Main(x as ITest33.I1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={attributesRef, csCompilation})
            comp1.AssertTheseEmitDiagnostics(
<error>
BC31558: Nested type 'ITest33.I1' cannot be embedded.
    Sub Main(x as ITest33.I1)
                  ~~~~~~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35852, "https://github.com/dotnet/roslyn/issues/35852")>
        Public Sub NoPia_07()
            Dim attributesRef = GetCSharpCompilation(NoPiaAttributes).EmitToImageReference()

            Dim csSource =
"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    static int F1;
}
"

            Dim csCompilation = GetCSharpCompilation(csSource, {attributesRef}).EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
class UsePia 
    Sub Main()
        Dim x = ITest33.F1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={attributesRef, csCompilation})
            comp1.AssertTheseEmitDiagnostics(
<error>
BC37307: Type 'ITest33' cannot be embedded because it has a non-abstract member. Consider setting the 'Embed Interop Types' property to false.
        Dim x = ITest33.F1
                ~~~~~~~~~~
</error>
            )
        End Sub

        <Fact>
        <WorkItem(35852, "https://github.com/dotnet/roslyn/issues/35852")>
        Public Sub NoPia_08()
            Dim attributesRef = GetCSharpCompilation(NoPiaAttributes).EmitToImageReference()

            Dim csSource =
"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    void M1();
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest44 : ITest33
{
    void ITest33.M1(){}
}
"

            Dim csCompilation = GetCSharpCompilation(csSource, {attributesRef}).EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
class UsePia 
    Sub Main(x as ITest44)
        x.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={attributesRef, csCompilation})
            comp1.AssertTheseEmitDiagnostics(
<expected>
BC37307: Type 'ITest44' cannot be embedded because it has a non-abstract member. Consider setting the 'Embed Interop Types' property to false.
    Sub Main(x as ITest44)
                  ~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35911, "https://github.com/dotnet/roslyn/issues/35911")>
        Public Sub NoPia_10()
            Dim attributesRef = GetCSharpCompilation(NoPiaAttributes).EmitToImageReference()

            Dim csSource =
"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    void M1();
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest44 : ITest33
{
    abstract void ITest33.M1();
}
"

            Dim csCompilation = GetCSharpCompilation(csSource, {attributesRef}).EmitToImageReference(embedInteropTypes:=True)

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
class UsePia 
    Sub Main(x as ITest44)
        x.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={attributesRef, csCompilation})
            comp1.AssertTheseEmitDiagnostics(
<expected>
BC37308: Type 'ITest44' cannot be embedded because it has a re-abstraction of a member from base interface. Consider setting the 'Embed Interop Types' property to false.
    Sub Main(x as ITest44)
                  ~~~~~~~
</expected>)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NoPiaNeedsDesktop)>
        Public Sub NoPia_09()
            Dim attributesRef = GetCSharpCompilation(NoPiaAttributes).EmitToImageReference()

            Dim pia =
"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    void M1();

    public interface ITest55
    {
        void M2();
    }
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest44 : ITest33
{
    void ITest33.M1(){}
}
"

            Dim piaReference = GetCSharpCompilation(pia, {attributesRef}).EmitToImageReference(embedInteropTypes:=True)

            Dim consumer1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public class UsePia 
    Public Shared Sub Test(x as ITest33)
        x.M1()
    End Sub
End Class
]]></file>
</compilation>

            Dim consumer2 =
<compilation>
    <file name="c.vb"><![CDATA[
class Test 
    Implements ITest33

    public shared Sub Main()
        UsePia.Test(new Test())
    End Sub

    Sub M1() Implements ITest33.M1
        System.Console.WriteLine("Test.M1")
    End Sub
End Class
]]></file>
</compilation>


            Dim pia2 As String =
"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: PrimaryInteropAssemblyAttribute(1,1)]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58279"")]
public interface ITest33
{
    void M1();
}
"

            Dim pia2Refernce = GetCSharpCompilation(pia2, {attributesRef}).EmitToImageReference()

            Dim compilation1 = CreateCompilation(consumer1, options:=TestOptions.ReleaseDll, references:={piaReference})

            For Each reference2 In {compilation1.ToMetadataReference(), compilation1.EmitToImageReference()}
                Dim compilation2 = CreateCompilation(consumer2, options:=TestOptions.ReleaseExe, references:={reference2, pia2Refernce})
                CompileAndVerify(compilation2, expectedOutput:="Test.M1")
            Next
        End Sub

        <Fact>
        Public Sub NestedTypes_01()

            Dim csSource =
"
public interface I1
{
    interface T1
    {
        void M1();
    }

    class T2
    {}

    struct T3
    {}

    enum T4
    {
        B
    }

    delegate void T5();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource, targetFramework:=TargetFramework.Standard).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test1

    Shared Sub Main()
        Dim a As I1.T1 = new Test2()
        a.M1()
        System.Console.WriteLine(new I1.T2())
        System.Console.WriteLine(new I1.T3())
        System.Console.WriteLine(I1.T4.B.ToString())
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
    End Sub
End Class

Public Class Test2
    Implements I1.T1
    Sub M1() Implements I1.T1.M1
        System.Console.WriteLine("M1")
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.Standard, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=
"M1
I1+T2
I1+T3
B
I1+T5")
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub NestedTypes_02()

            Dim csSource =
"
public interface I1
{
    protected interface T1
    {
        void M1();
    }

    protected class T2
    {}

    protected struct T3
    {}

    protected enum T4
    {
        B
    }

    protected delegate void T5();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test1
    Implements I1

    Shared Sub Main()
        Dim a As I1.T1 = new Test2()
        a.M1()
        System.Console.WriteLine(new I1.T2())
        System.Console.WriteLine(new I1.T3())
        System.Console.WriteLine(I1.T4.B.ToString())
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
    End Sub

    Public Class Test2
        Implements I1.T1
        Sub M1() Implements I1.T1.M1
            System.Console.WriteLine("M1")
        End Sub
    End Class
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"M1
I1+T2
I1+T3
B
I1+T5", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub NestedTypes_03()

            Dim csSource =
"
public interface I1
{
    protected interface T1
    {
        void M1();
    }

    protected class T2
    {}

    protected struct T3
    {}

    protected enum T4
    {
        B
    }

    protected delegate void T5();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test1

    Shared Sub Main()
        Dim a As I1.T1 = new Test1()
        a.M1()
        System.Console.WriteLine(new I1.T2())
        System.Console.WriteLine(new I1.T3())
        System.Console.WriteLine(I1.T4.B.ToString())
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
    End Sub

    Public Class Test2
        Implements I1.T1
        Sub M1() Implements I1.T1.M1
            System.Console.WriteLine("M1")
        End Sub
    End Class
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.T1' is not accessible in this context because it is 'Protected'.
        Dim a As I1.T1 = new Test1()
                 ~~~~~
BC30389: 'I1.T2' is not accessible in this context because it is 'Protected'.
        System.Console.WriteLine(new I1.T2())
                                     ~~~~~
BC30389: 'I1.T3' is not accessible in this context because it is 'Protected'.
        System.Console.WriteLine(new I1.T3())
                                     ~~~~~
BC30389: 'I1.T4' is not accessible in this context because it is 'Protected'.
        System.Console.WriteLine(I1.T4.B.ToString())
                                 ~~~~~
BC30389: 'I1.T5' is not accessible in this context because it is 'Protected'.
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
                                     ~~~~~
BC30389: 'I1.T1' is not accessible in this context because it is 'Protected'.
        Implements I1.T1
                   ~~~~~
BC30389: 'I1.T1' is not accessible in this context because it is 'Protected'.
        Sub M1() Implements I1.T1.M1
                            ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub NestedTypes_04()

            Dim csSource =
"
public interface I1
{
    protected interface T1
    {
        void M1();
    }

    protected class T2
    {}

    protected struct T3
    {}

    protected enum T4
    {
        B
    }

    protected delegate void T5();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test1
    Implements I1

    Shared Sub Main()
        Dim a As I1.T1 = new Test1()
        a.M1()
        System.Console.WriteLine(new I1.T2())
        System.Console.WriteLine(new I1.T3())
        System.Console.WriteLine(I1.T4.B.ToString())
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
    End Sub


    Public Class Test2
        Implements I1.T1
        Sub M1() Implements I1.T1.M1
            System.Console.WriteLine("M1")
        End Sub
    End Class
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        Dim a As I1.T1 = new Test1()
                 ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(new I1.T2())
                                     ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(new I1.T3())
                                     ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(I1.T4.B.ToString())
                                 ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
                                     ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        Implements I1.T1
                   ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        Sub M1() Implements I1.T1.M1
                            ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub NestedTypes_05()

            Dim csSource =
"
public interface I1
{
    protected internal interface T1
    {
        void M1();
    }

    protected internal class T2
    {}

    protected internal struct T3
    {}

    protected internal enum T4
    {
        B
    }

    protected delegate void T5();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test1
    Implements I1

    Shared Sub Main()
        Dim a As I1.T1 = new Test2()
        a.M1()
        System.Console.WriteLine(new I1.T2())
        System.Console.WriteLine(new I1.T3())
        System.Console.WriteLine(I1.T4.B.ToString())
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
    End Sub

    Public Class Test2
        Implements I1.T1
        Sub M1() Implements I1.T1.M1
            System.Console.WriteLine("M1")
        End Sub
    End Class
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"M1
I1+T2
I1+T3
B
I1+T5", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub NestedTypes_06()

            Dim csSource =
"
public interface I1
{
    protected internal interface T1
    {
        void M1();
    }

    protected internal class T2
    {}

    protected internal struct T3
    {}

    protected internal enum T4
    {
        B
    }

    protected internal delegate void T5();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test1

    Shared Sub Main()
        Dim a As I1.T1 = new Test1()
        a.M1()
        System.Console.WriteLine(new I1.T2())
        System.Console.WriteLine(new I1.T3())
        System.Console.WriteLine(I1.T4.B.ToString())
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
    End Sub


    Public Class Test2
        Implements I1.T1
        Sub M1() Implements I1.T1.M1
            System.Console.WriteLine("M1")
        End Sub
    End Class
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.T1' is not accessible in this context because it is 'Protected Friend'.
        Dim a As I1.T1 = new Test1()
                 ~~~~~
BC30389: 'I1.T2' is not accessible in this context because it is 'Protected Friend'.
        System.Console.WriteLine(new I1.T2())
                                     ~~~~~
BC30389: 'I1.T3' is not accessible in this context because it is 'Protected Friend'.
        System.Console.WriteLine(new I1.T3())
                                     ~~~~~
BC30389: 'I1.T4' is not accessible in this context because it is 'Protected Friend'.
        System.Console.WriteLine(I1.T4.B.ToString())
                                 ~~~~~
BC30389: 'I1.T5' is not accessible in this context because it is 'Protected Friend'.
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
                                     ~~~~~
BC30389: 'I1.T1' is not accessible in this context because it is 'Protected Friend'.
        Implements I1.T1
                   ~~~~~
BC30389: 'I1.T1' is not accessible in this context because it is 'Protected Friend'.
        Sub M1() Implements I1.T1.M1
                            ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub NestedTypes_07()

            Dim csSource =
"
public interface I1
{
    protected internal interface T1
    {
        void M1();
    }

    protected internal class T2
    {}

    protected internal struct T3
    {}

    protected internal enum T4
    {
        B
    }

    protected internal delegate void T5();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test1
    Implements I1

    Shared Sub Main()
        Dim a As I1.T1 = new Test1()
        a.M1()
        System.Console.WriteLine(new I1.T2())
        System.Console.WriteLine(new I1.T3())
        System.Console.WriteLine(I1.T4.B.ToString())
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
    End Sub


    Public Class Test2
        Implements I1.T1
        Sub M1() Implements I1.T1.M1
            System.Console.WriteLine("M1")
        End Sub
    End Class
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        Dim a As I1.T1 = new Test1()
                 ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(new I1.T2())
                                     ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(new I1.T3())
                                     ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(I1.T4.B.ToString())
                                 ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
                                     ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        Implements I1.T1
                   ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        Sub M1() Implements I1.T1.M1
                            ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub NestedTypes_08()

            Dim csSource =
"
public interface I1
{
    internal interface T1
    {
        void M1();
    }

    internal class T2
    {}

    internal struct T3
    {}

    internal enum T4
    {
        B
    }

    internal delegate void T5();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test1
    Implements I1

    Shared Sub Main()
        Dim a As I1.T1 = new Test1()
        a.M1()
        System.Console.WriteLine(new I1.T2())
        System.Console.WriteLine(new I1.T3())
        System.Console.WriteLine(I1.T4.B.ToString())
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
    End Sub


    Public Class Test2
        Implements I1.T1
        Sub M1() Implements I1.T1.M1
            System.Console.WriteLine("M1")
        End Sub
    End Class
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.T1' is not accessible in this context because it is 'Friend'.
        Dim a As I1.T1 = new Test1()
                 ~~~~~
BC30389: 'I1.T2' is not accessible in this context because it is 'Friend'.
        System.Console.WriteLine(new I1.T2())
                                     ~~~~~
BC30389: 'I1.T3' is not accessible in this context because it is 'Friend'.
        System.Console.WriteLine(new I1.T3())
                                     ~~~~~
BC30389: 'I1.T4' is not accessible in this context because it is 'Friend'.
        System.Console.WriteLine(I1.T4.B.ToString())
                                 ~~~~~
BC30389: 'I1.T5' is not accessible in this context because it is 'Friend'.
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
                                     ~~~~~
BC30389: 'I1.T1' is not accessible in this context because it is 'Friend'.
        Implements I1.T1
                   ~~~~~
BC30389: 'I1.T1' is not accessible in this context because it is 'Friend'.
        Sub M1() Implements I1.T1.M1
                            ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub NestedTypes_09()

            Dim csSource =
"
public interface I1
{
    private interface T1
    {
        void M1();
    }

    private class T2
    {}

    private struct T3
    {}

    private enum T4
    {
        B
    }

    private delegate void T5();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test1
    Implements I1

    Shared Sub Main()
        Dim a As I1.T1 = new Test1()
        a.M1()
        System.Console.WriteLine(new I1.T2())
        System.Console.WriteLine(new I1.T3())
        System.Console.WriteLine(I1.T4.B.ToString())
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
    End Sub


    Public Class Test2
        Implements I1.T1
        Sub M1() Implements I1.T1.M1
            System.Console.WriteLine("M1")
        End Sub
    End Class
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.T1' is not accessible in this context because it is 'Private'.
        Dim a As I1.T1 = new Test1()
                 ~~~~~
BC30389: 'I1.T2' is not accessible in this context because it is 'Private'.
        System.Console.WriteLine(new I1.T2())
                                     ~~~~~
BC30389: 'I1.T3' is not accessible in this context because it is 'Private'.
        System.Console.WriteLine(new I1.T3())
                                     ~~~~~
BC30389: 'I1.T4' is not accessible in this context because it is 'Private'.
        System.Console.WriteLine(I1.T4.B.ToString())
                                 ~~~~~
BC30389: 'I1.T5' is not accessible in this context because it is 'Private'.
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
                                     ~~~~~
BC30389: 'I1.T1' is not accessible in this context because it is 'Private'.
        Implements I1.T1
                   ~~~~~
BC30389: 'I1.T1' is not accessible in this context because it is 'Private'.
        Sub M1() Implements I1.T1.M1
                            ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub NestedTypes_10()

            Dim csSource =
"
public interface I1
{
    private protected interface T1
    {
        void M1();
    }

    private protected class T2
    {}

    private protected struct T3
    {}

    private protected enum T4
    {
        B
    }

    private protected delegate void T5();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class Test1
    Implements I1

    Shared Sub Main()
        Dim a As I1.T1 = new Test1()
        a.M1()
        System.Console.WriteLine(new I1.T2())
        System.Console.WriteLine(new I1.T3())
        System.Console.WriteLine(I1.T4.B.ToString())
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
    End Sub


    Public Class Test2
        Implements I1.T1
        Sub M1() Implements I1.T1.M1
            System.Console.WriteLine("M1")
        End Sub
    End Class
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.T1' is not accessible in this context because it is 'Private Protected'.
        Dim a As I1.T1 = new Test1()
                 ~~~~~
BC30389: 'I1.T2' is not accessible in this context because it is 'Private Protected'.
        System.Console.WriteLine(new I1.T2())
                                     ~~~~~
BC30389: 'I1.T3' is not accessible in this context because it is 'Private Protected'.
        System.Console.WriteLine(new I1.T3())
                                     ~~~~~
BC30389: 'I1.T4' is not accessible in this context because it is 'Private Protected'.
        System.Console.WriteLine(I1.T4.B.ToString())
                                 ~~~~~
BC30389: 'I1.T5' is not accessible in this context because it is 'Private Protected'.
        System.Console.WriteLine(new I1.T5(AddressOf a.M1))
                                     ~~~~~
BC30389: 'I1.T1' is not accessible in this context because it is 'Private Protected'.
        Implements I1.T1
                   ~~~~~
BC30389: 'I1.T1' is not accessible in this context because it is 'Private Protected'.
        Sub M1() Implements I1.T1.M1
                            ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub NestedTypes_11()

            Dim csSource =
"
public interface I1
{
    protected interface T1
    {
        void M1();
    }

    protected class T2
    {}

    protected struct T3
    {}

    protected enum T4
    {
        B
    }

    protected delegate void T5();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Interface Test1
    Inherits I1

    Class Test3
        Shared Sub Main()
            Dim a As I1.T1 = new Test2()
            a.M1()
            System.Console.WriteLine(new I1.T2())
            System.Console.WriteLine(new I1.T3())
            System.Console.WriteLine(I1.T4.B.ToString())
            System.Console.WriteLine(new I1.T5(AddressOf a.M1))
        End Sub
    End Class

    Class Test2
        Implements I1.T1
        Sub M1() Implements I1.T1.M1
            System.Console.WriteLine("M1")
        End Sub
    End Class
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"M1
I1+T2
I1+T3
B
I1+T5", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub NestedTypes_12()

            Dim csSource =
"
public interface I1
{
    protected interface T1
    {
        void M1();
    }

    protected class T2
    {}

    protected struct T3
    {}

    protected enum T4
    {
        B
    }

    protected delegate void T5();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface Test1
    Inherits I1

    Class Test3
        Shared Sub Main()
            Dim a As I1.T1 = new Test2()
            a.M1()
            System.Console.WriteLine(new I1.T2())
            System.Console.WriteLine(new I1.T3())
            System.Console.WriteLine(I1.T4.B.ToString())
            System.Console.WriteLine(new I1.T5(AddressOf a.M1))
        End Sub

        Private Interface I2
            Inherits I1.T1
        End Interface

        Class Test2
            Implements I2
            Sub M1() Implements I1.T1.M1
                System.Console.WriteLine("M1")
            End Sub
        End Class
    End Class
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"M1
I1+T2
I1+T3
B
I1+T5", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub NestedTypes_13()

            Dim csSource =
"
public interface I1
{
    protected interface T1
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface Test1
    Inherits I1, I1.T1
End Interface
Interface Test2
    Inherits I1.T1, I1
End Interface
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.T1' is not accessible in this context because it is 'Protected'.
    Inherits I1, I1.T1
                 ~~~~~
BC30389: 'I1.T1' is not accessible in this context because it is 'Protected'.
    Inherits I1.T1, I1
             ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub NestedTypes_14()

            Dim csSource =
"
public interface I1
{
    protected interface T1
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test1
    Implements I1, I1.T1
End Class
Class Test2
    Implements I1.T1, I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.T1' is not accessible in this context because it is 'Protected'.
    Implements I1, I1.T1
                   ~~~~~
BC30389: 'I1.T1' is not accessible in this context because it is 'Protected'.
    Implements I1.T1, I1
               ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub NestedTypes_15()

            Dim csSource =
"
public interface I1
{
    protected class T1
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class Test1
    Inherits I1.T1
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.T1' is not accessible in this context because it is 'Protected'.
    Inherits I1.T1
             ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub NestedTypes_16()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Class C1
    Protected Interface T1
    End Interface
End Class

Class Test1
    Inherits C1
    Implements C1.T1
End Class

Class Test2
    Inherits C1
    Implements T1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest)
            comp1.AssertTheseDiagnostics()
        End Sub

        <Fact>
        <WorkItem(38398, "https://github.com/dotnet/roslyn/issues/38398")>
        Public Sub InconsistentAccessibility_01()

            Dim csSource =
"
public interface I1
{
    protected interface I2
    {
    }
}

public class C1
{
    protected interface I2
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
interface I3 
    Inherits I1

    Function M1() As I1.I2
End Interface

Class C3
    Implements I1

    protected overridable Sub M1(x As I1.I2)
    End Sub

    protected interface I7
        Function M7() As I1.I2
    End Interface
End Class

class CC3 
    Inherits C3

    protected overrides Sub M1(x As I1.I2)
    End Sub

    class CI7 
        Implements C3.I7
        Private Function M7() As I1.I2 Implements C3.I7.M7
            Return Nothing
        End Function
    End Class
End Class

class C33 
    Inherits C1
    protected overridable Sub M1(x As C1.I2)
    End Sub

    public class C44
        protected overridable Function M44() As C1.I2
            Return Nothing
        End Function
    End Class

    protected class C55
        public overridable Function M55() As C1.I2
            Return Nothing
        End Function
    End Class
End Class

class CC33
    Inherits C33

    protected overrides Sub M1(x As C1.I2)
    End Sub

    private class CC55
        Inherits C33.C55
    
        public overrides Function M55() As C1.I2
            Return Nothing
        End Function
    End Class
End Class

class CC44 
    Inherits C33.C44

    protected overrides Function M44() As C1.I2
        Return Nothing
    End Function
End Class

]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error><![CDATA[
BC30508: 'M1' cannot expose type 'I1.I2' in namespace '<Default>' through interface 'I3'.
    Function M1() As I1.I2
                     ~~~~~
BC30508: 'M44' cannot expose type 'C1.I2' in namespace '<Default>' through class 'C44'.
        protected overridable Function M44() As C1.I2
                                                ~~~~~
BC30437: 'Protected Overrides Function M44() As C1.I2' cannot override 'Protected Overridable Function M44() As C1.I2' because they differ by their return types.
    protected overrides Function M44() As C1.I2
                                 ~~~
BC30389: 'C1.I2' is not accessible in this context because it is 'Protected'.
    protected overrides Function M44() As C1.I2
                                          ~~~~~
]]></error>)
        End Sub

        <Fact>
        <WorkItem(38398, "https://github.com/dotnet/roslyn/issues/38398")>
        Public Sub InconsistentAccessibility_02()

            Dim csSource =
"
public interface I1<T>
{
    protected interface I2
    {
    }
}

public class C1<T>
{
    protected interface I2
    {
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
interface I3 
    Inherits I1(Of Integer)

    Function M1() As I1(Of String).I2
End Interface

Class C3
    Implements I1(Of Integer)

    protected overridable Sub M1(x As I1(Of String).I2)
    End Sub

    protected interface I7
        Function M7() As I1(Of String).I2
    End Interface
End Class

class CC3 
    Inherits C3

    protected overrides Sub M1(x As I1(Of String).I2)
    End Sub

    class CI7 
        Implements C3.I7
        Private Function M7() As I1(Of String).I2 Implements C3.I7.M7
            Return Nothing
        End Function
    End Class
End Class

class C33 
    Inherits C1(Of Integer)
    protected overridable Sub M1(x As C1(Of String).I2)
    End Sub

    public class C44
        protected overridable Function M44() As C1(Of String).I2
            Return Nothing
        End Function
    End Class

    protected class C55
        public overridable Function M55() As C1(Of String).I2
            Return Nothing
        End Function
    End Class
End Class

class CC33
    Inherits C33

    protected overrides Sub M1(x As C1(Of String).I2)
    End Sub

    private class CC55
        Inherits C33.C55
    
        public overrides Function M55() As C1(Of String).I2
            Return Nothing
        End Function
    End Class
End Class

class CC44 
    Inherits C33.C44

    protected overrides Function M44() As C1(Of String).I2
        Return Nothing
    End Function
End Class

]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error><![CDATA[
BC30508: 'M1' cannot expose type 'I1(Of String).I2' in namespace '<Default>' through interface 'I3'.
    Function M1() As I1(Of String).I2
                     ~~~~~~~~~~~~~~~~
BC30508: 'M44' cannot expose type 'C1(Of String).I2' in namespace '<Default>' through class 'C44'.
        protected overridable Function M44() As C1(Of String).I2
                                                ~~~~~~~~~~~~~~~~
BC30437: 'Protected Overrides Function M44() As C1.I2' cannot override 'Protected Overridable Function M44() As C1(Of String).I2' because they differ by their return types.
    protected overrides Function M44() As C1(Of String).I2
                                 ~~~
BC30389: 'C1(Of String).I2' is not accessible in this context because it is 'Protected'.
    protected overrides Function M44() As C1(Of String).I2
                                          ~~~~~~~~~~~~~~~~
]]></error>)
        End Sub

        <Fact>
        <WorkItem(35820, "https://github.com/dotnet/roslyn/issues/35820")>
        Public Sub PropertyImplementation_001()

            Dim csSource =
"
public interface I1
{
    int P1 {get => throw null; set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'Property P1 As Integer' for interface 'I1'.
    Implements I1
               ~~
</errors>
            )
        End Sub

        <Fact>
        <WorkItem(35821, "https://github.com/dotnet/roslyn/issues/35821")>
        Public Sub PropertyImplementation_002()

            Dim csSource =
"
public interface I1
{
    int P1 {get => throw null; set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property

    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 += 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Get
C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_003()

            Dim csSource =
"
public interface I1
{
    int P1 {get;set;}
}

public interface I2 : I1
{
    int I1.P1 {get => throw null; set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'Property P1 As Integer' for interface 'I1'.
    Implements I2
               ~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub PropertyImplementation_004()

            Dim csSource =
"
public interface I1
{
    int P1 {get;set;}
}

public interface I2 : I1
{
    int I1.P1 {get => throw null; set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property

    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 += 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Get
C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35823, "https://github.com/dotnet/roslyn/issues/35823")>
        Public Sub PropertyImplementation_005()

            Dim csSource =
"
public interface I1
{
    int P1 {get;set;}
}

public interface I2 : I1
{
    abstract int I1.P1 {get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'Property P1 As Integer' for interface 'I1'.
    Implements I2
               ~~
</errors>
            )
        End Sub

        <Fact>
        <WorkItem(35823, "https://github.com/dotnet/roslyn/issues/35823")>
        Public Sub PropertyImplementation_006()

            Dim csSource =
"
public interface I1
{
    int P1 {get;set;}
}

public interface I2 : I1
{
    abstract int I1.P1 {get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property

    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 += 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Get
C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub PropertyImplementation_007()

            Dim csSource =
"
public interface I1
{
    internal int P1 {get;set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C1
    Implements I1

    Property P1 As Integer Implements I1.P1
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
End Class

Public Class C2
    Implements I1

    Property P1 As Integer Implements I1.P1 ' 2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
    Property P1 As Integer Implements I1.P1
                                      ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
    Property P1 As Integer Implements I1.P1 ' 2
                                      ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub PropertyImplementation_008()

            Dim csSource =
"
public interface I1
{
    int P1 {get; internal set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C1
    Implements I1

    Property P1 As Integer Implements I1.P1
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
End Class

Public Class C2
    Implements I1

    Property P1 As Integer Implements I1.P1 ' 2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30390: 'I1.Property Set P1(value As Integer)' is not accessible in this context because it is 'Friend'.
    Property P1 As Integer Implements I1.P1
                                      ~~~~~
BC30390: 'I1.Property Set P1(value As Integer)' is not accessible in this context because it is 'Friend'.
    Property P1 As Integer Implements I1.P1 ' 2
                                      ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub PropertyImplementation_009()

            Dim csSource =
"
public interface I1
{
    int P1 {internal get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C1
    Implements I1

    Property P1 As Integer Implements I1.P1
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
End Class

Public Class C2
    Implements I1

    Property P1 As Integer Implements I1.P1 ' 2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30390: 'I1.Property Get P1() As Integer' is not accessible in this context because it is 'Friend'.
    Property P1 As Integer Implements I1.P1
                                      ~~~~~
BC30390: 'I1.Property Get P1() As Integer' is not accessible in this context because it is 'Friend'.
    Property P1 As Integer Implements I1.P1 ' 2
                                      ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_010()

            Dim csSource =
"
public interface I1
{
    protected int P1 {get;set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Get
C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_011()

            Dim csSource =
"
public interface I1
{
    int P1 {get;protected set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Get
C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_012()

            Dim csSource =
"
public interface I1
{
    int P1 {protected get;set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Get
C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_013()

            Dim csSource =
"
public interface I1
{
    protected internal int P1 {get;set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Get
C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_014()

            Dim csSource =
"
public interface I1
{
    int P1 {get;protected internal set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Get
C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_015()

            Dim csSource =
"
public interface I1
{
    int P1 {protected internal get;set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Get
C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub PropertyImplementation_016()

            Dim csSource =
"
public interface I1
{
    private protected int P1 {get;set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C1
    Implements I1

    Property P1 As Integer Implements I1.P1
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
End Class

Public Class C2
    Implements I1

    Property P1 As Integer Implements I1.P1 ' 2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
    Property P1 As Integer Implements I1.P1
                                      ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
    Property P1 As Integer Implements I1.P1 ' 2
                                      ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub PropertyImplementation_017()

            Dim csSource =
"
public interface I1
{
    int P1 {get;private protected set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C1
    Implements I1

    Property P1 As Integer Implements I1.P1
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
End Class

Public Class C2
    Implements I1

    Property P1 As Integer Implements I1.P1 ' 2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30390: 'I1.Property Set P1(value As Integer)' is not accessible in this context because it is 'Private Protected'.
    Property P1 As Integer Implements I1.P1
                                      ~~~~~
BC30390: 'I1.Property Set P1(value As Integer)' is not accessible in this context because it is 'Private Protected'.
    Property P1 As Integer Implements I1.P1 ' 2
                                      ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub PropertyImplementation_018()

            Dim csSource =
"
public interface I1
{
    int P1 {private protected get;set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C1
    Implements I1

    Property P1 As Integer Implements I1.P1
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
End Class

Public Class C2
    Implements I1

    Property P1 As Integer Implements I1.P1 ' 2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30390: 'I1.Property Get P1() As Integer' is not accessible in this context because it is 'Private Protected'.
    Property P1 As Integer Implements I1.P1
                                      ~~~~~
BC30390: 'I1.Property Get P1() As Integer' is not accessible in this context because it is 'Private Protected'.
    Property P1 As Integer Implements I1.P1 ' 2
                                      ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35821, "https://github.com/dotnet/roslyn/issues/35821")>
        Public Sub PropertyImplementation_019()

            Dim csSource =
"
public interface I1
{
    int P1 {private get => throw null; set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property

    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 = 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})

            Dim validator = Sub(m As ModuleSymbol)
                                Dim p1 = m.GlobalNamespace.GetMember(Of PropertySymbol)("C.P1")

                                Dim i1P1 As PropertySymbol = p1.ExplicitInterfaceImplementations.Single()
                                Assert.Equal(If(DirectCast(i1P1.ContainingModule, PEModuleSymbol).ImportOptions = MetadataImportOptions.All, "", "WriteOnly ") + "Property I1.P1 As System.Int32", i1P1.ToTestDisplayString())

                                Dim p1Get = p1.GetMethod
                                Dim p1Set = p1.SetMethod

                                Assert.True(p1Set.IsMetadataVirtual)
                                Assert.True(p1Set.IsMetadataFinal)
                                Assert.False(p1Set.IsMustOverride)
                                Assert.False(p1Set.IsOverridable)
                                Assert.Equal("Sub I1.set_P1(value As System.Int32)", p1Set.ExplicitInterfaceImplementations.Single().ToTestDisplayString())

                                Assert.False(p1Get.IsMetadataVirtual)
                                Assert.False(p1Get.IsMetadataFinal)
                                Assert.False(p1Get.IsMustOverride)
                                Assert.False(p1Get.IsOverridable)
                                Assert.Empty(p1Get.ExplicitInterfaceImplementations)
                            End Sub

            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr, sourceSymbolValidator:=validator, symbolValidator:=validator)

            comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All), targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr, sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

        <Fact>
        <WorkItem(35821, "https://github.com/dotnet/roslyn/issues/35821")>
        Public Sub PropertyImplementation_020()

            Dim csSource =
"
public interface I1
{
    string P1 {private get => throw null; set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Property P1 As String Implements I1.P1

    Shared Sub Main()
        Dim c1 As new C()
        Dim i1 As I1 = c1
        i1.P1 = "C.P1.Set"
        System.Console.WriteLine(c1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})

            Dim validator = Sub(m As ModuleSymbol)
                                Dim p1 = m.GlobalNamespace.GetMember(Of PropertySymbol)("C.P1")

                                Dim i1P1 As PropertySymbol = p1.ExplicitInterfaceImplementations.Single()
                                Assert.Equal(If(DirectCast(i1P1.ContainingModule, PEModuleSymbol).ImportOptions = MetadataImportOptions.All, "", "WriteOnly ") + "Property I1.P1 As System.String", i1P1.ToTestDisplayString())

                                Dim p1Get = p1.GetMethod
                                Dim p1Set = p1.SetMethod

                                Assert.True(p1Set.IsMetadataVirtual)
                                Assert.True(p1Set.IsMetadataFinal)
                                Assert.False(p1Set.IsMustOverride)
                                Assert.False(p1Set.IsOverridable)
                                Assert.Equal("Sub I1.set_P1(value As System.String)", p1Set.ExplicitInterfaceImplementations.Single().ToTestDisplayString())

                                Assert.False(p1Get.IsMetadataVirtual)
                                Assert.False(p1Get.IsMetadataFinal)
                                Assert.False(p1Get.IsMustOverride)
                                Assert.False(p1Get.IsOverridable)
                                Assert.Empty(p1Get.ExplicitInterfaceImplementations)
                            End Sub

            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr, sourceSymbolValidator:=validator, symbolValidator:=validator)

            comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All), targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr, sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

        <Fact>
        <WorkItem(35821, "https://github.com/dotnet/roslyn/issues/35821")>
        Public Sub PropertyImplementation_021()

            Dim csSource =
"
public interface I1
{
    int P1 {get => throw null; private set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property

    Shared Sub Main()
        Dim i1 As I1 = new C()
        Dim x = i1.P1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})

            Dim validator = Sub(m As ModuleSymbol)
                                Dim p1 = m.GlobalNamespace.GetMember(Of PropertySymbol)("C.P1")

                                Dim i1P1 As PropertySymbol = p1.ExplicitInterfaceImplementations.Single()
                                Assert.Equal(If(DirectCast(i1P1.ContainingModule, PEModuleSymbol).ImportOptions = MetadataImportOptions.All, "", "ReadOnly ") + "Property I1.P1 As System.Int32", i1P1.ToTestDisplayString())

                                Dim p1Get = p1.GetMethod
                                Dim p1Set = p1.SetMethod

                                Assert.True(p1Get.IsMetadataVirtual)
                                Assert.True(p1Get.IsMetadataFinal)
                                Assert.False(p1Get.IsMustOverride)
                                Assert.False(p1Get.IsOverridable)
                                Assert.Equal("Function I1.get_P1() As System.Int32", p1Get.ExplicitInterfaceImplementations.Single().ToTestDisplayString())

                                Assert.False(p1Set.IsMetadataVirtual)
                                Assert.False(p1Set.IsMetadataFinal)
                                Assert.False(p1Set.IsMustOverride)
                                Assert.False(p1Set.IsOverridable)
                                Assert.Empty(p1Set.ExplicitInterfaceImplementations)
                            End Sub

            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr, sourceSymbolValidator:=validator, symbolValidator:=validator)

            comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All), targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr, sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

        <Fact>
        <WorkItem(35821, "https://github.com/dotnet/roslyn/issues/35821")>
        Public Sub PropertyImplementation_022()

            Dim csSource =
"
public interface I1
{
    string P1 {get => throw null; private set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Sub New()
        P1 = "C.P1.Get"
    End Sub

    Property P1 As String Implements I1.P1

    Shared Sub Main()
        Dim i1 As I1 = new C()
        System.Console.WriteLine(i1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})

            Dim validator = Sub(m As ModuleSymbol)
                                Dim p1 = m.GlobalNamespace.GetMember(Of PropertySymbol)("C.P1")

                                Dim i1P1 As PropertySymbol = p1.ExplicitInterfaceImplementations.Single()
                                Assert.Equal(If(DirectCast(i1P1.ContainingModule, PEModuleSymbol).ImportOptions = MetadataImportOptions.All, "", "ReadOnly ") + "Property I1.P1 As System.String", i1P1.ToTestDisplayString())

                                Dim p1Get = p1.GetMethod
                                Dim p1Set = p1.SetMethod

                                Assert.True(p1Get.IsMetadataVirtual)
                                Assert.True(p1Get.IsMetadataFinal)
                                Assert.False(p1Get.IsMustOverride)
                                Assert.False(p1Get.IsOverridable)
                                Assert.Equal("Function I1.get_P1() As System.String", p1Get.ExplicitInterfaceImplementations.Single().ToTestDisplayString())

                                Assert.False(p1Set.IsMetadataVirtual)
                                Assert.False(p1Set.IsMetadataFinal)
                                Assert.False(p1Set.IsMustOverride)
                                Assert.False(p1Set.IsOverridable)
                                Assert.Empty(p1Set.ExplicitInterfaceImplementations)
                            End Sub

            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr, sourceSymbolValidator:=validator, symbolValidator:=validator)

            comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All), targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr, sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

        <Fact>
        <WorkItem(35821, "https://github.com/dotnet/roslyn/issues/35821")>
        Public Sub PropertyImplementation_023()

            Dim csSource =
"
public interface I1
{
    int P1 {private get => throw null; set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    WriteOnly Property P1 As Integer Implements I1.P1
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property

    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 = 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)

            comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All), targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35821, "https://github.com/dotnet/roslyn/issues/35821")>
        Public Sub PropertyImplementation_024()

            Dim csSource =
"
public interface I1
{
    int P1 {get => throw null; private set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    ReadOnly Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
    End Property

    Shared Sub Main()
        Dim i1 As I1 = new C()
        Dim x = i1.P1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr)

            comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All), targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35821, "https://github.com/dotnet/roslyn/issues/35821")>
        Public Sub PropertyImplementation_025()

            Dim csSource =
"
public interface I1
{
    string P1 {get => throw null; private set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Sub New()
        P1 = "C.P1.Get"
    End Sub

    ReadOnly Property P1 As String Implements I1.P1

    Shared Sub Main()
        Dim i1 As I1 = new C()
        System.Console.WriteLine(i1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr)

            comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe.WithMetadataImportOptions(MetadataImportOptions.All), targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_026()

            Dim csSource =
"
public interface I1
{
    static int P1 {get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="100")
        End Sub

        <Fact>
        Public Sub PropertyImplementation_027()

            Dim csSource =
"
public interface I1
{
    internal static int P1 {get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        I1.P1 = 100
        ~~~~~
BC30456: 'P1' is not a member of 'I1'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
        I1.P1 = 100
        ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_028()

            Dim csSource =
"
public interface I1
{
    static int P1 {get; internal set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30526: Property 'P1' is 'ReadOnly'.
        I1.P1 = 100
        ~~~~~~~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        I1.P1 = 100
        ~~~~~~~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_029()

            Dim csSource =
"
public interface I1
{
    static int P1 {internal get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30524: Property 'P1' is 'WriteOnly'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_030()

            Dim csSource =
"
public interface I1
{
    protected static int P1 {get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "100", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_031()

            Dim csSource =
"
public interface I1
{
    static int P1 {get; protected set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "100", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_032()

            Dim csSource =
"
public interface I1
{
    static int P1 {protected get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "100", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_033()

            Dim csSource =
"
public interface I1
{
    protected internal static int P1 {get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "100", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_034()

            Dim csSource =
"
public interface I1
{
    static int P1 {get; protected internal set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "100", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_035()

            Dim csSource =
"
public interface I1
{
    static int P1 {protected internal get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "100", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_036()

            Dim csSource =
"
public interface I1
{
    private protected static int P1 {get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        I1.P1 = 100
        ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        I1.P1 = 100
        ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_037()

            Dim csSource =
"
public interface I1
{
    static int P1 {get; private protected set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        I1.P1 = 100
        ~~~~~~~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        I1.P1 = 100
        ~~~~~~~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_038()

            Dim csSource =
"
public interface I1
{
    static int P1 {private protected get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_039()

            Dim csSource =
"
public interface I1
{
    protected static int P1 {get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        I1.P1 = 100
        ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_040()

            Dim csSource =
"
public interface I1
{
    static int P1 {get; protected set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        I1.P1 = 100
        ~~~~~~~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_041()

            Dim csSource =
"
public interface I1
{
    static int P1 {protected get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_042()

            Dim csSource =
"
public interface I1
{
    protected internal static int P1 {get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        I1.P1 = 100
        ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_043()

            Dim csSource =
"
public interface I1
{
    static int P1 {get; protected internal set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        I1.P1 = 100
        ~~~~~~~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_044()

            Dim csSource =
"
public interface I1
{
    static int P1 {protected internal get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_045()

            Dim csSource =
"
public interface I1
{
    private static int P1 {get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        I1.P1 = 100
        ~~~~~
BC30456: 'P1' is not a member of 'I1'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private'.
        I1.P1 = 100
        ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Private'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_046()

            Dim csSource =
"
public interface I1
{
    static int P1 {get; private set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30526: Property 'P1' is 'ReadOnly'.
        I1.P1 = 100
        ~~~~~~~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        I1.P1 = 100
        ~~~~~~~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_047()

            Dim csSource =
"
public interface I1
{
    static int P1 {private get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30524: Property 'P1' is 'WriteOnly'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_048()

            Dim csSource =
"
public interface I1
{
    sealed int P1 
    {
        get {System.Console.WriteLine(""I1.P1.Get""); return 0;}
        set => System.Console.WriteLine(""I1.P1.Set"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"I1.P1.Get
I1.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_049()

            Dim csSource =
"
public interface I1
{
    internal sealed int P1 { get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        i1.P1 += 1
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_050()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { get => throw null; internal set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30526: Property 'P1' is 'ReadOnly'.
        i1.P1 += 1
        ~~~~~~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_051()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { internal get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30524: Property 'P1' is 'WriteOnly'.
        i1.P1 += 1
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_052()

            Dim csSource =
"
public interface I1
{
    protected sealed int P1 
    {
        get {System.Console.WriteLine(""I1.P1.Get""); return 0;}
        set => System.Console.WriteLine(""I1.P1.Set"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"I1.P1.Get
I1.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_053()

            Dim csSource =
"
public interface I1
{
    sealed int P1 
    {
        get {System.Console.WriteLine(""I1.P1.Get""); return 0;}
        protected set => System.Console.WriteLine(""I1.P1.Set"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"I1.P1.Get
I1.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_054()

            Dim csSource =
"
public interface I1
{
    sealed int P1 
    {
        protected get {System.Console.WriteLine(""I1.P1.Get""); return 0;}
        set => System.Console.WriteLine(""I1.P1.Set"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"I1.P1.Get
I1.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_055()

            Dim csSource =
"
public interface I1
{
    protected internal sealed int P1 
    {
        get {System.Console.WriteLine(""I1.P1.Get""); return 0;}
        set => System.Console.WriteLine(""I1.P1.Set"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"I1.P1.Get
I1.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_056()

            Dim csSource =
"
public interface I1
{
    sealed int P1 
    {
        get {System.Console.WriteLine(""I1.P1.Get""); return 0;}
        protected internal set => System.Console.WriteLine(""I1.P1.Set"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"I1.P1.Get
I1.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_057()

            Dim csSource =
"
public interface I1
{
    sealed int P1 
    {
        protected internal get {System.Console.WriteLine(""I1.P1.Get""); return 0;}
        set => System.Console.WriteLine(""I1.P1.Set"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"I1.P1.Get
I1.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_058()

            Dim csSource =
"
public interface I1
{
    private protected sealed int P1 { get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        i1.P1 += 1
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_059()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { get => throw null; private protected set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_060()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { private protected get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_061()

            Dim csSource =
"
public interface I1
{
    protected sealed int P1 { get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_062()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { get => throw null; protected set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_063()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { protected get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_064()

            Dim csSource =
"
public interface I1
{
    protected sealed int P1 { get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 += 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_065()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { get => throw null; protected set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 += 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_066()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { protected get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 += 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_067()

            Dim csSource =
"
public interface I1
{
    protected internal sealed int P1 { get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_068()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { get => throw null; protected internal set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_069()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { protected internal get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_070()

            Dim csSource =
"
public interface I1
{
    protected internal sealed int P1 { get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 += 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_071()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { get => throw null; protected internal set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 += 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_072()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { protected internal get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 += 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_073()

            Dim csSource =
"
public interface I1
{
    private int P1 { get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        i1.P1 += 1
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private'.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_074()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { get => throw null; private set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30526: Property 'P1' is 'ReadOnly'.
        i1.P1 += 1
        ~~~~~~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC31102: 'Set' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_075()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { private get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30524: Property 'P1' is 'WriteOnly'.
        i1.P1 += 1
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC31103: 'Get' accessor of property 'P1' is not accessible.
        i1.P1 += 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_076()

            Dim csSource =
"
public interface I1
{
    protected static int P1 {get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        I1.P1 = 100
        ~~~~~~~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_077()

            Dim csSource =
"
public interface I1
{
    static int P1 {get; protected set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        I1.P1 = 100
        ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_078()

            Dim csSource =
"
public interface I1
{
    static int P1 {protected get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_079()

            Dim csSource =
"
public interface I1
{
    protected internal static int P1 {get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        I1.P1 = 100
        ~~~~~~~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_080()

            Dim csSource =
"
public interface I1
{
    static int P1 {get; protected internal set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        I1.P1 = 100
        ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_081()

            Dim csSource =
"
public interface I1
{
    static int P1 {protected internal get; set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35885, "https://github.com/dotnet/roslyn/issues/35885")>
        Public Sub PropertyImplementation_082()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { get => throw null; set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 += 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37309: Target runtime doesn't support default interface implementation.
        i1.P1 += 1
        ~~~~~
BC37309: Target runtime doesn't support default interface implementation.
        i1.P1 += 1
        ~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_083()

            Dim csSource =
"
public interface I1
{
    protected int P1 {get;set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            i2.P1 += 1
            ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            i2.P1 += 1
            ~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_084()

            Dim csSource =
"
public interface I1
{
    int P1 {get; protected set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            i2.P1 += 1
            ~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_085()

            Dim csSource =
"
public interface I1
{
    int P1 {protected get;set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            i2.P1 += 1
            ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_086()

            Dim csSource =
"
public interface I1
{
    protected internal int P1 {get;set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            i2.P1 += 1
            ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            i2.P1 += 1
            ~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_087()

            Dim csSource =
"
public interface I1
{
    int P1 {get; protected internal set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            i2.P1 += 1
            ~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_088()

            Dim csSource =
"
public interface I1
{
    int P1 {protected internal get;set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 += 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            i2.P1 += 1
            ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35820, "https://github.com/dotnet/roslyn/issues/35820")>
        Public Sub PropertyImplementation_089()

            Dim csSource =
"
public interface I1
{
    int P1 {get => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'ReadOnly Property P1 As Integer' for interface 'I1'.
    Implements I1
               ~~
</errors>
            )
        End Sub

        <Fact>
        <WorkItem(35821, "https://github.com/dotnet/roslyn/issues/35821")>
        Public Sub PropertyImplementation_090()

            Dim csSource =
"
public interface I1
{
    int P1 {get => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Readonly Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
    End Property

    Shared Sub Main()
        Dim i1 As I1 = new C()
        Dim x = i1.P1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_091()

            Dim csSource =
"
public interface I1
{
    int P1 {get;}
}

public interface I2 : I1
{
    int I1.P1 {get => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'ReadOnly Property P1 As Integer' for interface 'I1'.
    Implements I2
               ~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub PropertyImplementation_092()

            Dim csSource =
"
public interface I1
{
    int P1 {get;}
}

public interface I2 : I1
{
    int I1.P1 {get => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2

    Readonly Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
    End Property

    Shared Sub Main()
        Dim i1 As I1 = new C()
        Dim x = i1.P1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35823, "https://github.com/dotnet/roslyn/issues/35823")>
        Public Sub PropertyImplementation_093()

            Dim csSource =
"
public interface I1
{
    int P1 {get;}
}

public interface I2 : I1
{
    abstract int I1.P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'ReadOnly Property P1 As Integer' for interface 'I1'.
    Implements I2
               ~~
</errors>
            )
        End Sub

        <Fact>
        <WorkItem(35823, "https://github.com/dotnet/roslyn/issues/35823")>
        Public Sub PropertyImplementation_094()

            Dim csSource =
"
public interface I1
{
    int P1 {get;}
}

public interface I2 : I1
{
    abstract int I1.P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2

    Readonly Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
    End Property

    Shared Sub Main()
        Dim i1 As I1 = new C()
        Dim x = i1.P1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub PropertyImplementation_095()

            Dim csSource =
"
public interface I1
{
    internal int P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C1
    Implements I1

    Readonly Property P1 As Integer Implements I1.P1
        Get
            Return 0
        End Get
    End Property
End Class

Public Class C2
    Implements I1

    Readonly Property P1 As Integer Implements I1.P1 ' 2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
    Readonly Property P1 As Integer Implements I1.P1
                                               ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
    Readonly Property P1 As Integer Implements I1.P1 ' 2
                                               ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_096()

            Dim csSource =
"
public interface I1
{
    protected int P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            Dim x = i2.P1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Readonly Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
    End Property
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_097()

            Dim csSource =
"
public interface I1
{
    protected internal int P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            Dim x = i2.P1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Readonly Property P1 As Integer Implements I1.P1
        Get
            System.Console.WriteLine("C.P1.Get")
            Return 0
        End Get
    End Property
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub PropertyImplementation_098()

            Dim csSource =
"
public interface I1
{
    private protected int P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C1
    Implements I1

    Readonly Property P1 As Integer Implements I1.P1
        Get
            Return 0
        End Get
    End Property
End Class

Public Class C2
    Implements I1

    Readonly Property P1 As Integer Implements I1.P1 ' 2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
    Readonly Property P1 As Integer Implements I1.P1
                                               ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
    Readonly Property P1 As Integer Implements I1.P1 ' 2
                                               ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_099()

            Dim csSource =
"
public interface I1
{
    static int P1 {get;} = 100;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="100")
        End Sub

        <Fact>
        Public Sub PropertyImplementation_100()

            Dim csSource =
"
public interface I1
{
    internal static int P1 {get;} = 100;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_101()

            Dim csSource =
"
public interface I1
{
    protected static int P1 {get;} = 100;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "100", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_102()

            Dim csSource =
"
public interface I1
{
    protected internal static int P1 {get;} = 100;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "100", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_103()

            Dim csSource =
"
public interface I1
{
    private protected static int P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_104()

            Dim csSource =
"
public interface I1
{
    protected static int P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_105()

            Dim csSource =
"
public interface I1
{
    protected internal static int P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_106()

            Dim csSource =
"
public interface I1
{
    private static int P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private'.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_107()

            Dim csSource =
"
public interface I1
{
    sealed int P1 
    {
        get {System.Console.WriteLine(""I1.P1.Get""); return 0;}
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        Dim x = i1.P1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_108()

            Dim csSource =
"
public interface I1
{
    internal sealed int P1 { get => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        Dim x = i1.P1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        Dim x = i1.P1
                ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
        Dim x = i1.P1
                ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_109()

            Dim csSource =
"
public interface I1
{
    protected sealed int P1 
    {
        get {System.Console.WriteLine(""I1.P1.Get""); return 0;}
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            Dim x = i2.P1
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_110()

            Dim csSource =
"
public interface I1
{
    protected internal sealed int P1 
    {
        get {System.Console.WriteLine(""I1.P1.Get""); return 0;}
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            Dim x = i2.P1
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.P1.Get", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_111()

            Dim csSource =
"
public interface I1
{
    private protected sealed int P1 { get => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        Dim x = i1.P1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        Dim x = i1.P1
                ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        Dim x = i1.P1
                ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_112()

            Dim csSource =
"
public interface I1
{
    protected sealed int P1 { get => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        Dim x = i1.P1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        Dim x = i1.P1
                ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_113()

            Dim csSource =
"
public interface I1
{
    protected sealed int P1 { get => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 As I1 = new C()
        Dim x = i1.P1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        Dim x = i1.P1
                ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_114()

            Dim csSource =
"
public interface I1
{
    protected internal sealed int P1 { get => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        Dim x = i1.P1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        Dim x = i1.P1
                ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_115()

            Dim csSource =
"
public interface I1
{
    protected internal sealed int P1 { get => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 As I1 = new C()
        Dim x = i1.P1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        Dim x = i1.P1
                ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_116()

            Dim csSource =
"
public interface I1
{
    private int P1 { get => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        Dim x = i1.P1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        Dim x = i1.P1
                ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private'.
        Dim x = i1.P1
                ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_117()

            Dim csSource =
"
public interface I1
{
    protected static int P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_118()

            Dim csSource =
"
public interface I1
{
    protected internal static int P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        System.Console.WriteLine(I1.P1)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        System.Console.WriteLine(I1.P1)
                                 ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35885, "https://github.com/dotnet/roslyn/issues/35885")>
        Public Sub PropertyImplementation_119()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { get => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        Dim x = i1.P1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37309: Target runtime doesn't support default interface implementation.
        Dim x = i1.P1
                ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_120()

            Dim csSource =
"
public interface I1
{
    protected int P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            Dim x = i2.P1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Readonly Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            Dim x = i2.P1
                    ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_121()

            Dim csSource =
"
public interface I1
{
    protected internal int P1 {get;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            Dim x = i2.P1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Readonly Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            Dim x = i2.P1
                    ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35820, "https://github.com/dotnet/roslyn/issues/35820")>
        Public Sub PropertyImplementation_122()

            Dim csSource =
"
public interface I1
{
    int P1 {set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'WriteOnly Property P1 As Integer' for interface 'I1'.
    Implements I1
               ~~
</errors>
            )
        End Sub

        <Fact>
        <WorkItem(35821, "https://github.com/dotnet/roslyn/issues/35821")>
        Public Sub PropertyImplementation_123()

            Dim csSource =
"
public interface I1
{
    int P1 {set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Writeonly Property P1 As Integer Implements I1.P1
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property

    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 = 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_124()

            Dim csSource =
"
public interface I1
{
    int P1 {set;}
}

public interface I2 : I1
{
    int I1.P1 {set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'WriteOnly Property P1 As Integer' for interface 'I1'.
    Implements I2
               ~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub PropertyImplementation_125()

            Dim csSource =
"
public interface I1
{
    int P1 {set;}
}

public interface I2 : I1
{
    int I1.P1 {set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2

    Writeonly Property P1 As Integer Implements I1.P1
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property

    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 = 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35823, "https://github.com/dotnet/roslyn/issues/35823")>
        Public Sub PropertyImplementation_126()

            Dim csSource =
"
public interface I1
{
    int P1 {set;}
}

public interface I2 : I1
{
    abstract int I1.P1 {set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'WriteOnly Property P1 As Integer' for interface 'I1'.
    Implements I2
               ~~
</errors>
            )
        End Sub

        <Fact>
        <WorkItem(35823, "https://github.com/dotnet/roslyn/issues/35823")>
        Public Sub PropertyImplementation_127()

            Dim csSource =
"
public interface I1
{
    int P1 {set;}
}

public interface I2 : I1
{
    abstract int I1.P1 {set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2

    Writeonly Property P1 As Integer Implements I1.P1
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property

    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 = 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub PropertyImplementation_128()

            Dim csSource =
"
public interface I1
{
    internal int P1 {set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C1
    Implements I1

    Writeonly Property P1 As Integer Implements I1.P1
        Set
        End Set
    End Property
End Class

Public Class C2
    Implements I1

    Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
    Writeonly Property P1 As Integer Implements I1.P1
                                                ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
    Property P1 As Integer Implements I1.P1
                                      ~~~~~
</expected>
            )
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_129()

            Dim csSource =
"
public interface I1
{
    protected int P1 {set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 = 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Writeonly Property P1 As Integer Implements I1.P1
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_130()

            Dim csSource =
"
public interface I1
{
    protected internal int P1 {set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 = 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Writeonly Property P1 As Integer Implements I1.P1
        Set
            System.Console.WriteLine("C.P1.Set")
        End Set
    End Property
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub PropertyImplementation_131()

            Dim csSource =
"
public interface I1
{
    private protected int P1 {set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C1
    Implements I1

    Writeonly Property P1 As Integer Implements I1.P1
        Set
        End Set
    End Property
End Class

Public Class C2
    Implements I1

    Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
    Writeonly Property P1 As Integer Implements I1.P1
                                                ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
    Property P1 As Integer Implements I1.P1
                                      ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_132()

            Dim csSource =
"
public interface I1
{
    private static int _p1;
    static int P1 {set => _p1 = value;}
    static int P2 => _p1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P2)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:="100")
        End Sub

        <Fact>
        Public Sub PropertyImplementation_133()

            Dim csSource =
"
public interface I1
{
    internal static int P1 {set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        I1.P1 = 100
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
        I1.P1 = 100
        ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_134()

            Dim csSource =
"
public interface I1
{
    private static int _p1;
    protected static int P1 {set => _p1 = value;}
    static int P2 => _p1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P2)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "100", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_135()

            Dim csSource =
"
public interface I1
{
    private static int _p1;
    protected internal static int P1 {set => _p1 = value;}
    static int P2 => _p1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
        System.Console.WriteLine(I1.P2)
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "100", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_136()

            Dim csSource =
"
public interface I1
{
    private protected static int P1 {set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        I1.P1 = 100
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        I1.P1 = 100
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_137()

            Dim csSource =
"
public interface I1
{
    protected static int P1 {set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        I1.P1 = 100
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_138()

            Dim csSource =
"
public interface I1
{
    protected internal static int P1 {set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        I1.P1 = 100
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_139()

            Dim csSource =
"
public interface I1
{
    private static int P1 {set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        I1.P1 = 100
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        I1.P1 = 100
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private'.
        I1.P1 = 100
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_140()

            Dim csSource =
"
public interface I1
{
    sealed int P1 
    {
        set => System.Console.WriteLine(""I1.P1.Set"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 = 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_141()

            Dim csSource =
"
public interface I1
{
    internal sealed int P1 { set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 = 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        i1.P1 = 1
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
        i1.P1 = 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_142()

            Dim csSource =
"
public interface I1
{
    protected sealed int P1 
    {
        set => System.Console.WriteLine(""I1.P1.Set"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            i2.P1 = 1
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub PropertyImplementation_143()

            Dim csSource =
"
public interface I1
{
    protected internal sealed int P1 
    {
        set => System.Console.WriteLine(""I1.P1.Set"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            i2.P1 = 1
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "I1.P1.Set", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_144()

            Dim csSource =
"
public interface I1
{
    private protected sealed int P1 { set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 = 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        i1.P1 = 1
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        i1.P1 = 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_145()

            Dim csSource =
"
public interface I1
{
    protected sealed int P1 { set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 = 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        i1.P1 = 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_146()

            Dim csSource =
"
public interface I1
{
    protected sealed int P1 { set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 = 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        i1.P1 = 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_147()

            Dim csSource =
"
public interface I1
{
    protected internal sealed int P1 { set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 = 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        i1.P1 = 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_148()

            Dim csSource =
"
public interface I1
{
    protected internal sealed int P1 { set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 As I1 = new C()
        i1.P1 = 1
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        i1.P1 = 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub PropertyImplementation_149()

            Dim csSource =
"
public interface I1
{
    private int P1 { set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 = 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        i1.P1 = 1
        ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private'.
        i1.P1 = 1
        ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_150()

            Dim csSource =
"
public interface I1
{
    protected static int P1 {set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        I1.P1 = 100
        ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_151()

            Dim csSource =
"
public interface I1
{
    protected internal static int P1 {set => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        I1.P1 = 100
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        I1.P1 = 100
        ~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35885, "https://github.com/dotnet/roslyn/issues/35885")>
        Public Sub PropertyImplementation_152()

            Dim csSource =
"
public interface I1
{
    sealed int P1 { set => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        i1.P1 = 1
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37309: Target runtime doesn't support default interface implementation.
        i1.P1 = 1
        ~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_153()

            Dim csSource =
"
public interface I1
{
    protected int P1 {set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 = 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            i2.P1 = 1
            ~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub PropertyImplementation_154()

            Dim csSource =
"
public interface I1
{
    protected internal int P1 {set;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            i2.P1 = 1
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Property P1 As Integer Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            i2.P1 = 1
            ~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35820, "https://github.com/dotnet/roslyn/issues/35820")>
        Public Sub EventImplementation_01()

            Dim csSource =
"
public interface I1
{
    event System.Action P1 {add => throw null; remove => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'Event P1 As Action' for interface 'I1'.
    Implements I1
               ~~
</errors>
            )
        End Sub

        <Fact>
        <WorkItem(35821, "https://github.com/dotnet/roslyn/issues/35821")>
        Public Sub EventImplementation_02()

            Dim csSource =
"
public interface I1
{
    event System.Action P1 {add => throw null; remove => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1

    Custom Event P1 As System.Action Implements I1.P1
        AddHandler(value As System.Action)
            System.Console.WriteLine("C.P1.Add")
        End AddHandler
        RemoveHandler(value As System.Action)
            System.Console.WriteLine("C.P1.Remove")
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event

    Shared Sub Main()
        Dim i1 As I1 = new C()
        AddHandler i1.P1, Nothing
        RemoveHandler i1.P1, Nothing
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Add
C.P1.Remove", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub EventImplementation_03()

            Dim csSource =
"
public interface I1
{
    event System.Action P1;
}

public interface I2 : I1
{
    event System.Action I1.P1 {add => throw null; remove => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'Event P1 As Action' for interface 'I1'.
    Implements I2
               ~~
</errors>
            )
        End Sub

        <Fact>
        Public Sub EventImplementation_04()

            Dim csSource =
"
public interface I1
{
    event System.Action P1;
}

public interface I2 : I1
{
    event System.Action I1.P1 {add => throw null; remove => throw null;}
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2

    Custom Event P1 As System.Action Implements I1.P1
        AddHandler(value As System.Action)
            System.Console.WriteLine("C.P1.Add")
        End AddHandler
        RemoveHandler(value As System.Action)
            System.Console.WriteLine("C.P1.Remove")
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event

    Shared Sub Main()
        Dim i1 As I1 = new C()
        AddHandler i1.P1, Nothing
        RemoveHandler i1.P1, Nothing
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Add
C.P1.Remove", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35823, "https://github.com/dotnet/roslyn/issues/35823")>
        Public Sub EventImplementation_05()

            Dim csSource =
"
public interface I1
{
    event System.Action P1;
}

public interface I2 : I1
{
    abstract event System.Action I1.P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<errors>
BC30149: Class 'C' must implement 'Event P1 As Action' for interface 'I1'.
    Implements I2
               ~~
</errors>
            )
        End Sub

        <Fact>
        <WorkItem(35823, "https://github.com/dotnet/roslyn/issues/35823")>
        Public Sub EventImplementation_06()

            Dim csSource =
"
public interface I1
{
    event System.Action P1;
}

public interface I2 : I1
{
    abstract event System.Action I1.P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I2

    Custom Event P1 As System.Action Implements I1.P1
        AddHandler(value As System.Action)
            System.Console.WriteLine("C.P1.Add")
        End AddHandler
        RemoveHandler(value As System.Action)
            System.Console.WriteLine("C.P1.Remove")
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event

    Shared Sub Main()
        Dim i1 As I1 = new C()
        AddHandler i1.P1, Nothing
        RemoveHandler i1.P1, Nothing
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Add
C.P1.Remove", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub EventImplementation_07()

            Dim csSource =
"
public interface I1
{
    internal event System.Action P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C1
    Implements I1

    Custom Event P1 As System.Action Implements I1.P1
        AddHandler(value As System.Action)
        End AddHandler
        RemoveHandler(value As System.Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Public Class C2
    Implements I1

    Event P1 As System.Action Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
    Custom Event P1 As System.Action Implements I1.P1
                                                ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
    Event P1 As System.Action Implements I1.P1
                                         ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub EventImplementation_08()

            Dim csSource =
"
public interface I1
{
    protected event System.Action P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            AddHandler i2.P1, Nothing
            RemoveHandler i2.P1, Nothing
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Custom Event P1 As System.Action Implements I1.P1
        AddHandler(value As System.Action)
            System.Console.WriteLine("C.P1.Add")
        End AddHandler
        RemoveHandler(value As System.Action)
            System.Console.WriteLine("C.P1.Remove")
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Add
C.P1.Remove", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub EventImplementation_09()

            Dim csSource =
"
public interface I1
{
    protected internal event System.Action P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            AddHandler i2.P1, Nothing
            RemoveHandler i2.P1, Nothing
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Custom Event P1 As System.Action Implements I1.P1
        AddHandler(value As System.Action)
            System.Console.WriteLine("C.P1.Add")
        End AddHandler
        RemoveHandler(value As System.Action)
            System.Console.WriteLine("C.P1.Remove")
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"C.P1.Add
C.P1.Remove", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35824, "https://github.com/dotnet/roslyn/issues/35824")>
        Public Sub EventImplementation_10()

            Dim csSource =
"
public interface I1
{
    private protected event System.Action P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C1
    Implements I1

    Custom Event P1 As System.Action Implements I1.P1
        AddHandler(value As System.Action)
        End AddHandler
        RemoveHandler(value As System.Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Public Class C2
    Implements I1

    Event P1 As System.Action Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
    Custom Event P1 As System.Action Implements I1.P1
                                                ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
    Event P1 As System.Action Implements I1.P1
                                         ~~~~~
</expected>
            )
        End Sub

        <Fact>
        Public Sub EventImplementation_11()

            Dim csSource =
"
public interface I1
{
    static event System.Action P1;
    static void Raise() => P1?.Invoke();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        AddHandler I1.P1, AddressOf M1
        I1.Raise()
        RemoveHandler I1.P1, AddressOf M1
        AddHandler I1.P1, AddressOf M2
        I1.Raise()
    End Sub

    Shared Sub M1()
        System.Console.WriteLine("M1")
    End Sub
    Shared Sub M2()
        System.Console.WriteLine("M2")
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"M1
M2", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35948, "https://github.com/dotnet/roslyn/issues/35948")>
        Public Sub EventImplementation_12()

            Dim csSource =
"
public interface I1
{
    internal static event System.Action P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        AddHandler I1.P1, Nothing
        RemoveHandler I1.P1, Nothing
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        AddHandler I1.P1, Nothing
                   ~~~~~
BC30456: 'P1' is not a member of 'I1'.
        RemoveHandler I1.P1, Nothing
                      ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
        AddHandler I1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
        RemoveHandler I1.P1, Nothing
                      ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub EventImplementation_13()

            Dim csSource =
"
public interface I1
{
    protected static event System.Action P1;
    static void Raise() => P1?.Invoke();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        AddHandler I1.P1, AddressOf M1
        I1.Raise()
        RemoveHandler I1.P1, AddressOf M1
        AddHandler I1.P1, AddressOf M2
        I1.Raise()
    End Sub

    Shared Sub M1()
        System.Console.WriteLine("M1")
    End Sub
    Shared Sub M2()
        System.Console.WriteLine("M2")
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"M1
M2", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub EventImplementation_14()

            Dim csSource =
"
public interface I1
{
    protected internal static event System.Action P1;
    static void Raise() => P1?.Invoke();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        AddHandler I1.P1, AddressOf M1
        I1.Raise()
        RemoveHandler I1.P1, AddressOf M1
        AddHandler I1.P1, AddressOf M2
        I1.Raise()
    End Sub

    Shared Sub M1()
        System.Console.WriteLine("M1")
    End Sub
    Shared Sub M2()
        System.Console.WriteLine("M2")
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"M1
M2", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35948, "https://github.com/dotnet/roslyn/issues/35948")>
        Public Sub EventImplementation_15()

            Dim csSource =
"
public interface I1
{
    private protected static event System.Action P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        AddHandler I1.P1, Nothing
        RemoveHandler I1.P1, Nothing
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        AddHandler I1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        RemoveHandler I1.P1, Nothing
                      ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        AddHandler I1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        RemoveHandler I1.P1, Nothing
                      ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub EventImplementation_16()

            Dim csSource =
"
public interface I1
{
    protected static event System.Action P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        AddHandler I1.P1, Nothing
        RemoveHandler I1.P1, Nothing
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        AddHandler I1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        RemoveHandler I1.P1, Nothing
                      ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub EventImplementation_17()

            Dim csSource =
"
public interface I1
{
    protected internal static event System.Action P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        AddHandler I1.P1, Nothing
        RemoveHandler I1.P1, Nothing
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        AddHandler I1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        RemoveHandler I1.P1, Nothing
                      ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub EventImplementation_18()

            Dim csSource =
"
public interface I1
{
    private static event System.Action P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        AddHandler I1.P1, Nothing
        RemoveHandler I1.P1, Nothing
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        AddHandler I1.P1, Nothing
                   ~~~~~
BC30456: 'P1' is not a member of 'I1'.
        RemoveHandler I1.P1, Nothing
                      ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private'.
        AddHandler I1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Private'.
        RemoveHandler I1.P1, Nothing
                      ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub EventImplementation_19()

            Dim csSource =
"
public interface I1
{
    sealed event System.Action P1 
    {
        add => System.Console.WriteLine(""I1.P1.Add"");
        remove => System.Console.WriteLine(""I1.P1.Remove"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        AddHandler i1.P1, Nothing
        RemoveHandler i1.P1, Nothing
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"I1.P1.Add
I1.P1.Remove", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub EventImplementation_20()

            Dim csSource =
"
public interface I1
{
    internal sealed event System.Action P1 { add => throw null; remove => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        AddHandler i1.P1, Nothing
        RemoveHandler i1.P1, Nothing
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        AddHandler i1.P1, Nothing
                   ~~~~~
BC30456: 'P1' is not a member of 'I1'.
        RemoveHandler i1.P1, Nothing
                      ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
        AddHandler i1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Friend'.
        RemoveHandler i1.P1, Nothing
                      ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub EventImplementation_21()

            Dim csSource =
"
public interface I1
{
    protected sealed event System.Action P1 
    {
        add => System.Console.WriteLine(""I1.P1.Add"");
        remove => System.Console.WriteLine(""I1.P1.Remove"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            AddHandler i2.P1, Nothing
            RemoveHandler i2.P1, Nothing
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"I1.P1.Add
I1.P1.Remove", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub EventImplementation_22()

            Dim csSource =
"
public interface I1
{
    protected internal sealed event System.Action P1 
    {
        add => System.Console.WriteLine(""I1.P1.Add"");
        remove => System.Console.WriteLine(""I1.P1.Remove"");
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1
    Class C1
        Shared Sub Main()
            Dim i2 as I2 = New C()
            AddHandler i2.P1, Nothing
            RemoveHandler i2.P1, Nothing
        End Sub
    End Class
End Interface

Class C
    Implements I2
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr,
"I1.P1.Add
I1.P1.Remove", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        Public Sub EventImplementation_23()

            Dim csSource =
"
public interface I1
{
    private protected sealed event System.Action P1 { add => throw null; remove => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        AddHandler i1.P1, Nothing
        RemoveHandler i1.P1, Nothing
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        AddHandler i1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        RemoveHandler i1.P1, Nothing
                      ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        AddHandler i1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Private Protected'.
        RemoveHandler i1.P1, Nothing
                      ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub EventImplementation_24()

            Dim csSource =
"
public interface I1
{
    protected sealed event System.Action P1 { add => throw null; remove => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        AddHandler i1.P1, Nothing
        RemoveHandler i1.P1, Nothing
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        AddHandler i1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        RemoveHandler i1.P1, Nothing
                      ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub EventImplementation_25()

            Dim csSource =
"
public interface I1
{
    protected sealed event System.Action P1 { add => throw null; remove => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 As I1 = new C()
        AddHandler i1.P1, Nothing
        RemoveHandler i1.P1, Nothing
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        AddHandler i1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected'.
        RemoveHandler i1.P1, Nothing
                      ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub EventImplementation_26()

            Dim csSource =
"
public interface I1
{
    protected internal sealed event System.Action P1 { add => throw null; remove => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        AddHandler i1.P1, Nothing
        RemoveHandler i1.P1, Nothing
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        AddHandler i1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        RemoveHandler i1.P1, Nothing
                      ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub EventImplementation_27()

            Dim csSource =
"
public interface I1
{
    protected internal sealed event System.Action P1 { add => throw null; remove => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        Dim i1 As I1 = new C()
        AddHandler i1.P1, Nothing
        RemoveHandler i1.P1, Nothing
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        AddHandler i1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Protected Friend'.
        RemoveHandler i1.P1, Nothing
                      ~~~~~
</error>)
        End Sub

        <Fact>
        Public Sub EventImplementation_28()

            Dim csSource =
"
public interface I1
{
    private event System.Action P1 { add => throw null; remove => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        AddHandler i1.P1, Nothing
        RemoveHandler i1.P1, Nothing
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<error>
BC30456: 'P1' is not a member of 'I1'.
        AddHandler i1.P1, Nothing
                   ~~~~~
BC30456: 'P1' is not a member of 'I1'.
        RemoveHandler i1.P1, Nothing
                      ~~~~~
</error>)

            Dim comp2 = CreateCompilation(source1, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation}, options:=TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All))
            comp2.AssertTheseDiagnostics(
<error>
BC30389: 'I1.P1' is not accessible in this context because it is 'Private'.
        AddHandler i1.P1, Nothing
                   ~~~~~
BC30389: 'I1.P1' is not accessible in this context because it is 'Private'.
        RemoveHandler i1.P1, Nothing
                      ~~~~~
</error>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub EventImplementation_29()

            Dim csSource =
"
public interface I1
{
    protected static event System.Action P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        AddHandler I1.P1, Nothing
        RemoveHandler I1.P1, Nothing
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        AddHandler I1.P1, Nothing
                   ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        RemoveHandler I1.P1, Nothing
                      ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub EventImplementation_30()

            Dim csSource =
"
public interface I1
{
    protected internal static event System.Action P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Implements I1
    Shared Sub Main()
        AddHandler I1.P1, Nothing
        RemoveHandler I1.P1, Nothing
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        AddHandler I1.P1, Nothing
                   ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
        RemoveHandler I1.P1, Nothing
                      ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35885, "https://github.com/dotnet/roslyn/issues/35885")>
        Public Sub EventImplementation_31()

            Dim csSource =
"
public interface I1
{
    sealed event System.Action P1 { add => throw null; remove => throw null; }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C
    Shared Sub Main()
        Dim i1 As I1 = new Test()
        AddHandler i1.P1, Nothing
        RemoveHandler i1.P1, Nothing
    End Sub
End Class

Public Class Test
    Implements I1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37309: Target runtime doesn't support default interface implementation.
        AddHandler i1.P1, Nothing
                   ~~~~~
BC37309: Target runtime doesn't support default interface implementation.
        RemoveHandler i1.P1, Nothing
                      ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub EventImplementation_32()

            Dim csSource =
"
public interface I1
{
    protected event System.Action P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            AddHandler i2.P1, Nothing
            RemoveHandler i2.P1, Nothing
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Event P1 As System.Action Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            AddHandler i2.P1, Nothing
                       ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            RemoveHandler i2.P1, Nothing
                          ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35834, "https://github.com/dotnet/roslyn/issues/35834")>
        Public Sub EventImplementation_33()

            Dim csSource =
"
public interface I1
{
    protected internal event System.Action P1;
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Dim i2 As I2 = new C()
            AddHandler i2.P1, Nothing
            RemoveHandler i2.P1, Nothing
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Event P1 As System.Action Implements I1.P1
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.DesktopLatestExtended, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            AddHandler i2.P1, Nothing
                       ~~~~~
BC37310: Target runtime doesn't support 'Protected', 'Protected Friend', or 'Private Protected' accessibility for a member of an interface.
            RemoveHandler i2.P1, Nothing
                          ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub ProtectedAccess_01()

            Dim csSource =
"
public interface I1
{
    protected void M1();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Test(new C())
        End Sub

        Shared Sub Test(Of T As I2)(i2 as T)
            i2.M1()
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Sub M1() Implements I1.M1
        System.Console.WriteLine("C.M1")
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub ProtectedAccess_02()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
    Public Class I1
        Protected Sub M1()

        End Sub
    End Class

    Class I2
        Inherits I1

        Class C1
            Shared Sub Main()
                Test(new I2())
            End Sub

            Shared Sub Test(Of T As I2)(i2 As T)
                i2.M1()
            End Sub
        End Class
    End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe)
            comp1.AssertTheseDiagnostics()

            Dim test = comp1.GetMember(Of MethodSymbol)("I2.C1.Test")
            Assert.True(DirectCast(comp1, Compilation).IsSymbolAccessibleWithin(symbol:=comp1.GetMember("I1.M1"),
                                                       within:=test.ContainingType,
                                                       throughType:=test.TypeParameters(0)))
        End Sub

        <Fact>
        <WorkItem(35827, "https://github.com/dotnet/roslyn/issues/35827")>
        Public Sub ProtectedAccess_03()

            Dim csSource =
"
public interface I1
{
    protected void M1();
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Interface I2
    Inherits I1

    Class C1
        Shared Sub Main()
            Test(Of C, C)(new C())
        End Sub

        Shared Sub Test(Of T1 As I2, T2 As T1)(i2 as T2)
            i2.M1()
        End Sub
    End Class
End Interface

Class C
    Implements I2

    Sub M1() Implements I1.M1
        System.Console.WriteLine("C.M1")
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugExe, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            CompileAndVerify(comp1, expectedOutput:=If(ExecutionConditionUtil.IsMonoOrCoreClr, "C.M1", Nothing), verify:=VerifyOnMonoOrCoreClr)
        End Sub

        <Fact>
        <WorkItem(35998, "https://github.com/dotnet/roslyn/issues/35998")>
        Public Sub Operators_01()
            Dim csSource =
"
public interface I1
{
    public static I1 operator +(I1 x)
    {
        return x;
    }

    public static I1 operator -(I1 x)
    {
        return x;
    }

    public static I1 operator !(I1 x)
    {
        return x;
    }

    public static I1 operator ~(I1 x)
    {
        return x;
    }

    public static I1 operator ++(I1 x)
    {
        return x;
    }

    public static I1 operator --(I1 x)
    {
        return x;
    }

    public static bool operator true(I1 x)
    {
        return true;
    }

    public static bool operator false(I1 x)
    {
        return false;
    }

    public static I1 operator +(I1 x, I1 y)
    {
        return x;
    }

    public static I1 operator -(I1 x, I1 y)
    {
        return x;
    }

    public static I1 operator *(I1 x, I1 y)
    {
        return x;
    }

    public static I1 operator /(I1 x, I1 y)
    {
        return x;
    }

    public static I1 operator %(I1 x, I1 y)
    {
        return x;
    }

    public static I1 operator &(I1 x, I1 y)
    {
        return x;
    }

    public static I1 operator |(I1 x, I1 y)
    {
        return x;
    }

    public static I1 operator ^(I1 x, I1 y)
    {
        return x;
    }

    public static I1 operator <<(I1 x, int y)
    {
        return x;
    }

    public static I1 operator >>(I1 x, int y)
    {
        return x;
    }

    public static I1 operator >(I1 x, I1 y)
    {
        return x;
    }

    public static I1 operator <(I1 x, I1 y)
    {
        return x;
    }

    public static I1 operator >=(I1 x, I1 y)
    {
        return x;
    }

    public static I1 operator <=(I1 x, I1 y)
    {
        return x;
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
class Test2 
    Implements I1

    Shared Sub Main()
        Dim x As I1 = new Test2()
        Dim y As I1 = new Test2()

        x = +x
        x = -x
        x = Not x
        'x = ~x;
        'x = ++x;
        'x = x--;

        x = x + y
        x = x - y
        x = x * y
        x = x / y
        x = x \ y
        x = x Mod y
        x = x ^ y
        if x AndAlso y
        ENd If
        x = x And y
        x = x Or y
        x = x Xor y
        x = x << 1
        x = x >> 2
        x = x > y
        x = x < y
        x = x >= y
        x = x <= y
        x = x Like y
        x = x & y
        if x OrElse y
        ENd If
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected><![CDATA[
BC30487: Operator '+' is not defined for type 'I1'.
        x = +x
            ~~
BC30487: Operator '-' is not defined for type 'I1'.
        x = -x
            ~~
BC30487: Operator 'Not' is not defined for type 'I1'.
        x = Not x
            ~~~~~
BC30452: Operator '+' is not defined for types 'I1' and 'I1'.
        x = x + y
            ~~~~~
BC30452: Operator '-' is not defined for types 'I1' and 'I1'.
        x = x - y
            ~~~~~
BC30452: Operator '*' is not defined for types 'I1' and 'I1'.
        x = x * y
            ~~~~~
BC30452: Operator '/' is not defined for types 'I1' and 'I1'.
        x = x / y
            ~~~~~
BC30452: Operator '\' is not defined for types 'I1' and 'I1'.
        x = x \ y
            ~~~~~
BC30452: Operator 'Mod' is not defined for types 'I1' and 'I1'.
        x = x Mod y
            ~~~~~~~
BC30452: Operator '^' is not defined for types 'I1' and 'I1'.
        x = x ^ y
            ~~~~~
BC30452: Operator 'AndAlso' is not defined for types 'I1' and 'I1'.
        if x AndAlso y
           ~~~~~~~~~~~
BC30452: Operator 'And' is not defined for types 'I1' and 'I1'.
        x = x And y
            ~~~~~~~
BC30452: Operator 'Or' is not defined for types 'I1' and 'I1'.
        x = x Or y
            ~~~~~~
BC30452: Operator 'Xor' is not defined for types 'I1' and 'I1'.
        x = x Xor y
            ~~~~~~~
BC30452: Operator '<<' is not defined for types 'I1' and 'Integer'.
        x = x << 1
            ~~~~~~
BC30452: Operator '>>' is not defined for types 'I1' and 'Integer'.
        x = x >> 2
            ~~~~~~
BC30452: Operator '>' is not defined for types 'I1' and 'I1'.
        x = x > y
            ~~~~~
BC30452: Operator '<' is not defined for types 'I1' and 'I1'.
        x = x < y
            ~~~~~
BC30452: Operator '>=' is not defined for types 'I1' and 'I1'.
        x = x >= y
            ~~~~~~
BC30452: Operator '<=' is not defined for types 'I1' and 'I1'.
        x = x <= y
            ~~~~~~
BC30452: Operator 'Like' is not defined for types 'I1' and 'I1'.
        x = x Like y
            ~~~~~~~~
BC30452: Operator '&' is not defined for types 'I1' and 'I1'.
        x = x & y
            ~~~~~
BC30452: Operator 'OrElse' is not defined for types 'I1' and 'I1'.
        if x OrElse y
           ~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact>
        <WorkItem(35998, "https://github.com/dotnet/roslyn/issues/35998")>
        Public Sub Operators_02()
            Dim csSource =
"
public class C1
{}

public interface I1
{
    public static I1 operator +(C1 x, I1 y)
    {
        return y;
    }

    public static I1 operator -(I1 x, C1 y)
    {
        return x;
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
class Test2 
    Implements I1

    Shared Sub Main()
        Dim x As I1 = new Test2()
        Dim y As C1 = new C1()

        x = x + y
        x = x - y
        x = y + x
        x = y - x
    End Sub
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})
            comp1.AssertTheseDiagnostics(
<expected>
BC30452: Operator '+' is not defined for types 'I1' and 'C1'.
        x = x + y
            ~~~~~
BC30452: Operator '-' is not defined for types 'I1' and 'C1'.
        x = x - y
            ~~~~~
BC30452: Operator '+' is not defined for types 'C1' and 'I1'.
        x = y + x
            ~~~~~
BC30452: Operator '-' is not defined for types 'C1' and 'I1'.
        x = y - x
            ~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(36532, "https://github.com/dotnet/roslyn/issues/36532")>
        Public Sub WindowsRuntimeEvent_01()

            Dim csSource =
"
public interface I1
{
    event System.Action WinRT
    {
        add { throw null; }
        remove { throw null; }
    }
}

public interface I2 : I1
{
    event System.Action I1.WinRT 
    { 
        add { throw null; }
        remove { throw null; }
    }
}
"
            Dim csCompilation = GetCSharpCompilation(csSource, compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.WindowsRuntimeMetadata)).EmitToImageReference()

            Dim source1 =
<compilation>
    <file name="c.vb"><![CDATA[
Public Class C1
    Implements I1

    Custom Event E1 As System.Action Implements I1.WinRT
        AddHandler(value As System.Action)
            Return new System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken()
        End AddHandler
        RemoveHandler(value As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Public Class C2
    Implements I2

    Custom Event E2 As System.Action Implements I1.WinRT
        AddHandler(value As System.Action)
            Return new System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken()
        End AddHandler
        RemoveHandler(value As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
]]></file>
</compilation>

            Dim comp1 = CreateCompilation(source1, options:=TestOptions.DebugDll, targetFramework:=TargetFramework.NetStandardLatest, references:={csCompilation})

            Dim validator = Sub(m As ModuleSymbol)
                                Dim c1 = m.GlobalNamespace.GetTypeMember("C1")
                                Dim c2 = m.GlobalNamespace.GetTypeMember("C2")
                                Dim i1 = c1.Interfaces.Single()
                                Dim i2 = i1.ContainingModule.GlobalNamespace.GetTypeMember("I2")

                                Dim i1WinRT = i1.GetMember(Of EventSymbol)("WinRT")
                                Dim i2WinRT = DirectCast(i2.GetMembers("I1.WinRT").Single(), EventSymbol)

                                Assert.True(i1WinRT.IsWindowsRuntimeEvent)
                                Assert.True(i2WinRT.IsWindowsRuntimeEvent)

                                Assert.Same(c1.GetMember(Of EventSymbol)("E1"), c1.FindImplementationForInterfaceMember(i1WinRT))
                                Assert.Same(c2.GetMember(Of EventSymbol)("E2"), c2.FindImplementationForInterfaceMember(i1WinRT))
                                Assert.Null(i2.FindImplementationForInterfaceMember(i1WinRT))
                                Assert.Null(i2.FindImplementationForInterfaceMember(i1WinRT.AddMethod))
                                Assert.Null(i2.FindImplementationForInterfaceMember(i1WinRT.RemoveMethod))
                                Assert.Same(i1WinRT, i2WinRT.ExplicitInterfaceImplementations.Single())
                                Assert.Same(i1WinRT.AddMethod, i2WinRT.AddMethod.ExplicitInterfaceImplementations.Single())
                                Assert.Same(i1WinRT.RemoveMethod, i2WinRT.RemoveMethod.ExplicitInterfaceImplementations.Single())
                            End Sub

            CompileAndVerify(comp1, verify:=VerifyOnMonoOrCoreClr, sourceSymbolValidator:=validator, symbolValidator:=validator)
        End Sub

    End Class

End Namespace

