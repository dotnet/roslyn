// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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

        CompileAndVerify([executable, source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1", symbolValidator: module =>
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
        }).VerifyDiagnostics();

        var comp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]);
        CompileAndVerify(executable, references: [useMetadataReference ? comp.ToMetadataReference() : comp.EmitToImageReference()], expectedOutput: "1").VerifyDiagnostics();
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
        CompileAndVerify(executable, references: [useMetadataReference ? comp.ToMetadataReference() : comp.EmitToImageReference()], expectedOutput: "1").VerifyDiagnostics();
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
        CompileAndVerify(executable, references: [useMetadataReference ? comp.ToMetadataReference() : comp.EmitToImageReference()], expectedOutput: "1").VerifyDiagnostics();
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
        CompileAndVerify(comp, symbolValidator: module =>
        {
            var c = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var ms = c.GetMembers("M").Cast<MethodSymbol>();
            Assert.All(ms, m => Assert.Equal(0, m.OverloadResolutionPriority));
        }).VerifyDiagnostics();

        CreateCompilation(executable, references: [useMetadataReference ? comp.ToMetadataReference() : comp.EmitToImageReference()]).VerifyDiagnostics(
            // (2,3): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(I1)' and 'C.M(I2)'
            // C.M(i3);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(I1)", "C.M(I2)").WithLocation(2, 3)
        );
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

    [Fact]
    public void MethodDiscoveryStopsAtFirstApplicableMethod()
    {
        var source = """
            using System.Runtime.CompilerServices;

            I3 i3 = null;
            Derived.M(i3);

            interface I1 {}
            interface I2 {}
            interface I3 : I1, I2 {}

            class Base
            {
                [OverloadResolutionPriority(1)]
                public static void M(I2 x) => throw null;
            }

            class Derived : Base
            {
                public static void M(I1 x) => System.Console.WriteLine(1);
            }
            """;
        CompileAndVerify([source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();
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
    public void LangVersion()
    {
        var source = """
            using System.Runtime.CompilerServices;

            public class C
            {
                [OverloadResolutionPriority(1)]
                public void M(object o) => System.Console.WriteLine("1");
                public void M(string s) => throw null;
            }
            """;

        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition], parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (5,6): error CS8652: The feature 'overload resolution priority' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "OverloadResolutionPriority(1)").WithArguments("overload resolution priority").WithLocation(5, 6));

        var definingComp = CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition], parseOptions: TestOptions.RegularNext).VerifyDiagnostics();

        var consumingSource = """
            var c = new C();
            c.M("test");
            """;

        // We don't error on consumption, only on definition, so this runs just fine.
        CompileAndVerify(consumingSource, references: [definingComp.ToMetadataReference()], parseOptions: TestOptions.Regular12, expectedOutput: "1").VerifyDiagnostics();
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

    [Fact]
    public void CycleOnOverloadResolutionPriorityConstructor_02()
    {
        var source = """
            namespace System.Runtime.CompilerServices;

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            public sealed class OverloadResolutionPriorityAttribute : Attribute
            {
                public OverloadResolutionPriorityAttribute(int priority)
                {
                }

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

                static void Test()
                {
                    I3 i3 = null;
                    i3.M();
                }
            }
            """;

        var verifier = CompileAndVerify(source).VerifyDiagnostics();

        var attr = ((CSharpCompilation)verifier.Compilation).GetTypeByMetadataName("System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute");
        var ctors = attr!.GetMembers(".ctor");

        AssertEx.Equal(["System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(System.Int32 priority)", "System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(System.Object priority)"],
            ctors.SelectAsArray(ctor => ((MethodSymbol)ctor).ToTestDisplayString()));

        var attrs = ctors.SelectAsArray(ctor => ctor.GetAttributes());

        Assert.Empty(attrs[0]);
        AssertEx.Equal("System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(System.Object priority)",
            attrs[1].Single().AttributeConstructor.ToTestDisplayString());

        verifier.VerifyIL("System.Runtime.CompilerServices.C.Test()", """
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
        CompileAndVerify(executable, references: [useMetadataReference ? comp.ToMetadataReference() : comp.EmitToImageReference()], expectedOutput: "12").VerifyDiagnostics();
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
            // (5,6): error CS9501: Cannot put 'OverloadResolutionPriorityAttribute' on a property that is not an indexer.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToNonIndexer, "OverloadResolutionPriority(1)").WithLocation(5, 6)
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
            // (10,6): error CS9500: Cannot put 'OverloadResolutionPriorityAttribute' on an overriding member.
            //     [OverloadResolutionPriority(1)]
            Diagnostic(ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToOverride, "OverloadResolutionPriority(1)").WithLocation(10, 6)
        );
    }

    // PROTOTYPE: applied to getter/setter? Different from the indexer? Email LDM, assume invalid
    // PROTOTYPE: confirm that restating the same priority as the overridden member is invalid
    // PROTOTYPE: through retargeting, for ctors, methods, and indexers
    // PROTOTYPE: more inheritance tests; when overridden with a new attribute in metadata, consumption when overridden with a new attribute in source (and via ToMetadataReference)
}
