// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Test;

public class OverloadResolutionPriorityTests : CSharpTestBase
{
    [Theory, CombinatorialData]
    public void IncreasedPriorityWins_01(bool useMetadataReference, bool i1First)
    {
        var executable = """
            I3 i3 = null;
            C.M(i3);
            """;

        var i1Source = """
                [OverloadResolutionPriority(1)]
                public static void M(I1 x) => System.Console.WriteLine(1);
            """;

        var i2Source = """
                public static void M(I2 x) => throw null;
            """;

        var source = $$"""
            using System.Runtime.CompilerServices;

            public interface I1 {}
            public interface I2 {}
            public interface I3 : I1, I2 {}

            public class C
            {
                {{(i1First ? i1Source : i2Source)}}
                {{(i1First ? i2Source : i1Source)}}
            }
            """;

        CompileAndVerify([executable, source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1", symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(executable, references: [AsReference(comp, useMetadataReference)], expectedOutput: "1").VerifyDiagnostics();

        static void validate(ModuleSymbol module)
        {
            var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var ms = c.GetMembers("M").Cast<MethodSymbol>();
            foreach (var m in ms)
            {
                if (m.Parameters[0].Type.Name == "I1")
                {
                    Assert.Equal(1, m.OverloadResolutionPriority);
                }
                else
                {
                    Assert.Equal(0, m.OverloadResolutionPriority);
                }
            }
        };
    }

    [Theory, CombinatorialData]
    public void IncreasedPriorityWins_02(bool useMetadataReference)
    {
        var executable = """
            I3 i3 = null;
            C.M(i3);
            """;

        var source = """
            using System.Runtime.CompilerServices;

            public interface I1 {}
            public interface I2 {}
            public interface I3 : I1, I2 {}

            public class C
            {
                [OverloadResolutionPriority(2)]
                public static void M(object o) => System.Console.WriteLine(1);

                [OverloadResolutionPriority(1)]
                public static void M(I1 x) => throw null;

                public static void M(I2 x) => throw null;
            }
            """;

        CompileAndVerify([executable, source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(executable, references: [AsReference(comp, useMetadataReference)], expectedOutput: "1").VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void DecreasedPriorityLoses(bool useMetadataReference, bool i1First)
    {
        var executable = """
            I3 i3 = null;
            C.M(i3);
            """;

        var i1Source = """
                public static void M(I1 x) => System.Console.WriteLine(1);
            """;

        var i2Source = """
                [OverloadResolutionPriority(-1)]
                public static void M(I2 x) => throw null;
            """;

        var source = $$"""
            using System.Runtime.CompilerServices;

            public interface I1 {}
            public interface I2 {}
            public interface I3 : I1, I2 {}

            public class C
            {
                {{(i1First ? i1Source : i2Source)}}
                {{(i1First ? i2Source : i1Source)}}
            }
            """;
        CompileAndVerify([executable, source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(executable, references: [AsReference(comp, useMetadataReference)], expectedOutput: "1").VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void ZeroIsTreatedAsDefault(bool useMetadataReference)
    {
        var executable = """
            I3 i3 = null;
            C.M(i3);
            """;

        var source = """
            using System.Runtime.CompilerServices;

            public interface I1 {}
            public interface I2 {}
            public interface I3 : I1, I2 {}

            public class C
            {
                public static void M(I1 x) => System.Console.WriteLine(1);

                [OverloadResolutionPriority(0)]
                public static void M(I2 x) => throw null;
            }
            """;
        CreateCompilation([executable, source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (2,3): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(I1)' and 'C.M(I2)'
            // C.M(i3);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(I1)", "C.M(I2)").WithLocation(2, 3)
        );

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

        CreateCompilation(executable, references: [AsReference(comp, useMetadataReference)]).VerifyDiagnostics(
            // (2,3): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(I1)' and 'C.M(I2)'
            // C.M(i3);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(I1)", "C.M(I2)").WithLocation(2, 3)
        );

        static void validate(ModuleSymbol module)
        {
            var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var ms = c.GetMembers("M").Cast<MethodSymbol>();
            Assert.All(ms, m => Assert.Equal(0, m.OverloadResolutionPriority));
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void AmbiguityWithinPriority(int priority)
    {
        var source = $$"""
            using System.Runtime.CompilerServices;

            I3 i3 = null;
            C.M(i3);

            interface I1 {}
            interface I2 {}
            interface I3 : I1, I2 {}

            class C
            {
                [OverloadResolutionPriority({{priority}})]
                public static void M(I1 x) => System.Console.WriteLine(1);

                [OverloadResolutionPriority({{priority}})]
                public static void M(I2 x) => throw null;
            }
            """;

        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (4,3): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(I1)' and 'C.M(I2)'
            // C.M(i3);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(I1)", "C.M(I2)").WithLocation(4, 3)
        );
    }

    [Theory, CombinatorialData]
    public void MethodDiscoveryStopsAtFirstApplicableMethod(bool useMetadataReference)
    {
        var @base = """
            using System.Runtime.CompilerServices;

            public interface I1 {}
            public interface I2 {}
            public interface I3 : I1, I2 {}

            public class Base
            {
                [OverloadResolutionPriority(1)]
                public static void M(I2 x) => throw null;
            }
            """;

        var derived = """
            public class Derived : Base
            {
                public static void M(I1 x) => System.Console.WriteLine(1);
            }
            """;

        var executable = """
            I3 i3 = null;
            Derived.M(i3);
            """;
        CompileAndVerify([executable, @base, derived, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();

        var baseComp = CreateCompilation([@base, OverloadResolutionPriorityAttributeDefinition]);
        var baseReference = useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference();

        var derivedComp = CreateCompilation(derived, references: [baseReference]);
        var derivedReference = useMetadataReference ? derivedComp.ToMetadataReference() : derivedComp.EmitToImageReference();

        CompileAndVerify(executable, references: [baseReference, derivedReference], expectedOutput: "1").VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void MethodDiscoveryStopsAtFirstApplicableIndexer(bool useMetadataReference)
    {
        var @base = """
            using System.Runtime.CompilerServices;

            public interface I1 {}
            public interface I2 {}
            public interface I3 : I1, I2 {}

            public class Base
            {
                [OverloadResolutionPriority(1)]
                public int this[I2 x]
                {
                    get => throw null;
                    set => throw null;
                }
            }
            """;

        var derived = """
            public class Derived : Base
            {
                public int this[I1 x]
                {
                    get { System.Console.Write(1); return 1; }
                    set => System.Console.Write(2);
                }
            }
            """;

        var executable = """
            I3 i3 = null;
            var d = new Derived();
            _ = d[i3];
            d[i3] = 0;
            """;
        CompileAndVerify([executable, @base, derived, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "12").VerifyDiagnostics();

        var baseComp = CreateCompilation([@base, OverloadResolutionPriorityAttributeDefinition]);
        var baseReference = useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference();

        var derivedComp = CreateCompilation(derived, references: [baseReference]);
        var derivedReference = useMetadataReference ? derivedComp.ToMetadataReference() : derivedComp.EmitToImageReference();

        CompileAndVerify(executable, references: [baseReference, derivedReference], expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void OrderingWithAnExtensionMethodContainingClass()
    {
        var source = """
            using System.Runtime.CompilerServices;

            I3 i3 = null;
            i3.M();

            interface I1 {}
            interface I2 {}
            interface I3 : I1, I2 {}

            static class C
            {
                [OverloadResolutionPriority(1)]
                public static void M(this I1 x) => System.Console.WriteLine(1);

                public static void M(this I2 x) => throw null;
            }
            """;
        CompileAndVerify([source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void DoesNotOrderBetweenExtensionMethodContainingClasses()
    {
        var source = """
            using System.Runtime.CompilerServices;

            I3 i3 = null;
            i3.M();

            interface I1 {}
            interface I2 {}
            interface I3 : I1, I2 {}

            static class C1
            {
                [OverloadResolutionPriority(1)]
                public static void M(this I1 x) => System.Console.WriteLine(1);
            }

            static class C2
            {
                public static void M(this I2 x) => throw null;
            }
            """;
        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (4,4): error CS0121: The call is ambiguous between the following methods or properties: 'C1.M(I1)' and 'C2.M(I2)'
            // i3.M();
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C1.M(I1)", "C2.M(I2)").WithLocation(4, 4)
        );
    }

    [Fact]
    public void Overrides_NoPriorityChangeFromBase_Methods()
    {
        var code = """
            using System.Runtime.CompilerServices;

            var d = new Derived();
            d.M("test");

            public class Base
            {
                [OverloadResolutionPriority(1)]
                public virtual void M(object o) => throw null;
                public virtual void M(string s) => throw null;
            }

            public class Derived : Base
            {
                public override void M(object o) => System.Console.WriteLine("1");
                public override void M(string s) => throw null;
            }
            """;

        CompileAndVerify([code, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void Overrides_ChangePriorityInSource_Methods()
    {
        var executable = """
            var d = new Derived();
            d.M("test");
            """;

        var code = """
            using System.Runtime.CompilerServices;

            public class Base
            {
                [OverloadResolutionPriority(1)]
                public virtual void M(object o) => System.Console.WriteLine("1");
                public virtual void M(string s) => throw null;
            }

            public class Derived : Base
            {
                [OverloadResolutionPriority(0)]
                public override void M(object o) => System.Console.WriteLine("1");
                [OverloadResolutionPriority(2)]
                public override void M(string s) => throw null;
            }
            """;

        var comp = CreateCompilation([executable, code, OverloadResolutionPriorityAttributeDefinition]);

        var expectedErrors = new[] {
            // (12,6): error CS9500: Cannot use 'OverloadResolutionPriorityAttribute' on an overriding member.
            //     [OverloadResolutionPriority(0)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToOverride, "OverloadResolutionPriority(0)").WithLocation(12, 6),
            // (14,6): error CS9500: Cannot use 'OverloadResolutionPriorityAttribute' on an overriding member.
            //     [OverloadResolutionPriority(2)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToOverride, "OverloadResolutionPriority(2)").WithLocation(14, 6)
        };

        comp.VerifyDiagnostics(expectedErrors);
        verify(comp);

        var comp2 = CreateCompilation([code, OverloadResolutionPriorityAttributeDefinition]);
        comp2.VerifyDiagnostics(expectedErrors);
        comp = CreateCompilation(executable, references: [comp2.ToMetadataReference()]);
        comp.VerifyDiagnostics();
        verify(comp);

        static void verify(CSharpCompilation comp)
        {
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            var method = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;
            Assert.Equal("void Derived.M(System.Object o)", method.ToTestDisplayString());
        }
    }

    [Fact]
    public void Overrides_ChangePriorityInMetadata_Methods()
    {
        var source1 = """
            using System.Runtime.CompilerServices;

            public class Base
            {
                [OverloadResolutionPriority(1)]
                public virtual void M(object o) => System.Console.WriteLine("1");
                public virtual void M(string s) => throw null;
            }
            """;

        var comp1 = CreateCompilation([source1, OverloadResolutionPriorityAttributeDefinition], assemblyName: "assembly1");
        var assembly1 = comp1.EmitToImageReference();

        // Equivalent to:
        //
        // public class Derived : Base
        // {
        //     [OverloadResolutionPriority(0)]
        //     public override void M(object o) => System.Console.WriteLine("1");
        //     [OverloadResolutionPriority(2)]
        //     public override void M(string s) => throw null;
        // }
        var il2 = """
            .assembly extern assembly1 {}

            .class public auto ansi beforefieldinit Derived extends [assembly1]Base
            {
                .method public hidebysig virtual 
                    instance void M (
                        object o
                    ) cil managed 
                {
                    .custom instance void [assembly1]System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
                        01 00 00 00 00 00 00 00
                    )
                    ldstr "1"
                    call void [mscorlib]System.Console::WriteLine(string)
                    ret
                } // end of method Derived::M

                .method public hidebysig virtual 
                    instance void M (
                        string s
                    ) cil managed 
                {
                    .custom instance void [assembly1]System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
                        01 00 02 00 00 00 00 00
                    )
                    ldnull
                    throw
                } // end of method Derived::M

                .method public hidebysig specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    ldarg.0
                    call instance void [assembly1]Base::.ctor()
                    ret
                } // end of method Derived::.ctor

            }
            """;
        var assembly2 = CompileIL(il2);

        var code = """
            var d = new Derived();
            d.M("test");
            """;

        CompileAndVerify(code, references: [assembly1, assembly2], expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void Overrides_ChangePriorityInMetadata_Indexers()
    {
        var source1 = """
            using System.Runtime.CompilerServices;

            public class Base
            {
                [OverloadResolutionPriority(1)]
                public virtual int this[object o]
                {
                    get => throw null;
                    set => throw null;
                }
                public virtual int this[string s]
                {
                    get => throw null;
                    set => throw null;
                }
            }
            """;

        var comp1 = CreateCompilation([source1, OverloadResolutionPriorityAttributeDefinition], assemblyName: "assembly1");
        var assembly1 = comp1.EmitToImageReference();

        // Equivalent to:
        //
        // public class Derived: Base
        // {
        //     public override int this[object o]
        //     {
        //         get { System.Console.Write(1); return 1; }
        //         set => System.Console.Write(2);
        //     }
        //     [OverloadResolutionPriority(2)]
        //     public override int this[string s]
        //     {
        //         get => throw null;
        //         set => throw null;
        //     }
        // }
        var il2 = """
            .assembly extern assembly1 {}

            .class public auto ansi beforefieldinit Derived extends [assembly1]Base
            {
                .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
                    01 00 04 49 74 65 6d 00 00
                )
                // Methods
                .method public hidebysig specialname virtual 
                    instance int32 get_Item (
                        object o
                    ) cil managed 
                {
                    ldc.i4.1
                    call void [mscorlib]System.Console::Write(int32)
                    ldc.i4.1
                    ret
                } // end of method Derived::get_Item

                .method public hidebysig specialname virtual 
                    instance void set_Item (
                        object o,
                        int32 'value'
                    ) cil managed 
                {
                    ldc.i4.2
                    call void [mscorlib]System.Console::Write(int32)
                    ret
                } // end of method Derived::set_Item

                .method public hidebysig specialname virtual 
                    instance int32 get_Item (
                        string s
                    ) cil managed 
                {
                    ldnull
                    throw
                } // end of method Derived::get_Item

                .method public hidebysig specialname virtual 
                    instance void set_Item (
                        string s,
                        int32 'value'
                    ) cil managed 
                {
                    ldnull
                    throw
                } // end of method Derived::set_Item

                .method public hidebysig specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    ldarg.0
                    call instance void [assembly1]Base::.ctor()
                    ret
                } // end of method Derived::.ctor

                // Properties
                .property instance int32 Item(
                    object o
                )
                {
                    .get instance int32 Derived::get_Item(object)
                    .set instance void Derived::set_Item(object, int32)
                }
                .property instance int32 Item(
                    string s
                )
                {
                    .custom instance void [assembly1]System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
                        01 00 02 00 00 00 00 00
                    )
                    .get instance int32 Derived::get_Item(string)
                    .set instance void Derived::set_Item(string, int32)
                }
            }
            """;
        var assembly2 = CompileIL(il2);

        var code = """
            var d = new Derived();
            _ = d["test"];
            d["test"] = 0;
            """;

        CompileAndVerify(code, references: [assembly1, assembly2], expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void ThroughRetargeting_Methods()
    {
        var source1 = """
            public class RetValue {}
            """;

        var comp1_1 = CreateCompilation(new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true), source1, TargetFrameworkUtil.StandardReferences);
        var comp1_2 = CreateCompilation(new AssemblyIdentity("Ret", new Version(2, 0, 0, 0), isRetargetable: true), source1, TargetFrameworkUtil.StandardReferences);

        var source2 = """
            using System.Runtime.CompilerServices;

            public class C
            {
                [OverloadResolutionPriority(1)]
                public RetValue M(object o)
                {
                    System.Console.WriteLine("1");
                    return new();
                }
                public RetValue M(string s) => throw null;
            }
            """;

        var comp2 = CreateCompilation([source2, OverloadResolutionPriorityAttributeDefinition], references: [comp1_1.ToMetadataReference()], targetFramework: TargetFramework.Standard);
        comp2.VerifyDiagnostics();

        var source3 = """
            var c = new C();
            c.M("test");
            """;

        var comp3 = CreateCompilation(source3, references: [comp2.ToMetadataReference(), comp1_2.ToMetadataReference()], targetFramework: TargetFramework.Standard);
        comp3.VerifyDiagnostics();

        var c = comp3.GetTypeByMetadataName("C")!;
        var ms = c.GetMembers("M");
        Assert.Equal(2, ms.Length);
        Assert.All(ms, m => Assert.IsType<RetargetingMethodSymbol>(m));

        var tree = comp3.SyntaxTrees[0];
        var model = comp3.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        var method = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;
        Assert.Equal("RetValue C.M(System.Object o)", method.ToTestDisplayString());
    }

    [Fact]
    public void ThroughRetargeting_Indexers()
    {
        var source1 = """
            public class RetValue {}
            """;

        var comp1_1 = CreateCompilation(new AssemblyIdentity("Ret", new Version(1, 0, 0, 0), isRetargetable: true), source1, TargetFrameworkUtil.StandardReferences);
        var comp1_2 = CreateCompilation(new AssemblyIdentity("Ret", new Version(2, 0, 0, 0), isRetargetable: true), source1, TargetFrameworkUtil.StandardReferences);

        var source2 = """
            using System.Runtime.CompilerServices;

            public class C
            {
                [OverloadResolutionPriority(1)]
                public RetValue this[object o]
                {
                    get
                    {
                        System.Console.WriteLine("1");
                        return new();
                    }
                    set
                    {
                        System.Console.WriteLine("2");
                    }
                }
                public RetValue this[string s]
                {
                    get => throw null;
                    set => throw null;
                }
            }
            """;

        var comp2 = CreateCompilation([source2, OverloadResolutionPriorityAttributeDefinition], references: [comp1_1.ToMetadataReference()], targetFramework: TargetFramework.Standard);
        comp2.VerifyDiagnostics();

        var source3 = """
            var c = new C();
            c["test"] = new();
            _ = c["test"];
            """;

        var comp3 = CreateCompilation(source3, references: [comp2.ToMetadataReference(), comp1_2.ToMetadataReference()], targetFramework: TargetFramework.Standard);
        comp3.VerifyDiagnostics();

        var c = comp3.GetTypeByMetadataName("C")!;
        var indexers = c.Indexers;
        Assert.Equal(2, indexers.Length);
        Assert.All(indexers, m => Assert.IsType<RetargetingPropertySymbol>(m));

        var tree = comp3.SyntaxTrees[0];
        var model = comp3.GetSemanticModel(tree);
        var accesses = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().ToArray();
        Assert.Equal(2, accesses.Length);
        AssertEx.Equal("RetValue C.this[System.Object o] { get; set; }", model.GetSymbolInfo(accesses[0]).Symbol!.ToTestDisplayString());
        AssertEx.Equal("RetValue C.this[System.Object o] { get; set; }", model.GetSymbolInfo(accesses[1]).Symbol!.ToTestDisplayString());
    }

    [Fact]
    public void ThroughRetargeting_Constructors()
    {
        var source1 = """
            public class Base {}
            """;

        var comp1_1 = CreateCompilation(new AssemblyIdentity("Base", new Version(1, 0, 0, 0), isRetargetable: true), source1, TargetFrameworkUtil.StandardReferences);
        var comp1_2 = CreateCompilation(new AssemblyIdentity("Base", new Version(2, 0, 0, 0), isRetargetable: true), source1, TargetFrameworkUtil.StandardReferences);

        var source2 = """
            using System.Runtime.CompilerServices;

            public class Derived : Base
            {
                [OverloadResolutionPriority(1)]
                public Derived(object o)
                {
                }
                public Derived(string s)
                {
                }
            }
            """;

        var comp2 = CreateCompilation([source2, OverloadResolutionPriorityAttributeDefinition], references: [comp1_1.ToMetadataReference()]);
        comp2.VerifyDiagnostics();

        var source3 = """
            var c = new Derived("test");
            """;

        var comp3 = CreateCompilation(source3, references: [comp2.ToMetadataReference(), comp1_2.ToMetadataReference()]);
        comp3.VerifyDiagnostics();

        var derived = comp3.GetTypeByMetadataName("Derived")!;
        var constructors = derived.Constructors;
        Assert.Equal(2, constructors.Length);
        Assert.All(constructors, m => Assert.IsType<RetargetingMethodSymbol>(m));

        var tree = comp3.SyntaxTrees[0];
        var model = comp3.GetSemanticModel(tree);
        var creation = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
        AssertEx.Equal("Derived..ctor(System.Object o)", model.GetSymbolInfo(creation).Symbol!.ToTestDisplayString());
    }

    [Fact]
    public void LangVersion()
    {
        var source = """
            using System.Runtime.CompilerServices;

            public class C
            {
                [OverloadResolutionPriority(1)]
                public void M(object o) => System.Console.Write("1");
                public void M(string s) => System.Console.Write("5");

                [OverloadResolutionPriority(1)]
                public int this[object o]
                {
                    get
                    {
                        System.Console.Write("2");
                        return 1;
                    }
                    set
                    {
                        System.Console.Write("3");
                    }
                }
                public int this[string s]
                {
                    get
                    {
                        System.Console.Write("6");
                        return 1;
                    }
                    set
                    {
                        System.Console.Write("7");
                    }
                }

                [OverloadResolutionPriority(1)]
                public C(object o)
                {
                    System.Console.Write("4");
                }

                public C(string s)
                {
                    System.Console.Write("8");
                }
            }
            """;

        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition], parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (5,6): error CS9202: Feature 'overload resolution priority' is not available in C# 12.0. Please use language version 13.0 or greater.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "OverloadResolutionPriority(1)").WithArguments("overload resolution priority", "13.0").WithLocation(5, 6),
            // (9,6): error CS9202: Feature 'overload resolution priority' is not available in C# 12.0. Please use language version 13.0 or greater.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "OverloadResolutionPriority(1)").WithArguments("overload resolution priority", "13.0").WithLocation(9, 6),
            // (35,6): error CS9202: Feature 'overload resolution priority' is not available in C# 12.0. Please use language version 13.0 or greater.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "OverloadResolutionPriority(1)").WithArguments("overload resolution priority", "13.0").WithLocation(35, 6));

        var definingComp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition], parseOptions: TestOptions.Regular13).VerifyDiagnostics();

        var consumingSource = """
            var c = new C("test");
            c["test"] = 0;
            _ = c["test"];
            c.M("test");
            """;

        CompileAndVerify(consumingSource, references: [definingComp.ToMetadataReference()], parseOptions: TestOptions.Regular12, expectedOutput: "8765").VerifyDiagnostics();
        CompileAndVerify(consumingSource, references: [definingComp.ToMetadataReference()], parseOptions: TestOptions.Regular13, expectedOutput: "4321").VerifyDiagnostics();
    }

    [Fact]
    public void AppliedToAttributeConstructors()
    {
        var source = """
            using System.Runtime.CompilerServices;

            [C("test")]
            public class C : System.Attribute
            {
                [OverloadResolutionPriority(1)]
                public C(object o) {}

                public C(string s) {}
            }
            """;

        var verifier = CompileAndVerify([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics();
        var c = ((CSharpCompilation)verifier.Compilation).GetTypeByMetadataName("C");

        var attr = c!.GetAttributes().Single();
        AssertEx.Equal("C..ctor(System.Object o)", attr.AttributeConstructor.ToTestDisplayString());
    }

    [Fact]
    public void CycleOnOverloadResolutionPriorityConstructor_01()
    {
        var source = """
            namespace System.Runtime.CompilerServices;

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            public sealed class OverloadResolutionPriorityAttribute : Attribute
            {
                [OverloadResolutionPriority(1)]
                public OverloadResolutionPriorityAttribute(int priority)
                {
                    Priority = priority;
                }

                public int Priority { get;}
            }
            """;

        CompileAndVerify(source).VerifyDiagnostics();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CycleOnOverloadResolutionPriorityConstructor_02(int ctorToForce)
    {
        var source = """
            namespace System.Runtime.CompilerServices;

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            public sealed class OverloadResolutionPriorityAttribute : Attribute
            {
                public OverloadResolutionPriorityAttribute(int priority)
                {
                }

                // Attribute is intentionally ignored, as this will cause a cycle
                [OverloadResolutionPriority(1)]
                public OverloadResolutionPriorityAttribute(object priority)
                {
                }

                public int Priority { get;}
            }

            interface I1 {}
            interface I2 {}
            interface I3 : I1, I2 {}

            static class C
            {
                [OverloadResolutionPriority(1)]
                public static void M(this I1 x) => System.Console.WriteLine(1);

                public static void M(this I2 x) => throw null;

                static void Main()
                {
                    I3 i3 = null;
                    i3.M();
                }
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var secondCtor = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Skip(ctorToForce).First();

        // Explicitly pull on the attributes to force binding of any attributes. This exposes a potential race condition in early attribute binding.
        var ctor = model.GetDeclaredSymbol(secondCtor)!.GetSymbol<SourceConstructorSymbol>();
        _ = ctor.GetAttributes();

        var verifier = CompileAndVerify(comp, expectedOutput: "1").VerifyDiagnostics();
        var attr = ((CSharpCompilation)verifier.Compilation).GetTypeByMetadataName("System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute");
        var ctors = attr!.GetMembers(".ctor");

        AssertEx.Equal(["System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(System.Int32 priority)", "System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(System.Object priority)"],
            ctors.SelectAsArray(ctor => ((MethodSymbol)ctor).ToTestDisplayString()));

        var attrs = ctors.SelectAsArray(ctor => ctor.GetAttributes());

        Assert.Empty(attrs[0]);
        AssertEx.Equal("System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(System.Int32 priority)",
            attrs[1].Single().AttributeConstructor.ToTestDisplayString());

        verifier.VerifyIL("System.Runtime.CompilerServices.C.Main()", """
            {
              // Code size        7 (0x7)
              .maxstack  1
              IL_0000:  ldnull
              IL_0001:  call       "void System.Runtime.CompilerServices.C.M(System.Runtime.CompilerServices.I1)"
              IL_0006:  ret
            }
            """);
    }

    [Fact]
    public void CycleOnOverloadResolutionPriorityConstructor_03()
    {
        var source = """
            namespace System.Runtime.CompilerServices;

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            public sealed class OverloadResolutionPriorityAttribute : Attribute
            {
                [OverloadResolutionPriority(1)]
                public OverloadResolutionPriorityAttribute(int priority)
                {
                    Priority = priority;
                }

                public required int Priority { get; set; }
            }
            """;

        CreateCompilation([source, RequiredMemberAttribute, CompilerFeatureRequiredAttribute]).VerifyDiagnostics(
            // (6,6): error CS9035: Required member 'OverloadResolutionPriorityAttribute.Priority' must be set in the object initializer or attribute constructor.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_RequiredMemberMustBeSet, "OverloadResolutionPriority").WithArguments("System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute.Priority").WithLocation(6, 6));
    }

    [Fact]
    public void CycleOnOverloadResolutionPriorityConstructor_04()
    {
        var source = """
            namespace System.Runtime.CompilerServices;

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            public sealed class OverloadResolutionPriorityAttribute : Attribute
            {
                [OtherAttribute()]
                public OverloadResolutionPriorityAttribute(int priority)
                {
                    Priority = priority;
                }

                public int Priority { get;}
            }

            class OtherAttribute : Attribute
            {
                [OverloadResolutionPriority(1)]
                public OtherAttribute() {}
            }
            """;

        CompileAndVerify(source).VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void OverloadResolutionAppliedToIndexers(bool useMetadataReference, bool i1First)
    {
        var executable = """
            var c = new C();
            I3 i3 = null;
            _ = c[i3];
            c[i3] = 0;
            """;

        var i1Source = """
                [OverloadResolutionPriority(1)]
                public int this[I1 x]
                {
                    get { System.Console.Write(1); return 0; }
                    set => System.Console.Write(2);
                }
            """;

        var i2Source = """
                public int this[I2 x]
                {
                    get => throw null;
                    set => throw null;
                }
            """;

        var source = $$"""
            using System.Runtime.CompilerServices;

            public interface I1 {}
            public interface I2 {}
            public interface I3 : I1, I2 {}

            public class C
            {
                {{(i1First ? i1Source : i2Source)}}
                {{(i1First ? i2Source : i1Source)}}
            }
            """;

        CompileAndVerify([executable, source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "12").VerifyDiagnostics();

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(executable, references: [AsReference(comp, useMetadataReference)], expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void AppliedToRegularProperty()
    {
        var source = """
            using System.Runtime.CompilerServices;

            public class C
            {
                [OverloadResolutionPriority(1)]
                public int P { get; set; }
            }
            """;

        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (5,6): error CS9501: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(5, 6)
        );
    }

    [Fact]
    public void AppliedToIndexerOverride()
    {
        var source = """
            using System.Runtime.CompilerServices;

            class Base
            {
                public virtual int this[int x] => throw null;
            }

            class Derived : Base
            {
                [OverloadResolutionPriority(1)]
                public override int this[int x] => throw null;
            }
            """;

        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (10,6): error CS9500: Cannot use 'OverloadResolutionPriorityAttribute' on an overriding member.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToOverride, "OverloadResolutionPriority(1)").WithLocation(10, 6)
        );
    }

    [Fact]
    public void NoImpactToFunctionType()
    {
        var source = """
            var m = C.M;

            class C
            {
                [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
                public static void M(string s) {}

                public static void M(int i) {}
            }
            """;

        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (1,9): error CS8917: The delegate type could not be inferred.
            // var m = C.M;
            Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "C.M").WithLocation(1, 9)
        );
    }

    [Theory, CombinatorialData]
    public void DelegateConversion(bool useMetadataReference)
    {
        var source = """
            using System.Runtime.CompilerServices;

            public class C
            {
                [OverloadResolutionPriority(1)]
                public static void M(object o) => System.Console.Write(1);
                public static void M(string s) => throw null;
            }
            """;

        var executable = """
            System.Action<string> a = C.M;
            a(null);
            """;

        CompileAndVerify([executable, source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(executable, references: [AsReference(comp, useMetadataReference)], expectedOutput: "1").VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void RestateOverriddenPriority_Method(bool useMetadataReference)
    {
        var @base = """
            using System.Runtime.CompilerServices;

            public class Base
            {
                [OverloadResolutionPriority(1)]
                public virtual void M(object o) => throw null;
                public virtual void M(string s) => throw null;
            }
            """;

        var derived = """
            using System.Runtime.CompilerServices;
            public class Derived : Base
            {
                [OverloadResolutionPriority(1)]
                public override void M(object o) => throw null;
                public override void M(string s) => throw null;
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (4,6): error CS9500: Cannot use 'OverloadResolutionPriorityAttribute' on an overriding member.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToOverride, "OverloadResolutionPriority(1)").WithLocation(4, 6)
        };

        CreateCompilation([derived, @base, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(expectedDiagnostics);

        var baseComp = CreateCompilation([@base, OverloadResolutionPriorityAttributeDefinition]);
        CreateCompilation(derived, references: [useMetadataReference ? baseComp.ToMetadataReference() : baseComp.EmitToImageReference()]).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, CombinatorialData]
    public void Interface_DifferentPriorities_Methods(bool useMetadataReference)
    {
        var executable = """
            var c = new C();
            I3 i3 = null;
            c.M(i3);
            ((I)c).M(i3);
            """;

        var source = """
            using System.Runtime.CompilerServices;

            public interface I1 {}
            public interface I2 {}
            public interface I3 : I1, I2 {}

            public interface I
            {
                [OverloadResolutionPriority(1)]
                void M(I1 i1);
                void M(I2 i2);
            }
            public class C : I
            {
                public void M(I1 i1) => System.Console.Write(1);
                [OverloadResolutionPriority(2)]
                public void M(I2 i2) => System.Console.Write(2);
            }
            """;

        CompileAndVerify([executable, source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "21").VerifyDiagnostics();

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(executable, references: [AsReference(comp, useMetadataReference)], expectedOutput: "21").VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void Interface_DifferentPriorities_Indexers(bool useMetadataReference)
    {
        var executable = """
            var c = new C();
            I3 i3 = null;
            c[i3] = 1;
            _ = c[i3];
            ((I)c)[i3] = 1;
            _ = ((I)c)[i3];
            """;

        var source = """
            using System.Runtime.CompilerServices;

            public interface I1 {}
            public interface I2 {}
            public interface I3 : I1, I2 {}

            public interface I
            {
                [OverloadResolutionPriority(1)]
                int this[I1 i1] { get; set; }
                int this[I2 i2] { get; set; }
            }
            public class C : I
            {
                public int this[I1 i1]
                {
                    get
                    {
                        System.Console.Write(1);
                        return 1;
                    }
                    set => System.Console.Write(2);
                }
                [OverloadResolutionPriority(1)]
                public int this[I2 i2]
                {
                    get
                    {
                        System.Console.Write(3);
                        return 1;
                    }
                    set => System.Console.Write(4);
                }
            }
            """;

        CompileAndVerify([executable, source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "4321").VerifyDiagnostics();

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(executable, references: [AsReference(comp, useMetadataReference)], expectedOutput: "4321").VerifyDiagnostics();
    }

    [Fact]
    public void AppliedToIndexerGetterSetter_Source()
    {
        var source = """
            using System.Runtime.CompilerServices;
            public class C
            {
                public int this[int x]
                {
                    [OverloadResolutionPriority(1)]
                    get => throw null;
                    [OverloadResolutionPriority(1)]
                    set => throw null;
                }
            }
            """;

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        comp.VerifyDiagnostics(
            // (6,10): error CS9502: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //         [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(6, 10),
            // (8,10): error CS9502: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //         [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(8, 10)
        );

        var c = comp.GetTypeByMetadataName("C")!;
        var indexer = c.GetMember<PropertySymbol>("this[]");

        Assert.Equal(0, indexer.OverloadResolutionPriority);
        Assert.Equal(0, indexer.GetMethod.OverloadResolutionPriority);
        Assert.Equal(0, indexer.SetMethod.OverloadResolutionPriority);
    }

    [Fact]
    public void AppliedToIndexerGetterSetter_Metadata()
    {
        // Equivalent to:
        // public class C
        // {
        //     public int this[object x]
        //     {
        //         [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        //         get => throw null;
        //         [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        //         set => throw null;
        //     }
        //     public int this[string x]
        //     {
        //         get { System.Console.Write(1); return 1; }
        //         set => System.Console.Write(2);
        //     }
        // }
        var il = """
            .class public auto ansi beforefieldinit C
                extends [mscorlib]System.Object
            {
                .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
                    01 00 04 49 74 65 6d 00 00
                )
                // Methods
                .method public hidebysig specialname 
                    instance int32 get_Item (
                        object x
                    ) cil managed 
                {
                    .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
                        01 00 01 00 00 00 00 00
                    )
                    ldnull
                    throw
                } // end of method C::get_Item

                .method public hidebysig specialname 
                    instance void set_Item (
                        object x,
                        int32 'value'
                    ) cil managed 
                {
                    .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
                        01 00 01 00 00 00 00 00
                    )
                    ldnull
                    throw
                } // end of method C::set_Item

                .method public hidebysig specialname 
                    instance int32 get_Item (
                        string x
                    ) cil managed 
                {
                    ldc.i4.1
                    call void [mscorlib]System.Console::Write(int32)
                    ldc.i4.1
                    ret
                } // end of method C::get_Item

                .method public hidebysig specialname 
                    instance void set_Item (
                        string x,
                        int32 'value'
                    ) cil managed 
                {
                    ldc.i4.2
                    call void [mscorlib]System.Console::Write(int32)
                    ret
                } // end of method C::set_Item

                .method public hidebysig specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    ldarg.0
                    call instance void [mscorlib]System.Object::.ctor()
                    ret
                } // end of method C::.ctor

                // Properties
                .property instance int32 Item(
                    object x
                )
                {
                    .get instance int32 C::get_Item(object)
                    .set instance void C::set_Item(object, int32)
                }
                .property instance int32 Item(
                    string x
                )
                {
                    .get instance int32 C::get_Item(string)
                    .set instance void C::set_Item(string, int32)
                }
            }

            """;

        var ilRef = CompileIL(il + OverloadResolutionPriorityAttributeILDefinition);

        var source = """
            var c = new C();
            _ = c["test"];
            c["test"] = 0;
            """;

        var comp = (CSharpCompilation)CompileAndVerify(source, references: [ilRef], expectedOutput: "12").VerifyDiagnostics().Compilation;

        var c = comp.GetTypeByMetadataName("C")!;
        var indexers = c.GetMembers("this[]");

        Assert.Equal(2, indexers.Length);

        var indexer = (PropertySymbol)indexers[0];
        AssertEx.Equal("System.Int32 C.this[System.Object x] { get; set; }", indexer.ToTestDisplayString());
        Assert.Equal(0, indexer.OverloadResolutionPriority);
        Assert.Equal(0, indexer.GetMethod.OverloadResolutionPriority);
        Assert.Equal(0, indexer.SetMethod.OverloadResolutionPriority);

        indexer = (PropertySymbol)indexers[1];
        AssertEx.Equal("System.Int32 C.this[System.String x] { get; set; }", indexer.ToTestDisplayString());
        Assert.Equal(0, indexer.OverloadResolutionPriority);
        Assert.Equal(0, indexer.GetMethod.OverloadResolutionPriority);
        Assert.Equal(0, indexer.SetMethod.OverloadResolutionPriority);
    }

    [Fact]
    public void AppliedToPropertyGetterSetter()
    {
        var source = """
            using System.Runtime.CompilerServices;
            public class C
            {
                public int Prop
                {
                    [OverloadResolutionPriority(1)]
                    get => throw null;
                    [OverloadResolutionPriority(1)]
                    set => throw null;
                }
            }
            """;

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        comp.VerifyDiagnostics(
            // (6,10): error CS9502: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //         [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(6, 10),
            // (8,10): error CS9502: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //         [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(8, 10)
        );

        var c = comp.GetTypeByMetadataName("C")!;
        var indexer = c.GetMember<PropertySymbol>("Prop");

        Assert.Equal(0, indexer.OverloadResolutionPriority);
        Assert.Equal(0, indexer.GetMethod.OverloadResolutionPriority);
        Assert.Equal(0, indexer.SetMethod.OverloadResolutionPriority);
    }

    [Fact]
    public void AppliedToEventGetterSetter()
    {
        var source = """
            using System.Runtime.CompilerServices;
            public class C
            {
                public event System.Action Prop
                {
                    [OverloadResolutionPriority(1)]
                    add { }
                    [OverloadResolutionPriority(1)]
                    remove { }
                }
            }
            """;

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        comp.VerifyDiagnostics(
            // (6,10): error CS9502: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //         [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(6, 10),
            // (8,10): error CS9502: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //         [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(8, 10)
        );

        var c = comp.GetTypeByMetadataName("C")!;
        var indexer = c.GetMember<EventSymbol>("Prop");

        Assert.Equal(0, indexer!.AddMethod!.OverloadResolutionPriority);
        Assert.Equal(0, indexer!.RemoveMethod!.OverloadResolutionPriority);
    }

    [Fact]
    public void ExplicitImplementation_AppliedToImplementation()
    {
        var source = """
            using System.Runtime.CompilerServices;
            public interface I
            {
                void M(object o);
                void M(string s);

                int this[object o] { get; set; }
                int this[string s] { get; set; }
            }

            public class C : I
            {
                [OverloadResolutionPriority(1)]
                void I.M(object o) => throw null;
                void I.M(string s) => throw null;

                [OverloadResolutionPriority(1)]
                int I.this[object o]
                {
                    get => throw null;
                    set => throw null;
                }
                int I.this[string s]
                {
                    get => throw null;
                    set => throw null;
                }
            }
            """;

        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (13,6): error CS9503: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(13, 6),
            // (17,6): error CS9503: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(17, 6)
        );
    }

    [Fact]
    public void ExplicitImplementation_AppliedToInterface()
    {
        var source = """
            using System.Runtime.CompilerServices;

            var c = new C();
            ((I)c).M("test");
            _ = ((I)c)["test"];
            ((I)c)["test"] = 1;

            public interface I
            {
                [OverloadResolutionPriority(1)]
                void M(object o);
                void M(string s);

                [OverloadResolutionPriority(1)]
                int this[object o] { get; set; }
                int this[string s] { get; set; }
            }

            public class C : I
            {
                void I.M(object o) => System.Console.Write(1);
                void I.M(string s) => throw null;

                int I.this[object o]
                {
                    get { System.Console.Write(2); return 1; }
                    set => System.Console.Write(3);
                }
                int I.this[string s]
                {
                    get => throw null;
                    set => throw null;
                }
            }
            """;

        CompileAndVerify([source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "123").VerifyDiagnostics();
    }

    [Fact]
    public void ExplicitImplementation_AppliedToBoth()
    {
        var source = """
            using System.Runtime.CompilerServices;
            public interface I
            {
                [OverloadResolutionPriority(1)]
                void M(object o);
                void M(string s);

                [OverloadResolutionPriority(1)]
                int this[object o] { get; set; }
                int this[string s] { get; set; }
            }

            public class C : I
            {
                [OverloadResolutionPriority(1)]
                void I.M(object o) => throw null;
                void I.M(string s) => throw null;

                [OverloadResolutionPriority(1)]
                int I.this[object o]
                {
                    get => throw null;
                    set => throw null;
                }
                int I.this[string s]
                {
                    get => throw null;
                    set => throw null;
                }
            }
            """;

        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (15,6): error CS9503: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(15, 6),
            // (19,6): error CS9503: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(19, 6)
        );
    }

    [Theory, CombinatorialData]
    public void Dynamic(bool useMetadataReference)
    {
        var source = @"
            using System.Runtime.CompilerServices;

            public class C
            {
                [OverloadResolutionPriority(1)]
                public C(object o) => throw null;
                public C(string s) => System.Console.Write(1);

                [OverloadResolutionPriority(1)]
                public void M(object o) => throw null;
                public void M(string s) => System.Console.Write(2);

                [OverloadResolutionPriority(1)]
                public int this[object o]
                {
                    get => throw null;
                    set => throw null;
                }
                public int this[string o]
                {
                    get { System.Console.Write(3); return 1; }
                    set => System.Console.Write(4);
                }
            }
            ";

        var executable = """
            dynamic arg = "test";
            C c = new C(arg);
            c.M(arg);
            _ = c[arg];
            c[arg] = 1;
            """;

        CompileAndVerify([executable, source, OverloadResolutionPriorityAttributeDefinition], targetFramework: TargetFramework.Mscorlib461AndCSharp, expectedOutput: "1234").VerifyDiagnostics();

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition], targetFramework: TargetFramework.Mscorlib461AndCSharp);
        CompileAndVerify(executable, references: new[] { AsReference(comp, useMetadataReference) }, targetFramework: TargetFramework.Mscorlib461AndCSharp, expectedOutput: "1234").VerifyDiagnostics();
    }

    [Fact]
    public void Destructor()
    {
        var source = """
            using System.Runtime.CompilerServices;

            public class C
            {
                [OverloadResolutionPriority(1)]
                ~C() => throw null;
            }
            """;
        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (5,6): error CS9501: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(5, 6)
        );
    }
    [Fact]
    public void IndexedProperty()
    {
        var attrRef = CreateCompilation(OverloadResolutionPriorityAttributeDefinition);

        string vbSource = """
            Imports System
            Imports System.Runtime.CompilerServices
            Imports System.Runtime.InteropServices

            <ComImport, Guid("0002095E-0000-0000-C000-000000000046")>
            Public Class B
                <OverloadResolutionPriority(1)>
                Public Property IndexedProperty(arg As Object) As Integer
                    Get
                        Return 0
                    End Get
                    Set
                    End Set
                End Property
                Public Property IndexedProperty(arg As String) As Integer
                    Get
                        Throw New Exception()
                    End Get
                    Set
                        Throw New Exception()
                    End Set
                End Property
            End Class
            """;
        var vb = CreateVisualBasicCompilation(GetUniqueName(), vbSource, referencedAssemblies: TargetFrameworkUtil.NetStandard20References, referencedCompilations: [attrRef]);
        var vbRef = vb.EmitToImageReference();

        var executable = """
            var b = new B();
            _ = b.IndexedProperty["test"];
            b.IndexedProperty["test"] = 0;
            """;

        var verifier = CompileAndVerify(executable, references: [attrRef.EmitToImageReference(), vbRef]).VerifyDiagnostics();
        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       29 (0x1d)
              .maxstack  3
              IL_0000:  newobj     "B..ctor()"
              IL_0005:  dup
              IL_0006:  ldstr      "test"
              IL_000b:  callvirt   "int B.IndexedProperty[object].get"
              IL_0010:  pop
              IL_0011:  ldstr      "test"
              IL_0016:  ldc.i4.0
              IL_0017:  callvirt   "void B.IndexedProperty[object].set"
              IL_001c:  ret
            }
            """);
    }

    [Fact]
    public void NoWarningOnLocalFunction()
    {
        var source = """
            using System.Runtime.CompilerServices;

            public class C
            {
                public static void Main()
                {
                    Local("test");

                    [OverloadResolutionPriority(1)]
                    void Local(object o) => System.Console.Write(1);
                }
            }
            """;

        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (9,10): error CS9262: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //         [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(9, 10)
        );
    }

    [Fact]
    public void ErrorOnLambda()
    {
        var source = """
            using System.Runtime.CompilerServices;

            public class C
            {
                public void M()
                {
                    var l = [OverloadResolutionPriority(1)] (object o) => System.Console.Write(1);
                }
            }
            """;

        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (7,18): error CS9501: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //         var l = [OverloadResolutionPriority(1)] (object o) => System.Console.Write(1);
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(7, 18)
        );
    }

    [Theory, CombinatorialData]
    public void BinaryOperators_SameType(bool useMetadataReference)
    {
        var source = """
            using System.Runtime.CompilerServices;

            public interface I1 {}
            public interface I2 {}
            public interface I3 : I1, I2 {}

            public class C
            {
                [OverloadResolutionPriority(1)]
                public static C operator +(C c, I1 i) { System.Console.Write(1); return c; }
                public static C operator +(C c, I2 i) => throw null;
            }
            """;

        var executable = """
            var c = new C();
            I3 i3 = null;
            _ = c + i3;
            """;

        CompileAndVerify([executable, source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(executable, references: [AsReference(comp, useMetadataReference)], expectedOutput: "1").VerifyDiagnostics();
    }

    [Theory, CombinatorialData]
    public void BinaryOperators_DifferentType(bool useMetadataReference)
    {
        var source = @"
            using System.Runtime.CompilerServices;

            public class C1
            {
                [OverloadResolutionPriority(1)]
                public static C1 operator +(C1 c1, C2 c2) => throw null;
            }

            public class C2
            {
                public static C2 operator +(C1 c1, C2 c2) => throw null;
            }
            ";

        var executable = @"
            var c1 = new C1();
            var c2 = new C2();
            _ = c1 + c2;
            ";

        var expectedDiagnostics = new[] {
            // (4,17): error CS0034: Operator '+' is ambiguous on operands of type 'C1' and 'C2'
            //             _ = c1 + c2;
            Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "c1 + c2").WithArguments("+", "C1", "C2").WithLocation(4, 17)
        };

        CreateCompilation([executable, source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(expectedDiagnostics);

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CreateCompilation(executable, references: [AsReference(comp, useMetadataReference)]).VerifyDiagnostics(expectedDiagnostics);
    }

    [Theory, CombinatorialData]
    public void UnaryOperator_Allowed(bool useMetadataReference)
    {
        var source = """
            using System.Runtime.CompilerServices;

            public struct S
            {
                public static S? operator!(S? s) => throw null;

                [OverloadResolutionPriority(1)]
                public static S operator!(S s)
                {
                    System.Console.Write("1");
                    return s;
                }
            }
            """;

        var executable = """
            S? s = new S();
            _ = !s;
            """;

        CompileAndVerify([executable, source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(executable, references: [AsReference(comp, useMetadataReference)], expectedOutput: "1").VerifyDiagnostics();
    }

    [Theory]
    [InlineData("implicit")]
    [InlineData("explicit")]
    public void ConversionOperators_Disallowed(string operatorType)
    {
        var source = $$"""
            using System.Runtime.CompilerServices;

            public class C
            {
                [OverloadResolutionPriority(1)]
                public static {{operatorType}} operator C(int i) => throw null;
            }
            """;

        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (5,6): error CS9501: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(5, 6)
        );
    }

    [Fact]
    public void DisallowedOnStaticCtors()
    {
        var code = """
            using System.Runtime.CompilerServices;

            public class C
            {
                [OverloadResolutionPriority(1)]
                static C() => throw null;
            }
            """;

        CreateCompilation([code, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (5,6): error CS9501: Cannot use 'OverloadResolutionPriorityAttribute' on this member.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember, "OverloadResolutionPriority(1)").WithLocation(5, 6)
        );
    }

    [Theory, CombinatorialData]
    public void InterpolatedStringHandlerPriority(bool useMetadataReference)
    {
        var handler = """
            using System;
            using System.Runtime.CompilerServices;

            [InterpolatedStringHandler]
            public struct Handler
            {
                public Handler(int literalLength, int formattedCount) {}

                [OverloadResolutionPriority(1)]
                public void AppendLiteral(string s, int i = 0) => Console.Write(1);
                public void AppendLiteral(string s) => throw null;
                [OverloadResolutionPriority(1)]
                public void AppendFormatted(object o) => Console.Write(2);
                public void AppendFormatted(int i) => throw null;

            }
            """;

        var executable = """
            Handler h = $"test {1}";
            """;

        var verifier = CompileAndVerify([handler, executable, OverloadResolutionPriorityAttributeDefinition, InterpolatedStringHandlerAttribute], expectedOutput: "12").VerifyDiagnostics();

        verifier.VerifyIL("<top-level-statements-entry-point>", """
            {
              // Code size       36 (0x24)
              .maxstack  3
              .locals init (Handler V_0)
              IL_0000:  ldloca.s   V_0
              IL_0002:  ldc.i4.5
              IL_0003:  ldc.i4.1
              IL_0004:  call       "Handler..ctor(int, int)"
              IL_0009:  ldloca.s   V_0
              IL_000b:  ldstr      "test "
              IL_0010:  ldc.i4.0
              IL_0011:  call       "void Handler.AppendLiteral(string, int)"
              IL_0016:  ldloca.s   V_0
              IL_0018:  ldc.i4.1
              IL_0019:  box        "int"
              IL_001e:  call       "void Handler.AppendFormatted(object)"
              IL_0023:  ret
            }
            """);

        var comp = CreateCompilation([handler, OverloadResolutionPriorityAttributeDefinition, InterpolatedStringHandlerAttribute]);
        CompileAndVerify(executable, references: [AsReference(comp, useMetadataReference)], expectedOutput: "12").VerifyDiagnostics();
    }

    [Theory]
    [InlineData("[System.Runtime.CompilerServices.OverloadResolutionPriority(1)]", "")]
    [InlineData("", "[System.Runtime.CompilerServices.OverloadResolutionPriority(1)]")]
    public void PartialMethod(string definitionPriority, string implementationPriority)
    {
        var definition = $$"""
            public partial class C
            {
                {{definitionPriority}}
                public partial void M(object o);
            }
            """;

        var implementation = $$"""
            public partial class C
            {
                {{implementationPriority}}
                public partial void M(object o) => System.Console.Write(1);
                public void M(string s) => throw null;
            }
            """;

        var executable = """
            var c = new C();
            c.M("");
            """;

        CompileAndVerify([executable, definition, implementation, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();
    }

    [Theory]
    [InlineData("[System.Runtime.CompilerServices.OverloadResolutionPriority(1)]", "")]
    [InlineData("", "[System.Runtime.CompilerServices.OverloadResolutionPriority(1)]")]
    public void PartialIndexer(string definitionPriority, string implementationPriority)
    {
        var definition = $$"""
            public partial class C
            {
                {{definitionPriority}}
                public partial int this[object o] { get; set; }
            }
            """;

        var implementation = $$"""
            public partial class C
            {
                {{implementationPriority}}
                public partial int this[object o]
                {
                    get { System.Console.Write(1); return 1; }
                    set => System.Console.Write(2);
                }
                public int this[string s]
                {
                    get => throw null;
                    set => throw null;
                }
            }
            """;

        var executable = """
            var c = new C();
            _ = c[""];
            c[""] = 0;
            """;

        CompileAndVerify([executable, definition, implementation, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void AttributeAppliedTwiceMethod_Source()
    {
        var source = """
            using System.Runtime.CompilerServices;

            var c = new C();
            c.M("");

            public class C
            {
                [OverloadResolutionPriority(1)]
                [OverloadResolutionPriority(2)]
                public void M(object o) => System.Console.Write(1);
                public void M(string s) => System.Console.Write(2);
            }
            """;

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        comp.VerifyDiagnostics(
            // (9,6): error CS0579: Duplicate 'OverloadResolutionPriority' attribute
            //     [OverloadResolutionPriority(2)]
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "OverloadResolutionPriority").WithArguments("OverloadResolutionPriority").WithLocation(9, 6)
        );

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        var symbol = model.GetSymbolInfo(invocation).Symbol;

        Assert.Equal("void C.M(System.Object o)", symbol.ToTestDisplayString());
        var underlyingSymbol = symbol.GetSymbol<MethodSymbol>();

        Assert.Equal(2, underlyingSymbol!.OverloadResolutionPriority);
    }

    [Fact]
    public void AttributeAppliedTwiceMethod_Metadata()
    {
        // Equivalent to:
        // public class C
        // {
        //     [OverloadResolutionPriority(1)]
        //     [OverloadResolutionPriority(2)]
        //     public void M(object o) => System.Console.Write(1);
        //     public void M(string s) => System.Console.Write(2);
        // }
        var il = """
            .class public auto ansi beforefieldinit C extends [mscorlib]System.Object
            {
                .method public hidebysig 
                    instance void M (
                        object o
                    ) cil managed 
                {
                    .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
                        01 00 01 00 00 00 00 00
                    )
                    .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
                        01 00 02 00 00 00 00 00
                    )
                    ldc.i4.1
                    call void [mscorlib]System.Console::Write(int32)
                    ret
                } // end of method C::M

                .method public hidebysig 
                    instance void M (
                        string s
                    ) cil managed 
                {
                    ldc.i4.2
                    call void [mscorlib]System.Console::Write(int32)
                    ret
                } // end of method C::M

                .method public hidebysig specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    ldarg.0
                    call instance void [mscorlib]System.Object::.ctor()
                    ret
                } // end of method C::.ctor
            } // end of class C

            """;

        var ilRef = CompileIL(il + OverloadResolutionPriorityAttributeILDefinition);

        var source = """
            var c = new C();
            c.M("");
            """;

        var comp = CreateCompilation(source, references: [ilRef]);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        var symbol = model.GetSymbolInfo(invocation).Symbol;

        Assert.Equal("void C.M(System.Object o)", symbol.ToTestDisplayString());
        var underlyingSymbol = symbol.GetSymbol<MethodSymbol>();

        Assert.Equal(2, underlyingSymbol!.OverloadResolutionPriority);
    }

    [Fact]
    public void AttributeAppliedTwiceConstructor_Source()
    {
        var source = """
            using System.Runtime.CompilerServices;

            var c = new C("");

            public class C
            {
                [OverloadResolutionPriority(1)]
                [OverloadResolutionPriority(2)]
                public C(object o) {}
                public C(string s) {}
            }
            """;

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        comp.VerifyDiagnostics(
            // (8,6): error CS0579: Duplicate 'OverloadResolutionPriority' attribute
            //     [OverloadResolutionPriority(2)]
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "OverloadResolutionPriority").WithArguments("OverloadResolutionPriority").WithLocation(8, 6)
        );

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First();
        var symbol = model.GetSymbolInfo(invocation).Symbol;

        Assert.Equal("C..ctor(System.Object o)", symbol.ToTestDisplayString());
        var underlyingSymbol = symbol.GetSymbol<MethodSymbol>();

        Assert.Equal(2, underlyingSymbol!.OverloadResolutionPriority);
    }

    [Fact]
    public void AttributeAppliedTwiceConstructor_Metadata()
    {
        // Equivalent to:
        // public class C
        // {
        //     [OverloadResolutionPriority(1)]
        //     [OverloadResolutionPriority(2)]
        //     public C(object o) {}
        //     public C(string s) {}
        // }
        var il = """
            .class public auto ansi beforefieldinit C extends [mscorlib]System.Object
            {
                .method public hidebysig specialname rtspecialname 
                    instance void .ctor (
                        object o
                    ) cil managed 
                {
                    .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
                        01 00 01 00 00 00 00 00
                    )
                    .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
                        01 00 02 00 00 00 00 00
                    )
                    ldarg.0
                    call instance void [mscorlib]System.Object::.ctor()
                    ret
                }

                .method public hidebysig specialname rtspecialname 
                    instance void .ctor (
                        string s
                    ) cil managed 
                {
                    ldarg.0
                    call instance void [mscorlib]System.Object::.ctor()
                    ret
                } // end of method C::.ctor
            } // end of class C

            """;

        var ilRef = CompileIL(il + OverloadResolutionPriorityAttributeILDefinition);

        var source = """
            var c = new C("");
            """;

        var comp = CreateCompilation(source, references: [ilRef]);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var invocation = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First();
        var symbol = model.GetSymbolInfo(invocation).Symbol;

        Assert.Equal("C..ctor(System.Object o)", symbol.ToTestDisplayString());
        var underlyingSymbol = symbol.GetSymbol<MethodSymbol>();

        Assert.Equal(2, underlyingSymbol!.OverloadResolutionPriority);
    }

    [Fact]
    public void AttributeAppliedTwiceIndexer_Source()
    {
        var source = """
            using System.Runtime.CompilerServices;

            var c = new C();
            _ = c[""];
            c[""] = 0;

            public class C
            {
                [OverloadResolutionPriority(1)]
                [OverloadResolutionPriority(2)]
                public int this[object o]
                {
                    get => throw null;
                    set => throw null;
                }
                public int this[string o]
                {
                    get => throw null;
                    set => throw null;
                }
            }
            """;

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        comp.VerifyDiagnostics(
            // (10,6): error CS0579: Duplicate 'OverloadResolutionPriority' attribute
            //     [OverloadResolutionPriority(2)]
            Diagnostic(ErrorCode.ERR_DuplicateAttribute, "OverloadResolutionPriority").WithArguments("OverloadResolutionPriority").WithLocation(10, 6)
        );

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var symbols = tree.GetRoot().DescendantNodes()
            .OfType<ElementAccessExpressionSyntax>()
            .Select(i => model.GetSymbolInfo(i).Symbol)
            .ToArray();

        Assert.Equal(2, symbols.Length);

        Assert.All(symbols, s =>
        {
            AssertEx.Equal("System.Int32 C.this[System.Object o] { get; set; }", s.ToTestDisplayString());
            var underlyingSymbol = s.GetSymbol<PropertySymbol>();

            Assert.Equal(2, underlyingSymbol!.OverloadResolutionPriority);
        });
    }

    [Fact]
    public void AttributeAppliedTwiceIndexer_Metadata()
    {
        // Equivalent to:
        // public class C
        // {
        //     [OverloadResolutionPriority(1)]
        //     [OverloadResolutionPriority(2)]
        //     public int this[object o]
        //     {
        //         get => throw null;
        //         set => throw null;
        //     }
        //     public int this[string o]
        //     {
        //         get => throw null;
        //         set => throw null;
        //     }
        // }
        var il = """
            .class public auto ansi beforefieldinit C extends [mscorlib]System.Object
            {
                .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
                    01 00 04 49 74 65 6d 00 00
                )
                // Methods
                .method public hidebysig specialname 
                    instance int32 get_Item (
                        object o
                    ) cil managed 
                {
                    ldnull
                    throw
                } // end of method C::get_Item

                .method public hidebysig specialname 
                    instance void set_Item (
                        object o,
                        int32 'value'
                    ) cil managed 
                {
                    ldnull
                    throw
                } // end of method C::set_Item

                .method public hidebysig specialname 
                    instance int32 get_Item (
                        string o
                    ) cil managed 
                {
                    ldnull
                    throw
                } // end of method C::get_Item

                .method public hidebysig specialname 
                    instance void set_Item (
                        string o,
                        int32 'value'
                    ) cil managed 
                {
                    ldnull
                    throw
                } // end of method C::set_Item

                .method public hidebysig specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    ldarg.0
                    call instance void [mscorlib]System.Object::.ctor()
                    ret
                } // end of method C::.ctor

                // Properties
                .property instance int32 Item(
                    object o
                )
                {
                    .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
                        01 00 01 00 00 00 00 00
                    )
                    .custom instance void System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::.ctor(int32) = (
                        01 00 02 00 00 00 00 00
                    )
                    .get instance int32 C::get_Item(object)
                    .set instance void C::set_Item(object, int32)
                }
                .property instance int32 Item(
                    string o
                )
                {
                    .get instance int32 C::get_Item(string)
                    .set instance void C::set_Item(string, int32)
                }

            } // end of class C

            """;

        var ilRef = CompileIL(il + OverloadResolutionPriorityAttributeILDefinition);

        var source = """
            var c = new C();
            _ = c[""];
            c[""] = 0;
            """;

        var comp = CreateCompilation(source, references: [ilRef]);
        comp.VerifyDiagnostics();

        var tree = comp.SyntaxTrees[0];
        var model = comp.GetSemanticModel(tree);
        var symbols = tree.GetRoot().DescendantNodes()
            .OfType<ElementAccessExpressionSyntax>()
            .Select(i => model.GetSymbolInfo(i).Symbol)
            .ToArray();

        Assert.Equal(2, symbols.Length);

        Assert.All(symbols, s =>
        {
            AssertEx.Equal("System.Int32 C.this[System.Object o] { get; set; }", s.ToTestDisplayString());
            var underlyingSymbol = s.GetSymbol<PropertySymbol>();

            Assert.Equal(2, underlyingSymbol!.OverloadResolutionPriority);
        });
    }

    [Fact]
    public void HonoredInsideExpressionTree()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq.Expressions;
            using System.Runtime.CompilerServices;

            Expression<Action> e = () => C.M(1, 2, 3);

            public class C
            {
                public static void M(params int[] a)
                {
                }

                [OverloadResolutionPriority(1)]
                public static void M(params IEnumerable<int> e)
                {
                }
            }
            """;

        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (6,30): error CS9226: An expression tree may not contain an expanded form of non-array params collection parameter.
            // Expression<Action> e = () => C.M(1, 2, 3);
            Diagnostic(ErrorCode.ERR_ParamsCollectionExpressionTree, "C.M(1, 2, 3)").WithLocation(6, 30)
        );
    }

    [Fact]
    public void QuerySyntax()
    {
        var source = """
            using System;
            using System.Runtime.CompilerServices;

            var c = new C();
            _ = from x in c select x;

            class C
            {
                [OverloadResolutionPriority(1)]
                public C Select(Func<int, int> selector, int i = 0) { Console.Write(1); return this; }
                public C Select(Func<int, int> selector) => throw null;
            }
            """;

        CompileAndVerify([source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void ObjectInitializers()
    {
        var source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            class C : IEnumerable<int>
            {
                private List<int> _list = new();
                public void Add(int x) { throw null; }
                [OverloadResolutionPriority(1)] public void Add(int x, int y = 0) { _list.Add(x); }
                public IEnumerator<int> GetEnumerator() => _list.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => null;

                [OverloadResolutionPriority(1)]
                public int this[int i, int y = 0]
                {
                    set { _list.Add(i); _list.Add(value); }
                }

                public int this[int i]
                {
                    set => throw null;
                }
            }

            class Program
            {
                static void Main()
                {
                    C c = new() { 1 };
                    foreach (var i in c) Console.Write(i);
                    c = [2];
                    foreach (var i in c) Console.Write(i);
                    c = new() { [3] = 4 };
                    foreach (var i in c) Console.Write(i);
                }
            }
            """;

        CompileAndVerify([source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1234");
    }
}
