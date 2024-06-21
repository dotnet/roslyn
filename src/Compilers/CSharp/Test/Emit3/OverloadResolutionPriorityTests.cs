// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Test;

public class OverloadResolutionPriorityTests : CSharpTestBase
{
    [Fact]
    public void IncreasedPriorityWins_01()
    {
        var source = """
            using System.Runtime.CompilerServices;

            I3 i3 = null;
            C.M(i3);

            interface I1 {}
            interface I2 {}
            interface I3 : I1, I2 {}

            class C
            {
                [OverloadResolutionPriority(1)]
                public static void M(I1 x) => System.Console.WriteLine(1);

                public static void M(I2 x) => throw null;
            }
            """;
        CompileAndVerify([source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void IncreasedPriorityWins_02()
    {
        var source = """
            using System.Runtime.CompilerServices;

            I3 i3 = null;
            C.M(i3);

            interface I1 {}
            interface I2 {}
            interface I3 : I1, I2 {}

            class C
            {
                [OverloadResolutionPriority(2)]
                public static void M(object o) => System.Console.WriteLine(1);

                [OverloadResolutionPriority(1)]
                public static void M(I1 x) => throw null;

                public static void M(I2 x) => throw null;
            }
            """;
        CompileAndVerify([source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void DecreasedPriorityLoses()
    {
        var source = """
            using System.Runtime.CompilerServices;

            I3 i3 = null;
            C.M(i3);

            interface I1 {}
            interface I2 {}
            interface I3 : I1, I2 {}

            class C
            {
                public static void M(I1 x) => System.Console.WriteLine(1);

                [OverloadResolutionPriority(-1)]
                public static void M(I2 x) => throw null;
            }
            """;
        CompileAndVerify([source, OverloadResolutionPriorityAttributeDefinition], expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void ZeroIsTreatedAsDefault()
    {
        var source = """
            using System.Runtime.CompilerServices;

            I3 i3 = null;
            C.M(i3);

            interface I1 {}
            interface I2 {}
            interface I3 : I1, I2 {}

            class C
            {
                public static void M(I1 x) => System.Console.WriteLine(1);

                [OverloadResolutionPriority(0)]
                public static void M(I2 x) => throw null;
            }
            """;
        CreateCompilation([source, OverloadResolutionPriorityAttributeDefinition]).VerifyDiagnostics(
            // (4,3): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(I1)' and 'C.M(I2)'
            // C.M(i3);
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(I1)", "C.M(I2)").WithLocation(4, 3)
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
            """;

        var verifier = CompileAndVerify(source).VerifyDiagnostics();

        var attr = ((CSharpCompilation)verifier.Compilation).GetTypeByMetadataName("System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute");
        var ctors = attr!.GetMembers(".ctor");

        AssertEx.Equal(["System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(System.Int32 priority)", "System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(System.Object priority)"],
            ctors.SelectAsArray(ctor => ((MethodSymbol)ctor).ToTestDisplayString()));

        var attrs = ctors.SelectAsArray(ctor => ctor.GetAttributes());

        Assert.Empty(attrs[0]);
        AssertEx.Equal("System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute..ctor(System.Int32 priority)",
            attrs[1].Single().AttributeConstructor.ToTestDisplayString());
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
}
