// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen;

public class CodeGenRefReadonlyParameterTests : CSharpTestBase
{
    private const string RequiresLocationAttributeName = "RequiresLocationAttribute";

    [Fact]
    public void Method()
    {
        var source = """
            class C
            {
                public void M(ref readonly int p) { }
            }
            """;
        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.NetStandard20,
            sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();
        verifier.VerifyTypeIL("C", """
            .class private auto ansi beforefieldinit C
                extends [netstandard]System.Object
            {
                // Methods
                .method public hidebysig 
                	instance void M (
                		[in] int32& p
                	) cil managed 
                {
                	.param [1]
                		.custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                			01 00 00 00
                		)
                	// Method begins at RVA 0x2067
                	// Code size 1 (0x1)
                	.maxstack 8
                	IL_0000: ret
                } // end of method C::M
                .method public hidebysig specialname rtspecialname 
                	instance void .ctor () cil managed 
                {
                	// Method begins at RVA 0x2069
                	// Code size 7 (0x7)
                	.maxstack 8
                	IL_0000: ldarg.0
                	IL_0001: call instance void [netstandard]System.Object::.ctor()
                	IL_0006: ret
                } // end of method C::.ctor
            } // end of class C
            """);

        static void verify(ModuleSymbol m)
        {
            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
            Assert.Equal(RefKind.RefReadOnlyParameter, p.RefKind);
        }
    }

    [Fact]
    public void Method_Virtual()
    {
        var source = """
            class C
            {
                public virtual void M(ref readonly int p) { }
            }
            """;
        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.NetStandard20,
            sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();
        verifier.VerifyTypeIL("C", """
            .class private auto ansi beforefieldinit C
                extends [netstandard]System.Object
            {
                // Methods
                .method public hidebysig newslot virtual 
                	instance void M (
                		[in] int32& modreq([netstandard]System.Runtime.InteropServices.InAttribute) p
                	) cil managed 
                {
                	.param [1]
                		.custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                			01 00 00 00
                		)
                	// Method begins at RVA 0x2067
                	// Code size 1 (0x1)
                	.maxstack 8
                	IL_0000: ret
                } // end of method C::M
                .method public hidebysig specialname rtspecialname 
                	instance void .ctor () cil managed 
                {
                	// Method begins at RVA 0x2069
                	// Code size 7 (0x7)
                	.maxstack 8
                	IL_0000: ldarg.0
                	IL_0001: call instance void [netstandard]System.Object::.ctor()
                	IL_0006: ret
                } // end of method C::.ctor
            } // end of class C
            """);

        static void verify(ModuleSymbol m)
        {
            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
            Assert.Equal(RefKind.RefReadOnlyParameter, p.RefKind);
        }
    }

    [Fact]
    public void Constructor()
    {
        var source = """
            class C
            {
                public C(ref readonly int p) { }
            }
            """;
        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.NetStandard20,
            sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();
        verifier.VerifyTypeIL("C", """
            .class private auto ansi beforefieldinit C
                extends [netstandard]System.Object
            {
                // Methods
                .method public hidebysig specialname rtspecialname 
                	instance void .ctor (
                		[in] int32& p
                	) cil managed 
                {
                	.param [1]
                		.custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                			01 00 00 00
                		)
                	// Method begins at RVA 0x2067
                	// Code size 7 (0x7)
                	.maxstack 8
                	IL_0000: ldarg.0
                	IL_0001: call instance void [netstandard]System.Object::.ctor()
                	IL_0006: ret
                } // end of method C::.ctor
            } // end of class C
            """);

        static void verify(ModuleSymbol m)
        {
            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C..ctor").Parameters.Single();
            Assert.Equal(RefKind.RefReadOnlyParameter, p.RefKind);
        }
    }

    [Fact]
    public void Delegate()
    {
        var source = """
            delegate void D(ref readonly int p);
            """;
        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.NetStandard20,
            sourceSymbolValidator: verify, symbolValidator: verify);
        verifier.VerifyDiagnostics();
        verifier.VerifyTypeIL("D", """
            .class private auto ansi sealed D
                extends [netstandard]System.MulticastDelegate
            {
                // Methods
                .method public hidebysig specialname rtspecialname 
                	instance void .ctor (
                		object 'object',
                		native int 'method'
                	) runtime managed 
                {
                } // end of method D::.ctor
                .method public hidebysig newslot virtual 
                	instance void Invoke (
                		[in] int32& modreq([netstandard]System.Runtime.InteropServices.InAttribute) p
                	) runtime managed 
                {
                	.param [1]
                		.custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                			01 00 00 00
                		)
                } // end of method D::Invoke
                .method public hidebysig newslot virtual 
                	instance class [netstandard]System.IAsyncResult BeginInvoke (
                		[in] int32& modreq([netstandard]System.Runtime.InteropServices.InAttribute) p,
                		class [netstandard]System.AsyncCallback callback,
                		object 'object'
                	) runtime managed 
                {
                	.param [1]
                		.custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                			01 00 00 00
                		)
                } // end of method D::BeginInvoke
                .method public hidebysig newslot virtual 
                	instance void EndInvoke (
                		[in] int32& modreq([netstandard]System.Runtime.InteropServices.InAttribute) p,
                		class [netstandard]System.IAsyncResult result
                	) runtime managed 
                {
                	.param [1]
                		.custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                			01 00 00 00
                		)
                } // end of method D::EndInvoke
            } // end of class D
            """);

        static void verify(ModuleSymbol m)
        {
            var p = m.GlobalNamespace.GetMember<MethodSymbol>("D.Invoke").Parameters.Single();
            Assert.Equal(RefKind.RefReadOnlyParameter, p.RefKind);
        }
    }

    [Fact]
    public void Lambda()
    {
        var source = """
            var lam = (ref readonly int p) => { };
            System.Console.WriteLine(lam.GetType());
            """;
        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.NetStandard20,
            expectedOutput: "<>f__AnonymousDelegate0`1[System.Int32]");
        verifier.VerifyDiagnostics();
        verifier.VerifyTypeIL("<>c", """
            .class nested private auto ansi sealed serializable beforefieldinit '<>c'
                extends [netstandard]System.Object
            {
                .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                	01 00 00 00
                )
                // Fields
                .field public static initonly class Program/'<>c' '<>9'
                .field public static class '<>f__AnonymousDelegate0`1'<int32> '<>9__0_0'
                // Methods
                .method private hidebysig specialname rtspecialname static 
                	void .cctor () cil managed 
                {
                	// Method begins at RVA 0x209a
                	// Code size 11 (0xb)
                	.maxstack 8
                	IL_0000: newobj instance void Program/'<>c'::.ctor()
                	IL_0005: stsfld class Program/'<>c' Program/'<>c'::'<>9'
                	IL_000a: ret
                } // end of method '<>c'::.cctor
                .method public hidebysig specialname rtspecialname 
                	instance void .ctor () cil managed 
                {
                	// Method begins at RVA 0x2092
                	// Code size 7 (0x7)
                	.maxstack 8
                	IL_0000: ldarg.0
                	IL_0001: call instance void [netstandard]System.Object::.ctor()
                	IL_0006: ret
                } // end of method '<>c'::.ctor
                .method assembly hidebysig 
                	instance void '<<Main>$>b__0_0' (
                		[in] int32& p
                	) cil managed 
                {
                	.param [1]
                		.custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                			01 00 00 00
                		)
                	// Method begins at RVA 0x20a6
                	// Code size 1 (0x1)
                	.maxstack 8
                	IL_0000: ret
                } // end of method '<>c'::'<<Main>$>b__0_0'
            } // end of class <>c
            """);
    }

    [Fact]
    public void LocalFunction()
    {
        var source = """
            void local(ref readonly int p) { }
            System.Console.WriteLine(((object)local).GetType());
            """;
        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.NetStandard20,
            expectedOutput: "<>f__AnonymousDelegate0`1[System.Int32]");
        verifier.VerifyDiagnostics();
        verifier.VerifyTypeIL("Program", """
            .class private auto ansi beforefieldinit Program
                extends [netstandard]System.Object
            {
                .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                	01 00 00 00
                )
                // Methods
                .method private hidebysig static 
                	void '<Main>$' (
                		string[] args
                	) cil managed 
                {
                	// Method begins at RVA 0x2067
                	// Code size 23 (0x17)
                	.maxstack 8
                	.entrypoint
                	IL_0000: ldnull
                	IL_0001: ldftn void Program::'<<Main>$>g__local|0_0'(int32&)
                	IL_0007: newobj instance void class '<>f__AnonymousDelegate0`1'<int32>::.ctor(object, native int)
                	IL_000c: call instance class [netstandard]System.Type [netstandard]System.Object::GetType()
                	IL_0011: call void [netstandard]System.Console::WriteLine(object)
                	IL_0016: ret
                } // end of method Program::'<Main>$'
                .method public hidebysig specialname rtspecialname 
                	instance void .ctor () cil managed 
                {
                	// Method begins at RVA 0x207f
                	// Code size 7 (0x7)
                	.maxstack 8
                	IL_0000: ldarg.0
                	IL_0001: call instance void [netstandard]System.Object::.ctor()
                	IL_0006: ret
                } // end of method Program::.ctor
                .method assembly hidebysig static 
                	void '<<Main>$>g__local|0_0' (
                		[in] int32& p
                	) cil managed 
                {
                	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                		01 00 00 00
                	)
                	.param [1]
                		.custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                			01 00 00 00
                		)
                	// Method begins at RVA 0x2087
                	// Code size 1 (0x1)
                	.maxstack 8
                	IL_0000: ret
                } // end of method Program::'<<Main>$>g__local|0_0'
            } // end of class Program
            """);
    }

    [Theory]
    [InlineData("var x = (ref readonly int p) => { };")]
    [InlineData("var x = local; void local(ref readonly int p) { }")]
    public void AnonymousDelegate(string def)
    {
        var source = $"""
            {def}
            System.Console.WriteLine(((object)x).GetType());
            """;
        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.NetStandard20,
            expectedOutput: "<>f__AnonymousDelegate0`1[System.Int32]");
        verifier.VerifyDiagnostics();
        verifier.VerifyTypeIL("<>f__AnonymousDelegate0`1", """
            .class private auto ansi sealed '<>f__AnonymousDelegate0`1'<T1>
                extends [netstandard]System.MulticastDelegate
            {
                .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                	01 00 00 00
                )
                // Methods
                .method public hidebysig specialname rtspecialname 
                	instance void .ctor (
                		object 'object',
                		native int 'method'
                	) runtime managed 
                {
                } // end of method '<>f__AnonymousDelegate0`1'::.ctor
                .method public hidebysig newslot virtual 
                	instance void Invoke (
                		[in] !T1& arg
                	) runtime managed 
                {
                	.param [1]
                		.custom instance void System.Runtime.CompilerServices.RequiresLocationAttribute::.ctor() = (
                			01 00 00 00
                		)
                } // end of method '<>f__AnonymousDelegate0`1'::Invoke
            } // end of class <>f__AnonymousDelegate0`1
            """);
    }

    [Fact]
    public void FunctionPointer()
    {
        var source = """
            class C
            {
                public unsafe void M(delegate*<ref readonly int, void> p) { }
            }
            """;
        var verifier = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, targetFramework: TargetFramework.NetStandard20,
            sourceSymbolValidator: verify); // PROTOTYPE:, symbolValidator: verify
        verifier.VerifyDiagnostics();
        verifier.VerifyTypeIL("C", """
            .class private auto ansi beforefieldinit C
                extends [netstandard]System.Object
            {
                // Methods
                .method public hidebysig 
                	instance void M (
                		method void *(int32& modreq([netstandard]System.Runtime.InteropServices.InAttribute)) p
                	) cil managed 
                {
                	// Method begins at RVA 0x2067
                	// Code size 1 (0x1)
                	.maxstack 8
                	IL_0000: ret
                } // end of method C::M
                .method public hidebysig specialname rtspecialname 
                	instance void .ctor () cil managed 
                {
                	// Method begins at RVA 0x2069
                	// Code size 7 (0x7)
                	.maxstack 8
                	IL_0000: ldarg.0
                	IL_0001: call instance void [netstandard]System.Object::.ctor()
                	IL_0006: ret
                } // end of method C::.ctor
            } // end of class C
            """);

        static void verify(ModuleSymbol m)
        {
            var p = m.GlobalNamespace.GetMember<MethodSymbol>("C.M").Parameters.Single();
            var ptr = (FunctionPointerTypeSymbol)p.Type;
            Assert.Equal(RefKind.RefReadOnlyParameter, ptr.Signature.Parameters.Single().RefKind);
        }
    }

    [Fact]
    public void AttributeIL()
    {
        var source = """
            class C
            {
                public void M(ref readonly int p) { }
            }
            """;
        var verifier = CompileAndVerify(source, targetFramework: TargetFramework.NetStandard20);
        verifier.VerifyDiagnostics();
        verifier.VerifyTypeIL(RequiresLocationAttributeName, """
            .class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.RequiresLocationAttribute
                extends [netstandard]System.Attribute
            {
                .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                	01 00 00 00
                )
                .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
                	01 00 00 00
                )
                // Methods
                .method public hidebysig specialname rtspecialname 
                	instance void .ctor () cil managed 
                {
                	// Method begins at RVA 0x2050
                	// Code size 7 (0x7)
                	.maxstack 8
                	IL_0000: ldarg.0
                	IL_0001: call instance void [netstandard]System.Attribute::.ctor()
                	IL_0006: ret
                } // end of method RequiresLocationAttribute::.ctor
            } // end of class System.Runtime.CompilerServices.RequiresLocationAttribute
            """);
    }
}
