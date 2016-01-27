' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Reflection.PortableExecutable
Imports System.Text
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
    Public Class CompilationEmitTests
        Inherits BasicTestBase

        <Fact>
        Public Sub CompilationEmitDiagnostics()
            ' Check that Compilation.Emit actually produces compilation errors.
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Module M1
    Sub Main()
        NoSuchMethod("hello")
    End Sub
End Module
    </file>
</compilation>)

            Dim emitResult As emitResult

            Using output = New MemoryStream()
                emitResult = compilation.Emit(output, Nothing, Nothing, Nothing)
            End Using

            CompilationUtils.AssertTheseDiagnostics(emitResult.Diagnostics,
<expected>
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("hello")
        ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub CompilationEmitWithQuotedMainTypeAndRootnamespace()
            ' Check that compilation with quoted main argument and rootnamespace switches produce diagnostics.
            ' MSBuild can return quoted value which is removed from the command line arguments or by parsing
            ' command line arguments , but we DO NOT unquote arguments which are 
            ' provided by the WithMainTypeName function Or WithRootNamespace (was originally exposed through using 
            ' a Cyrillic Namespace And building Using MSBuild.)

            Dim source = <compilation>
                             <file name="a.vb">
Module Module1
    Sub Main()        
    End Sub
End Module
    </file>
                         </compilation>

            'Compilation with unquote Rootnamespace and MainTypename.
            CreateCompilationWithMscorlibAndVBRuntime(source, options:=TestOptions.ReleaseExe.WithRootNamespace("Test").WithMainTypeName("Test.Module1")).VerifyDiagnostics()

            ' Compilation with quoted Rootnamespace and MainTypename still produces diagnostics.
            ' we do not unquote the options on WithRootnamespace or WithMainTypeName functions 
            CreateCompilationWithMscorlibAndVBRuntime(source, options:=TestOptions.ReleaseExe.WithRootNamespace("""Test""").WithMainTypeName("""Test.Module1""")).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("RootNamespace", """Test""").WithLocation(1, 1),
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("""Test.Module1""").WithLocation(1, 1))

            ' Use of Cyrillic rootnamespace and maintypename
            CreateCompilationWithMscorlibAndVBRuntime(source, options:=TestOptions.ReleaseExe.WithRootNamespace("решения").WithMainTypeName("решения.Module1")).VerifyDiagnostics()

            CreateCompilationWithMscorlibAndVBRuntime(source, options:=TestOptions.ReleaseExe.WithRootNamespace("""решения""").WithMainTypeName("""решения.Module1""")).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("RootNamespace", """решения""").WithLocation(1, 1),
                Diagnostic(ERRID.ERR_StartupCodeNotFound1).WithArguments("""решения.Module1""").WithLocation(1, 1))

        End Sub

        <Fact>
        Public Sub CompilationGetDeclarationDiagnostics()
            ' Check that Compilation.GetDeclarationDiagnostics and Bindings.GetDeclarationDiagnostics work as expected.
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb">
Class C1
    Public Sub Foo1(x as Blech) 
    End Sub
    Public Sub Bar1()
        NoSuchMethod("from C1")
    End Sub
End Class

Partial Class C2
    Public Sub Bar2()
        NoSuchMethod("from C2")
    End Sub
End Class
    </file>
    <file name="b.vb">
Partial Class C2
    Public Sub Foo2(x as Blech) 
    End Sub
End Class

Class C3
    Public Sub Foo3(x as Blech) 
    End Sub
    Public Sub Bar3()
        NoSuchMethod("from C3")
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation.GetDeclarationDiagnostics(),
<expected>
BC30002: Type 'Blech' is not defined.
    Public Sub Foo1(x as Blech) 
                         ~~~~~
BC30002: Type 'Blech' is not defined.
    Public Sub Foo2(x as Blech) 
                         ~~~~~
BC30002: Type 'Blech' is not defined.
    Public Sub Foo3(x as Blech) 
                         ~~~~~
</expected>)

            CompilationUtils.AssertTheseDiagnostics(compilation.GetSemanticModel(CompilationUtils.GetTree(compilation, "a.vb")).GetDeclarationDiagnostics(),
<expected>
BC30002: Type 'Blech' is not defined.
    Public Sub Foo1(x as Blech) 
                         ~~~~~
</expected>)

            CompilationUtils.AssertTheseDiagnostics(compilation.GetSemanticModel(CompilationUtils.GetTree(compilation, "b.vb")).GetDeclarationDiagnostics(),
<expected>
BC30002: Type 'Blech' is not defined.
    Public Sub Foo2(x as Blech) 
                         ~~~~~
BC30002: Type 'Blech' is not defined.
    Public Sub Foo3(x as Blech) 
                         ~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub CompilationGetDiagnostics()
            ' Check that Compilation.GetDiagnostics and Bindings.GetDiagnostics work as expected.
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb">
Class C1
    Public Sub Foo1(x as Blech) 
    End Sub
    Public Sub Bar1()
        NoSuchMethod("from C1")
    End Sub
End Class

Partial Class C2
    Public Sub Bar2()
        NoSuchMethod("from C2")
    End Sub
End Class
    </file>
    <file name="b.vb">
Partial Class C2
    Public Sub Foo2(x as Blech) 
    End Sub
End Class

Class C3
    Public Sub Foo3(x as Blech) 
    End Sub
    Public Sub Bar3()
        NoSuchMethod("from C3")
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation.GetDiagnostics(),
<expected>
BC30002: Type 'Blech' is not defined.
    Public Sub Foo1(x as Blech) 
                         ~~~~~
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("from C1")
        ~~~~~~~~~~~~
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("from C2")
        ~~~~~~~~~~~~
BC30002: Type 'Blech' is not defined.
    Public Sub Foo2(x as Blech) 
                         ~~~~~
BC30002: Type 'Blech' is not defined.
    Public Sub Foo3(x as Blech) 
                         ~~~~~
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("from C3")
        ~~~~~~~~~~~~
</expected>)

            CompilationUtils.AssertTheseDiagnostics(compilation.GetSemanticModel(CompilationUtils.GetTree(compilation, "a.vb")).GetDiagnostics(),
<expected>
BC30002: Type 'Blech' is not defined.
    Public Sub Foo1(x as Blech) 
                         ~~~~~
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("from C1")
        ~~~~~~~~~~~~
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("from C2")
        ~~~~~~~~~~~~
</expected>)

            CompilationUtils.AssertTheseDiagnostics(compilation.GetSemanticModel(CompilationUtils.GetTree(compilation, "b.vb")).GetDiagnostics(),
<expected>
BC30002: Type 'Blech' is not defined.
    Public Sub Foo2(x as Blech) 
                         ~~~~~
BC30002: Type 'Blech' is not defined.
    Public Sub Foo3(x as Blech) 
                         ~~~~~
BC30451: 'NoSuchMethod' is not declared. It may be inaccessible due to its protection level.
        NoSuchMethod("from C3")
        ~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub EmitMetadataOnly()
            ' Check that Compilation.EmitMetadataOnly works.
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb">
Imports System        

Namespace Foo.Bar
    Public Class X
        Public x As Integer
        Private y As String
        Public Shared Sub SayHello()
            Console.WriteLine("hello")
        End Sub
        Public Sub New()
            x = 7
        End Sub
        Friend Function foo(arg as String) as Integer
            Return x
        End Function
    End Class
End Namespace
    </file>
</compilation>)

            Dim emitResult As emitResult
            Dim mdOnlyImage As Byte()

            Using output = New MemoryStream()
                emitResult = compilation.Emit(output, options:=New EmitOptions(metadataOnly:=True))
                mdOnlyImage = output.ToArray()
            End Using

            Assert.True(emitResult.Success)
            CompilationUtils.AssertNoErrors(emitResult.Diagnostics)
            Assert.True(mdOnlyImage.Length > 0, "no metadata emitted")

            Dim srcUsing =
<compilation>
    <file name="a.vb">
Imports System
Imports Foo.Bar        
Class M1
    Sub Main()
        X.SayHello()
    End Sub
End Class

    </file>
</compilation>

            Dim usingComp = CreateCompilationWithMscorlib(srcUsing, references:={MetadataReference.CreateFromImage(mdOnlyImage.AsImmutableOrNull())})

            Using output = New MemoryStream()
                emitResult = usingComp.Emit(output)

                Assert.True(emitResult.Success)
                CompilationUtils.AssertNoErrors(emitResult.Diagnostics)
                Assert.True(output.ToArray().Length > 0, "no metadata emitted")
            End Using
        End Sub

        <WorkItem(4344, "DevDiv_Projects/Roslyn")>
        <Fact>
        Public Sub Bug4344()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Module M
  Sub Main()
    Dim a as String = "A"
    Dim b as String = "B"
    System.Console.WriteLine(a &lt; b)
  End Sub
End Module
    </file>
</compilation>)

            CompileAndVerify(compilation)
        End Sub

        <WorkItem(540643, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540643")>
        <Fact>
        Public Sub Bug6981()
            ' tests different casing of the method
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

MustInherit Class Base
    Public MustOverride Sub Foo()
End Class

Class Derived
    Inherits Base
    
    Public Overrides Sub foO()
        Console.WriteLine("Keep calm and carry on.")
    End Sub

    Shared Sub Main()
        Dim d as New Derived()
        d.foo()
    End Sub
End Class

    </file>
</compilation>

            CompileAndVerify(source,
                             expectedOutput:=<![CDATA[
Keep calm and carry on.]]>)

            source =
            <compilation>
                <file name="a.vb">
Imports System

    MustInherit Class Base
        Public MustOverride Sub Foo()
    End Class

    Class Derived
        Inherits Base
        Public overrides Sub FOo()
        End Sub
    End Class
    
    Class DerivedDerived
        Inherits Derived

        Public Overrides Sub FOO()
            Console.WriteLine("Keep calm and carry on.")
        End Sub

        Shared Sub Main()
            Dim d As New DerivedDerived()
            d.foo()
        End Sub
    End Class

    </file>
            </compilation>

            CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
Keep calm and carry on.]]>)


        End Sub

        <Fact>
        Public Sub MatchMultipleOverrides()
            ' tests different casing of the method

            Dim source =
            <compilation>
                <file name="a.vb">
Imports System

    MustInherit Class Base
        Public MustOverride Sub Foo()
        Public MustOverride Sub foO(x as integer)
    End Class

    MustInherit Class Derived
        Inherits Base
        Public Overrides Sub FOo()
        End Sub
        Public Overloads MustOverride Sub FoO(z As String)
    End Class
    
    Class DerivedDerived
        Inherits Derived

        Public Overrides Sub FOO()
            Console.WriteLine("ABC.")
        End Sub

        Public Overrides Sub fOO(x as Integer)
            Console.WriteLine("Life is {0}.", x)
        End Sub
 
        Public Overrides Sub fOo(y as String)
            Console.WriteLine("This is a {0}.", y)
        End Sub

        Public Overloads Sub foo(x as integer, y as String)
            Console.WriteLine("All done.")
        End Sub

        Shared Sub Main()
            Dim d As Base = New DerivedDerived()
            d.Foo()
            d.foO(42)
            DirectCast(d, Derived).FoO("elderberries")
            DirectCast(d, DerivedDerived).foo(42, "elderberries")
        End Sub
    End Class

    </file>
            </compilation>

            CompileAndVerify(source,
                                 expectedOutput:=<![CDATA[
ABC.
Life is 42.
This is a elderberries.
All done.]]>)


        End Sub

        <Fact>
        Public Sub MatchMultipleOverridesCrossLang()
            'Referenced class:
            'public class Base
            '{
            '    public virtual void BANana(int x) { }
            '    public virtual void banANA(string x) { }
            '}

            Dim source =
            <compilation>
                <file name="a.vb">
Imports System

    Class Derived
        Inherits Base

        Public Overloads Sub bAnAnA()
        End Sub

        Public Overrides Sub banana(x as integer)
            Console.WriteLine("Keep calm and carry on.")
        End Sub

        Public Overrides Sub BANANA(xyz as String)
            Console.WriteLine("The authorities have been called.")
        End Sub
 
        Shared Sub Main()
            Dim d As Base = New Derived()
            d.banana(1)
            d.banana("hello")
        End Sub
    End Class

    </file>
            </compilation>

            Dim verifier = CompileAndVerify(source,
                                            additionalRefs:={TestReferences.SymbolsTests.DifferByCase.CsharpDifferCaseOverloads},
                                 expectedOutput:=<![CDATA[
Keep calm and carry on.
The authorities have been called.]]>)

            Dim compilation = verifier.Compilation
            Dim derivedClass = compilation.GetTypeByMetadataName("Derived")
            Dim allMethods = derivedClass.GetMembers("baNANa").OfType(Of MethodSymbol)()

            AssertNoErrors(compilation)

            ' All methods in Derived should have metadata name "BANana" except the 2nd override.
            Dim count = 0
            For Each m In allMethods
                count = count + 1
                If m.ParameterCount = 1 AndAlso m.Parameters(0).Name = "xyz" Then
                    Assert.Equal("banANA", m.MetadataName)
                Else
                    Assert.Equal("BANana", m.MetadataName)
                End If
            Next
            Assert.Equal(3, count)

        End Sub

        <Fact>
        Public Sub EnsureModulesRoundTrip()
            Dim source =
            <compilation>
                <file name="a.vb">
Module M
  Sub F()
  End Sub
End Module
    </file>
            </compilation>

            Dim c As Compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            Dim s = New MemoryStream()
            Assert.True(c.Emit(s).Success)
            c = VisualBasicCompilation.Create("Nothing", references:={MetadataReference.CreateFromImage(s.ToImmutable())})

            Dim m = c.GlobalNamespace.GetModuleMembers()
            Assert.Equal(m.Single().TypeKind, TypeKind.Module)

            Assert.Equal(1, c.GlobalNamespace.GetModuleMembers("M").Length)
            Assert.Equal(0, c.GlobalNamespace.GetModuleMembers("Z").Length)
        End Sub

#Region "PE and metadata bits"

        <Fact()>
        Public Sub TestRuntimeMetaDataVersion()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithReferences(source, {TestReferences.NetFx.v2_0_50727.mscorlib}, Nothing)
            Dim metadata = ModuleMetadata.CreateFromImage(compilation.EmitToArray())

            ' this is built with a 2.0 mscorlib. The runtimeMetadataVersion should be the same as the runtimeMetadataVersion stored in the assembly
            ' that contains System.Object.
            Dim metadataReader = metadata.Module.GetMetadataReader()
            Assert.Equal("v2.0.50727", metadataReader.MetadataVersion)
        End Sub

        <Fact()>
        Public Sub TestCorFlags()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, options:=Nothing)
            Dim peHeaders = New peHeaders(compilation.EmitToStream())
            Assert.Equal(CorFlags.ILOnly, peHeaders.CorHeader.Flags)

            compilation = CreateCompilationWithMscorlib(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(Platform.X86))
            peHeaders = New peHeaders(compilation.EmitToStream())
            Assert.Equal(CorFlags.ILOnly Or CorFlags.Requires32Bit, peHeaders.CorHeader.Flags)

            compilation = CreateCompilationWithMscorlib(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(Platform.X64))
            peHeaders = New peHeaders(compilation.EmitToStream())
            Assert.Equal(CorFlags.ILOnly, peHeaders.CorHeader.Flags)
            Assert.True(peHeaders.Requires64Bits)
            Assert.True(peHeaders.RequiresAmdInstructionSet)

            compilation = CreateCompilationWithMscorlib(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(Platform.AnyCpu32BitPreferred))
            peHeaders = New peHeaders(compilation.EmitToStream())
            Assert.False(peHeaders.Requires64Bits)
            Assert.False(peHeaders.RequiresAmdInstructionSet)
            Assert.Equal(CorFlags.ILOnly Or CorFlags.Requires32Bit Or CorFlags.Prefers32Bit, peHeaders.CorHeader.Flags)
        End Sub

        <Fact()>
        Public Sub TestCOFFAndPEOptionalHeaders32()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithPlatform(Platform.AnyCpu))
            Dim peHeaders = New peHeaders(compilation.EmitToStream())

            'interesting COFF bits
            Assert.False(peHeaders.Requires64Bits)
            Assert.True(peHeaders.IsDll)
            Assert.False(peHeaders.IsExe)
            Assert.True(peHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware))

            'interesting Optional PE header bits
            'We will use a range beginning with &H50 to identify the Roslyn VB compiler family.
            Assert.Equal(&H50, peHeaders.PEHeader.MajorLinkerVersion)
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion)
            Assert.Equal(&H10000000UL, peHeaders.PEHeader.ImageBase)

            'section alignment is fixed at 0x2000. 
            Assert.Equal(&H200, peHeaders.PEHeader.FileAlignment)
            Assert.Equal(CType(&H8540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE

            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfStackReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfStackCommit)
            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfHeapReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfHeapCommit)

            ' test an exe as well:
            compilation = CreateCompilationWithMscorlib(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(Platform.AnyCpu))
            peHeaders = New peHeaders(compilation.EmitToStream())

            'interesting COFF bits
            Assert.False(peHeaders.Requires64Bits)
            Assert.False(peHeaders.IsDll)
            Assert.True(peHeaders.IsExe)

            'interesting Optional PE header bits
            'We will use a range beginning with &H50 to identify the Roslyn VB compiler family.
            Assert.Equal(&H50, peHeaders.PEHeader.MajorLinkerVersion)
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion)
            Assert.Equal(CType(&H400000, ULong), peHeaders.PEHeader.ImageBase)

            Assert.Equal(&H200, peHeaders.PEHeader.FileAlignment)
            Assert.True(peHeaders.IsConsoleApplication)
            Assert.Equal(CType(&H8540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE

            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfStackReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfStackCommit)
            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfHeapReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfHeapCommit)
        End Sub

        <Fact()>
        Public Sub TestCOFFAndPEOptionalHeadersArm()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithPlatform(Platform.Arm))
            Dim peHeaders = New peHeaders(compilation.EmitToStream())

            'interesting COFF bits
            Assert.False(peHeaders.Requires64Bits)
            Assert.True(peHeaders.IsDll)
            Assert.False(peHeaders.IsExe)
            Assert.True(peHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware))

            'interesting Optional PE header bits
            'We will use a range beginning with &H50 to identify the Roslyn VB compiler family.
            Assert.Equal(&H50, peHeaders.PEHeader.MajorLinkerVersion)
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion)
            Assert.Equal(&H10000000UL, peHeaders.PEHeader.ImageBase)

            'section alignment is fixed at 0x2000. 
            Assert.Equal(&H200, peHeaders.PEHeader.FileAlignment)
            Assert.Equal(CType(&H8540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE

            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfStackReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfStackCommit)
            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfHeapReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfHeapCommit)

            ' test an exe as well:
            compilation = CreateCompilationWithMscorlib(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(Platform.AnyCpu))
            peHeaders = New peHeaders(compilation.EmitToStream())

            'interesting COFF bits
            Assert.False(peHeaders.Requires64Bits)
            Assert.False(peHeaders.IsDll)
            Assert.True(peHeaders.IsExe)

            'interesting Optional PE header bits
            'We will use a range beginning with &H50 to identify the Roslyn VB compiler family.
            Assert.Equal(&H50, peHeaders.PEHeader.MajorLinkerVersion)
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion)
            Assert.Equal(CType(&H400000, ULong), peHeaders.PEHeader.ImageBase)

            Assert.Equal(&H200, peHeaders.PEHeader.FileAlignment)
            Assert.True(peHeaders.IsConsoleApplication)
            Assert.Equal(CType(&H8540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE

            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfStackReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfStackCommit)
            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfHeapReserve)
            Assert.Equal(&H1000UL, peHeaders.PEHeader.SizeOfHeapCommit)
        End Sub

        <Fact()>
        Public Sub TestCOFFAndPEOptionalHeaders64()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithPlatform(Platform.X64))
            Dim peHeaders = New peHeaders(compilation.EmitToStream())

            'interesting COFF bits
            Assert.True(peHeaders.Requires64Bits)
            Assert.True(peHeaders.IsDll)
            Assert.False(peHeaders.IsExe)
            Assert.True(peHeaders.CoffHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware))

            'interesting Optional PE header bits
            'We will use a range beginning with &H50 to identify the Roslyn VB compiler family.
            Assert.Equal(&H50, peHeaders.PEHeader.MajorLinkerVersion)
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion)
            Assert.Equal(&H180000000UL, peHeaders.PEHeader.ImageBase)

            'section alignment is fixed at 0x2000. 
            Assert.Equal(&H200, peHeaders.PEHeader.FileAlignment)   'doesn't change based on architecture
            Assert.Equal(CType(&H8540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE

            ' test an exe as well:
            compilation = CreateCompilationWithMscorlib(source, options:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(Platform.X64))
            peHeaders = New peHeaders(compilation.EmitToStream())

            'interesting COFF bits
            Assert.True(peHeaders.Requires64Bits)
            Assert.False(peHeaders.IsDll)
            Assert.True(peHeaders.IsExe)

            'interesting Optional PE header bits
            'We will use a range beginning with &H50 to identify the Roslyn VB compiler family.
            Assert.Equal(&H50, peHeaders.PEHeader.MajorLinkerVersion)
            Assert.Equal(0, peHeaders.PEHeader.MinorLinkerVersion)
            Assert.Equal(&H140000000UL, peHeaders.PEHeader.ImageBase)

            Assert.Equal(&H200, peHeaders.PEHeader.FileAlignment)
            Assert.True(peHeaders.IsConsoleApplication)
            Assert.Equal(CType(&H8540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE

            'Verify additional items
            Assert.Equal(&H400000UL, peHeaders.PEHeader.SizeOfStackReserve)
            Assert.Equal(&H4000UL, peHeaders.PEHeader.SizeOfStackCommit)
            Assert.Equal(&H100000UL, peHeaders.PEHeader.SizeOfHeapReserve) ' is the 32bit value!
            Assert.Equal(&H2000UL, peHeaders.PEHeader.SizeOfHeapCommit)
        End Sub


        <Fact()>
        Public Sub CheckDllCharacteristicsHighEntropyVA()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source)
            Dim peHeaders = New PEHeaders(compilation.EmitToStream(options:=New EmitOptions(highEntropyVirtualAddressSpace:=True)))
            Assert.Equal(CType(&H8560, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE | HIGH_ENTROPY_VA (0x20)
        End Sub

        <WorkItem(764418, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/764418")>
        <Fact()>
        Public Sub CheckDllCharacteristicsWinRtApp()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, options:=New VisualBasicCompilationOptions(OutputKind.WindowsRuntimeApplication))
            Dim peHeaders = New peHeaders(compilation.EmitToStream())
            Assert.Equal(CType(&H9540, UShort), peHeaders.PEHeader.DllCharacteristics)  'DYNAMIC_BASE | NX_COMPAT | NO_SEH | TERMINAL_SERVER_AWARE | HIGH_ENTROPY_VA (0x20)
        End Sub

        <Fact()>
        Public Sub TestBaseAddress()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            ' last four hex digits get zero'ed out
            Dim compilation = CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseExe)
            Dim peHeaders = New PEHeaders(compilation.EmitToStream(options:=New EmitOptions(baseAddress:=&H10111111)))
            Assert.Equal(CType(&H10110000, ULong), peHeaders.PEHeader.ImageBase)

            ' rounded up by 0x8000
            compilation = CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseExe)
            peHeaders = New PEHeaders(compilation.EmitToStream(options:=New EmitOptions(baseAddress:=&H8000)))
            Assert.Equal(CType(&H10000, ULong), peHeaders.PEHeader.ImageBase)

            ' valued less than 0x8000 are being ignored
            compilation = CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseExe)
            peHeaders = New PEHeaders(compilation.EmitToStream(options:=New EmitOptions(baseAddress:=&H7FFF)))
            Assert.Equal(&H400000UL, peHeaders.PEHeader.ImageBase)

            ' default for 32 bit
            compilation = CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseExe.WithPlatform(Platform.X86))
            peHeaders = New peHeaders(compilation.EmitToStream())
            Assert.Equal(&H400000UL, peHeaders.PEHeader.ImageBase)

            compilation = CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseExe.WithPlatform(Platform.X64))
            peHeaders = New peHeaders(compilation.EmitToStream())
            Assert.Equal(&H140000000UL, peHeaders.PEHeader.ImageBase)

            compilation = CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseDll.WithPlatform(Platform.X64))
            peHeaders = New peHeaders(compilation.EmitToStream())
            Assert.Equal(&H180000000UL, peHeaders.PEHeader.ImageBase)

        End Sub

        <Fact()>
        Public Sub TestFileAlignment()
            Dim source =
            <compilation>
                <file name="a.vb">
        Class C1
            Public Shared Sub Main()
            End Sub
        End Class
                </file>
            </compilation>

            Dim compilation = CreateCompilationWithMscorlib(source)
            Dim peHeaders = New PEHeaders(compilation.EmitToStream(options:=New EmitOptions(fileAlignment:=1024)))
            Assert.Equal(1024, peHeaders.PEHeader.FileAlignment)

            compilation = CreateCompilationWithMscorlib(source)
            peHeaders = New peHeaders(compilation.EmitToStream(options:=New EmitOptions(fileAlignment:=4096)))
            Assert.Equal(4096, peHeaders.PEHeader.FileAlignment)
        End Sub

#End Region


        <Fact()>
        Public Sub Bug10273()

            Dim source =
            <compilation name="C">
                <file name="a.vb">
Imports System

    Public Structure C
        Public C As Integer
        Public Shared B As Integer = 12

        Public Sub F()
        End Sub

        Public A As Integer
    End Structure

    Public Delegate Sub B()

    Public Class A

        Public C As Integer
        Public Shared B As Integer = 12

        Public Sub F()
        End Sub

        Public A As Integer

        Public Property I As Integer
        Public Sub E()
        End Sub
        Public Property H As Integer
        Public Property G As Integer

        Public Event L As System.Action
        Public Sub D()
        End Sub
        Public Event K As System.Action
        Public Event J As System.Action

        Public Class O
        End Class
        Public Class N
        End Class
        Public Class M
        End Class

        Partial Public Class N
        End Class
        Partial Public Class M
        End Class
        Partial Public Class O
        End Class

        Public Sub F(x as Integer)
        End Sub
        Public Sub E(x as Integer)
        End Sub
        Public Sub D(x as Integer)
        End Sub
    End Class

    Namespace F
    End Namespace

    Public Class G
    End Class

    Namespace E
    End Namespace
    Namespace D
    End Namespace
    </file>
            </compilation>

            Dim sourceSymbolValidator = Sub(m As ModuleSymbol)
                                            Dim expectedGlobalMembers = {"C", "B", "A", "F", "G", "E", "D"}
                                            Dim actualGlobalMembers = DirectCast(m, SourceModuleSymbol).GlobalNamespace.GetMembers().ToArray()

                                            Assert.NotNull(m.GlobalNamespace.ContainingModule)
                                            Assert.Equal(Of String)("C.dll", m.GlobalNamespace.ContainingModule.ToString)

                                            For i As Integer = 0 To Math.Max(expectedGlobalMembers.Length, actualGlobalMembers.Length) - 1
                                                Assert.Equal(expectedGlobalMembers(i), actualGlobalMembers(i).Name)
                                            Next

                                            Dim expectedAMembers = {".cctor", ".ctor",
                                                                    "C", "B", "F", "A",
                                                                    "_I", "get_I", "set_I", "I",
                                                                    "E",
                                                                    "_H", "get_H", "set_H", "H",
                                                                    "_G", "get_G", "set_G", "G",
                                                                    "LEvent", "add_L", "remove_L", "L",
                                                                    "D",
                                                                    "KEvent", "add_K", "remove_K", "K",
                                                                    "JEvent", "add_J", "remove_J", "J",
                                                                    "O", "N", "M",
                                                                    "F", "E", "D"}
                                            Dim actualAMembers = DirectCast(m, SourceModuleSymbol).GlobalNamespace.GetTypeMembers("A").Single().GetMembers().ToArray()

                                            For i As Integer = 0 To Math.Max(expectedAMembers.Length, actualAMembers.Length) - 1
                                                Assert.Equal(expectedAMembers(i), actualAMembers(i).Name)
                                            Next

                                            Dim expectedBMembers = {".ctor", "BeginInvoke", "EndInvoke", "Invoke"}
                                            Dim actualBMembers = DirectCast(m, SourceModuleSymbol).GlobalNamespace.GetTypeMembers("B").Single().GetMembers().ToArray()

                                            For i As Integer = 0 To Math.Max(expectedBMembers.Length, actualBMembers.Length) - 1
                                                Assert.Equal(expectedBMembers(i), actualBMembers(i).Name)
                                            Next

                                            Dim expectedCMembers = {".cctor", ".ctor",
                                                                    "C", "B", "F", "A"}
                                            Dim actualCMembers = DirectCast(m, SourceModuleSymbol).GlobalNamespace.GetTypeMembers("C").Single().GetMembers().ToArray()

                                            For i As Integer = 0 To Math.Max(expectedCMembers.Length, actualCMembers.Length) - 1
                                                Assert.Equal(expectedCMembers(i), actualCMembers(i).Name)
                                            Next
                                        End Sub

            Dim peSymbolValidator = Sub(m As ModuleSymbol)
                                        Dim expectedAMembers = {"C", "B", "A",
                                                                ".ctor",
                                                                "F",
                                                                "get_I", "set_I",
                                                                "E",
                                                                "get_H", "set_H",
                                                                "get_G", "set_G",
                                                                "add_L", "remove_L",
                                                                "D",
                                                                "add_K", "remove_K",
                                                                "add_J", "remove_J",
                                                                "F", "E", "D",
                                                                "I", "H", "G",
                                                                "L", "K", "J",
                                                                "O", "N", "M"}
                                        Dim actualAMembers = m.GlobalNamespace.GetTypeMembers("A").Single().GetMembers().ToArray()

                                        For i As Integer = 0 To Math.Max(expectedAMembers.Length, actualAMembers.Length) - 1
                                            Assert.Equal(expectedAMembers(i), actualAMembers(i).Name)
                                        Next

                                        Dim expectedBMembers = {".ctor", "BeginInvoke", "EndInvoke", "Invoke"}
                                        Dim actualBMembers = m.GlobalNamespace.GetTypeMembers("B").Single().GetMembers().ToArray()

                                        For i As Integer = 0 To Math.Max(expectedBMembers.Length, actualBMembers.Length) - 1
                                            Assert.Equal(expectedBMembers(i), actualBMembers(i).Name)
                                        Next

                                        Dim expectedCMembers = {"C", "B", "A", ".ctor", "F"}
                                        Dim actualCMembers = m.GlobalNamespace.GetTypeMembers("C").Single().GetMembers().ToArray()

                                        For i As Integer = 0 To Math.Max(expectedCMembers.Length, actualCMembers.Length) - 1
                                            Assert.Equal(expectedCMembers(i), actualCMembers(i).Name)
                                        Next
                                    End Sub

            CompileAndVerify(source,
                             sourceSymbolValidator:=sourceSymbolValidator,
                             symbolValidator:=peSymbolValidator)


        End Sub

        ''' <summary>
        ''' Validate the contents of the DeclSecurity metadata table.
        ''' </summary>
        Private Shared Sub ValidateDeclSecurity(compilation As VisualBasicCompilation, ParamArray expectedEntries As DeclSecurityEntry())
            Dim metadataReader = ModuleMetadata.CreateFromImage(compilation.EmitToArray()).Module.GetMetadataReader()
            Assert.Equal(expectedEntries.Length, metadataReader.DeclarativeSecurityAttributes.Count)

            Dim i = 0
            For Each actualHandle In metadataReader.DeclarativeSecurityAttributes
                Dim actual = metadataReader.GetDeclarativeSecurityAttribute(actualHandle)
                Dim expected = expectedEntries(CInt(i))

                Assert.Equal(expected.ActionFlags, actual.Action)
                Assert.Equal(GetExpectedParentToken(metadataReader, expected), actual.Parent)

                Dim actualPermissionSetBytes = metadataReader.GetBlobBytes(actual.PermissionSet)
                Dim actualPermissionSet = New String(actualPermissionSetBytes.Select(Function(b) ChrW(b)).ToArray())

                Assert.Equal(expected.PermissionSet, actualPermissionSet)

                i = i + 1
            Next
        End Sub

        Private Shared Function GetExpectedParentToken(metadataReader As MetadataReader, entry As DeclSecurityEntry) As EntityHandle
            Select Case entry.ParentKind
                Case SymbolKind.Assembly
                    Return EntityHandle.AssemblyDefinition

                Case SymbolKind.NamedType
                    Return GetTokenForType(metadataReader, entry.ParentNameOpt)

                Case SymbolKind.Method
                    Return GetTokenForMethod(metadataReader, entry.ParentNameOpt)

                Case Else
                    Throw TestExceptionUtilities.UnexpectedValue(entry.ParentKind)
            End Select
        End Function

        Private Shared Function GetTokenForType(metadataReader As MetadataReader, typeName As String) As TypeDefinitionHandle
            Assert.NotNull(typeName)
            Assert.NotEmpty(typeName)

            For Each typeDef In metadataReader.TypeDefinitions
                Dim typeDefRow = metadataReader.GetTypeDefinition(typeDef)
                Dim name As String = metadataReader.GetString(typeDefRow.Name)

                If typeName.Equals(name) Then
                    Return typeDef
                End If
            Next

            AssertEx.Fail("Unable to find type:" + typeName)
            Return Nothing
        End Function

        Private Shared Function GetTokenForMethod(metadataReader As MetadataReader, methodName As String) As MethodDefinitionHandle
            Assert.NotNull(methodName)
            Assert.NotEmpty(methodName)

            For Each methodDef In metadataReader.MethodDefinitions
                Dim name = metadataReader.GetString(metadataReader.GetMethodDefinition(methodDef).Name)

                If methodName.Equals(name) Then
                    Return methodDef
                End If
            Next

            AssertEx.Fail("Unable to find method:" + methodName)
            Return Nothing
        End Function

        Private Class DeclSecurityEntry
            Public ActionFlags As DeclarativeSecurityAction
            Public ParentKind As SymbolKind
            Public ParentNameOpt As String
            Public PermissionSet As String
        End Class

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeOnType()
            Dim source =
<compilation name="Test">
    <file name="a.vb">
Imports System.Security
Imports System.Security.Permissions

class MySecurityAttribute 
    inherits SecurityAttribute

    public sub new (a as SecurityAction)
        mybase.new(a)
    end sub

    public overrides function CreatePermission() as IPermission 
        return nothing
    end function
end class

&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
&lt;MySecurityAttribute(SecurityAction.Assert)&gt;
public class C
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            compilation.VerifyDiagnostics()

            ValidateDeclSecurity(compilation,
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.NamedType,
                                    .ParentNameOpt = "C",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"},
                                    New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Assert,
                                    .ParentKind = SymbolKind.NamedType,
                                    .ParentNameOpt = "C",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H50) &
                                        "MySecurityAttribute, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" &
                                        ChrW(1) &
                                        ChrW(0)})
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeOnMethod()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions
    
public class C
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
    public sub foo()
    end sub
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            compilation.VerifyDiagnostics()

            ValidateDeclSecurity(compilation,
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.Method,
                                    .ParentNameOpt = "foo",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"})
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeSameTypeSameAction()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions

&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
public class C
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            compilation.VerifyDiagnostics()

            ValidateDeclSecurity(compilation,
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.NamedType,
                                    .ParentNameOpt = "C",
                                    .PermissionSet =
                                        "." &
                                        ChrW(2) &
 _
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1" &
 _
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"})
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeSameMethodSameAction()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions
    
public class C
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
    public sub foo()
    end sub
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            compilation.VerifyDiagnostics()

            ValidateDeclSecurity(compilation,
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.Method,
                                    .ParentNameOpt = "foo",
                                    .PermissionSet =
                                        "." &
                                        ChrW(2) &
 _
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1" &
 _
                                ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"})
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeSameTypeDifferentAction()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions

&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
&lt;PrincipalPermission(SecurityAction.Assert, Role:="User2")&gt;
public class C
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            compilation.VerifyDiagnostics()

            ValidateDeclSecurity(compilation,
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.NamedType,
                                    .ParentNameOpt = "C",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"},
                                New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Assert,
                                    .ParentKind = SymbolKind.NamedType,
                                    .ParentNameOpt = "C",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User2"})
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeSameMethodDifferentAction()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions

public class C
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
    &lt;PrincipalPermission(SecurityAction.Assert, Role:="User2")&gt;
    public sub foo()
    end sub
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            compilation.VerifyDiagnostics()

            ValidateDeclSecurity(compilation,
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.Method,
                                    .ParentNameOpt = "foo",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"},
                                New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Assert,
                                    .ParentKind = SymbolKind.Method,
                                    .ParentNameOpt = "foo",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User2"})
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeDifferentTypeSameAction()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions
    
public class C1
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
    public sub foo1()
    end sub
end class

    
public class C2
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
    public sub foo2()
    end sub
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            compilation.VerifyDiagnostics()

            ValidateDeclSecurity(compilation,
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.Method,
                                    .ParentNameOpt = "foo1",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"},
                                New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.Method,
                                    .ParentNameOpt = "foo2",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"})
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributeDifferentMethodSameAction()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions

&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
public class C1
end class

&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;
public class C2
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            compilation.VerifyDiagnostics()

            ValidateDeclSecurity(compilation,
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.NamedType,
                                    .ParentNameOpt = "C1",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"},
                                New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.NamedType,
                                    .ParentNameOpt = "C2",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"})
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributesMultipleAssemblyTypeMethod()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Security.Permissions

&lt;assembly: SecurityPermission(SecurityAction.RequestOptional, RemotingConfiguration:=true)&gt;
&lt;assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode:=true)&gt;

&lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;        
public class C1
    &lt;PrincipalPermission(SecurityAction.Assert, Role:="User2")&gt;
    public sub foo1()
    end sub
end class

Module Program
    Sub Main()
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source)
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestOptional").WithArguments("RequestOptional", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.RequestMinimum").WithArguments("RequestMinimum", "Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."))

            ValidateDeclSecurity(compilation,
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.RequestOptional,
                                    .ParentKind = SymbolKind.Assembly,
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H84) &
                                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&H1A) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&H2) &
                                        ChrW(&H15) &
                                        "RemotingConfiguration" &
                                        ChrW(&H1)},
                                New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.RequestMinimum,
                                    .ParentKind = SymbolKind.Assembly,
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H84) &
                                        "System.Security.Permissions.SecurityPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&H12) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&H2) &
                                        ChrW(&HD) &
                                        "UnmanagedCode" &
                                        ChrW(&H1)},
                                 New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Demand,
                                    .ParentKind = SymbolKind.NamedType,
                                    .ParentNameOpt = "C1",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User1"},
                                New DeclSecurityEntry() With {
                                    .ActionFlags = DeclarativeSecurityAction.Assert,
                                    .ParentKind = SymbolKind.Method,
                                    .ParentNameOpt = "foo1",
                                    .PermissionSet =
                                        "." &
                                        ChrW(1) &
                                        ChrW(&H80) &
                                        ChrW(&H85) &
                                        "System.Security.Permissions.PrincipalPermissionAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                                        ChrW(&HE) &
                                        ChrW(&H1) &
                                        ChrW(&H54) &
                                        ChrW(&HE) &
                                        ChrW(&H4) &
                                        "Role" &
                                        ChrW(&H5) &
                                        "User2"})
        End Sub

        <Fact()>
        Public Sub TestSecurityPseudoCustomAttributesMultipleAssemblyTypeMethodSymbolApi()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Security
Imports System.Security.Permissions
Imports System.Security.Principal

    &lt;assembly: SecurityPermission(SecurityAction.RequestOptional, RemotingConfiguration:=true)&gt;
    &lt;assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode:=true)&gt;
namespace n
    &lt;PrincipalPermission(SecurityAction.Demand, Role:="User1")&gt;        
    &lt;PrincipalPermission(SecurityAction.Assert, Role:="User2")&gt;        
	public class C1
        &lt;PrincipalPermission(SecurityAction.Demand, Role:="User3")&gt;
        &lt;PrincipalPermission(SecurityAction.Assert, Role:="User4")&gt;
        public sub foo1()
        end sub
	end class

Module Program
    Sub Main()
    End Sub
End Module
end namespace
    </file>
</compilation>

            Dim attributeValidator As Action(Of ModuleSymbol) =
                Sub([module] As ModuleSymbol)
                    Dim assembly = [module].ContainingAssembly
                    Dim ns = DirectCast([module].GlobalNamespace.GetMembers("N").Single, NamespaceSymbol)
                    Dim namedType = DirectCast(ns.GetMembers("C1").Single, NamedTypeSymbol)
                    Dim type = DirectCast(namedType, Microsoft.Cci.ITypeDefinition)
                    Dim method = DirectCast(namedType.GetMembers("foo1").Single, Microsoft.Cci.IMethodDefinition)

                    Dim sourceAssembly = DirectCast(assembly, SourceAssemblySymbol)
                    Dim compilation = sourceAssembly.DeclaringCompilation

                    Dim emitOptions = New emitOptions(outputNameOverride:=sourceAssembly.Name)

                    Dim cciModule = DirectCast(
                        New PEAssemblyBuilder(sourceAssembly, emitOptions, OutputKind.DynamicallyLinkedLibrary, GetDefaultModulePropertiesForSerialization(), SpecializedCollections.EmptyEnumerable(Of ResourceDescription), Nothing),
                        Cci.IModule)
                    Dim assemblySecurityAttributes As IEnumerable(Of Cci.SecurityAttribute) = cciModule.AssemblySecurityAttributes

                    ' Verify Assembly security attributes
                    Assert.Equal(2, assemblySecurityAttributes.Count)
                    Dim emittedName = MetadataTypeName.FromNamespaceAndTypeName("System.Security.Permissions", "SecurityPermissionAttribute")
                    Dim securityPermissionAttr As NamedTypeSymbol = sourceAssembly.CorLibrary.LookupTopLevelMetadataType(emittedName, True)

                    ' Verify <assembly: SecurityPermission(SecurityAction.RequestOptional, RemotingConfiguration:=true)>
                    Dim securityAttribute As Cci.SecurityAttribute = assemblySecurityAttributes.First()
                    Assert.Equal(DeclarativeSecurityAction.RequestOptional, securityAttribute.Action)
                    Dim typeAttribute = DirectCast(securityAttribute.Attribute, VisualBasicAttributeData)
                    Assert.Equal(securityPermissionAttr, typeAttribute.AttributeClass)
                    Assert.Equal(1, typeAttribute.CommonConstructorArguments.Length)
                    typeAttribute.VerifyValue(0, TypedConstantKind.Enum, CInt(DeclarativeSecurityAction.RequestOptional))
                    Assert.Equal(1, typeAttribute.CommonNamedArguments.Length)
                    typeAttribute.VerifyNamedArgumentValue(0, "RemotingConfiguration", TypedConstantKind.Primitive, True)

                    ' Verify <assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode:=true)>
                    securityAttribute = assemblySecurityAttributes.Last()
                    Assert.Equal(DeclarativeSecurityAction.RequestMinimum, securityAttribute.Action)
                    typeAttribute = DirectCast(securityAttribute.Attribute, VisualBasicAttributeData)
                    Assert.Equal(securityPermissionAttr, typeAttribute.AttributeClass)
                    Assert.Equal(1, typeAttribute.CommonConstructorArguments.Length)
                    typeAttribute.VerifyValue(0, TypedConstantKind.Enum, CInt(DeclarativeSecurityAction.RequestMinimum))
                    Assert.Equal(1, typeAttribute.CommonNamedArguments.Length)
                    typeAttribute.VerifyNamedArgumentValue(0, "UnmanagedCode", TypedConstantKind.Primitive, True)

                    ' Get System.Security.Permissions.PrincipalPermissionAttribute
                    emittedName = MetadataTypeName.FromNamespaceAndTypeName("System.Security.Permissions", "PrincipalPermissionAttribute")
                    Dim principalPermAttr As NamedTypeSymbol = sourceAssembly.CorLibrary.LookupTopLevelMetadataType(emittedName, True)
                    Assert.NotNull(principalPermAttr)

                    ' Verify type security attributes: different security action
                    Debug.Assert(type.HasDeclarativeSecurity)
                    Dim typeSecurityAttributes As IEnumerable(Of Microsoft.Cci.SecurityAttribute) = type.SecurityAttributes
                    Assert.Equal(2, typeSecurityAttributes.Count)

                    ' Verify <PrincipalPermission(SecurityAction.Demand, Role:="User1")>
                    securityAttribute = typeSecurityAttributes.First()
                    Assert.Equal(DeclarativeSecurityAction.Demand, securityAttribute.Action)
                    typeAttribute = DirectCast(securityAttribute.Attribute, VisualBasicAttributeData)
                    Assert.Equal(principalPermAttr, typeAttribute.AttributeClass)
                    Assert.Equal(1, typeAttribute.CommonConstructorArguments.Length)
                    typeAttribute.VerifyValue(0, TypedConstantKind.Enum, CInt(DeclarativeSecurityAction.Demand))
                    Assert.Equal(1, typeAttribute.CommonNamedArguments.Length)
                    typeAttribute.VerifyNamedArgumentValue(0, "Role", TypedConstantKind.Primitive, "User1")

                    ' Verify <PrincipalPermission(SecurityAction.RequestOptional, Role:="User2")>
                    securityAttribute = typeSecurityAttributes.Last()
                    Assert.Equal(DeclarativeSecurityAction.Assert, securityAttribute.Action)
                    typeAttribute = DirectCast(securityAttribute.Attribute, VisualBasicAttributeData)
                    Assert.Equal(principalPermAttr, typeAttribute.AttributeClass)
                    Assert.Equal(1, typeAttribute.CommonConstructorArguments.Length)
                    typeAttribute.VerifyValue(0, TypedConstantKind.Enum, CInt(DeclarativeSecurityAction.Assert))
                    Assert.Equal(1, typeAttribute.CommonNamedArguments.Length)
                    typeAttribute.VerifyNamedArgumentValue(0, "Role", TypedConstantKind.Primitive, "User2")

                    ' Verify method security attributes: same security action
                    Debug.Assert(method.HasDeclarativeSecurity)
                    Dim methodSecurityAttributes As IEnumerable(Of Microsoft.Cci.SecurityAttribute) = method.SecurityAttributes
                    Assert.Equal(2, methodSecurityAttributes.Count)

                    ' Verify <PrincipalPermission(SecurityAction.Demand, Role:="User3")>
                    securityAttribute = methodSecurityAttributes.First()
                    Assert.Equal(DeclarativeSecurityAction.Demand, securityAttribute.Action)
                    Dim methodAttribute = DirectCast(securityAttribute.Attribute, VisualBasicAttributeData)
                    Assert.Equal(principalPermAttr, methodAttribute.AttributeClass)
                    Assert.Equal(1, methodAttribute.CommonConstructorArguments.Length)
                    methodAttribute.VerifyValue(0, TypedConstantKind.Enum, CInt(DeclarativeSecurityAction.Demand))
                    Assert.Equal(1, methodAttribute.CommonNamedArguments.Length)
                    methodAttribute.VerifyNamedArgumentValue(0, "Role", TypedConstantKind.Primitive, "User3")

                    ' Verify PrincipalPermission(SecurityAction.RequestOptional, Role:="User4")
                    securityAttribute = methodSecurityAttributes.Last()
                    Assert.Equal(DeclarativeSecurityAction.Assert, securityAttribute.Action)
                    methodAttribute = DirectCast(securityAttribute.Attribute, VisualBasicAttributeData)
                    Assert.Equal(principalPermAttr, methodAttribute.AttributeClass)
                    Assert.Equal(1, methodAttribute.CommonConstructorArguments.Length)
                    methodAttribute.VerifyValue(0, TypedConstantKind.Enum, CInt(DeclarativeSecurityAction.Assert))
                    Assert.Equal(1, methodAttribute.CommonNamedArguments.Length)
                    methodAttribute.VerifyNamedArgumentValue(0, "Role", TypedConstantKind.Primitive, "User4")
                End Sub

            CompileAndVerify(source, sourceSymbolValidator:=attributeValidator)
        End Sub

        ' a.netmodule to b.netmodule
        <Fact()>
        Public Sub EmitModuleWithDifferentName()
            Dim name = "a"
            Dim extension = ".netmodule"
            Dim nameOverride = "b"

            Dim source =
<compilation name=<%= name %>>
    <file name="a.vb">
Class A
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseModule)
            compilation.VerifyDiagnostics()

            Dim assembly = compilation.Assembly
            Assert.Equal("a", assembly.Name)

            Dim [module] = assembly.Modules.Single()
            Assert.Equal(name & extension, [module].Name)

            Dim stream As New MemoryStream()
            Assert.True(compilation.Emit(stream, options:=New EmitOptions(outputNameOverride:=nameOverride & extension)).Success)

            Using metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable())
                Dim metadataReader = metadata.Module.GetMetadataReader()

                Assert.False(metadataReader.IsAssembly)

                Assert.Equal([module].Name, metadataReader.GetString(metadataReader.GetModuleDefinition().Name))
            End Using
        End Sub

        ' a.dll to b.dll - expected use case
        <Fact()>
        Public Sub EmitAssemblyWithDifferentName1()
            Dim name = "a"
            Dim extension = ".dll"
            Dim nameOverride = "b"

            Dim source =
<compilation name=<%= name %>>
    <file name="a.vb">
Class A
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics()

            Dim assembly = compilation.Assembly
            Assert.Equal(name, assembly.Name)

            Dim [module] = assembly.Modules.Single()
            Assert.Equal(name & extension, [module].Name)

            Dim stream As New MemoryStream()
            Assert.True(compilation.Emit(stream, , options:=New EmitOptions(outputNameOverride:=nameOverride & extension)).Success)

            Using metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable())
                Dim metadataReader = metadata.Module.GetMetadataReader()

                Assert.True(metadataReader.IsAssembly)

                Assert.Equal(nameOverride, metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name))
                Assert.Equal([module].Name, metadataReader.GetString(metadataReader.GetModuleDefinition().Name))
            End Using
        End Sub

        ' a.dll to b - allowable, but odd
        <Fact()>
        Public Sub EmitAssemblyWithDifferentName2()
            Dim name = "a"
            Dim extension = ".dll"
            Dim nameOverride = "b"

            Dim source =
<compilation name=<%= name %>>
    <file name="a.vb">
Class A
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics()

            Dim assembly = compilation.Assembly
            Assert.Equal(name, assembly.Name)

            Dim [module] = assembly.Modules.Single()
            Assert.Equal(name & extension, [module].Name)

            Dim stream As New MemoryStream()
            Assert.True(compilation.Emit(stream, , options:=New EmitOptions(outputNameOverride:=nameOverride)).Success)

            Using metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable())
                Dim metadataReader = metadata.Module.GetMetadataReader()

                Assert.True(metadataReader.IsAssembly)

                Assert.Equal(nameOverride, metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name))
                Assert.Equal([module].Name, metadataReader.GetString(metadataReader.GetModuleDefinition().Name))
            End Using
        End Sub

        ' a to b.dll - allowable, but odd
        <Fact()>
        Public Sub EmitAssemblyWithDifferentName3()
            Dim name = "a"
            Dim extension = ".dll"
            Dim nameOverride = "b"

            Dim source =
<compilation name=<%= name %>>
    <file name="a.vb">
Class A
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics()

            Dim assembly = compilation.Assembly
            Assert.Equal(name, assembly.Name)

            Dim [module] = assembly.Modules.Single()
            Assert.Equal(name & extension, [module].Name)

            Dim stream As New MemoryStream()
            Assert.True(compilation.Emit(stream, , options:=New EmitOptions(outputNameOverride:=nameOverride & extension)).Success)

            Using metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable())
                Dim metadataReader = metadata.Module.GetMetadataReader()

                Assert.True(metadataReader.IsAssembly)

                Assert.Equal(nameOverride, metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name))
                Assert.Equal([module].Name, metadataReader.GetString(metadataReader.GetModuleDefinition().Name))
            End Using
        End Sub

        ' a to b - allowable, but odd
        <Fact()>
        Public Sub EmitAssemblyWithDifferentName4()
            Dim name = "a"
            Dim extension = ".dll"
            Dim nameOverride = "b"

            Dim source =
<compilation name=<%= name %>>
    <file name="a.vb">
Class A
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics()

            Dim assembly = compilation.Assembly
            Assert.Equal(name, assembly.Name)

            Dim [module] = assembly.Modules.Single()
            Assert.Equal(name & extension, [module].Name)

            Dim stream As New MemoryStream()
            Assert.True(compilation.Emit(stream, , options:=New EmitOptions(outputNameOverride:=nameOverride)).Success)

            Using metadata = ModuleMetadata.CreateFromImage(stream.ToImmutable())
                Dim metadataReader = metadata.Module.GetMetadataReader()

                Assert.True(metadataReader.IsAssembly)

                Assert.Equal(nameOverride, metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name))
                Assert.Equal([module].Name, metadataReader.GetString(metadataReader.GetModuleDefinition().Name))
            End Using
        End Sub

        <Fact()>
        Public Sub IllegalNameOverride()
            Dim source =
<compilation>
    <file name="a.vb">
Class A
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics()

            Dim result = compilation.Emit(New MemoryStream(), options:=New EmitOptions(outputNameOverride:=" "))
        End Sub

        <WorkItem(545084, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545084"), WorkItem(529492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529492")>
        <Fact>
        Public Sub PermissionSetAttribute_Fixup()
            Dim tempDir = Temp.CreateDirectory()
            Dim tempFile = tempDir.CreateFile("pset.xml")

            Dim text = <![CDATA[
<PermissionSet class="System.Security.PermissionSet" version="1">
    <Permission class="System.Security.Permissions.FileIOPermission, mscorlib" version="1"><AllWindows/></Permission>
    <Permission class="System.Security.Permissions.RegistryPermission, mscorlib" version="1"><Unrestricted/></Permission>
</PermissionSet>]]>.Value

            tempFile.WriteAllText(text)

            Dim hexFileContent = PermissionSetAttributeWithFileReference.ConvertToHex(New MemoryStream(Encoding.UTF8.GetBytes(text)))

            Dim source =
<compilation>
    <file name="a.vb">
imports System.Security.Permissions

&lt;PermissionSetAttribute(SecurityAction.Deny, File:="pset.xml")&gt;
public class AClass
    public shared sub Main() 
        GetType(AClass).GetCustomAttributes(false)
    end sub
End Class
</file>
</compilation>

            Dim syntaxTree = CreateParseTree(source)
            Dim resolver = New XmlFileResolver(tempDir.Path)
            Dim comp = VisualBasicCompilation.Create(
                GetUniqueName(),
                {syntaxTree},
                {MscorlibRef},
                TestOptions.ReleaseDll.WithXmlReferenceResolver(resolver))

            comp.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.Deny").WithArguments(
                    "Deny",
                    "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."))

            Dim lengthHi = &H82
            Dim lengthLo = &H86

            ValidateDeclSecurity(comp,
                New DeclSecurityEntry() With {
                    .ActionFlags = DeclarativeSecurityAction.Deny,
                    .ParentKind = SymbolKind.NamedType,
                    .ParentNameOpt = "AClass",
                    .PermissionSet =
                        "." &
                        ChrW(&H1) &
                        ChrW(&H7F) &
                        "System.Security.Permissions.PermissionSetAttribute, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" &
                        ChrW(&H82) & ChrW(&H8F) &
                        ChrW(&H1) &
                        ChrW(&H54) &
                        ChrW(&HE) &
                        ChrW(&H3) &
                        "Hex" &
                        ChrW(lengthHi) & ChrW(lengthLo) &
                        hexFileContent})
        End Sub

        <WorkItem(545084, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545084"), WorkItem(529492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529492")>
        <Fact>
        Public Sub PermissionSetAttributeInvalidFile()
            Dim source =
<compilation>
    <file name="a.vb">
imports System.Security.Permissions

&lt;PermissionSetAttribute(SecurityAction.Deny, File:="NonExistentFile.xml")&gt;
&lt;PermissionSetAttribute(SecurityAction.Deny, File:=nothing)&gt;
public class AClass 
end class
    </file>
</compilation>

            CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseDll.WithXmlReferenceResolver(XmlFileResolver.Default)).VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.Deny").WithArguments("Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                    Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.Deny").WithArguments("Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."),
                    Diagnostic(ERRID.ERR_PermissionSetAttributeInvalidFile, "File:=""NonExistentFile.xml""").WithArguments("NonExistentFile.xml", "File"),
                    Diagnostic(ERRID.ERR_PermissionSetAttributeInvalidFile, "File:=nothing").WithArguments("<empty>", "File"))
        End Sub

        <Fact>
        Public Sub PermissionSetAttribute_NoResolver()
            Dim source =
<compilation>
    <file name="a.vb">
imports System.Security.Permissions

&lt;PermissionSetAttribute(SecurityAction.Deny, File:="NonExistentFile.xml")&gt;
public class AClass 
end class
    </file>
</compilation>

            CreateCompilationWithMscorlib(source, options:=TestOptions.ReleaseDll.WithXmlReferenceResolver(Nothing)).VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.Deny").WithArguments("Deny", "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.").WithLocation(3, 25),
                Diagnostic(ERRID.ERR_PermissionSetAttributeInvalidFile, "File:=""NonExistentFile.xml""").WithArguments("NonExistentFile.xml", "File").WithLocation(3, 46))
        End Sub

        <WorkItem(546074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546074")>
        <Fact()>
        Public Sub ReDimPreserve()
            Dim source =
<compilation>
    <file name="a.vb">
Module MainModule
    Sub Main()
        Dim o As Object
        ReDim Preserve o.M1(15)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source)
            CompileAndVerify(comp)
        End Sub

        <WorkItem(546074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546074")>
        <Fact()>
        Public Sub ReDimPreserve2()
            Dim source =
<compilation>
    <file name="a.vb">
Module MainModule
    Sub Main()
        Dim oo(123) As Object
        ReDim Preserve oo(1).M1(15)
    End Sub
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source)
            CompileAndVerify(comp)
        End Sub

        <WorkItem(546074, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546074")>
        <Fact()>
        Public Sub ReDimPreserve3()
            Dim source =
<compilation>
    <file name="a.vb">
Module MainModule
    Sub Main()
        ReDim Preserve F().M1(15)
    End Sub
    Function F() As Object
        Return Nothing
    End Function
End Module
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source)
            CompileAndVerify(comp)
        End Sub

        <WorkItem(545084, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545084"), WorkItem(529492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529492")>
        <Fact>
        Public Sub PermissionSetAttributeFileReadError()
            Dim tempDir = Temp.CreateDirectory()
            Dim filePath = Path.Combine(tempDir.Path, "pset_01.xml")

            Dim source =
<compilation>
    <file name="a.vb">
imports System.Security.Permissions

&lt;PermissionSetAttribute(SecurityAction.Deny, File:="pset_01.xml")&gt;
public class AClass
    public shared sub Main()
        GetType(AClass).GetCustomAttributes(false)
    end Sub
End Class
</file>
</compilation>

            Dim syntaxTree = CreateParseTree(source)
            Dim comp As VisualBasicCompilation

            ' create file with no file sharing allowed and verify ERR_PermissionSetAttributeFileReadError during emit
            Using File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None)
                comp = VisualBasicCompilation.Create(
                    GetUniqueName(),
                    {syntaxTree},
                    {MscorlibRef},
                    TestOptions.ReleaseDll.WithXmlReferenceResolver(New XmlFileResolver(Path.GetDirectoryName(filePath))))

                comp.VerifyDiagnostics(Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2, "SecurityAction.Deny").WithArguments(
                    "Deny",
                    "Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information."))

                Using output = New MemoryStream()
                    Dim emitResult = comp.Emit(output)
                    Assert.False(emitResult.Success)

                    emitResult.Diagnostics.VerifyErrorCodes(
                        Diagnostic(ERRID.WRN_UseOfObsoleteSymbol2),
                        Diagnostic(ERRID.ERR_PermissionSetAttributeFileReadError))
                End Using
            End Using

            ' emit succeeds now since we closed the file:

            Using output = New MemoryStream()
                Dim emitResult = comp.Emit(output)
                Assert.True(emitResult.Success)
            End Using
        End Sub

        <WorkItem(654522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/654522")>
        <Fact>
        Public Sub Bug654522()
            Dim refSource =
<compilation>
    <file name="a.vb">
Public Interface I(Of W As Structure) : End Interface
</file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib(refSource).VerifyDiagnostics()

            Dim metadataValidator As Action(Of ModuleSymbol) =
                Sub([module] As ModuleSymbol)
                    Dim metadata = DirectCast([module], PEModuleSymbol).Module

                    Dim typeI = DirectCast([module].GlobalNamespace.GetTypeMembers("I").Single(), PENamedTypeSymbol)
                    Assert.Equal(1, typeI.TypeParameters.Length)

                    Dim tp = DirectCast(typeI.TypeParameters(0), PETypeParameterSymbol)

                    Dim name As String = Nothing
                    Dim flags As GenericParameterAttributes = GenericParameterAttributes.None
                    metadata.GetGenericParamPropsOrThrow(tp.Handle, name, flags)

                    Assert.Equal(GenericParameterAttributes.DefaultConstructorConstraint,
                                 flags And GenericParameterAttributes.DefaultConstructorConstraint)


                    Dim constraints = metadata.GetGenericParamConstraintsOrThrow(tp.Handle)
                    Assert.Equal(1, constraints.Length)

                    Dim tokenDecoder = New MetadataDecoder(DirectCast([module], PEModuleSymbol), typeI)
                    Dim typeSymbol = tokenDecoder.GetTypeOfToken(constraints(0))
                    Assert.Equal(SpecialType.System_ValueType, typeSymbol.SpecialType)
                End Sub

            CompileAndVerify(comp, symbolValidator:=metadataValidator)
        End Sub

        <WorkItem(546450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546450")>
        <Fact>
        Public Sub EmitNetModuleWithReferencedNetModule()
            Dim source1 =
<compilation>
    <file name="Source1.vb">
Public Class A
End Class
</file>
</compilation>

            Dim source2 =
<compilation>
    <file name="Source2.vb">
Public Class B
  Inherits A
End Class
</file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib(source1, OutputKind.NetModule)
            Dim metadataRef = comp.EmitToImageReference()
            CompileAndVerify(source2, additionalRefs:={metadataRef}, options:=TestOptions.ReleaseModule, verify:=False)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_01()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            refCompilation.VerifyEmitDiagnostics()
            Dim compRef = New VisualBasicCompilationReference(refCompilation)
            Dim imageRef = refCompilation.EmitToImageReference()


            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>

            Dim useCompilation = CreateCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.AnyCpu))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics, <expected></expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.AnyCpu))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics, <expected></expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.AnyCpu))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics, <expected></expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.AnyCpu))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics, <expected></expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.X86))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.X86))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.X86))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.X86))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            'confirm that shutting off 40010, turns off the new alink warning 42372
            Dim warns = New Dictionary(Of String, ReportDiagnostic)
            warns.Add(MessageProvider.Instance.GetIdForErrorCode(40010), ReportDiagnostic.Suppress)
            useCompilation = useCompilation.WithOptions(useCompilation.Options.WithSpecificDiagnosticOptions(warns))
            Assert.Empty(useCompilation.Emit(New MemoryStream()).Diagnostics)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_02()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            refCompilation.VerifyEmitDiagnostics()
            Dim imageRef = refCompilation.EmitToImageReference()


            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>

            Dim useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.AnyCpu))


            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC37213: Agnostic assembly cannot have a processor specific module 'PlatformMismatch.netmodule'.
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.X86))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC37214: Assembly and module 'PlatformMismatch.netmodule' cannot target different processors.
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.X86))

            ' No BC37213 when building a module and adding a conflicting one
            useCompilation.VerifyEmitDiagnostics()

        End Sub

        <Fact>
        Public Sub PlatformMismatch_03()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseDll.WithPlatform(Platform.X86))

            refCompilation.VerifyEmitDiagnostics()
            Dim compRef = New VisualBasicCompilationReference(refCompilation)
            Dim imageRef = refCompilation.EmitToImageReference()


            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>


            Dim useCompilation = CreateCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC42372: Referenced assembly 'PlatformMismatch, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' targets a different processor.
</expected>)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_04()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseModule.WithPlatform(Platform.X86))

            refCompilation.VerifyEmitDiagnostics()
            Dim imageRef = refCompilation.EmitToImageReference()


            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>

            Dim useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
BC37214: Assembly and module 'PlatformMismatch.netmodule' cannot target different processors.
</expected>)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_05()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseDll.WithPlatform(Platform.AnyCpu))

            refCompilation.VerifyEmitDiagnostics()
            Dim compRef = New VisualBasicCompilationReference(refCompilation)
            Dim imageRef = refCompilation.EmitToImageReference()


            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>


            Dim useCompilation = CreateCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_06()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseModule.WithPlatform(Platform.AnyCpu))

            refCompilation.VerifyEmitDiagnostics()
            Dim imageRef = refCompilation.EmitToImageReference()


            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>

            Dim useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_07()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            refCompilation.VerifyEmitDiagnostics()
            Dim compRef = New VisualBasicCompilationReference(refCompilation)
            Dim imageRef = refCompilation.EmitToImageReference()


            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>


            Dim useCompilation = CreateCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {compRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)

            useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)
        End Sub

        <Fact>
        Public Sub PlatformMismatch_08()
            Dim refSource =
<compilation name="PlatformMismatch">
    <file name="a.vb">
public interface ITestPlatform
End interface
</file>
</compilation>

            Dim refCompilation = CreateCompilationWithReferences(refSource, New MetadataReference() {}, TestOptions.ReleaseModule.WithPlatform(Platform.Itanium))

            refCompilation.VerifyEmitDiagnostics()
            Dim imageRef = refCompilation.EmitToImageReference()


            Dim useSource =
<compilation>
    <file name="b.vb">
public interface IUsePlatform
    Function M() As ITestPlatform
End interface
</file>
</compilation>

            Dim useCompilation = CreateCompilationWithReferences(useSource,
                {imageRef},
                TestOptions.ReleaseDll.WithPlatform(Platform.Itanium))

            AssertTheseDiagnostics(useCompilation.Emit(New MemoryStream()).Diagnostics,
<expected>
</expected>)
        End Sub

        <Fact, WorkItem(769741, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/769741"), WorkItem(1001945, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1001945")>
        Public Sub Bug769741()
            Dim source =
<compilation>
    <file name="a.vb">
    </file>
</compilation>

            Dim compilation = CreateCompilationWithReferences(source, {TestReferences.SymbolsTests.netModule.x64COFF}, TestOptions.DebugDll)

            CompileAndVerify(compilation, verify:=False)
            Assert.NotSame(compilation.Assembly.CorLibrary, compilation.Assembly)
            compilation.GetSpecialType(SpecialType.System_Int32)
        End Sub

        <Fact>
        Public Sub FoldMethods()

            Dim useSource =
<compilation>
    <file name="b.vb">
        Class Viewable
            Sub Maine()
                Dim v = New Viewable()
                Dim x = v.P1
                Dim y = x AndAlso v.P2
            End Sub


            ReadOnly Property P1 As Boolean
                Get
                    Return True
                End Get
            End Property

            ReadOnly Property P2 As Boolean
                Get
                    Return True
                End Get
            End Property
        End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(useSource, TestOptions.ReleaseDll)
            Dim metadataReader = ModuleMetadata.CreateFromImage(compilation.EmitToArray()).Module.GetMetadataReader()

            Dim P1RVA = 0
            Dim P2RVA = 0

            For Each handle In metadataReader.TypeDefinitions

                Dim typeDef = metadataReader.GetTypeDefinition(handle)

                If (metadataReader.StringComparer.Equals(typeDef.Name, "Viewable")) Then
                    For Each m In typeDef.GetMethods()
                        Dim method = metadataReader.GetMethodDefinition(m)
                        If (metadataReader.StringComparer.Equals(method.Name, "get_P1")) Then
                            P1RVA = method.RelativeVirtualAddress
                        End If
                        If (metadataReader.StringComparer.Equals(method.Name, "get_P2")) Then
                            P2RVA = method.RelativeVirtualAddress
                        End If
                    Next
                End If
            Next

            Assert.NotEqual(0, P1RVA)
            Assert.Equal(P2RVA, P1RVA)
        End Sub

        Private Shared Function SequenceMatches(buffer As Byte(), startIndex As Integer, pattern As Byte()) As Boolean
            For i = 0 To pattern.Length - 1
                If buffer(startIndex + i) <> pattern(i) Then Return False
            Next
            Return True
        End Function

        Private Shared Function IndexOfPattern(buffer As Byte(), startIndex As Integer, pattern As Byte()) As Integer
            Dim [end] = buffer.Length - pattern.Length
            For i = startIndex To [end] - 1
                If SequenceMatches(buffer, i, pattern) Then Return i
            Next
            Return -1
        End Function

        <Fact, WorkItem(1669, "https://github.com/dotnet/roslyn/issues/1669")>
        Public Sub FoldMethods2()
            ' Verifies that IL folding eliminates duplicate copies of small method bodies by
            ' examining the emitted binary.
            Dim source =
<compilation>
    <file name="a.vb">
Class C
    Function M() As ULong
        Return &amp;H8675309ABCDE4225UL
    End Function
    ReadOnly Property P As Long 
        Get
            Return -8758040459200282075
        End Get
    End Property
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.ReleaseDll)
            Using stream As Stream = compilation.EmitToStream()
                Dim len As Integer = CType(stream.Length, Integer)
                Dim bytes(len) As Byte
                Assert.Equal(len, stream.Read(bytes, 0, len))

                ' The constant should appear exactly once
                Dim pattern() As Byte = {&H25, &H42, &HDE, &HBC, &H9A, &H30, &H75, &H86}
                Dim firstMatch = IndexOfPattern(bytes, 0, pattern)
                Assert.True(firstMatch >= 0, "Couldn't find the expected byte pattern in the output.")
                Dim secondMatch = IndexOfPattern(bytes, firstMatch + 1, pattern)
                Assert.True(secondMatch < 0, "Expected to find just one occurrence of the pattern in the output.")
            End Using

        End Sub

        ''' <summary>
        ''' Ordering of anonymous type definitions
        ''' in metadata should be deterministic.
        ''' </summary>
        <Fact>
        Public Sub AnonymousTypeMetadataOrder()
            Dim source =
<compilation>
    <file name="a.vb">
Class C1
    Private F As Object = New With {.C = 1, .D = 2}
End Class
Class C2
    Private F As Object = New With {.A = 3, .B = 4}
End Class
Class C3
    Private F As Object = New With {.AB = 5}
End Class
Class C4
    Private F As Object = New With {.E = 6, .F = 2}
End Class
Class C5
    Private F As Object = New With {.E = 7, .F = 8}
End Class
Class C6
    Private F As Object = New With {.AB = 9}
End Class
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib(source, TestOptions.ReleaseDll)
            Dim bytes = compilation.EmitToArray()
            Using metadata = ModuleMetadata.CreateFromImage(bytes)
                Dim reader = metadata.MetadataReader
                Dim actualNames = reader.GetTypeDefNames().Select(Function(h) reader.GetString(h))
                Dim expectedNames = {
                    "<Module>",
                    "VB$AnonymousType_0`2",
                    "VB$AnonymousType_1`2",
                    "VB$AnonymousType_2`1",
                    "VB$AnonymousType_3`2",
                    "C1",
                    "C2",
                    "C3",
                    "C4",
                    "C5",
                    "C6"
                    }
                AssertEx.Equal(expectedNames, actualNames)
            End Using
        End Sub

    End Class
End Namespace
