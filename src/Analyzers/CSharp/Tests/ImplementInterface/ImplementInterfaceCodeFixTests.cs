// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ImplementInterface;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ImplementInterface;

using VerifyCS = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    CSharpImplementInterfaceCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
public sealed class ImplementInterfaceCodeFixTests
{
    private readonly NamingStylesTestOptionSets _options = new(LanguageNames.CSharp);

    private const string CompilerFeatureRequiredAttribute = """

        namespace System.Runtime.CompilerServices
        {
            public sealed class CompilerFeatureRequiredAttribute : Attribute
            {
                public CompilerFeatureRequiredAttribute(string featureName)
                {
                }
            }
        }
        """;

    private static OptionsCollection AllOptionsOff
        => new(LanguageNames.CSharp)
        {
             { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    private static OptionsCollection AllOptionsOn
        => new(LanguageNames.CSharp)
        {
             { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
        };

    private static OptionsCollection AccessorOptionsOn
        => new(LanguageNames.CSharp)
        {
             { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
             { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    internal static Task TestWithAllCodeStyleOptionsOffAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup,
        int? index = null)
        => new VerifyCS.Test
        {
            TestCode = initialMarkup,
            FixedCode = expectedMarkup,
            Options = { AllOptionsOff },
            CodeActionIndex = index,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    internal static Task TestWithAllCodeStyleOptionsOnAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup)
        => new VerifyCS.Test
        {
            TestCode = initialMarkup,
            FixedCode = expectedMarkup,
            Options = { AllOptionsOn },
        }.RunAsync();

    internal static Task TestWithAccessorCodeStyleOptionsOnAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup)
        => new VerifyCS.Test
        {
            TestCode = initialMarkup,
            FixedCode = expectedMarkup,
            Options = { AccessorOptionsOn },
        }.RunAsync();

    private static Task TestInRegularAndScriptAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup,
        int? index = null)
        => new VerifyCS.Test
        {
            TestCode = initialMarkup,
            FixedCode = expectedMarkup,
            CodeActionIndex = index,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestMethod()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface
            {
                void Method1();
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            """
            interface IInterface
            {
                void Method1();
            }

            class Class : IInterface
            {
                public void Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestMethodInRecord()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface IInterface
            {
                void Method1();
            }

            record Record : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                void Method1();
            }

            record Record : IInterface
            {
                public void Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42986")]
    public async Task TestMethodWithNativeIntegers()
    {
        var nativeIntegerAttributeDefinition = """
            namespace System.Runtime.CompilerServices
            {
                [System.AttributeUsage(AttributeTargets.All)]
                public sealed class NativeIntegerAttribute : System.Attribute
                {
                    public NativeIntegerAttribute()
                    {
                    }
                    public NativeIntegerAttribute(bool[] flags)
                    {
                    }
                }
            }
            """;

        // Note: we're putting the attribute by hand to simulate metadata
        await new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp9,
            TestCode = """
            interface IInterface
            {
                [return: {|CS8335:System.Runtime.CompilerServices.NativeInteger(new[] { true, true })|}]
                (nint, nuint) Method(nint x, nuint x2);
            }

            class Class : {|CS0535:IInterface|}
            {
            }

            """ + nativeIntegerAttributeDefinition,
            FixedCode = """
            interface IInterface
            {
                [return: {|CS8335:System.Runtime.CompilerServices.NativeInteger(new[] { true, true })|}]
                (nint, nuint) Method(nint x, nuint x2);
            }

            class Class : IInterface
            {
                public (nint, nuint) Method(nint x, nuint x2)
                {
                    throw new System.NotImplementedException();
                }
            }

            """ + nativeIntegerAttributeDefinition,
            Options = { AllOptionsOff },
        }.RunAsync();
    }

    [Fact]
    public Task TestMethodWithTuple()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface
            {
                (int, int) Method((string, string) x);
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            """
            interface IInterface
            {
                (int, int) Method((string, string) x);
            }

            class Class : IInterface
            {
                public (int, int) Method((string, string) x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16793")]
    public Task TestMethodWithValueTupleArity1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            interface I
            {
                ValueTuple<object> F();
            }
            class C : {|CS0535:I|}
            {
            }
            """,
            """
            using System;
            interface I
            {
                ValueTuple<object> F();
            }
            class C : I
            {
                public ValueTuple<object> F()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestExpressionBodiedMethod1()
        => TestWithAllCodeStyleOptionsOnAsync(
            """
            interface IInterface
            {
                void Method1();
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            """
            interface IInterface
            {
                void Method1();
            }

            class Class : IInterface
            {
                public void Method1() => throw new System.NotImplementedException();
            }
            """);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    public Task TupleWithNamesInMethod()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface
            {
                [return: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { "a", "b" })|}]
                (int a, int b)[] Method1((int c, string) x);
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            """
            interface IInterface
            {
                [return: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { "a", "b" })|}]
                (int a, int b)[] Method1((int c, string) x);
            }

            class Class : IInterface
            {
                public (int a, int b)[] Method1((int c, string) x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    public Task TupleWithNamesInMethod_Explicitly()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface
            {
                [return: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { "a", "b" })|}]
                (int a, int b)[] Method1((int c, string) x);
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            """
            interface IInterface
            {
                [return: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { "a", "b" })|}]
                (int a, int b)[] Method1((int c, string) x);
            }

            class Class : IInterface
            {
                (int a, int b)[] IInterface.Method1((int c, string) x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            index: 1);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    public Task TupleWithNamesInProperty()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface
            {
                [{|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { "a", "b" })|}]
                (int a, int b)[] Property1 { [return: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { "a", "b" })|}] get; [param: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { "a", "b" })|}] set; }
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            """
            interface IInterface
            {
                [{|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { "a", "b" })|}]
                (int a, int b)[] Property1 { [return: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { "a", "b" })|}] get; [param: {|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { "a", "b" })|}] set; }
            }

            class Class : IInterface
            {
                public (int a, int b)[] Property1
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }

                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    public Task TupleWithNamesInEvent()
        => new VerifyCS.Test
        {
            TestCode = """
            interface IInterface
            {
                [{|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { "a", "b" })|}]
                event System.Func<(int a, int b)> Event1;
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            using System;

            interface IInterface
            {
                [{|CS8138:System.Runtime.CompilerServices.TupleElementNames(new[] { "a", "b" })|}]
                event System.Func<(int a, int b)> Event1;
            }

            class Class : IInterface
            {
                public event Func<(int a, int b)> Event1;
            }
            """,
            Options = { AllOptionsOff },
        }.RunAsync();

    [Fact]
    public Task NoDynamicAttributeInMethod()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface
            {
                [return: {|CS1970:System.Runtime.CompilerServices.DynamicAttribute()|}]
                object Method1();
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            """
            interface IInterface
            {
                [return: {|CS1970:System.Runtime.CompilerServices.DynamicAttribute()|}]
                object Method1();
            }

            class Class : IInterface
            {
                public object Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public async Task NoNullableAttributesInMethodFromMetadata()
    {
        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    #nullable enable

                    using System;

                    class C : {|CS0535:{|CS0535:IInterface|}|}
                    {
                    }
                    """,
                },
                AdditionalProjects =
                {
                    ["Assembly1"] =
                    {
                        Sources =
                        {
                            """
                            #nullable enable

                            public interface IInterface
                            {
                                void M(string? s1, string s2);
                                string this[string? s1, string s2] { get; set; }
                            }
                            """
                        },
                    },
                },
                AdditionalProjectReferences =
                {
                    "Assembly1",
                },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    #nullable enable

                    using System;

                    class C : IInterface
                    {
                        public string this[string? s1, string s2]
                        {
                            get
                            {
                                throw new NotImplementedException();
                            }

                            set
                            {
                                throw new NotImplementedException();
                            }
                        }

                        public void M(string? s1, string s2)
                        {
                            throw new NotImplementedException();
                        }
                    }
                    """,
                },
            },
        };

        test.Options.AddRange(AllOptionsOff);
        await test.RunAsync();
    }

    [Fact]
    public Task TestMethodWhenClassBracesAreMissing()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface
            {
                void Method1();
            }

            class Class : {|CS0535:IInterface|}{|CS1513:|}{|CS1514:|}
            """,
            """
            interface IInterface
            {
                void Method1();
            }

            class Class : IInterface
            {
                public void Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/26323")]
    public Task TestMethodWhenClassBracesAreMissing2(
        [CombinatorialValues(0, 1)] int behavior)
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
            
                namespace WPFConsoleApplication1
                {
                    class Program
                    {
                        private class Test : {|CS0535:ICloneable|}{|CS1513:|}{|CS1514:|}
            
                        static void Main(string[] args) { }
                    }
                }
                """,
            FixedCode = """
                using System;
            
                namespace WPFConsoleApplication1
                {
                    class Program
                    {
                        private class Test : ICloneable
                        {
                            public object Clone()
                            {
                                throw new NotImplementedException();
                            }
                        }
            
                        static void Main(string[] args) { }
                    }
                }
                """,
            Options =
            {
                new OptionsCollection(LanguageNames.CSharp)
                {
                    { ImplementTypeOptionsStorage.InsertionBehavior, (ImplementTypeInsertionBehavior)behavior }
                }
            }
        }.RunAsync();

    [Fact]
    public Task TestInheritance1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface1
            {
                void Method1();
            }

            interface IInterface2 : IInterface1
            {
            }

            class Class : {|CS0535:IInterface2|}
            {
            }
            """,
            """
            interface IInterface1
            {
                void Method1();
            }

            interface IInterface2 : IInterface1
            {
            }

            class Class : IInterface2
            {
                public void Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestInheritance2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface1
            {
            }

            interface IInterface2 : IInterface1
            {
                void Method1();
            }

            class Class : {|CS0535:IInterface2|}
            {
            }
            """,
            """
            interface IInterface1
            {
            }

            interface IInterface2 : IInterface1
            {
                void Method1();
            }

            class Class : IInterface2
            {
                public void Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestInheritance3()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface1
            {
                void Method1();
            }

            interface IInterface2 : IInterface1
            {
                void Method2();
            }

            class Class : {|CS0535:{|CS0535:IInterface2|}|}
            {
            }
            """,
            """
            interface IInterface1
            {
                void Method1();
            }

            interface IInterface2 : IInterface1
            {
                void Method2();
            }

            class Class : IInterface2
            {
                public void Method1()
                {
                    throw new System.NotImplementedException();
                }

                public void Method2()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestInheritanceMatchingMethod()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface1
            {
                void Method1();
            }

            interface IInterface2 : IInterface1
            {
                void Method1();
            }

            class Class : {|CS0535:{|CS0535:IInterface2|}|}
            {
            }
            """,
            """
            interface IInterface1
            {
                void Method1();
            }

            interface IInterface2 : IInterface1
            {
                void Method1();
            }

            class Class : IInterface2
            {
                public void Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestExistingConflictingMethodReturnType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface1
            {
                void Method1();
            }

            class Class : {|CS0738:IInterface1|}
            {
                public int Method1()
                {
                    return 0;
                }
            }
            """,
            """
            interface IInterface1
            {
                void Method1();
            }

            class Class : IInterface1
            {
                public int Method1()
                {
                    return 0;
                }

                void IInterface1.Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestExistingConflictingMethodParameters()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface1
            {
                void Method1(int i);
            }

            class Class : {|CS0535:IInterface1|}
            {
                public void Method1(string i)
                {
                }
            }
            """,
            """
            interface IInterface1
            {
                void Method1(int i);
            }

            class Class : IInterface1
            {
                public void Method1(string i)
                {
                }

                public void Method1(int i)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestImplementGenericType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface1<T>
            {
                void Method1(T t);
            }

            class Class : {|CS0535:IInterface1<int>|}
            {
            }
            """,
            """
            interface IInterface1<T>
            {
                void Method1(T t);
            }

            class Class : IInterface1<int>
            {
                public void Method1(int t)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestImplementGenericTypeWithGenericMethod()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface1<T>
            {
                void Method1<U>(T t, U u);
            }

            class Class : {|CS0535:IInterface1<int>|}
            {
            }
            """,
            """
            interface IInterface1<T>
            {
                void Method1<U>(T t, U u);
            }

            class Class : IInterface1<int>
            {
                public void Method1<U>(int t, U u)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestImplementGenericTypeWithGenericMethodWithNaturalConstraint()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Collections.Generic;
            interface IInterface1<T>
            {
                void Method1<U>(T t, U u) where U : IList<T>;
            }

            class Class : {|CS0535:IInterface1<int>|}
            {
            }
            """,
            """
            using System.Collections.Generic;
            interface IInterface1<T>
            {
                void Method1<U>(T t, U u) where U : IList<T>;
            }

            class Class : IInterface1<int>
            {
                public void Method1<U>(int t, U u) where U : IList<int>
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestImplementGenericTypeWithGenericMethodWithUnexpressibleConstraint()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface1<T>
            {
                void Method1<U>(T t, U u) where U : T;
            }

            class Class : {|CS0535:IInterface1<int>|}
            {
            }
            """,
            """
            interface IInterface1<T>
            {
                void Method1<U>(T t, U u) where U : T;
            }

            class Class : IInterface1<int>
            {
                void IInterface1<int>.Method1<U>(int t, U u)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestArrayType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                string[] M();
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            interface I
            {
                string[] M();
            }

            class C : I
            {
                public string[] M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestImplementThroughFieldMember()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Method1();
            }

            class C : {|CS0535:I|}
            {
                I i;
            }
            """,
            """
            interface I
            {
                void Method1();
            }

            class C : I
            {
                I i;

                public void Method1()
                {
                    i.Method1();
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69177")]
    public Task TestImplementThroughPrimaryConstructorParameter1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Method1();
            }

            class C(I i) : {|CS0535:I|}
            {
            }
            """,
            """
            interface I
            {
                void Method1();
            }

            class C(I i) : I
            {
                public void Method1()
                {
                    i.Method1();
                }
            }
            """,
            index: 1);

    [Fact]
    public Task TestImplementThroughFieldMember_FixAll_SameMemberInDifferentType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Method1();
            }

            class C : {|CS0535:I|}
            {
                I i;
            }

            class D : {|CS0535:I|}
            {
                I i;
            }
            """,
            """
            interface I
            {
                void Method1();
            }

            class C : I
            {
                I i;

                public void Method1()
                {
                    i.Method1();
                }
            }

            class D : I
            {
                I i;

                public void Method1()
                {
                    i.Method1();
                }
            }
            """,
            index: 1);

    [Fact]
    public Task TestImplementThroughFieldMember_FixAll_FieldInOnePropInAnother()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Method1();
            }

            class C : {|CS0535:I|}
            {
                I i;
            }

            class D : {|CS0535:I|}
            {
                I i { get; }
            }
            """,
            """
            interface I
            {
                void Method1();
            }

            class C : I
            {
                I i;

                public void Method1()
                {
                    i.Method1();
                }
            }

            class D : I
            {
                I i { get; }

                public void Method1()
                {
                    i.Method1();
                }
            }
            """,
            index: 1);

    [Fact]
    public async Task TestImplementThroughFieldMember_FixAll_FieldInOneNonViableInAnother()
    {
        var test = new VerifyCS.Test
        {
            TestCode = """
            interface I
            {
                void Method1();
            }

            class C : {|CS0535:I|}
            {
                I i;
            }

            class D : {|CS0535:I|}
            {
                int i;
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    interface I
                    {
                        void Method1();
                    }

                    class C : I
                    {
                        I i;

                        public void Method1()
                        {
                            i.Method1();
                        }
                    }

                    class D : {|CS0535:I|}
                    {
                        int i;
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            CodeActionIndex = 1,
        };

        test.Options.AddRange(AllOptionsOff);
        await test.RunAsync();
    }

    [Fact]
    public Task TestImplementThroughFieldMemberInterfaceWithIndexer()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IGoo
            {
                int this[int x] { get; set; }
            }

            class Goo : {|CS0535:IGoo|}
            {
                IGoo f;
            }
            """,
            """
            interface IGoo
            {
                int this[int x] { get; set; }
            }

            class Goo : IGoo
            {
                IGoo f;

                public int this[int x]
                {
                    get
                    {
                        return f[x];
                    }

                    set
                    {
                        f[x] = value;
                    }
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/472")]
    public Task TestImplementThroughFieldMemberRemoveUnnecessaryCast()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Collections;

            sealed class X : {|CS0535:IComparer|}
            {
                X x;
            }
            """,
            """
            using System.Collections;

            sealed class X : IComparer
            {
                X x;

                public int Compare(object x, object y)
                {
                    return this.x.Compare(x, y);
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/472")]
    public Task TestImplementThroughFieldMemberRemoveUnnecessaryCastAndThis()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Collections;

            sealed class X : {|CS0535:IComparer|}
            {
                X a;
            }
            """,
            """
            using System.Collections;

            sealed class X : IComparer
            {
                X a;

                public int Compare(object x, object y)
                {
                    return a.Compare(x, y);
                }
            }
            """,
            index: 1);

    [Fact]
    public Task TestImplementAbstract()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Method1();
            }

            abstract class C : {|CS0535:I|}
            {
            }
            """,
            """
            interface I
            {
                void Method1();
            }

            abstract class C : I
            {
                public abstract void Method1();
            }
            """,
            index: 1);

    [Fact]
    public Task TestImplementInterfaceWithRefOutParameters()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            class C : {|CS0535:{|CS0535:I|}|}
            {
                I goo;
            }

            interface I
            {
                void Method1(ref int x, out int y, ref readonly int w, int z);
                int Method2();
            }
            """,
            """
            class C : I
            {
                I goo;

                public void Method1(ref int x, out int y, ref readonly int w, int z)
                {
                    goo.Method1(ref x, out y, in w, z);
                }

                public int Method2()
                {
                    return goo.Method2();
                }
            }

            interface I
            {
                void Method1(ref int x, out int y, ref readonly int w, int z);
                int Method2();
            }
            """,
            index: 1);

    [Fact]
    public Task TestConflictingMethods1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            class B
            {
                public int Method1()
                {
                    return 0;
                }
            }

            class C : B, {|CS0738:I|}
            {
            }

            interface I
            {
                void Method1();
            }
            """,
            """
            class B
            {
                public int Method1()
                {
                    return 0;
                }
            }

            class C : B, I
            {
                void I.Method1()
                {
                    throw new System.NotImplementedException();
                }
            }

            interface I
            {
                void Method1();
            }
            """);

    [Fact]
    public Task TestConflictingProperties()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            class Test : {|CS0737:I1|}
            {
                int Prop { get; set; }
            }

            interface I1
            {
                int Prop { get; set; }
            }
            """,
            """
            class Test : I1
            {
                int Prop { get; set; }

                int I1.Prop
                {
                    get
                    {
                        return Prop;
                    }

                    set
                    {
                        Prop = value;
                    }
                }
            }

            interface I1
            {
                int Prop { get; set; }
            }
            """);

    [Fact]
    public Task TestConflictingProperties2()
        => TestWithAllCodeStyleOptionsOnAsync(
            """
            class Test : {|CS0737:I1|}
            {
                int Prop { get; set; }
            }

            interface I1
            {
                int Prop { get; set; }
            }
            """,
            """
            class Test : I1
            {
                int Prop { get; set; }
                int I1.Prop { get => Prop; set => Prop = value; }
            }

            interface I1
            {
                int Prop { get; set; }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539043")]
    public async Task TestExplicitProperties()
    {
        var code =
            """
            interface I2
            {
                decimal Calc { get; }
            }

            class C : I2
            {
                protected decimal pay;

                decimal I2.Calc
                {
                    get
                    {
                        return pay;
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
    public Task TestEscapedMethodName()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface
            {
                void @M();
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            """
            interface IInterface
            {
                void @M();
            }

            class Class : IInterface
            {
                public void M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
    public Task TestEscapedMethodKeyword()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface
            {
                void @int();
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            """
            interface IInterface
            {
                void @int();
            }

            class Class : IInterface
            {
                public void @int()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
    public Task TestEscapedInterfaceName1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface @IInterface
            {
                void M();
            }

            class Class : {|CS0737:@IInterface|}
            {
                string M() => "";
            }
            """,
            """
            interface @IInterface
            {
                void M();
            }

            class Class : @IInterface
            {
                string M() => "";

                void IInterface.M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
    public Task TestEscapedInterfaceName2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface @IInterface
            {
                void @M();
            }

            class Class : {|CS0737:@IInterface|}
            {
                string M() => "";
            }
            """,
            """
            interface @IInterface
            {
                void @M();
            }

            class Class : @IInterface
            {
                string M() => "";

                void IInterface.M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
    public Task TestEscapedInterfaceKeyword1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface @int
            {
                void M();
            }

            class Class : {|CS0737:@int|}
            {
                string M() => "";
            }
            """,
            """
            interface @int
            {
                void M();
            }

            class Class : @int
            {
                string M() => "";

                void @int.M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
    public Task TestEscapedInterfaceKeyword2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface @int
            {
                void @bool();
            }

            class Class : {|CS0737:@int|}
            {
                string @bool() => "";
            }
            """,
            """
            interface @int
            {
                void @bool();
            }

            class Class : @int
            {
                string @bool() => "";

                void @int.@bool()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539522")]
    public Task TestPropertyFormatting()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            public interface DD
            {
                int Prop { get; set; }
            }
            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int Prop { get; set; }
            }
            public class A : DD
            {
                public int Prop
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }

                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact]
    public Task TestProperty_PropertyCodeStyleOn1()
        => TestWithAllCodeStyleOptionsOnAsync(
            """
            public interface DD
            {
                int Prop { get; }
            }

            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int Prop { get; }
            }

            public class A : DD
            {
                public int Prop => throw new System.NotImplementedException();
            }
            """);

    [Fact]
    public Task TestProperty_AccessorCodeStyleOn1()
        => TestWithAccessorCodeStyleOptionsOnAsync(
            """
            public interface DD
            {
                int Prop { get; }
            }

            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int Prop { get; }
            }

            public class A : DD
            {
                public int Prop { get => throw new System.NotImplementedException(); }
            }
            """);

    [Fact]
    public Task TestIndexer_IndexerCodeStyleOn1()
        => TestWithAllCodeStyleOptionsOnAsync(
            """
            public interface DD
            {
                int this[int i] { get; }
            }

            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int this[int i] { get; }
            }

            public class A : DD
            {
                public int this[int i] => throw new System.NotImplementedException();
            }
            """);

    [Fact]
    public Task TestIndexer_AccessorCodeStyleOn1()
        => TestWithAccessorCodeStyleOptionsOnAsync(
            """
            public interface DD
            {
                int this[int i] { get; }
            }

            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int this[int i] { get; }
            }

            public class A : DD
            {
                public int this[int i] { get => throw new System.NotImplementedException(); }
            }
            """);

    [Fact]
    public Task TestMethod_AllCodeStyleOn1()
        => TestWithAllCodeStyleOptionsOnAsync(
            """
            public interface DD
            {
                int M();
            }

            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int M();
            }

            public class A : DD
            {
                public int M() => throw new System.NotImplementedException();
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539522")]
    public Task TestReadonlyPropertyExpressionBodyYes1()
        => TestWithAllCodeStyleOptionsOnAsync(
            """
            public interface DD
            {
                int Prop { get; }
            }
            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int Prop { get; }
            }
            public class A : DD
            {
                public int Prop => throw new System.NotImplementedException();
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539522")]
    public Task TestReadonlyPropertyAccessorBodyYes1()
        => TestWithAccessorCodeStyleOptionsOnAsync(
            """
            public interface DD
            {
                int Prop { get; }
            }

            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int Prop { get; }
            }

            public class A : DD
            {
                public int Prop { get => throw new System.NotImplementedException(); }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539522")]
    public Task TestReadonlyPropertyAccessorBodyYes2()
        => TestWithAccessorCodeStyleOptionsOnAsync(
            """
            public interface DD
            {
                int Prop { get; set; }
            }

            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int Prop { get; set; }
            }

            public class A : DD
            {
                public int Prop { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539522")]
    public Task TestReadonlyPropertyExpressionBodyNo1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            public interface DD
            {
                int Prop { get; }
            }

            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int Prop { get; }
            }

            public class A : DD
            {
                public int Prop
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact]
    public Task TestIndexerExpressionBodyYes1()
        => TestWithAllCodeStyleOptionsOnAsync(
            """
            public interface DD
            {
                int this[int i] { get; }
            }

            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int this[int i] { get; }
            }

            public class A : DD
            {
                public int this[int i] => throw new System.NotImplementedException();
            }
            """);

    [Fact]
    public Task TestIndexerExpressionBodyNo1()
        => TestWithAllCodeStyleOptionsOnAsync(
            """
            public interface DD
            {
                int this[int i] { get; set; }
            }

            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int this[int i] { get; set; }
            }

            public class A : DD
            {
                public int this[int i] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
            }
            """);

    [Fact]
    public Task TestIndexerAccessorExpressionBodyYes1()
        => TestWithAccessorCodeStyleOptionsOnAsync(
            """
            public interface DD
            {
                int this[int i] { get; }
            }

            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int this[int i] { get; }
            }

            public class A : DD
            {
                public int this[int i] { get => throw new System.NotImplementedException(); }
            }
            """);

    [Fact]
    public Task TestIndexerAccessorExpressionBodyYes2()
        => TestWithAllCodeStyleOptionsOnAsync(
            """
            public interface DD
            {
                int this[int i] { get; set; }
            }

            public class A : {|CS0535:DD|}
            {
            }
            """,
            """
            public interface DD
            {
                int this[int i] { get; set; }
            }

            public class A : DD
            {
                public int this[int i] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
            }
            """);

    [Fact]
    public Task TestCommentPlacement()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            public interface DD
            {
                void Goo();
            }
            public class A : {|CS0535:DD|}
            {
                //comments
            }
            """,
            """
            public interface DD
            {
                void Goo();
            }
            public class A : DD
            {
                //comments
                public void Goo()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539991")]
    public Task TestBracePlacement()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            class C : {|CS0535:IServiceProvider|}{|CS1513:|}{|CS1514:|}
            """,
            """
            using System;
            class C : IServiceProvider
            {
                public object GetService(Type serviceType)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540318")]
    public async Task TestMissingWithIncompleteMember()
    {
        var code =
            """
            interface ITest
            {
                void Method();
            }

            class Test : ITest
            {
                p {|CS1585:public|} void Method()
                {
                    throw new System.NotImplementedException();
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541380")]
    public Task TestExplicitProperty()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface i1
            {
                int p { get; set; }
            }

            class c1 : {|CS0535:i1|}
            {
            }
            """,
            """
            interface i1
            {
                int p { get; set; }
            }

            class c1 : i1
            {
                int i1.p
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }

                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541981")]
    public async Task TestNoDelegateThroughField1()
    {
        var code =
            """
            interface I
            {
                void Method1();
            }

            class C : {|CS0535:I|}
            {
                I i { get; set; }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = """
            interface I
            {
                void Method1();
            }

            class C : I
            {
                I i { get; set; }

                public void Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            Options = { AllOptionsOff },
            CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
        }.RunAsync();

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = """
            interface I
            {
                void Method1();
            }

            class C : I
            {
                I i { get; set; }

                public void Method1()
                {
                    i.Method1();
                }
            }
            """,
            Options = { AllOptionsOff },
            CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
            CodeActionIndex = 1,
        }.RunAsync();

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = """
            interface I
            {
                void Method1();
            }

            class C : I
            {
                I i { get; set; }

                void I.Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            Options = { AllOptionsOff },
            CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
            CodeActionIndex = 2,
        }.RunAsync();
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
    public Task TestImplementIReadOnlyListThroughField()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Collections.Generic;

            class A : {|CS0535:{|CS0535:{|CS0535:{|CS0535:IReadOnlyList<int>|}|}|}|}
            {
                int[] field;
            }
            """,
            """
            using System.Collections;
            using System.Collections.Generic;

            class A : IReadOnlyList<int>
            {
                int[] field;

                public int this[int index]
                {
                    get
                    {
                        return ((IReadOnlyList<int>)field)[index];
                    }
                }

                public int Count
                {
                    get
                    {
                        return ((IReadOnlyCollection<int>)field).Count;
                    }
                }

                public IEnumerator<int> GetEnumerator()
                {
                    return ((IEnumerable<int>)field).GetEnumerator();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return field.GetEnumerator();
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
    public Task TestImplementIReadOnlyListThroughProperty()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Collections.Generic;

            class A : {|CS0535:{|CS0535:{|CS0535:{|CS0535:IReadOnlyList<int>|}|}|}|}
            {
                int[] field { get; set; }
            }
            """,
            """
            using System.Collections;
            using System.Collections.Generic;

            class A : IReadOnlyList<int>
            {
                public int this[int index]
                {
                    get
                    {
                        return ((IReadOnlyList<int>)field)[index];
                    }
                }

                public int Count
                {
                    get
                    {
                        return ((IReadOnlyCollection<int>)field).Count;
                    }
                }

                int[] field { get; set; }

                public IEnumerator<int> GetEnumerator()
                {
                    return ((IEnumerable<int>)field).GetEnumerator();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return field.GetEnumerator();
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
    public Task TestImplementInterfaceThroughField()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                int M();
            }

            class A : I
            {
                int I.M()
                {
                    return 0;
                }
            }

            class B : {|CS0535:I|}
            {
                A a;
            }
            """,
            """
            interface I
            {
                int M();
            }

            class A : I
            {
                int I.M()
                {
                    return 0;
                }
            }

            class B : I
            {
                A a;

                public int M()
                {
                    return ((I)a).M();
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
    public async Task TestImplementInterfaceThroughField_FieldImplementsMultipleInterfaces()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            interface I
            {
                int M();
            }

            interface I2
            {
                int M2();
            }

            class A : I, I2
            {
                int I.M()
                {
                    return 0;
                }

                int I2.M2()
                {
                    return 0;
                }
            }

            class B : {|CS0535:I|}, {|CS0535:I2|}
            {
                A a;
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    interface I
                    {
                        int M();
                    }

                    interface I2
                    {
                        int M2();
                    }

                    class A : I, I2
                    {
                        int I.M()
                        {
                            return 0;
                        }

                        int I2.M2()
                        {
                            return 0;
                        }
                    }

                    class B : I, {|CS0535:I2|}
                    {
                        A a;

                        public int M()
                        {
                            return ((I)a).M();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
            DiagnosticSelector = diagnostics => diagnostics[0],
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            CodeActionIndex = 1,
        }.RunAsync();

        await new VerifyCS.Test
        {
            TestCode = """
            interface I
            {
                int M();
            }

            interface I2
            {
                int M2();
            }

            class A : I, I2
            {
                int I.M()
                {
                    return 0;
                }

                int I2.M2()
                {
                    return 0;
                }
            }

            class B : {|CS0535:I|}, {|CS0535:I2|}
            {
                A a;
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    interface I
                    {
                        int M();
                    }

                    interface I2
                    {
                        int M2();
                    }

                    class A : I, I2
                    {
                        int I.M()
                        {
                            return 0;
                        }

                        int I2.M2()
                        {
                            return 0;
                        }
                    }

                    class B : {|CS0535:I|}, I2
                    {
                        A a;

                        public int M2()
                        {
                            return ((I2)a).M2();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
            DiagnosticSelector = diagnostics => diagnostics[1],
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            CodeActionIndex = 1,
        }.RunAsync();
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
    public async Task TestImplementInterfaceThroughField_MultipleFieldsCanImplementInterface()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            interface I
            {
                int M();
            }

            class A : I
            {
                int I.M()
                {
                    return 0;
                }
            }

            class B : {|CS0535:I|}
            {
                A a;
                A aa;
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    interface I
                    {
                        int M();
                    }

                    class A : I
                    {
                        int I.M()
                        {
                            return 0;
                        }
                    }

                    class B : I
                    {
                        A a;
                        A aa;

                        public int M()
                        {
                            return ((I)a).M();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeActionsVerifier = codeActions => Assert.Equal(4, codeActions.Length),
            CodeActionIndex = 1,
        }.RunAsync();

        await new VerifyCS.Test
        {
            TestCode = """
            interface I
            {
                int M();
            }

            class A : I
            {
                int I.M()
                {
                    return 0;
                }
            }

            class B : {|CS0535:I|}
            {
                A a;
                A aa;
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    interface I
                    {
                        int M();
                    }

                    class A : I
                    {
                        int I.M()
                        {
                            return 0;
                        }
                    }

                    class B : I
                    {
                        A a;
                        A aa;

                        public int M()
                        {
                            return ((I)aa).M();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeActionsVerifier = codeActions => Assert.Equal(4, codeActions.Length),
            CodeActionIndex = 2,
        }.RunAsync();
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
    public async Task TestImplementInterfaceThroughField_MultipleFieldsForMultipleInterfaces()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            interface I
            {
                int M();
            }

            interface I2
            {
                int M2();
            }

            class A : I
            {
                int I.M()
                {
                    return 0;
                }
            }

            class B : I2
            {
                int I2.M2()
                {
                    return 0;
                }
            }

            class C : {|CS0535:I|}, {|CS0535:I2|}
            {
                A a;
                B b;
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    interface I
                    {
                        int M();
                    }

                    interface I2
                    {
                        int M2();
                    }

                    class A : I
                    {
                        int I.M()
                        {
                            return 0;
                        }
                    }

                    class B : I2
                    {
                        int I2.M2()
                        {
                            return 0;
                        }
                    }

                    class C : I, {|CS0535:I2|}
                    {
                        A a;
                        B b;

                        public int M()
                        {
                            return ((I)a).M();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
            DiagnosticSelector = diagnostics => diagnostics[0],
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            CodeActionIndex = 1,
        }.RunAsync();

        await new VerifyCS.Test
        {
            TestCode = """
            interface I
            {
                int M();
            }

            interface I2
            {
                int M2();
            }

            class A : I
            {
                int I.M()
                {
                    return 0;
                }
            }

            class B : I2
            {
                int I2.M2()
                {
                    return 0;
                }
            }

            class C : {|CS0535:I|}, {|CS0535:I2|}
            {
                A a;
                B b;
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    interface I
                    {
                        int M();
                    }

                    interface I2
                    {
                        int M2();
                    }

                    class A : I
                    {
                        int I.M()
                        {
                            return 0;
                        }
                    }

                    class B : I2
                    {
                        int I2.M2()
                        {
                            return 0;
                        }
                    }

                    class C : {|CS0535:I|}, I2
                    {
                        A a;
                        B b;

                        public int M2()
                        {
                            return ((I2)b).M2();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
            DiagnosticSelector = diagnostics => diagnostics[1],
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            CodeActionIndex = 1,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18556")]
    public Task TestImplementInterfaceThroughExplicitProperty()
        => new VerifyCS.Test
        {
            TestCode = """
            interface IA
            {
                IB B { get; }
            }
            interface IB
            {
                int M();
            }
            class AB : IA, {|CS0535:IB|}
            {
                IB IA.B => null;
            }
            """,
            FixedCode = """
            interface IA
            {
                IB B { get; }
            }
            interface IB
            {
                int M();
            }
            class AB : IA, IB
            {
                IB IA.B => null;

                public int M()
                {
                    return ((IA)this).B.M();
                }
            }
            """,
            Options = { AllOptionsOff },
            CodeActionsVerifier = codeActions => Assert.Equal(3, codeActions.Length),
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
    public Task TestNoImplementThroughIndexer()
        => new VerifyCS.Test
        {
            TestCode = """
            interface I
            {
                int M();
            }

            class A : I
            {
                int I.M()
                {
                    return 0;
                }
            }

            class B : {|CS0535:I|}
            {
                A this[int index]
                {
                    get
                    {
                        return null;
                    }
                }
            }
            """,
            FixedCode = """
            interface I
            {
                int M();
            }

            class A : I
            {
                int I.M()
                {
                    return 0;
                }
            }

            class B : I
            {
                A this[int index]
                {
                    get
                    {
                        return null;
                    }
                }

                public int M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionsVerifier = codeActions => Assert.Equal(2, codeActions.Length),
        }.RunAsync();

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768799")]
    public Task TestNoImplementThroughWriteOnlyProperty()
        => new VerifyCS.Test
        {
            TestCode = """
            interface I
            {
                int M();
            }

            class A : I
            {
                int I.M()
                {
                    return 0;
                }
            }

            class B : {|CS0535:I|}
            {
                A a
                {
                    set
                    {
                    }
                }
            }
            """,
            FixedCode = """
            interface I
            {
                int M();
            }

            class A : I
            {
                int I.M()
                {
                    return 0;
                }
            }

            class B : {|CS0535:I|}
            {
                A a
                {
                    set
                    {
                    }
                }

                public int M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionsVerifier = codeActions => Assert.Equal(2, codeActions.Length),
        }.RunAsync();

    [Fact]
    public Task TestImplementEventThroughMember()
        => TestInRegularAndScriptAsync("""
            interface IGoo
            {
                event System.EventHandler E;
            }

            class CanGoo : IGoo
            {
                public event System.EventHandler E;
            }

            class HasCanGoo : {|CS0535:IGoo|}
            {
                CanGoo canGoo;
            }
            """,
            """
            using System;

            interface IGoo
            {
                event System.EventHandler E;
            }

            class CanGoo : IGoo
            {
                public event System.EventHandler E;
            }

            class HasCanGoo : IGoo
            {
                CanGoo canGoo;

                public event EventHandler E
                {
                    add
                    {
                        ((IGoo)canGoo).E += value;
                    }

                    remove
                    {
                        ((IGoo)canGoo).E -= value;
                    }
                }
            }
            """, index: 1);

    [Fact]
    public Task TestImplementEventThroughExplicitMember()
        => TestInRegularAndScriptAsync(
@"interface IGoo { event System . EventHandler E ; } class CanGoo : IGoo { event System.EventHandler IGoo.E { add { } remove { } } } class HasCanGoo : {|CS0535:IGoo|} { CanGoo canGoo; }",
"""
using System;

interface IGoo { event System . EventHandler E ; } class CanGoo : IGoo { event System.EventHandler IGoo.E { add { } remove { } } } class HasCanGoo : IGoo { CanGoo canGoo;

    public event EventHandler E
    {
        add
        {
            ((IGoo)canGoo).E += value;
        }

        remove
        {
            ((IGoo)canGoo).E -= value;
        }
    }
}
""",
index: 1);

    [Fact]
    public Task TestImplementEvent()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IGoo
            {
                event System.EventHandler E;
            }

            abstract class Goo : {|CS0535:IGoo|}
            {
            }
            """,
            """
            using System;

            interface IGoo
            {
                event System.EventHandler E;
            }

            abstract class Goo : IGoo
            {
                public event EventHandler E;
            }
            """);

    [Fact]
    public Task TestImplementEventAbstractly()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IGoo
            {
                event System.EventHandler E;
            }

            abstract class Goo : {|CS0535:IGoo|}
            {
            }
            """,
            """
            using System;

            interface IGoo
            {
                event System.EventHandler E;
            }

            abstract class Goo : IGoo
            {
                public abstract event EventHandler E;
            }
            """,
            index: 1);

    [Fact]
    public Task TestImplementEventExplicitly()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IGoo
            {
                event System.EventHandler E;
            }

            abstract class Goo : {|CS0535:IGoo|}
            {
            }
            """,
            """
            using System;

            interface IGoo
            {
                event System.EventHandler E;
            }

            abstract class Goo : IGoo
            {
                event EventHandler IGoo.E
                {
                    add
                    {
                        throw new NotImplementedException();
                    }

                    remove
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """,
            index: 2);

    [Fact]
    public Task TestFaultToleranceInStaticMembers_01()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IFoo
            {
                static string Name { set; get; }

                static int {|CS0501:Foo|}(string s);
            }

            class Program : IFoo
            {
            }
            """,
        }.RunAsync();

    [Fact]
    public async Task TestFaultToleranceInStaticMembers_02()
    {
        var test = new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IFoo
            {
                string Name { set; get; }

                static int {|CS0501:Foo|}(string s);
            }

            class Program : {|CS0535:IFoo|}
            {
            }
            """,
            FixedCode = """
            interface IFoo
            {
                string Name { set; get; }

                static int {|CS0501:Foo|}(string s);
            }

            class Program : IFoo
            {
                public string Name
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }

                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """,
        };

        test.Options.AddRange(AllOptionsOff);
        await test.RunAsync();
    }

    [Fact]
    public async Task TestFaultToleranceInStaticMembers_03()
    {
        var test = new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IGoo
            {
                static string Name { set; get; }

                int Goo(string s);
            }

            class Program : {|CS0535:IGoo|}
            {
            }
            """,
            FixedCode = """
            interface IGoo
            {
                static string Name { set; get; }

                int Goo(string s);
            }

            class Program : IGoo
            {
                public int Goo(string s)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
        };

        test.Options.AddRange(AllOptionsOff);
        await test.RunAsync();
    }

    [Fact]
    public Task TestIndexers()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            public interface ISomeInterface
            {
                int this[int index] { get; set; }
            }

            class IndexerClass : {|CS0535:ISomeInterface|}
            {
            }
            """,
            """
            public interface ISomeInterface
            {
                int this[int index] { get; set; }
            }

            class IndexerClass : ISomeInterface
            {
                public int this[int index]
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }

                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact]
    public Task TestIndexersExplicit()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            public interface ISomeInterface
            {
                int this[int index] { get; set; }
            }

            class IndexerClass : {|CS0535:ISomeInterface|}
            {
            }
            """,
            """
            public interface ISomeInterface
            {
                int this[int index] { get; set; }
            }

            class IndexerClass : ISomeInterface
            {
                int ISomeInterface.this[int index]
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }

                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """,
            index: 1);

    [Fact]
    public Task TestIndexersWithASingleAccessor()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            public interface ISomeInterface
            {
                int this[int index] { get; }
            }

            class IndexerClass : {|CS0535:ISomeInterface|}
            {
            }
            """,
            """
            public interface ISomeInterface
            {
                int this[int index] { get; }
            }

            class IndexerClass : ISomeInterface
            {
                public int this[int index]
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542357")]
    public Task TestConstraints1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Goo<T>() where T : class;
            }

            class A : {|CS0535:I|}
            {
            }
            """,
            """
            interface I
            {
                void Goo<T>() where T : class;
            }

            class A : I
            {
                public void Goo<T>() where T : class
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542357")]
    public Task TestConstraintsExplicit()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Goo<T>() where T : class;
            }

            class A : {|CS0535:I|}
            {
            }
            """,
            """
            interface I
            {
                void Goo<T>() where T : class;
            }

            class A : I
            {
                void I.Goo<T>()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542357")]
    public Task TestUsingAddedForConstraint()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Goo<T>() where T : System.Attribute;
            }

            class A : {|CS0535:I|}
            {
            }
            """,
            """
            using System;

            interface I
            {
                void Goo<T>() where T : System.Attribute;
            }

            class A : I
            {
                public void Goo<T>() where T : Attribute
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542379")]
    public Task TestIndexer()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                int this[int x] { get; set; }
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            interface I
            {
                int this[int x] { get; set; }
            }

            class C : I
            {
                public int this[int x]
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }

                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542588")]
    public Task TestRecursiveConstraint1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I
            {
                void Goo<T>() where T : IComparable<T>;
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            using System;

            interface I
            {
                void Goo<T>() where T : IComparable<T>;
            }

            class C : I
            {
                public void Goo<T>() where T : IComparable<T>
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542588")]
    public Task TestRecursiveConstraint2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I
            {
                void Goo<T>() where T : IComparable<T>;
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            using System;

            interface I
            {
                void Goo<T>() where T : IComparable<T>;
            }

            class C : I
            {
                void I.Goo<T>()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
    public Task TestUnexpressibleConstraint1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : {|CS0535:I<string>|}
            {
            }
            """,
            """
            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : I<string>
            {
                void I<string>.Goo<T>()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
    public Task TestUnexpressibleConstraint2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : {|CS0535:I<object>|}
            {
            }
            """,
            """
            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : I<object>
            {
                public void Goo<T>() where T : class
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
    public Task TestUnexpressibleConstraint3()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : {|CS0535:I<object>|}
            {
            }
            """,
            """
            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : I<object>
            {
                void I<object>.Goo<T>()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
    public Task TestUnexpressibleConstraint4()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : {|CS0535:I<Delegate>|}
            {
            }
            """,
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : I<Delegate>
            {
                void I<Delegate>.Goo<T>()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
    public Task TestUnexpressibleConstraint5()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : {|CS0535:I<MulticastDelegate>|}
            {
            }
            """,
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : I<MulticastDelegate>
            {
                void I<MulticastDelegate>.Goo<T>()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
    public Task TestUnexpressibleConstraint6()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            delegate void Bar();

            class A : {|CS0535:I<Bar>|}
            {
            }
            """,
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            delegate void Bar();

            class A : I<Bar>
            {
                void I<Bar>.Goo<T>()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
    public Task TestUnexpressibleConstraint7()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : {|CS0535:I<Enum>|}
            {
            }
            """,
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : I<Enum>
            {
                void I<Enum>.Goo<T>()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
    public Task TestUnexpressibleConstraint8()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : {|CS0535:I<int[]>|}
            {
            }
            """,
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class A : I<int[]>
            {
                void I<int[]>.Goo<T>()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542587")]
    public Task TestUnexpressibleConstraint9()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            enum E
            {
            }

            class A : {|CS0535:I<E>|}
            {
            }
            """,
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            enum E
            {
            }

            class A : I<E>
            {
                void I<E>.Goo<{|CS0455:T|}>()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542621")]
    public Task TestUnexpressibleConstraint10_CSharp72()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp7_2,
            TestCode =
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : S;
            }

            class A : {|CS0535:I<ValueType>|}
            {
            }
            """,
            FixedCode =
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : S;
            }

            class A : I<ValueType>
            {
                void I<ValueType>.Goo<T>()
                {
                    throw new NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542621")]
    public Task TestUnexpressibleConstraint10_CSharp8()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp8,
            TestCode =
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : S;
            }

            class A : {|CS0535:I<ValueType>|}
            {
            }
            """,
            FixedCode =
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : S;
            }

            class A : I<ValueType>
            {
                void I<ValueType>.Goo<T>()
                {
                    throw new NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542669")]
    public Task TestArrayConstraint()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : S;
            }

            class C : {|CS0535:I<Array>|}
            {
            }
            """,
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : S;
            }

            class C : I<Array>
            {
                void I<Array>.Goo<T>()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542743")]
    public Task TestMultipleClassConstraints()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : Exception, S;
            }

            class C : {|CS0535:I<Attribute>|}
            {
            }
            """,
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : Exception, S;
            }

            class C : I<Attribute>
            {
                void I<Attribute>.Goo<{|CS0455:T|}>()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542751")]
    public Task TestClassConstraintAndRefConstraint()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class C : {|CS0535:I<Exception>|}
            {
            }
            """,
            """
            using System;

            interface I<S>
            {
                void Goo<T>() where T : class, S;
            }

            class C : I<Exception>
            {
                void I<Exception>.Goo<T>()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542505")]
    public Task TestRenameConflictingTypeParameters1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            using System.Collections.Generic;

            interface I<T>
            {
                void Goo<S>(T x, IList<S> list) where S : T;
            }

            class A<S> : {|CS0535:I<S>|}
            {
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            interface I<T>
            {
                void Goo<S>(T x, IList<S> list) where S : T;
            }

            class A<S> : I<S>
            {
                public void Goo<S1>(S x, IList<S1> list) where S1 : S
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542505")]
    public Task TestRenameConflictingTypeParameters2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            using System.Collections.Generic;

            interface I<T>
            {
                void Goo<S>(T x, IList<S> list) where S : T;
            }

            class A<S> : {|CS0535:I<S>|}
            {
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            interface I<T>
            {
                void Goo<S>(T x, IList<S> list) where S : T;
            }

            class A<S> : I<S>
            {
                void I<S>.Goo<S1>(S x, IList<S1> list)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542505")]
    public Task TestRenameConflictingTypeParameters3()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            using System.Collections.Generic;

            interface I<X, Y>
            {
                void Goo<A, B>(X x, Y y, IList<A> list1, IList<B> list2)
                    where A : IList<B>
                    where B : IList<A>;
            }

            class C<A, B> : {|CS0535:I<A, B>|}
            {
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            interface I<X, Y>
            {
                void Goo<A, B>(X x, Y y, IList<A> list1, IList<B> list2)
                    where A : IList<B>
                    where B : IList<A>;
            }

            class C<A, B> : I<A, B>
            {
                public void Goo<A1, B1>(A x, B y, IList<A1> list1, IList<B1> list2)
                    where A1 : IList<B1>
                    where B1 : IList<A1>
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542505")]
    public Task TestRenameConflictingTypeParameters4()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            using System.Collections.Generic;

            interface I<X, Y>
            {
                void Goo<A, B>(X x, Y y, IList<A> list1, IList<B> list2)
                    where A : IList<B>
                    where B : IList<A>;
            }

            class C<A, B> : {|CS0535:I<A, B>|}
            {
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            interface I<X, Y>
            {
                void Goo<A, B>(X x, Y y, IList<A> list1, IList<B> list2)
                    where A : IList<B>
                    where B : IList<A>;
            }

            class C<A, B> : I<A, B>
            {
                void I<A, B>.Goo<A1, B1>(A x, B y, IList<A1> list1, IList<B1> list2)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542506")]
    public Task TestNameSimplification()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            class A<T>
            {
                class B
                {
                }

                interface I
                {
                    void Goo(B x);
                }

                class C<U> : {|CS0535:I|}
                {
                }
            }
            """,
            """
            using System;

            class A<T>
            {
                class B
                {
                }

                interface I
                {
                    void Goo(B x);
                }

                class C<U> : I
                {
                    public void Goo(B x)
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542506")]
    public Task TestNameSimplification2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            class A<T>
            {
                class B
                {
                }

                interface I
                {
                    void Goo(B[] x);
                }

                class C<U> : {|CS0535:I|}
                {
                }
            }
            """,
            """
            class A<T>
            {
                class B
                {
                }

                interface I
                {
                    void Goo(B[] x);
                }

                class C<U> : I
                {
                    public void Goo(B[] x)
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542506")]
    public Task TestNameSimplification3()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            class A<T>
            {
                class B
                {
                }

                interface I
                {
                    void Goo(B[][,][,,][,,,] x);
                }

                class C<U> : {|CS0535:I|}
                {
                }
            }
            """,
            """
            class A<T>
            {
                class B
                {
                }

                interface I
                {
                    void Goo(B[][,][,,][,,,] x);
                }

                class C<U> : I
                {
                    public void Goo(B[][,][,,][,,,] x)
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544166")]
    public Task TestImplementAbstractProperty()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IGoo
            {
                int Gibberish { get; set; }
            }

            abstract class Goo : {|CS0535:IGoo|}
            {
            }
            """,
            """
            interface IGoo
            {
                int Gibberish { get; set; }
            }

            abstract class Goo : IGoo
            {
                public abstract int Gibberish { get; set; }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544210")]
    public async Task TestMissingOnWrongArity()
    {
        var code =
            """
            interface I1<T>
            {
                int X { get; set; }
            }

            class C : {|CS0305:I1|}
            {
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544281")]
    public Task TestImplicitDefaultValue()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IOptional
            {
                int Goo(int g = 0);
            }

            class Opt : {|CS0535:IOptional|}
            {
            }
            """,
            """
            interface IOptional
            {
                int Goo(int g = 0);
            }

            class Opt : IOptional
            {
                public int Goo(int g = 0)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544281")]
    public Task TestExplicitDefaultValue()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IOptional
            {
                int Goo(int g = 0);
            }

            class Opt : {|CS0535:IOptional|}
            {
            }
            """,
            """
            interface IOptional
            {
                int Goo(int g = 0);
            }

            class Opt : IOptional
            {
                int IOptional.Goo(int g)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            index: 1);

    [Fact]
    public async Task TestMissingInHiddenType()
    {
        var code =
            """
            using System;

            class Program : {|CS0535:IComparable|}
            {
            #line hidden
            }
            #line default
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public Task TestGenerateIntoVisiblePart()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            #line default
            using System;

            partial class Program : {|CS0535:IComparable|}
            {
                void Goo()
                {
            #line hidden
                }
            }
            #line default
            """,
            """
            #line default
            using System;

            partial class Program : IComparable
            {
                public int CompareTo(object obj)
                {
                    throw new NotImplementedException();
                }

                void Goo()
                {
            #line hidden
                }
            }
            #line default
            """);

    [Fact]
    public Task TestGenerateIfAvailableRegionExists()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            partial class Program : {|CS0535:IComparable|}
            {
            #line hidden
            }
            #line default

            partial class Program
            {
            }
            """,
            """
            using System;

            partial class Program : IComparable
            {
            #line hidden
            }
            #line default

            partial class Program
            {
                public int CompareTo(object obj)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545334")]
    public async Task TestNoGenerateInVenusCase1()
    {
        var code =
            """
            using System;
            #line 1 "Bar"
            class Goo : {|CS0535:IComparable|}{|CS1513:|}{|CS1514:|}


            #line default
            #line hidden
            // stuff
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545476")]
    public Task TestOptionalDateTime1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            interface IGoo
            {
                void Goo([Optional][DateTimeConstant(100)] DateTime x);
            }

            public class C : {|CS0535:IGoo|}
            {
            }
            """,
            FixedCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            interface IGoo
            {
                void Goo([Optional][DateTimeConstant(100)] DateTime x);
            }

            public class C : IGoo
            {
                public void Goo([DateTimeConstant(100), Optional] DateTime x)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            Options = { AllOptionsOff },
        }.RunAsync();

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545476")]
    public Task TestOptionalDateTime2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            interface IGoo
            {
                void Goo([Optional][DateTimeConstant(100)] DateTime x);
            }

            public class C : {|CS0535:IGoo|}
            {
            }
            """,
            """
            using System;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            interface IGoo
            {
                void Goo([Optional][DateTimeConstant(100)] DateTime x);
            }

            public class C : IGoo
            {
                void IGoo.Goo(DateTime x)
                {
                    throw new NotImplementedException();
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545477")]
    public Task TestIUnknownIDispatchAttributes1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            interface IGoo
            {
                void Goo1([Optional][IUnknownConstant] object x);
                void Goo2([Optional][IDispatchConstant] object x);
            }

            public class C : {|CS0535:{|CS0535:IGoo|}|}
            {
            }
            """,
            """
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            interface IGoo
            {
                void Goo1([Optional][IUnknownConstant] object x);
                void Goo2([Optional][IDispatchConstant] object x);
            }

            public class C : IGoo
            {
                public void Goo1([IUnknownConstant, Optional] object x)
                {
                    throw new System.NotImplementedException();
                }

                public void Goo2([IDispatchConstant, Optional] object x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545477")]
    public Task TestIUnknownIDispatchAttributes2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            interface IGoo
            {
                void Goo1([Optional][IUnknownConstant] object x);
                void Goo2([Optional][IDispatchConstant] object x);
            }

            public class C : {|CS0535:{|CS0535:IGoo|}|}
            {
            }
            """,
            """
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            interface IGoo
            {
                void Goo1([Optional][IUnknownConstant] object x);
                void Goo2([Optional][IDispatchConstant] object x);
            }

            public class C : IGoo
            {
                void IGoo.Goo1(object x)
                {
                    throw new System.NotImplementedException();
                }

                void IGoo.Goo2(object x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545464")]
    public Task TestTypeNameConflict()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IGoo
            {
                void Goo();
            }

            public class Goo : {|CS0535:IGoo|}
            {
            }
            """,
            """
            interface IGoo
            {
                void Goo();
            }

            public class Goo : IGoo
            {
                void IGoo.Goo()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestStringLiteral()
        => TestWithAllCodeStyleOptionsOffAsync(
@"interface IGoo { void Goo ( string s = ""\"""" ) ; } class B : {|CS0535:IGoo|} { }",
"""
interface IGoo { void Goo ( string s = "\"" ) ; }
class B : IGoo
{
    public void Goo(string s = "\"")
    {
        throw new System.NotImplementedException();
    }
}
""");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")]
    public Task TestOptionalNullableStructParameter1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            struct b
            {
            }

            interface d
            {
                void m(b? x = null, b? y = default(b?));
            }

            class c : {|CS0535:d|}
            {
            }
            """,
            """
            struct b
            {
            }

            interface d
            {
                void m(b? x = null, b? y = default(b?));
            }

            class c : d
            {
                public void m(b? x = null, b? y = null)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")]
    public Task TestOptionalNullableStructParameter2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            struct b
            {
            }

            interface d
            {
                void m(b? x = null, b? y = default(b?));
            }

            class c : {|CS0535:d|}
            {
            }
            """,
            """
            struct b
            {
            }

            interface d
            {
                void m(b? x = null, b? y = default(b?));
            }

            class c : d
            {
                void d.m(b? x, b? y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """, index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916114")]
    public Task TestOptionalNullableIntParameter()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface d
            {
                void m(int? x = 5, int? y = null);
            }

            class c : {|CS0535:d|}
            {
            }
            """,
            """
            interface d
            {
                void m(int? x = 5, int? y = null);
            }

            class c : d
            {
                public void m(int? x = 5, int? y = null)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545613")]
    public Task TestOptionalWithNoDefaultValue()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Runtime.InteropServices;

            interface I
            {
                void Goo([Optional] I o);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            using System.Runtime.InteropServices;

            interface I
            {
                void Goo([Optional] I o);
            }

            class C : I
            {
                public void Goo([Optional] I o)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestIntegralAndFloatLiterals()
        => new VerifyCS.Test
        {
            TestCode = """
            interface I
            {
                void M01(short s = short.MinValue);
                void M02(short s = -1);
                void M03(short s = short.MaxValue);
                void M04(ushort s = ushort.MinValue);
                void M05(ushort s = 1);
                void M06(ushort s = ushort.MaxValue);
                void M07(int s = int.MinValue);
                void M08(int s = -1);
                void M09(int s = int.MaxValue);
                void M10(uint s = uint.MinValue);
                void M11(uint s = 1);
                void M12(uint s = uint.MaxValue);
                void M13(long s = long.MinValue);
                void M14(long s = -1);
                void M15(long s = long.MaxValue);
                void M16(ulong s = ulong.MinValue);
                void M17(ulong s = 1);
                void M18(ulong s = ulong.MaxValue);
                void M19(float s = float.MinValue);
                void M20(float s = 1);
                void M21(float s = float.MaxValue);
                void M22(double s = double.MinValue);
                void M23(double s = 1);
                void M24(double s = double.MaxValue);
            }

            class C : {|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:I|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}|}
            {
            }
            """,
            FixedCode = """
            interface I
            {
                void M01(short s = short.MinValue);
                void M02(short s = -1);
                void M03(short s = short.MaxValue);
                void M04(ushort s = ushort.MinValue);
                void M05(ushort s = 1);
                void M06(ushort s = ushort.MaxValue);
                void M07(int s = int.MinValue);
                void M08(int s = -1);
                void M09(int s = int.MaxValue);
                void M10(uint s = uint.MinValue);
                void M11(uint s = 1);
                void M12(uint s = uint.MaxValue);
                void M13(long s = long.MinValue);
                void M14(long s = -1);
                void M15(long s = long.MaxValue);
                void M16(ulong s = ulong.MinValue);
                void M17(ulong s = 1);
                void M18(ulong s = ulong.MaxValue);
                void M19(float s = float.MinValue);
                void M20(float s = 1);
                void M21(float s = float.MaxValue);
                void M22(double s = double.MinValue);
                void M23(double s = 1);
                void M24(double s = double.MaxValue);
            }

            class C : I
            {
                public void M01(short s = short.MinValue)
                {
                    throw new System.NotImplementedException();
                }

                public void M02(short s = -1)
                {
                    throw new System.NotImplementedException();
                }

                public void M03(short s = short.MaxValue)
                {
                    throw new System.NotImplementedException();
                }

                public void M04(ushort s = 0)
                {
                    throw new System.NotImplementedException();
                }

                public void M05(ushort s = 1)
                {
                    throw new System.NotImplementedException();
                }

                public void M06(ushort s = ushort.MaxValue)
                {
                    throw new System.NotImplementedException();
                }

                public void M07(int s = int.MinValue)
                {
                    throw new System.NotImplementedException();
                }

                public void M08(int s = -1)
                {
                    throw new System.NotImplementedException();
                }

                public void M09(int s = int.MaxValue)
                {
                    throw new System.NotImplementedException();
                }

                public void M10(uint s = 0)
                {
                    throw new System.NotImplementedException();
                }

                public void M11(uint s = 1)
                {
                    throw new System.NotImplementedException();
                }

                public void M12(uint s = uint.MaxValue)
                {
                    throw new System.NotImplementedException();
                }

                public void M13(long s = long.MinValue)
                {
                    throw new System.NotImplementedException();
                }

                public void M14(long s = -1)
                {
                    throw new System.NotImplementedException();
                }

                public void M15(long s = long.MaxValue)
                {
                    throw new System.NotImplementedException();
                }

                public void M16(ulong s = 0)
                {
                    throw new System.NotImplementedException();
                }

                public void M17(ulong s = 1)
                {
                    throw new System.NotImplementedException();
                }

                public void M18(ulong s = ulong.MaxValue)
                {
                    throw new System.NotImplementedException();
                }

                public void M19(float s = float.MinValue)
                {
                    throw new System.NotImplementedException();
                }

                public void M20(float s = 1)
                {
                    throw new System.NotImplementedException();
                }

                public void M21(float s = float.MaxValue)
                {
                    throw new System.NotImplementedException();
                }

                public void M22(double s = double.MinValue)
                {
                    throw new System.NotImplementedException();
                }

                public void M23(double s = 1)
                {
                    throw new System.NotImplementedException();
                }

                public void M24(double s = double.MaxValue)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            Options = { AllOptionsOff },
        }.RunAsync();

    [Fact]
    public Task TestEnumLiterals()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            enum E
            {
               A = 1,
               B = 2  
            }

            [FlagsAttribute]
            enum FlagE
            {
               A = 1,
               B = 2
            }

            interface I
            {
                void M1(E e = E.A | E.B);
                void M2(FlagE e = FlagE.A | FlagE.B);
            }

            class C : {|CS0535:{|CS0535:I|}|}
            {
            }
            """,
            """
            using System;

            enum E
            {
               A = 1,
               B = 2  
            }

            [FlagsAttribute]
            enum FlagE
            {
               A = 1,
               B = 2
            }

            interface I
            {
                void M1(E e = E.A | E.B);
                void M2(FlagE e = FlagE.A | FlagE.B);
            }

            class C : I
            {
                public void M1(E e = (E)3)
                {
                    throw new NotImplementedException();
                }

                public void M2(FlagE e = FlagE.A | FlagE.B)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestCharLiterals()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I
            {
                void M01(char c = '\0');
                void M02(char c = '\r');
                void M03(char c = '\n');
                void M04(char c = '\t');
                void M05(char c = '\b');
                void M06(char c = '\v');
                void M07(char c = '\'');
                void M08(char c = '“');
                void M09(char c = 'a');
                void M10(char c = '"');
                void M11(char c = '\u2029');
            }

            class C : {|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:I|}|}|}|}|}|}|}|}|}|}|}
            {
            }
            """,
            """
            using System;

            interface I
            {
                void M01(char c = '\0');
                void M02(char c = '\r');
                void M03(char c = '\n');
                void M04(char c = '\t');
                void M05(char c = '\b');
                void M06(char c = '\v');
                void M07(char c = '\'');
                void M08(char c = '“');
                void M09(char c = 'a');
                void M10(char c = '"');
                void M11(char c = '\u2029');
            }

            class C : I
            {
                public void M01(char c = '\0')
                {
                    throw new NotImplementedException();
                }

                public void M02(char c = '\r')
                {
                    throw new NotImplementedException();
                }

                public void M03(char c = '\n')
                {
                    throw new NotImplementedException();
                }

                public void M04(char c = '\t')
                {
                    throw new NotImplementedException();
                }

                public void M05(char c = '\b')
                {
                    throw new NotImplementedException();
                }

                public void M06(char c = '\v')
                {
                    throw new NotImplementedException();
                }

                public void M07(char c = '\'')
                {
                    throw new NotImplementedException();
                }

                public void M08(char c = '“')
                {
                    throw new NotImplementedException();
                }

                public void M09(char c = 'a')
                {
                    throw new NotImplementedException();
                }

                public void M10(char c = '"')
                {
                    throw new NotImplementedException();
                }

                public void M11(char c = '\u2029')
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545695")]
    public Task TestRemoveParenthesesAroundTypeReference1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I
            {
                void Goo(DayOfWeek x = DayOfWeek.Friday);
            }

            class C : {|CS0535:I|}
            {
                DayOfWeek DayOfWeek { get; set; }
            }
            """,
            """
            using System;

            interface I
            {
                void Goo(DayOfWeek x = DayOfWeek.Friday);
            }

            class C : I
            {
                DayOfWeek DayOfWeek { get; set; }

                public void Goo(DayOfWeek x = DayOfWeek.Friday)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545696")]
    public Task TestDecimalConstants1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Goo(decimal x = decimal.MaxValue);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            interface I
            {
                void Goo(decimal x = decimal.MaxValue);
            }

            class C : I
            {
                public void Goo(decimal x = decimal.MaxValue)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545711")]
    public Task TestNullablePrimitiveLiteral()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Goo(decimal? x = decimal.MaxValue);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            interface I
            {
                void Goo(decimal? x = decimal.MaxValue);
            }

            class C : I
            {
                public void Goo(decimal? x = decimal.MaxValue)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545715")]
    public Task TestNullableEnumType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I
            {
                void Goo(DayOfWeek? x = DayOfWeek.Friday);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            using System;

            interface I
            {
                void Goo(DayOfWeek? x = DayOfWeek.Friday);
            }

            class C : I
            {
                public void Goo(DayOfWeek? x = DayOfWeek.Friday)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545752")]
    public Task TestByteLiterals()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Goo(byte x = 1);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            interface I
            {
                void Goo(byte x = 1);
            }

            class C : I
            {
                public void Goo(byte x = 1)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545736")]
    public Task TestCastedOptionalParameter1()
        => TestWithAllCodeStyleOptionsOffAsync("""
            using System;
            interface I
            {
                void Goo(ConsoleColor x = (ConsoleColor)(-1));
            }

            class C : {|CS0535:I|}
            {
            }
            """, """
            using System;
            interface I
            {
                void Goo(ConsoleColor x = (ConsoleColor)(-1));
            }

            class C : I
            {
                public void Goo(ConsoleColor x = (ConsoleColor)(-1))
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545737")]
    public Task TestCastedEnumValue()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface I
            {
                void Goo(ConsoleColor x = (ConsoleColor)int.MaxValue);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            using System;

            interface I
            {
                void Goo(ConsoleColor x = (ConsoleColor)int.MaxValue);
            }

            class C : I
            {
                public void Goo(ConsoleColor x = (ConsoleColor)int.MaxValue)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545785")]
    public Task TestNoCastFromZeroToEnum()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            enum E
            {
                A = 1,
            }

            interface I
            {
                void Goo(E x = 0);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            enum E
            {
                A = 1,
            }

            interface I
            {
                void Goo(E x = 0);
            }

            class C : I
            {
                public void Goo(E x = 0)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545793")]
    public Task TestMultiDimArray()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Runtime.InteropServices;

            interface I
            {
                void Goo([Optional][DefaultParameterValue(1)] int x, int[,] y);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            using System.Runtime.InteropServices;

            interface I
            {
                void Goo([Optional][DefaultParameterValue(1)] int x, int[,] y);
            }

            class C : I
            {
                public void Goo([{|CS1745:DefaultParameterValue|}(1), {|CS1745:Optional|}] int x = {|CS8017:1|}, int[,] y = null)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545794")]
    public Task TestParametersAfterOptionalParameter()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Runtime.InteropServices;

            interface I
            {
                void Goo([Optional, DefaultParameterValue(1)] int x, int[] y, int[] z);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            using System.Runtime.InteropServices;

            interface I
            {
                void Goo([Optional, DefaultParameterValue(1)] int x, int[] y, int[] z);
            }

            class C : I
            {
                public void Goo([{|CS1745:DefaultParameterValue|}(1), {|CS1745:Optional|}] int x = {|CS8017:1|}, int[] y = null, int[] z = null)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545605")]
    public async Task TestAttributeInParameter()
    {
        var test = new VerifyCS.Test
        {
            TestCode =
            """
            using System;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            interface I
            {
                void Goo([Optional][DateTimeConstant(100)] DateTime d1, [Optional][IUnknownConstant] object d2);
            }
            class C : {|CS0535:I|}
            {
            }
            """,
            FixedCode =
            """
            using System;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            interface I
            {
                void Goo([Optional][DateTimeConstant(100)] DateTime d1, [Optional][IUnknownConstant] object d2);
            }
            class C : I
            {
                public void Goo([DateTimeConstant(100), Optional] DateTime d1, [IUnknownConstant, Optional] object d2)
                {
                    throw new NotImplementedException();
                }
            }
            """,
        };

        test.Options.AddRange(AllOptionsOff);
        await test.RunAsync();
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545897")]
    public Task TestNameConflictBetweenMethodAndTypeParameter()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I<S>
            {
                void T1<T>(S x, T y);
            }

            class C<T> : {|CS0535:I<T>|}
            {
            }
            """,
            """
            interface I<S>
            {
                void T1<T>(S x, T y);
            }

            class C<T> : I<T>
            {
                public void T1<T2>(T x, T2 y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545895")]
    public Task TestTypeParameterReplacementWithOuterType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Collections.Generic;

            interface I<S>
            {
                void Goo<T>(S y, List<T>.Enumerator x);
            }

            class D<T> : {|CS0535:I<T>|}
            {
            }
            """,
            """
            using System.Collections.Generic;

            interface I<S>
            {
                void Goo<T>(S y, List<T>.Enumerator x);
            }

            class D<T> : I<T>
            {
                public void Goo<T1>(T y, List<T1>.Enumerator x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545864")]
    public Task TestFloatConstant()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Goo(float x = 1E10F);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            interface I
            {
                void Goo(float x = 1E10F);
            }

            class C : I
            {
                public void Goo(float x = 1E+10F)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544640")]
    public Task TestKeywordForTypeParameterName()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Goo<@class>();
            }

            class C : {|CS0535:I|}{|CS1513:|}{|CS1514:|}
            """,
            """
            interface I
            {
                void Goo<@class>();
            }

            class C : I
            {
                public void Goo<@class>()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545922")]
    public Task TestExtremeDecimals()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Goo1(decimal x = 1E28M);
                void Goo2(decimal x = -1E28M);
            }

            class C : {|CS0535:{|CS0535:I|}|}
            {
            }
            """,
            """
            interface I
            {
                void Goo1(decimal x = 1E28M);
                void Goo2(decimal x = -1E28M);
            }

            class C : I
            {
                public void Goo1(decimal x = 10000000000000000000000000000M)
                {
                    throw new System.NotImplementedException();
                }

                public void Goo2(decimal x = -10000000000000000000000000000M)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544659")]
    public Task TestNonZeroScaleDecimals()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Goo(decimal x = 0.1M);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            interface I
            {
                void Goo(decimal x = 0.1M);
            }

            class C : I
            {
                public void Goo(decimal x = 0.1M)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544639")]
    public Task TestUnterminatedComment()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            // Implement interface
            class C : {|CS0535:IServiceProvider|} {|CS1035:|}/*
            {|CS1513:|}{|CS1514:|}
            """,
            """
            using System;

            // Implement interface
            class C : IServiceProvider /*
            */
            {
                public object GetService(Type serviceType)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529920")]
    public Task TestNewLineBeforeDirective()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            // Implement interface
            class C : {|CS0535:IServiceProvider|}{|CS1513:|}{|CS1514:|}
            #pragma warning disable
            """,
            """
            using System;

            // Implement interface
            class C : IServiceProvider
            {
                public object GetService(Type serviceType)
                {
                    throw new NotImplementedException();
                }
            }
            #pragma warning disable
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529947")]
    public Task TestCommentAfterInterfaceList1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            class C : {|CS0535:IServiceProvider|}{|CS1513:|}{|CS1514:|} // Implement interface

            """,
            """
            using System;

            class C : IServiceProvider // Implement interface
            {
                public object GetService(Type serviceType)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529947")]
    public Task TestCommentAfterInterfaceList2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            class C : {|CS0535:IServiceProvider|}{|CS1513:|}{|CS1514:|} 
            // Implement interface
            """,
            """
            using System;

            class C : IServiceProvider
            {
                public object GetService(Type serviceType)
                {
                    throw new NotImplementedException();
                }
            }
            // Implement interface
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958699")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994456")]
    public Task TestImplementIDisposable_NoDisposePattern()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            class C : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}
            """,
            """
            using System;
            class C : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958699")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994456")]
    public Task TestImplementIDisposable_DisposePattern()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            class C : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}
            """,
            $$"""
            using System;
            class C : IDisposable
            {
                private bool disposedValue;

            {{DisposePattern("protected virtual ", "C", "public void ")}}
            }
            """, index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958699")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994456")]
    public Task TestImplementIDisposableExplicitly_NoDisposePattern()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            class C : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}
            """,
            """
            using System;
            class C : IDisposable
            {
                void IDisposable.Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """, index: 2);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/941469")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994456")]
    public Task TestImplementIDisposableExplicitly_DisposePattern()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            class C : {|CS0535:System.IDisposable|}
            {
                class IDisposable
                {
                }
            }
            """,
            $$"""
            using System;
            class C : System.IDisposable
            {
                private bool disposedValue;

                class IDisposable
                {
                }

            {{DisposePattern("protected virtual ", "C", "void System.IDisposable.")}}
            }
            """, index: 3);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958699")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994456")]
    public Task TestImplementIDisposableAbstractly_NoDisposePattern()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            abstract class C : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}
            """,
            """
            using System;
            abstract class C : IDisposable
            {
                public abstract void Dispose();
            }
            """, index: 2);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958699")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994456")]
    public Task TestImplementIDisposableThroughMember_NoDisposePattern()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            class C : {|CS0535:IDisposable|}
            {
                private IDisposable goo;
            }
            """,
            """
            using System;
            class C : IDisposable
            {
                private IDisposable goo;

                public void Dispose()
                {
                    goo.Dispose();
                }
            }
            """, index: 2);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/941469")]
    public Task TestImplementIDisposableExplicitly_NoNamespaceImportForSystem()
        => new VerifyCS.Test
        {
            TestCode = @"class C : {|CS0535:System.IDisposable|}{|CS1513:|}{|CS1514:|}",
            FixedCode = $$"""
            class C : System.IDisposable
            {
                private bool disposedValue;

            {{DisposePattern("protected virtual ", "C", "void System.IDisposable.", gcPrefix: "System.")}}
            }
            """,
            CodeActionIndex = 3,
        }.RunAsync();

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/951968")]
    public Task TestImplementIDisposableViaBaseInterface_NoDisposePattern()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            interface I : IDisposable
            {
                void F();
            }
            class C : {|CS0535:{|CS0535:I|}|}
            {
            }
            """,
            """
            using System;
            interface I : IDisposable
            {
                void F();
            }
            class C : I
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }

                public void F()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/951968")]
    public Task TestImplementIDisposableViaBaseInterface()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            interface I : IDisposable
            {
                void F();
            }
            class C : {|CS0535:{|CS0535:I|}|}
            {
            }
            """,
            $$"""
            using System;
            interface I : IDisposable
            {
                void F();
            }
            class C : I
            {
                private bool disposedValue;

                public void F()
                {
                    throw new NotImplementedException();
                }

            {{DisposePattern("protected virtual ", "C", "public void ")}}
            }
            """, index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/951968")]
    public Task TestImplementIDisposableExplicitlyViaBaseInterface()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            interface I : IDisposable
            {
                void F();
            }
            class C : {|CS0535:{|CS0535:I|}|}
            {
            }
            """,
            $$"""
            using System;
            interface I : IDisposable
            {
                void F();
            }
            class C : I
            {
                private bool disposedValue;

                void I.F()
                {
                    throw new NotImplementedException();
                }

            {{DisposePattern("protected virtual ", "C", "void IDisposable.")}}
            }
            """, index: 3);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/941469")]
    public Task TestDoNotImplementDisposePatternForLocallyDefinedIDisposable()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            namespace System
            {
                interface IDisposable
                {
                    void Dispose();
                }

                class C : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}
            }
            """,
            """
            namespace System
            {
                interface IDisposable
                {
                    void Dispose();
                }

                class C : IDisposable
                {
                    void IDisposable.Dispose()
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """, index: 1);

    [Fact]
    public Task TestDoNotImplementDisposePatternForStructures1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            struct S : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}
            """,
            """
            using System;
            struct S : IDisposable
            {
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestDoNotImplementDisposePatternForStructures2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            struct S : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}
            """,
            """
            using System;
            struct S : IDisposable
            {
                void IDisposable.Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """, index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545924")]
    public async Task TestEnumNestedInGeneric()
    {
        var test = new VerifyCS.Test()
        {
            TestCode = """
            class C<T>
            {
                public enum E
                {
                    X
                }
            }

            interface I
            {
                void Goo<T>(C<T>.E x = C<T>.E.X);
            }

            class D : {|CS0535:I|}
            {
            }
            """,
            FixedCode = """
            class C<T>
            {
                public enum E
                {
                    X
                }
            }

            interface I
            {
                void Goo<T>(C<T>.E x = C<T>.E.X);
            }

            class D : I
            {
                public void Goo<T>(C<T>.E x = C<T>.E.X)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
        };

        test.Options.AddRange(AllOptionsOff);
        await test.RunAsync();
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545939")]
    public Task TestUnterminatedString1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            class C : {|CS0535:IServiceProvider|} {|CS1039:|}@"{|CS1513:|}{|CS1514:|}
            """,
            """
            using System;

            class C : IServiceProvider {|CS1003:@""|}{
                public object GetService(Type serviceType)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545939")]
    public Task TestUnterminatedString2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            class C : {|CS0535:IServiceProvider|} {|CS1010:|}"{|CS1513:|}{|CS1514:|}
            """,
            """
            using System;

            class C : IServiceProvider {|CS1003:""|}{
                public object GetService(Type serviceType)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545939")]
    public Task TestUnterminatedString3()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            class C : {|CS0535:IServiceProvider|} {|CS1039:|}@"{|CS1513:|}{|CS1514:|}
            """,
            """
            using System;

            class C : IServiceProvider {|CS1003:@""|}{
                public object GetService(Type serviceType)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545939")]
    public Task TestUnterminatedString4()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            class C : {|CS0535:IServiceProvider|} {|CS1010:|}"{|CS1513:|}{|CS1514:|}
            """,
            """
            using System;

            class C : IServiceProvider {|CS1003:""|}{
                public object GetService(Type serviceType)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545940")]
    public Task TestDecimalENotation()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void Goo1(decimal x = 1E-25M);
                void Goo2(decimal x = -1E-25M);
                void Goo3(decimal x = 1E-24M);
                void Goo4(decimal x = -1E-24M);
            }

            class C : {|CS0535:{|CS0535:{|CS0535:{|CS0535:I|}|}|}|}
            {
            }
            """,
            """
            interface I
            {
                void Goo1(decimal x = 1E-25M);
                void Goo2(decimal x = -1E-25M);
                void Goo3(decimal x = 1E-24M);
                void Goo4(decimal x = -1E-24M);
            }

            class C : I
            {
                public void Goo1(decimal x = 0.0000000000000000000000001M)
                {
                    throw new System.NotImplementedException();
                }

                public void Goo2(decimal x = -0.0000000000000000000000001M)
                {
                    throw new System.NotImplementedException();
                }

                public void Goo3(decimal x = 0.000000000000000000000001M)
                {
                    throw new System.NotImplementedException();
                }

                public void Goo4(decimal x = -0.000000000000000000000001M)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545938")]
    public async Task TestGenericEnumWithRenamedTypeParameters()
    {
        var test = new VerifyCS.Test
        {
            TestCode = """
            class C<T>
            {
                public enum E
                {
                    X
                }
            }

            interface I<S>
            {
                void Goo<T>(S y, C<T>.E x = C<T>.E.X);
            }

            class D<T> : {|CS0535:I<T>|}
            {
            }
            """,
            FixedCode = """
            class C<T>
            {
                public enum E
                {
                    X
                }
            }

            interface I<S>
            {
                void Goo<T>(S y, C<T>.E x = C<T>.E.X);
            }

            class D<T> : I<T>
            {
                public void Goo<T1>(T y, C<T1>.E x = C<T1>.E.X)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
        };

        test.Options.AddRange(AllOptionsOff);
        await test.RunAsync();
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545919")]
    public Task TestDoNotRenameTypeParameterToParameterName()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I<S>
            {
                void Goo<T>(S T1);
            }

            class C<T> : {|CS0535:I<T>|}
            {
            }
            """,
            """
            interface I<S>
            {
                void Goo<T>(S T1);
            }

            class C<T> : I<T>
            {
                public void Goo<T2>(T T1)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530265")]
    public Task TestAttributes()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Runtime.InteropServices;

            interface I
            {
                [return: MarshalAs(UnmanagedType.U1)]
                bool Goo([MarshalAs(UnmanagedType.U1)] bool x);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            using System.Runtime.InteropServices;

            interface I
            {
                [return: MarshalAs(UnmanagedType.U1)]
                bool Goo([MarshalAs(UnmanagedType.U1)] bool x);
            }

            class C : I
            {
                [return: MarshalAs(UnmanagedType.U1)]
                public bool Goo([MarshalAs(UnmanagedType.U1)] bool x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530265")]
    public Task TestAttributesExplicit()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Runtime.InteropServices;

            interface I
            {
                [return: MarshalAs(UnmanagedType.U1)]
                bool Goo([MarshalAs(UnmanagedType.U1)] bool x);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            using System.Runtime.InteropServices;

            interface I
            {
                [return: MarshalAs(UnmanagedType.U1)]
                bool Goo([MarshalAs(UnmanagedType.U1)] bool x);
            }

            class C : I
            {
                bool I.Goo(bool x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546443")]
    public Task TestParameterNameWithTypeName()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            interface IGoo
            {
                void Bar(DateTime DateTime);
            }

            class C : {|CS0535:IGoo|}
            {
            }
            """,
            """
            using System;

            interface IGoo
            {
                void Bar(DateTime DateTime);
            }

            class C : IGoo
            {
                public void Bar(DateTime DateTime)
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530521")]
    public Task TestUnboundGeneric()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Collections.Generic;
            using System.Runtime.InteropServices;

            interface I
            {
                [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(List<>))]
                void Goo();
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            using System.Collections.Generic;
            using System.Runtime.InteropServices;

            interface I
            {
                [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(List<>))]
                void Goo();
            }

            class C : I
            {
                [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(List<>))]
                public void Goo()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/752436")]
    public Task TestQualifiedNameImplicitInterface()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            namespace N
            {
                public interface I
                {
                    void M();
                }
            }

            class C : {|CS0535:N.I|}
            {
            }
            """,
            """
            namespace N
            {
                public interface I
                {
                    void M();
                }
            }

            class C : N.I
            {
                public void M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/752436")]
    public Task TestQualifiedNameExplicitInterface()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            namespace N
            {
                public interface I
                {
                    void M();
                }
            }

            class C : {|CS0535:N.I|}
            {
            }
            """,
            """
            using N;

            namespace N
            {
                public interface I
                {
                    void M();
                }
            }

            class C : N.I
            {
                void I.M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """, index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/847464")]
    public Task TestImplementInterfaceForPartialType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            public interface I
            {
                void Goo();
            }

            partial class C
            {
            }

            partial class C : {|CS0535:I|}
            {
            }
            """,
            """
            public interface I
            {
                void Goo();
            }

            partial class C
            {
            }

            partial class C : I
            {
                void I.Goo()
                {
                    throw new System.NotImplementedException();
                }
            }
            """, index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/847464")]
    public Task TestImplementInterfaceForPartialType2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            public interface I
            {
                void Goo();
            }

            partial class C : {|CS0535:I|}
            {
            }

            partial class C
            {
            }
            """,
            """
            public interface I
            {
                void Goo();
            }

            partial class C : I
            {
                void I.Goo()
                {
                    throw new System.NotImplementedException();
                }
            }

            partial class C
            {
            }
            """, index: 1);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/847464")]
    public Task TestImplementInterfaceForPartialType3()
        => new VerifyCS.Test
        {
            TestCode = """
            public interface I
            {
                void Goo();
            }

            public interface I2
            {
                void Goo2();
            }

            partial class C : {|CS0535:I|}
            {
            }

            partial class C : {|CS0535:I2|}
            {
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    public interface I
                    {
                        void Goo();
                    }

                    public interface I2
                    {
                        void Goo2();
                    }

                    partial class C : I
                    {
                        void I.Goo()
                        {
                            throw new System.NotImplementedException();
                        }
                    }

                    partial class C : {|CS0535:I2|}
                    {
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/752447")]
    public async Task TestExplicitImplOfIndexedProperty()
    {
        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    public class Test : {|CS0535:{|CS0535:IGoo|}|}
                    {
                    }
                    """,
                },
                AdditionalProjects =
                {
                    ["Assembly1", LanguageNames.VisualBasic] =
                    {
                        Sources =
                        {
                            """
                            Public Interface IGoo
                                Property IndexProp(ByVal p1 As Integer) As String
                            End Interface
                            """,
                        },
                    },
                },
                AdditionalProjectReferences =
                {
                    "Assembly1",
                },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    public class Test : IGoo
                    {
                        string IGoo.get_IndexProp(int p1)
                        {
                            throw new System.NotImplementedException();
                        }

                        void IGoo.set_IndexProp(int p1, string Value)
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                    """,
                },
            },
            CodeActionIndex = 1,
        };

        test.Options.AddRange(AllOptionsOff);
        await test.RunAsync();
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602475")]
    public Task TestImplicitImplOfIndexedProperty()
        => new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    using System;

                    class C : {|CS0535:{|CS0535:I|}|}
                    {
                    }
                    """,
                },
                AdditionalProjects =
                {
                    ["Assembly1", LanguageNames.VisualBasic] =
                    {
                        Sources =
                        {
                            """
                            Public Interface I
                                Property P(x As Integer)
                            End Interface
                            """,
                        },
                    },
                },
                AdditionalProjectReferences = { "Assembly1" },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    using System;

                    class C : I
                    {
                        public object get_P(int x)
                        {
                            throw new NotImplementedException();
                        }

                        public void set_P(int x, object Value)
                        {
                            throw new NotImplementedException();
                        }
                    }
                    """,
                },
            },
        }.RunAsync();

    [Fact]
    public async Task TestImplementationOfIndexerWithInaccessibleAttributes()
    {
        var test = new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    using System;

                    class C : {|CS0535:I|}
                    {
                    }
                    """,
                },
                AdditionalProjects =
                {
                    ["Assembly1"] =
                    {
                        Sources =
                        {
                            """
                            using System;
                            internal class ShouldBeRemovedAttribute : Attribute { }
                            public interface I
                            {
                                string this[[ShouldBeRemovedAttribute] int i] { get; set; }
                            }
                            """
                        },
                    },
                },
                AdditionalProjectReferences =
                {
                    "Assembly1",
                },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    using System;

                    class C : I
                    {
                        public string this[int i]
                        {
                            get
                            {
                                throw new NotImplementedException();
                            }

                            set
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }
                    """,
                },
            },
        };

        test.Options.AddRange(AllOptionsOff);
        await test.RunAsync();
    }

#if false
    [WorkItem(13677)]
    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
    public async Task TestNoGenerateInVenusCase2()
    {
        await TestMissingAsync(
@"using System;
#line 1 ""Bar""
class Goo : [|IComparable|]
#line default
#line hidden");
    }
#endif

    [Fact]
    public Task TestImplementInterfaceForImplicitIDisposable()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            class Program : {|CS0535:IDisposable|}
            {
            }
            """,
            $$"""
            using System;

            class Program : IDisposable
            {
                private bool disposedValue;

            {{DisposePattern("protected virtual ", "Program", "public void ")}}
            }
            """, index: 1);

    [Fact]
    public Task TestImplementInterfaceForExplicitIDisposable()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            class Program : {|CS0535:IDisposable|}
            {
                private bool DisposedValue;
            }
            """,
            $$"""
            using System;

            class Program : IDisposable
            {
                private bool DisposedValue;
                private bool disposedValue;

            {{DisposePattern("protected virtual ", "Program", "void IDisposable.")}}
            }
            """, index: 3);

    [Fact]
    public Task TestImplementInterfaceForIDisposableNonApplicable1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            class Program : {|CS0535:IDisposable|}
            {
                private bool disposedValue;
            }
            """,
            """
            using System;

            class Program : IDisposable
            {
                private bool disposedValue;

                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestImplementInterfaceForIDisposableNonApplicable2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            class Program : {|CS0535:IDisposable|}
            {
                public void Dispose(bool flag)
                {
                }
            }
            """,
            """
            using System;

            class Program : IDisposable
            {
                public void Dispose(bool flag)
                {
                }

                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestImplementInterfaceForExplicitIDisposableWithSealedClass()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            sealed class Program : {|CS0535:IDisposable|}
            {
            }
            """,
            $$"""
            using System;

            sealed class Program : IDisposable
            {
                private bool disposedValue;

            {{DisposePattern("private ", "Program", "void IDisposable.")}}
            }
            """, index: 3);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9760")]
    public Task TestImplementInterfaceForExplicitIDisposableWithExistingField()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            class Program : {|CS0535:IDisposable|}
            {
                private bool disposedValue;
            }
            """,
            $$"""
            using System;

            class Program : IDisposable
            {
                private bool disposedValue;
                private bool disposedValue1;

            {{DisposePattern("protected virtual ", "Program", "public void ", disposeField: "disposedValue1")}}
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9760")]
    public Task TestImplementInterfaceUnderscoreNameForFields()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            class Program : {|CS0535:IDisposable|}
            {
            }
            """,
            FixedCode = $$"""
            using System;

            class Program : IDisposable
            {
                private bool _disposedValue;

            {{DisposePattern("protected virtual ", "Program", "public void ", disposeField: "_disposedValue")}}
            }
            """,
            Options =
            {
                _options.FieldNamesAreCamelCaseWithUnderscorePrefix,
            },
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939123")]
    public Task TestNoComAliasNameAttributeOnMethodParameters()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                void M([System.Runtime.InteropServices.ComAliasName("pAlias")] int p);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            interface I
            {
                void M([System.Runtime.InteropServices.ComAliasName("pAlias")] int p);
            }

            class C : I
            {
                public void M(int p)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939123")]
    public Task TestNoComAliasNameAttributeOnMethodReturnType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Runtime.InteropServices;

            interface I
            {
                [return: ComAliasName("pAlias1")]
                long M([ComAliasName("pAlias2")] int p);
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            using System.Runtime.InteropServices;

            interface I
            {
                [return: ComAliasName("pAlias1")]
                long M([ComAliasName("pAlias2")] int p);
            }

            class C : I
            {
                public long M(int p)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939123")]
    public Task TestNoComAliasNameAttributeOnIndexerParameters()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I
            {
                long this[[System.Runtime.InteropServices.ComAliasName("pAlias")] int p] { get; }
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            """
            interface I
            {
                long this[[System.Runtime.InteropServices.ComAliasName("pAlias")] int p] { get; }
            }

            class C : I
            {
                public long this[int p]
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/947819")]
    public Task TestMissingOpenBrace()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            namespace Scenarios
            {
                public interface TestInterface
                {
                    void M1();
                }

                struct TestStruct1 : {|CS0535:TestInterface|}{|CS1513:|}{|CS1514:|}


                // Comment
            }
            """,
            """
            namespace Scenarios
            {
                public interface TestInterface
                {
                    void M1();
                }

                struct TestStruct1 : TestInterface
                {
                    public void M1()
                    {
                        throw new System.NotImplementedException();
                    }
                }


                // Comment
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994328")]
    public async Task TestDisposePatternWhenAdditionalUsingsAreIntroduced1()
    {
#if NET
        var extraUsing = """

            using System.Diagnostics.CodeAnalysis;
            """;

        var equalsMethod = """
                public bool Equals([AllowNull] int other)
                {
                    throw new NotImplementedException();
                }
            """;
#else
        var extraUsing = "";

        var equalsMethod = """
                public bool Equals(int other)
                {
                    throw new NotImplementedException();
                }
            """;
#endif

        //CSharpCodeFixesResources.DisposePattern
        await TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I<T, U> : System.IDisposable, System.IEquatable<int> where U : T
            {
                System.Collections.Generic.List<U> M(System.Collections.Generic.Dictionary<T, System.Collections.Generic.List<U>> a, T b, U c);
                System.Collections.Generic.List<UU> M<TT, UU>(System.Collections.Generic.Dictionary<TT, System.Collections.Generic.List<UU>> a, TT b, UU c) where UU : TT;
            }

            partial class C
            {
            }

            partial class C : {|CS0535:{|CS0535:{|CS0535:I<System.Exception, System.AggregateException>|}|}|}, {|CS0535:System.IDisposable|}
            {
            }
            """,
            $$"""
            using System;
            using System.Collections.Generic;{{extraUsing}}

            interface I<T, U> : System.IDisposable, System.IEquatable<int> where U : T
            {
                System.Collections.Generic.List<U> M(System.Collections.Generic.Dictionary<T, System.Collections.Generic.List<U>> a, T b, U c);
                System.Collections.Generic.List<UU> M<TT, UU>(System.Collections.Generic.Dictionary<TT, System.Collections.Generic.List<UU>> a, TT b, UU c) where UU : TT;
            }

            partial class C
            {
            }

            partial class C : I<System.Exception, System.AggregateException>, System.IDisposable
            {
                private bool disposedValue;
            
            {{equalsMethod}}

                public List<AggregateException> M(Dictionary<Exception, List<AggregateException>> a, Exception b, AggregateException c)
                {
                    throw new NotImplementedException();
                }

                public List<UU> M<TT, UU>(Dictionary<TT, List<UU>> a, TT b, UU c) where UU : TT
                {
                    throw new NotImplementedException();
                }

            {{DisposePattern("protected virtual ", "C", "public void ")}}
            }
            """, index: 1);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994328")]
    public Task TestDisposePatternWhenAdditionalUsingsAreIntroduced2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface I<T, U> : System.IDisposable, System.IEquatable<int> where U : T
            {
                System.Collections.Generic.List<U> M(System.Collections.Generic.Dictionary<T, System.Collections.Generic.List<U>> a, T b, U c);
                System.Collections.Generic.List<UU> M<TT, UU>(System.Collections.Generic.Dictionary<TT, System.Collections.Generic.List<UU>> a, TT b, UU c) where UU : TT;
            }

            partial class C : {|CS0535:{|CS0535:{|CS0535:I<System.Exception, System.AggregateException>|}|}|}, {|CS0535:System.IDisposable|}
            {
            }

            partial class C
            {
            }
            """,
            $$"""
            using System;
            using System.Collections.Generic;

            interface I<T, U> : System.IDisposable, System.IEquatable<int> where U : T
            {
                System.Collections.Generic.List<U> M(System.Collections.Generic.Dictionary<T, System.Collections.Generic.List<U>> a, T b, U c);
                System.Collections.Generic.List<UU> M<TT, UU>(System.Collections.Generic.Dictionary<TT, System.Collections.Generic.List<UU>> a, TT b, UU c) where UU : TT;
            }

            partial class C : I<System.Exception, System.AggregateException>, System.IDisposable
            {
                private bool disposedValue;

                bool IEquatable<int>.Equals(int other)
                {
                    throw new NotImplementedException();
                }

                List<AggregateException> I<Exception, AggregateException>.M(Dictionary<Exception, List<AggregateException>> a, Exception b, AggregateException c)
                {
                    throw new NotImplementedException();
                }

                List<UU> I<Exception, AggregateException>.M<TT, UU>(Dictionary<TT, List<UU>> a, TT b, UU c)
                {
                    throw new NotImplementedException();
                }

            {{DisposePattern("protected virtual ", "C", "void IDisposable.")}}
            }

            partial class C
            {
            }
            """, index: 3);

    private static string DisposePattern(
        string disposeVisibility,
        string className,
        string implementationVisibility,
        string disposeField = "disposedValue",
        string gcPrefix = "")
    {
        return $$"""
                {{disposeVisibility}}void Dispose(bool disposing)
                {
                    if (!{{disposeField}})
                    {
                        if (disposing)
                        {
                            // {{CodeFixesResources.TODO_colon_dispose_managed_state_managed_objects}}
                        }

                        // {{CodeFixesResources.TODO_colon_free_unmanaged_resources_unmanaged_objects_and_override_finalizer}}
                        // {{CodeFixesResources.TODO_colon_set_large_fields_to_null}}
                        {{disposeField}} = true;
                    }
                }

                // // {{string.Format(CodeFixesResources.TODO_colon_override_finalizer_only_if_0_has_code_to_free_unmanaged_resources, "Dispose(bool disposing)")}}
                // ~{{className}}()
                // {
                //     // {{string.Format(CodeFixesResources.Do_not_change_this_code_Put_cleanup_code_in_0_method, "Dispose(bool disposing)")}}
                //     Dispose(disposing: false);
                // }

                {{implementationVisibility}}Dispose()
                {
                    // {{string.Format(CodeFixesResources.Do_not_change_this_code_Put_cleanup_code_in_0_method, "Dispose(bool disposing)")}}
                    Dispose(disposing: true);
                    {{gcPrefix}}GC.SuppressFinalize(this);
                }
            """;
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1132014")]
    public Task TestInaccessibleAttributes()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;

            public class Goo : {|CS0535:Holder.SomeInterface|}
            {
            }

            public class Holder
            {
                public interface SomeInterface
                {
                    void Something([SomeAttribute] string helloWorld);
                }

                private class SomeAttribute : Attribute
                {
                }
            }
            """,
            """
            using System;

            public class Goo : Holder.SomeInterface
            {
                public void Something(string helloWorld)
                {
                    throw new NotImplementedException();
                }
            }

            public class Holder
            {
                public interface SomeInterface
                {
                    void Something([SomeAttribute] string helloWorld);
                }

                private class SomeAttribute : Attribute
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2785")]
    public Task TestImplementInterfaceThroughStaticMemberInGenericClass()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Issue2785<T> : {|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:IList<object>|}|}|}|}|}|}|}|}|}|}|}|}|}
            {
                private static List<object> innerList = new List<object>();
            }
            """,
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Issue2785<T> : IList<object>
            {
                private static List<object> innerList = new List<object>();

                public object this[int index]
                {
                    get
                    {
                        return ((IList<object>)innerList)[index];
                    }

                    set
                    {
                        ((IList<object>)innerList)[index] = value;
                    }
                }

                public int Count
                {
                    get
                    {
                        return ((ICollection<object>)innerList).Count;
                    }
                }

                public bool IsReadOnly
                {
                    get
                    {
                        return ((ICollection<object>)innerList).IsReadOnly;
                    }
                }

                public void Add(object item)
                {
                    ((ICollection<object>)innerList).Add(item);
                }

                public void Clear()
                {
                    ((ICollection<object>)innerList).Clear();
                }

                public bool Contains(object item)
                {
                    return ((ICollection<object>)innerList).Contains(item);
                }

                public void CopyTo(object[] array, int arrayIndex)
                {
                    ((ICollection<object>)innerList).CopyTo(array, arrayIndex);
                }

                public IEnumerator<object> GetEnumerator()
                {
                    return ((IEnumerable<object>)innerList).GetEnumerator();
                }

                public int IndexOf(object item)
                {
                    return ((IList<object>)innerList).IndexOf(item);
                }

                public void Insert(int index, object item)
                {
                    ((IList<object>)innerList).Insert(index, item);
                }

                public bool Remove(object item)
                {
                    return ((ICollection<object>)innerList).Remove(item);
                }

                public void RemoveAt(int index)
                {
                    ((IList<object>)innerList).RemoveAt(index);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((IEnumerable)innerList).GetEnumerator();
                }
            }
            """,
            index: 1);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    public Task LongTuple()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface
            {
                (int, string, int, string, int, string, int, string) Method1((int, string, int, string, int, string, int, string) y);
            }

            class Class : {|CS0535:IInterface|}
            {
                (int, string) x;
            }
            """,
            """
            interface IInterface
            {
                (int, string, int, string, int, string, int, string) Method1((int, string, int, string, int, string, int, string) y);
            }

            class Class : IInterface
            {
                (int, string) x;

                public (int, string, int, string, int, string, int, string) Method1((int, string, int, string, int, string, int, string) y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task LongTupleWithNames()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface
            {
                (int a, string b, int c, string d, int e, string f, int g, string h) Method1((int a, string b, int c, string d, int e, string f, int g, string h) y);
            }

            class Class : {|CS0535:IInterface|}
            {
                (int, string) x;
            }
            """,
            """
            interface IInterface
            {
                (int a, string b, int c, string d, int e, string f, int g, string h) Method1((int a, string b, int c, string d, int e, string f, int g, string h) y);
            }

            class Class : IInterface
            {
                (int, string) x;

                public (int a, string b, int c, string d, int e, string f, int g, string h) Method1((int a, string b, int c, string d, int e, string f, int g, string h) y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task GenericWithTuple()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface<TA, TB>
            {
                (TA, TB) Method1((TA, TB) y);
            }

            class Class : {|CS0535:IInterface<(int, string), int>|}
            {
                (int, string) x;
            }
            """,
            """
            interface IInterface<TA, TB>
            {
                (TA, TB) Method1((TA, TB) y);
            }

            class Class : IInterface<(int, string), int>
            {
                (int, string) x;

                public ((int, string), int) Method1(((int, string), int) y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task GenericWithTupleWithNamess()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            interface IInterface<TA, TB>
            {
                (TA a, TB b) Method1((TA a, TB b) y);
            }

            class Class : {|CS0535:IInterface<(int, string), int>|}
            {
                (int, string) x;
            }
            """,
            """
            interface IInterface<TA, TB>
            {
                (TA a, TB b) Method1((TA a, TB b) y);
            }

            class Class : IInterface<(int, string), int>
            {
                (int, string) x;

                public ((int, string) a, int b) Method1(((int, string) a, int b) y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15387")]
    public Task TestWithGroupingOff1()
        => new VerifyCS.Test
        {
            TestCode = """
            interface IInterface
            {
                int Prop { get; }
            }

            class Class : {|CS0535:IInterface|}
            {
                void M() { }
            }
            """,
            FixedCode = """
            interface IInterface
            {
                int Prop { get; }
            }

            class Class : IInterface
            {
                void M() { }

                public int Prop => throw new System.NotImplementedException();
            }
            """,
            Options =
            {
                new OptionsCollection(LanguageNames.CSharp)
                {
                    { ImplementTypeOptionsStorage.InsertionBehavior, ImplementTypeInsertionBehavior.AtTheEnd }
                }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15387")]
    public Task TestDoNotReorderComImportMembers_01()
        => TestInRegularAndScriptAsync(
            """
            using System.Runtime.InteropServices;

            [ComImport]
            [Guid("00000000-0000-0000-0000-000000000000")]
            interface IComInterface
            {
                void MOverload();
                void X();
                void MOverload(int i);
                int Prop { get; }
            }

            class Class : {|CS0535:{|CS0535:{|CS0535:{|CS0535:IComInterface|}|}|}|}
            {
            }
            """,
            """
            using System.Runtime.InteropServices;

            [ComImport]
            [Guid("00000000-0000-0000-0000-000000000000")]
            interface IComInterface
            {
                void MOverload();
                void X();
                void MOverload(int i);
                int Prop { get; }
            }

            class Class : IComInterface
            {
                public void MOverload()
                {
                    throw new System.NotImplementedException();
                }

                public void X()
                {
                    throw new System.NotImplementedException();
                }

                public void MOverload(int i)
                {
                    throw new System.NotImplementedException();
                }

                public int Prop => throw new System.NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15387")]
    public Task TestDoNotReorderComImportMembers_02()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode =
            """
            using System.Runtime.InteropServices;

            [ComImport]
            [Guid("00000000-0000-0000-0000-000000000000")]
            interface IComInterface
            {
                void {|CS0423:MOverload|}() { }
                void {|CS0423:X|}() { }
                void {|CS0423:MOverload|}(int i) { }
                int Prop { get; }
            }

            class Class : {|CS0535:IComInterface|}
            {
            }
            """,
            FixedCode =
            """
            using System.Runtime.InteropServices;

            [ComImport]
            [Guid("00000000-0000-0000-0000-000000000000")]
            interface IComInterface
            {
                void {|CS0423:MOverload|}() { }
                void {|CS0423:X|}() { }
                void {|CS0423:MOverload|}(int i) { }
                int Prop { get; }
            }

            class Class : IComInterface
            {
                public int Prop => throw new System.NotImplementedException();
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestRefReturns()
        => TestInRegularAndScriptAsync(
            """
            using System;

            interface I {
                ref int IGoo();
                ref int Goo { get; }
                ref int this[int i] { get; }
            }

            class C : {|CS0535:{|CS0535:{|CS0535:I|}|}|}
            {
            }
            """,
            """
            using System;

            interface I {
                ref int IGoo();
                ref int Goo { get; }
                ref int this[int i] { get; }
            }

            class C : I
            {
                public ref int this[int i] => throw new NotImplementedException();

                public ref int Goo => throw new NotImplementedException();

                public ref int IGoo()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5898")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13932")]
    public Task TestAutoProperties()
        => new VerifyCS.Test()
        {
            TestCode = """
            interface IInterface
            {
                int ReadOnlyProp { get; }
                int ReadWriteProp { get; set; }
                int WriteOnlyProp { set; }
            }

            class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                int ReadOnlyProp { get; }
                int ReadWriteProp { get; set; }
                int WriteOnlyProp { set; }
            }

            class Class : IInterface
            {
                public int ReadOnlyProp { get; }
                public int ReadWriteProp { get; set; }
                public int WriteOnlyProp { set => throw new System.NotImplementedException(); }
            }
            """,
            Options =
            {
                new OptionsCollection(LanguageNames.CSharp)
                {
                    { ImplementTypeOptionsStorage.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferAutoProperties }
                }
            }
        }.RunAsync();

    [Fact]
    public Task TestOptionalParameterWithDefaultLiteral()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp7_1,
            TestCode = """
            using System.Threading;

            interface IInterface
            {
                void Method1(CancellationToken cancellationToken = default(CancellationToken));
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            using System.Threading;

            interface IInterface
            {
                void Method1(CancellationToken cancellationToken = default(CancellationToken));
            }

            class Class : IInterface
            {
                public void Method1(CancellationToken cancellationToken = default)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            Options = { AllOptionsOff },
        }.RunAsync();

    [Theory, CombinatorialData]
    public Task TestRefWithMethod_Parameters([CombinatorialValues("ref", "in", "ref readonly")] string modifier)
        => TestInRegularAndScriptAsync(
            $$"""
            interface ITest
            {
                void Method({{modifier}} int p);
            }
            public class Test : {|CS0535:ITest|}
            {
            }
            """,
            $$"""
            interface ITest
            {
                void Method({{modifier}} int p);
            }
            public class Test : ITest
            {
                public void Method({{modifier}} int p)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestRefReadOnlyWithMethod_ReturnType()
        => TestInRegularAndScriptAsync(
            """
            interface ITest
            {
                ref readonly int Method();
            }
            public class Test : {|CS0535:ITest|}
            {
            }
            """,
            """
            interface ITest
            {
                ref readonly int Method();
            }
            public class Test : ITest
            {
                public ref readonly int Method()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestRefReadOnlyWithProperty()
        => TestInRegularAndScriptAsync(
            """
            interface ITest
            {
                ref readonly int Property { get; }
            }
            public class Test : {|CS0535:ITest|}
            {
            }
            """,
            """
            interface ITest
            {
                ref readonly int Property { get; }
            }
            public class Test : ITest
            {
                public ref readonly int Property => throw new System.NotImplementedException();
            }
            """);

    [Theory, CombinatorialData]
    public Task TestRefWithIndexer_Parameters([CombinatorialValues("in", "ref readonly")] string modifier)
        => TestInRegularAndScriptAsync(
            $$"""
            interface ITest
            {
                int this[{{modifier}} int p] { set; }
            }
            public class Test : {|CS0535:ITest|}
            {
            }
            """,
            $$"""
            interface ITest
            {
                int this[{{modifier}} int p] { set; }
            }
            public class Test : ITest
            {
                public int this[{{modifier}} int p] { set => throw new System.NotImplementedException(); }
            }
            """);

    [Fact]
    public Task TestRefReadOnlyWithIndexer_ReturnType()
        => TestInRegularAndScriptAsync(
            """
            interface ITest
            {
                ref readonly int this[int p] { get; }
            }
            public class Test : {|CS0535:ITest|}
            {
            }
            """,
            """
            interface ITest
            {
                ref readonly int this[int p] { get; }
            }
            public class Test : ITest
            {
                public ref readonly int this[int p] => throw new System.NotImplementedException();
            }
            """);

    [Fact]
    public Task TestUnmanagedConstraint()
        => TestInRegularAndScriptAsync(
            """
            public interface ITest
            {
                void M<T>() where T : unmanaged;
            }
            public class Test : {|CS0535:ITest|}
            {
            }
            """,
            """
            public interface ITest
            {
                void M<T>() where T : unmanaged;
            }
            public class Test : ITest
            {
                public void M<T>() where T : unmanaged
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestSealedMember_01()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                sealed void M1() {}
                sealed int P1 => 1;
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                void Method1();

                sealed void M1() {}
                sealed int P1 => 1;
            }

            class Class : IInterface
            {
                public void Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            Options = { AllOptionsOff },
        }.RunAsync();

    [Fact]
    public Task TestSealedMember_02()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                sealed void M1() {}
                sealed int P1 => 1;
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                void Method1();

                sealed void M1() {}
                sealed int P1 => 1;
            }

            class Class : IInterface
            {
                void IInterface.Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            Options = { AllOptionsOff },
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestSealedMember_03()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                sealed void M1() {}
                sealed int P1 => 1;
            }

            abstract class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                void Method1();

                sealed void M1() {}
                sealed int P1 => 1;
            }

            abstract class Class : IInterface
            {
                public abstract void Method1();
            }
            """,
            Options = { AllOptionsOff },
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestNonPublicMember_01()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                protected void M1();
                protected int P1 {get;}
            }

            class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
            {
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    interface IInterface
                    {
                        void Method1();

                        protected void M1();
                        protected int P1 {get;}
                    }

                    class Class : {|CS0535:IInterface|}
                    {
                        public void Method1()
                        {
                            throw new System.NotImplementedException();
                        }
                    
                        void IInterface.M1()
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeActionEquivalenceKey = "False;False;True:global::IInterface;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
        }.RunAsync();

    [Fact]
    public Task TestNonPublicMember_02()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                protected void M1();
                protected int P1 {get;}
            }

            class Class : {|CS0535:{|CS0535:IInterface|}|}
            {
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    interface IInterface
                    {
                        protected void M1();
                        protected int P1 {get;}
                    }

                    class Class : IInterface
                    {
                        int IInterface.P1
                        {
                            get
                            {
                                throw new System.NotImplementedException();
                            }
                        }

                        void IInterface.M1()
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            DiagnosticSelector = diagnostics => diagnostics[1],
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
        }.RunAsync();

    [Fact]
    public Task TestNonPublicMember_03()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                protected void M1();
                protected int P1 {get;}
            }

            abstract class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
            {
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    interface IInterface
                    {
                        void Method1();

                        protected void M1();
                        protected int P1 {get;}
                    }

                    abstract class Class : {|CS0535:IInterface|}
                    {
                        public abstract void Method1();

                        void IInterface.M1()
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeActionEquivalenceKey = "False;True;True:global::IInterface;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
        }.RunAsync();

    [Fact]
    public Task TestNonPublicAccessor_01()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                int P1 {get; protected set;}
                int P2 {protected get; set;}
            }

            class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
            {
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    interface IInterface
                    {
                        void Method1();

                        int P1 {get; protected set;}
                        int P2 {protected get; set;}
                    }

                    class Class : {|CS0535:{|CS0535:IInterface|}|}
                    {
                        public void Method1()
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeActionEquivalenceKey = "False;False;True:global::IInterface;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
        }.RunAsync();

    [Fact]
    public Task TestNonPublicAccessor_02()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                int P1 {get; protected set;}
                int P2 {protected get; set;}
            }

            class Class : {|CS0535:{|CS0535:IInterface|}|}
            {
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    interface IInterface
                    {
                        int P1 {get; protected set;}
                        int P2 {protected get; set;}
                    }

                    class Class : IInterface
                    {
                        int IInterface.P1
                        {
                            get
                            {
                                throw new System.NotImplementedException();
                            }

                            set
                            {
                                throw new System.NotImplementedException();
                            }
                        }

                        int IInterface.P2
                        {
                            get
                            {
                                throw new System.NotImplementedException();
                            }

                            set
                            {
                                throw new System.NotImplementedException();
                            }
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeActionEquivalenceKey = "True;False;False:global::IInterface;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
        }.RunAsync();

    [Fact]
    public Task TestNonPublicAccessor_03()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                int P1 {get; protected set;}
                int P2 {protected get; set;}
            }

            abstract class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
            {
            }
            """,
            FixedState =
            {
                Sources =
                {
                    """
                    interface IInterface
                    {
                        void Method1();

                        int P1 {get; protected set;}
                        int P2 {protected get; set;}
                    }

                    abstract class Class : {|CS0535:{|CS0535:IInterface|}|}
                    {
                        public abstract void Method1();
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeActionEquivalenceKey = "False;True;True:global::IInterface;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
        }.RunAsync();

    [Fact]
    public Task TestPrivateAccessor_01()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                int P1 {get => 0; private set {}}
                int P2 {private get => 0; set {}}
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                void Method1();

                int P1 {get => 0; private set {}}
                int P2 {private get => 0; set {}}
            }

            class Class : IInterface
            {
                public void Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            Options = { AllOptionsOff },
        }.RunAsync();

    [Fact]
    public Task TestPrivateAccessor_02()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                int P1 {get => 0; private set {}}
                int P2 {private get => 0; set {}}
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                void Method1();

                int P1 {get => 0; private set {}}
                int P2 {private get => 0; set {}}
            }

            class Class : IInterface
            {
                void IInterface.Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            Options = { AllOptionsOff },
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestPrivateAccessor_03()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                int P1 {get => 0; private set {}}
                int P2 {private get => 0; set {}}
            }

            abstract class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                void Method1();

                int P1 {get => 0; private set {}}
                int P2 {private get => 0; set {}}
            }

            abstract class Class : IInterface
            {
                public abstract void Method1();
            }
            """,
            Options = { AllOptionsOff },
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestInaccessibleMember_01()
        => new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
                    {
                    }
                    """,
                },
                AdditionalProjects =
                {
                    ["Assembly1"] =
                    {
                        Sources =
                        {
                            """
                            public interface IInterface
                            {
                                void Method1();

                                internal void M1();
                                internal int P1 {get;}
                            }
                            """,
                        },
                    },
                },
                AdditionalProjectReferences = { "Assembly1" },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    class Class : {|CS0535:{|CS0535:IInterface|}|}
                    {
                        public void Method1()
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },

            // Specify the code action by equivalence key only to avoid trying to implement the interface explicitly with a second code fix pass.
            CodeActionEquivalenceKey = "False;False;True:global::IInterface;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
        }.RunAsync();

    [Fact]
    public Task TestInaccessibleMember_02()
        => new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
                    {
                    }
                    """,
                },
                AdditionalProjects =
                {
                    ["Assembly1"] =
                    {
                        Sources =
                        {
                            """
                            public interface IInterface
                            {
                                void Method1();

                                internal void M1();
                                internal int P1 {get;}
                            }
                            """,
                        },
                    },
                },
                AdditionalProjectReferences = { "Assembly1" },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    class Class : {|CS0535:{|CS0535:IInterface|}|}
                    {
                        void IInterface.Method1()
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestInaccessibleMember_03()
        => new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    abstract class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
                    {
                    }
                    """,
                },
                AdditionalProjects =
                {
                    ["Assembly1"] =
                    {
                        Sources =
                        {
                            """
                            public interface IInterface
                            {
                                void Method1();

                                internal void M1();
                                internal int P1 {get;}
                            }
                            """,
                        },
                    },
                },
                AdditionalProjectReferences = { "Assembly1" },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    abstract class Class : {|CS0535:{|CS0535:IInterface|}|}
                    {
                        public abstract void Method1();
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },

            // Specify the code action by equivalence key only to avoid trying to execute a second code fix pass with a different action
            CodeActionEquivalenceKey = "False;True;True:global::IInterface;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4146")]
    public Task TestAccessibility_Property()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            internal class Goo {}

            internal interface I
            {
                Goo MyProperty { get; }
            }

            public class C : {|CS0535:I|}
            {
            }
            """,
            """
            internal class Goo {}

            internal interface I
            {
                Goo MyProperty { get; }
            }

            public class C : {|CS0535:I|}
            {
                Goo I.MyProperty
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4146")]
    public Task TestAccessibility_Method_InaccessibleReturnType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            internal class Goo {}

            internal interface I
            {
                Goo M();
            }

            public class C : {|CS0535:I|}
            {
            }
            """,
            """
            internal class Goo {}

            internal interface I
            {
                Goo M();
            }

            public class C : {|CS0535:I|}
            {
                Goo I.M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4146")]
    public Task TestAccessibility_Method_InaccessibleParameterType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            internal class Goo {}

            internal interface I
            {
                void M(Goo goo);
            }

            public class C : {|CS0535:I|}
            {
            }
            """,
            """
            internal class Goo {}

            internal interface I
            {
                void M(Goo goo);
            }

            public class C : {|CS0535:I|}
            {
                void I.M(Goo goo)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4146")]
    public Task TestAccessibility_Event()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            internal delegate void MyDelegate();

            internal interface I
            {
                event MyDelegate Event;
            }

            public class C : {|CS0535:I|}
            {
            }
            """,
            """
            internal delegate void MyDelegate();

            internal interface I
            {
                event MyDelegate Event;
            }

            public class C : {|CS0535:I|}
            {
                event MyDelegate I.Event
                {
                    add
                    {
                        throw new System.NotImplementedException();
                    }

                    remove
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4146")]
    public Task TestAccessibility_Indexer_InaccessibleReturnType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            internal class Goo {}

            internal interface I
            {
                Goo this[int i] { get; }
            }

            public class C : {|CS0535:I|}
            {
            }
            """,
            """
            internal class Goo {}

            internal interface I
            {
                Goo this[int i] { get; }
            }

            public class C : {|CS0535:I|}
            {
                Goo I.this[int i]
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4146")]
    public Task TestAccessibility_Indexer_InaccessibleParameterType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            internal class Goo {}

            internal interface I
            {
                int this[Goo goo] { get; }
            }

            public class C : {|CS0535:I|}
            {
            }
            """,
            """
            internal class Goo {}

            internal interface I
            {
                int this[Goo goo] { get; }
            }

            public class C : {|CS0535:I|}
            {
                int I.this[Goo goo]
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4146")]
    public Task TestAccessibility_InaccessibleMemberAsGenericArgument()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System.Collections.Generic;

            internal class Goo {}

            internal interface I
            {
                List<Goo> M();
            }

            public class C : {|CS0535:I|}
            {
            }
            """,
            """
            using System.Collections.Generic;

            internal class Goo {}

            internal interface I
            {
                List<Goo> M();
            }

            public class C : {|CS0535:I|}
            {
                List<Goo> I.M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4146")]
    public Task TestAccessibility_InaccessibleMemberDueToContainingType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            internal class Container
            {
                public class Goo {}
            }

            internal interface I
            {
                Container.Goo M();
            }

            public class C : {|CS0535:I|}
            {
            }
            """,
            """
            internal class Container
            {
                public class Goo {}
            }

            internal interface I
            {
                Container.Goo M();
            }

            public class C : {|CS0535:I|}
            {
                Container.Goo I.M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4146")]
    public Task TestAccessibility_InaccessibleGenericConstraintAsReturnType()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            internal class Goo {}

            internal interface I
            {
                T M<T>() where T: Goo;
            }

            public class C : {|CS0535:I|}
            {
            }
            """,
            """
            internal class Goo {}

            internal interface I
            {
                T M<T>() where T: Goo;
            }

            public class C : {|CS0535:I|}
            {
                T I.M<T>()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4146")]
    public Task TestAccessibility_InaccessibleGenericConstraintAsParameter()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            internal class Goo {}

            internal interface I
            {
                void M<T>(T arg) where T: Goo;
            }

            public class C : {|CS0535:I|}
            {
            }
            """,
            """
            internal class Goo {}

            internal interface I
            {
                void M<T>(T arg) where T: Goo;
            }

            public class C : {|CS0535:I|}
            {
                void I.M<T>(T arg)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4146")]
    public Task TestAccessibility_InaccessibleGenericConstraintWhichIsNotUsed()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            internal class Goo {}

            internal interface I
            {
                void M<T>() where T: Goo;
            }

            public class C : {|CS0535:I|}
            {
            }
            """,
            """
            internal class Goo {}

            internal interface I
            {
                void M<T>() where T: Goo;
            }

            public class C : {|CS0535:I|}
            {
                void I.M<T>()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4146")]
    public Task TestAccessibility_SeveralMembers_ShouldExplicitlyImplementOnlyInaccessible()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            internal class Goo {}

            internal interface I
            {
                int N();
                Goo M();
            }

            public class C : {|CS0535:{|CS0535:I|}|}
            {
            }
            """,
            """
            internal class Goo {}

            internal interface I
            {
                int N();
                Goo M();
            }

            public class C : {|CS0535:{|CS0535:I|}|}
            {
                public int N()
                {
                    throw new System.NotImplementedException();
                }

                Goo I.M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestInaccessibleAccessor_01()
        => new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
                    {
                    }
                    """,
                },
                AdditionalProjects =
                {
                    ["Assembly1"] =
                    {
                        Sources =
                        {
                            """
                            public interface IInterface
                            {
                                void Method1();

                                int P1 {get; internal set;}
                                int P2 {internal get; set;}
                            }
                            """,
                        },
                    },
                },
                AdditionalProjectReferences = { "Assembly1" },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    class Class : {|CS0535:{|CS0535:IInterface|}|}
                    {
                        public void Method1()
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },

            // Specify the code action by equivalence key only to avoid trying to implement the interface explicitly with a second code fix pass.
            CodeActionEquivalenceKey = "False;False;True:global::IInterface;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
        }.RunAsync();

    [Fact]
    public Task TestInaccessibleAccessor_02()
        => new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
                    {
                    }
                    """,
                },
                AdditionalProjects =
                {
                    ["Assembly1"] =
                    {
                        Sources =
                        {
                            """
                            public interface IInterface
                            {
                                void Method1();

                                int P1 {get; internal set;}
                                int P2 {internal get; set;}
                            }
                            """,
                        },
                    },
                },
                AdditionalProjectReferences = { "Assembly1" },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    class Class : {|CS0535:{|CS0535:IInterface|}|}
                    {
                        void IInterface.Method1()
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestInaccessibleAccessor_03()
        => new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    abstract class Class : {|CS0535:{|CS0535:{|CS0535:IInterface|}|}|}
                    {
                    }
                    """,
                },
                AdditionalProjects =
                {
                    ["Assembly1"] =
                    {
                        Sources =
                        {
                            """
                            public interface IInterface
                            {
                                void Method1();

                                int P1 {get; internal set;}
                                int P2 {internal get; set;}
                            }
                            """,
                        },
                    },
                },
                AdditionalProjectReferences = { "Assembly1" },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    abstract class Class : {|CS0535:{|CS0535:IInterface|}|}
                    {
                        public abstract void Method1();
                    }
                    """,
                },
                MarkupHandling = MarkupMode.Allow,
            },
            Options = { AllOptionsOff },

            // Specify the code action by equivalence key only to avoid trying to execute a second code fix pass with a different action
            CodeActionEquivalenceKey = "False;True;True:global::IInterface;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
        }.RunAsync();

    [Fact]
    public Task TestVirtualMember_01()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                virtual void M1() {}
                virtual int P1 => 1;
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                void Method1();

                virtual void M1() {}
                virtual int P1 => 1;
            }

            class Class : IInterface
            {
                public void Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            Options = { AllOptionsOff },
        }.RunAsync();

    [Fact]
    public Task TestVirtualMember_02()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                virtual void M1() {}
                virtual int P1 => 1;
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                void Method1();

                virtual void M1() {}
                virtual int P1 => 1;
            }

            class Class : IInterface
            {
                void IInterface.Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            Options = { AllOptionsOff },
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestVirtualMember_03()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                virtual void M1() {}
                virtual int P1 => 1;
            }

            abstract class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                void Method1();

                virtual void M1() {}
                virtual int P1 => 1;
            }

            abstract class Class : IInterface
            {
                public abstract void Method1();
            }
            """,
            Options = { AllOptionsOff },
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestStaticMember_01()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                static void M1() {}
                static int P1 => 1;
                static int F1;
                public abstract class C {}
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                void Method1();

                static void M1() {}
                static int P1 => 1;
                static int F1;
                public abstract class C {}
            }

            class Class : IInterface
            {
                public void Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            Options = { AllOptionsOff },
        }.RunAsync();

    [Fact]
    public Task TestStaticMember_02()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                static void M1() {}
                static int P1 => 1;
                static int F1;
                public abstract class C {}
            }

            class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                void Method1();

                static void M1() {}
                static int P1 => 1;
                static int F1;
                public abstract class C {}
            }

            class Class : IInterface
            {
                void IInterface.Method1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            Options = { AllOptionsOff },
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestStaticMember_03()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            interface IInterface
            {
                void Method1();

                static void M1() {}
                static int P1 => 1;
                static int F1;
                public abstract class C {}
            }

            abstract class Class : {|CS0535:IInterface|}
            {
            }
            """,
            FixedCode = """
            interface IInterface
            {
                void Method1();

                static void M1() {}
                static int P1 => 1;
                static int F1;
                public abstract class C {}
            }

            abstract class Class : IInterface
            {
                public abstract void Method1();
            }
            """,
            Options = { AllOptionsOff },
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact]
    public Task TestNotNullConstraint()
        => TestInRegularAndScriptAsync(
            """
            public interface ITest
            {
                void M<T>() where T : notnull;
            }
            public class Test : {|CS0535:ITest|}
            {
            }
            """,
            """
            public interface ITest
            {
                void M<T>() where T : notnull;
            }
            public class Test : ITest
            {
                public void M<T>() where T : notnull
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestWithNullableProperty()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            public interface ITest
            {
                string? P { get; }
            }
            public class Test : {|CS0535:ITest|}
            {
            }
            """,
            """
            #nullable enable

            public interface ITest
            {
                string? P { get; }
            }
            public class Test : ITest
            {
                public string? P => throw new System.NotImplementedException();
            }
            """);

    [Fact]
    public async Task TestWithNullablePropertyAlreadyImplemented()
    {
        var code =
            """
            #nullable enable

            public interface ITest
            {
                string? P { get; }
            }
            public class Test : ITest
            {
                public string? P => throw new System.NotImplementedException();
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public Task TestWithNullableMethod()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            public interface ITest
            {
                string? P();
            }
            public class Test : {|CS0535:ITest|}
            {
            }
            """,
            """
            #nullable enable

            public interface ITest
            {
                string? P();
            }
            public class Test : ITest
            {
                public string? P()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestWithNullableEvent()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            using System;

            public interface ITest
            {
                event EventHandler? SomeEvent;
            }
            public class Test : {|CS0535:ITest|}
            {
            }
            """,
            """
            #nullable enable

            using System;

            public interface ITest
            {
                event EventHandler? SomeEvent;
            }
            public class Test : ITest
            {
                public event EventHandler? SomeEvent;
            }
            """);

    [Fact]
    public Task TestWithNullableDisabled()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            public interface ITest
            {
                string? P { get; }
            }

            #nullable disable

            public class Test : {|CS0535:ITest|}
            {
            }
            """,
            """
            #nullable enable

            public interface ITest
            {
                string? P { get; }
            }

            #nullable disable

            public class Test : ITest
            {
                public string P => throw new System.NotImplementedException();
            }
            """);

    [Fact]
    public Task GenericInterfaceNotNull1()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            #nullable enable

            using System.Diagnostics.CodeAnalysis;

            interface IFoo<T>
            {
                [return: NotNull]
                T Bar([DisallowNull] T bar);

                [return: MaybeNull]
                T Baz([AllowNull] T bar);
            }

            class A : {|CS0535:{|CS0535:IFoo<int>|}|}
            {
            }
            """,
            FixedCode = """
            #nullable enable

            using System.Diagnostics.CodeAnalysis;

            interface IFoo<T>
            {
                [return: NotNull]
                T Bar([DisallowNull] T bar);

                [return: MaybeNull]
                T Baz([AllowNull] T bar);
            }

            class A : IFoo<int>
            {
                [return: NotNull]
                public int Bar([DisallowNull] int bar)
                {
                    throw new System.NotImplementedException();
                }

                [return: MaybeNull]
                public int Baz([AllowNull] int bar)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13427")]
    public Task TestDoNotAddNewWithGenericAndNonGenericMethods()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            class B
            {
                public void M<T>() { }
            }

            interface I
            {
                void M();
            }

            class D : B, {|CS0535:I|}
            {
            }
            """,
            """
            class B
            {
                public void M<T>() { }
            }

            interface I
            {
                void M();
            }

            class D : B, I
            {
                public void M()
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task ImplementRemainingExplicitlyWhenPartiallyImplemented()
        => TestInRegularAndScriptAsync("""
            interface I
            {
                void M1();
                void M2();
            }

            class C : {|CS0535:I|}
            {
                public void M1(){}
            }
            """,
            """
            interface I
            {
                void M1();
                void M2();
            }

            class C : {|CS0535:I|}
            {
                public void M1(){}

                void I.M2()
                {
                    throw new System.NotImplementedException();
                }
            }
            """, index: 2);

    [Fact]
    public Task ImplementInitOnlyProperty()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            LanguageVersion = LanguageVersion.CSharp9,
            TestCode = """
            interface I
            {
                int Property { get; init; }
            }

            class C : {|CS0535:I|}
            {
            }
            """,
            FixedCode = """
            interface I
            {
                int Property { get; init; }
            }

            class C : I
            {
                public int Property { get => throw new System.NotImplementedException(); init => throw new System.NotImplementedException(); }
            }
            """,
        }.RunAsync();

    [Fact]
    public async Task ImplementRemainingExplicitlyMissingWhenAllImplemented()
    {
        var code = """
            interface I
            {
                void M1();
                void M2();
            }

            class C : I
            {
                public void M1(){}
                public void M2(){}
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public Task ImplementRemainingExplicitlyMissingWhenAllImplementedAreExplicit()
        => new VerifyCS.Test
        {
            TestCode = """
            interface I
            {
                void M1();
                void M2();
            }

            class C : {|CS0535:I|}
            {
                void I.M1(){}
            }
            """,
            FixedCode = """
            interface I
            {
                void M1();
                void M2();
            }

            class C : I
            {
                public void M2()
                {
                    throw new System.NotImplementedException();
                }

                void I.M1(){}
            }
            """,
            CodeActionsVerifier = codeActions => Assert.Equal(2, codeActions.Length),
        }.RunAsync();

    [Fact]
    public Task TestImplementRemainingExplicitlyNonPublicMember()
        => TestInRegularAndScriptAsync("""
            interface I
            {
                void M1();
                internal void M2();
            }

            class C : {|CS0535:I|}
            {
                public void M1(){}
            }
            """,
            """
            interface I
            {
                void M1();
                internal void M2();
            }

            class C : {|CS0535:I|}
            {
                public void M1(){}

                void I.M2()
                {
                    throw new System.NotImplementedException();
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48295")]
    public Task TestImplementOnRecord_WithSemiColon()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface I
            {
                void M1();
            }

            record C : {|CS0535:I|};
            """,
            FixedCode = """
            interface I
            {
                void M1();
            }

            record C : {|CS0535:I|}
            {
                public void M1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestImplementOnClass_WithSemiColon()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface I
            {
                void M1();
            }

            class C : {|CS0535:I|};
            """,
            FixedCode = """
            interface I
            {
                void M1();
            }

            class C : {|CS0535:I|}
            {
                public void M1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestImplementOnStruct_WithSemiColon()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface I
            {
                void M1();
            }

            struct C : {|CS0535:I|};
            """,
            FixedCode = """
            interface I
            {
                void M1();
            }

            struct C : {|CS0535:I|}
            {
                public void M1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48295")]
    public Task TestImplementOnRecord_WithBracesAndTrivia()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface I
            {
                void M1();
            }

            record C : {|CS0535:I|} { } // hello
            """,
            FixedCode = """
            interface I
            {
                void M1();
            }

            record C : {|CS0535:I|}
            {
                public void M1()
                {
                    throw new System.NotImplementedException();
                }
            } // hello
            """,
        }.RunAsync();

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/48295")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("class")]
    [InlineData("struct")]
    public Task TestImplementOnRecord_WithSemiColonAndTrivia(string record)
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = $$"""

            interface I
            {
                void M1();
            }

            {{record}} C : {|CS0535:I|}; // hello

            """,
            FixedCode = $$"""

            interface I
            {
                void M1();
            }

            {{record}} C : {|CS0535:I|} // hello
            {
                public void M1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49019")]
    public Task TestUnconstrainedGenericInstantiatedWithValueType()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp9,
            TestCode = """
            #nullable enable
            interface IGoo<T>
            {
                void Bar(T? x);
            }

            class C : {|CS0535:IGoo<int>|}
            {
            }
            """,
            FixedCode = """
            #nullable enable
            interface IGoo<T>
            {
                void Bar(T? x);
            }

            class C : IGoo<int>
            {
                public void Bar(int x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49019")]
    public Task TestConstrainedGenericInstantiatedWithValueType()
        => TestInRegularAndScriptAsync("""
            interface IGoo<T> where T : struct
            {
                void Bar(T? x);
            }

            class C : {|CS0535:IGoo<int>|}
            {
            }
            """,
            """
            interface IGoo<T> where T : struct
            {
                void Bar(T? x);
            }

            class C : IGoo<int>
            {
                public void Bar(int? x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49019")]
    public Task TestUnconstrainedGenericInstantiatedWithReferenceType()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp9,
            TestCode = """
            interface IGoo<T>
            {
            #nullable enable
                void Bar(T? x);
            #nullable restore
            }

            class C : {|CS0535:IGoo<string>|}
            {
            }
            """,
            FixedCode = """
            interface IGoo<T>
            {
            #nullable enable
                void Bar(T? x);
            #nullable restore
            }

            class C : IGoo<string>
            {
                public void Bar(string x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49019")]
    public Task TestUnconstrainedGenericInstantiatedWithReferenceType_NullableEnable()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp9,
            TestCode = """
            #nullable enable

            interface IGoo<T>
            {
                void Bar(T? x);
            }

            class C : {|CS0535:IGoo<string>|}
            {
            }
            """,
            FixedCode = """
            #nullable enable

            interface IGoo<T>
            {
                void Bar(T? x);
            }

            class C : IGoo<string>
            {
                public void Bar(string? x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49019")]
    public Task TestConstrainedGenericInstantiatedWithReferenceType()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp9,
            TestCode = """
            #nullable enable
            interface IGoo<T> where T : class
            {
                void Bar(T? x);
            }

            class C : {|CS0535:IGoo<string>|}
            {
            }
            """,
            FixedCode = """
            #nullable enable
            interface IGoo<T> where T : class
            {
                void Bar(T? x);
            }

            class C : IGoo<string>
            {
                public void Bar(string? x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49019")]
    public Task TestConstrainedGenericInstantiatedWithReferenceType_NullableEnable()
        => TestInRegularAndScriptAsync("""
            #nullable enable

            interface IGoo<T> where T : class
            {
                void Bar(T? x);
            }

            class C : {|CS0535:IGoo<string>|}
            {
            }
            """,
            """
            #nullable enable

            interface IGoo<T> where T : class
            {
                void Bar(T? x);
            }

            class C : IGoo<string>
            {
                public void Bar(string? x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53012")]
    public Task TestNullableTypeParameter()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            interface I
            {
                void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d);
            }

            class D : {|CS0535:I|}
            {
            }
            """,
            """
            #nullable enable

            interface I
            {
                void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d);
            }

            class D : I
            {
                public void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53012")]
    public Task TestNullableTypeParameter_ExplicitInterfaceImplementation()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            interface I
            {
                void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d);
            }

            class D : {|CS0535:I|}
            {
            }
            """,
            """
            #nullable enable

            interface I
            {
                void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d);
            }

            class D : I
            {
                void I.M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d)
                    where T1 : default
                    where T3 : default
                {
                    throw new System.NotImplementedException();
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53012")]
    public Task TestNullableTypeParameter_ExplicitInterfaceImplementationWithClassConstraint()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            interface I
            {
                void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d) where T1 : class;
            }

            class D : {|CS0535:I|}
            {
            }
            """,
            """
            #nullable enable

            interface I
            {
                void M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d) where T1 : class;
            }

            class D : I
            {
                void I.M<T1, T2, T3>(T1? a, T2 b, T1? c, T3? d)
                    where T1 : class
                    where T3 : default
                {
                    throw new System.NotImplementedException();
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51779")]
    public Task TestImplementTwoPropertiesOfCSharp5()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp5,
            TestCode = """
            interface ITest
            {
                int Bar { get; }
                int Foo { get; }
            }

            class Program : {|CS0535:{|CS0535:ITest|}|}
            {
            }
            """,
            FixedCode = """
            interface ITest
            {
                int Bar { get; }
                int Foo { get; }
            }

            class Program : ITest
            {
                public int Bar
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }
                }

                public int Foo
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53925")]
    public Task TestStaticAbstractInterfaceMember()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest
            {
                static abstract void M1();
            }

            class C : {|CS0535:ITest|}
            {
            }
            """,
            FixedCode = """
            interface ITest
            {
                static abstract void M1();
            }

            class C : ITest
            {
                public static void M1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_interface, codeAction.Title),
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53925")]
    public Task TestStaticAbstractInterfaceMemberExplicitly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest
            {
                static abstract void M1();
            }

            class C : {|CS0535:ITest|}
            {
            }
            """,
            FixedCode = """
            interface ITest
            {
                static abstract void M1();
            }

            class C : ITest
            {
                static void ITest.M1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_all_members_explicitly, codeAction.Title),
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53925")]
    public Task TestStaticAbstractInterfaceMember_ImplementAbstractly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest
            {
                static abstract void M1();
            }

            abstract class C : {|CS0535:ITest|}
            {
            }
            """,
            FixedCode = """
            interface ITest
            {
                static abstract void M1();
            }

            abstract class C : ITest
            {
                public static void M1()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_interface_abstractly, codeAction.Title),
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53927")]
    public Task TestStaticAbstractInterfaceOperator_OnlyExplicitlyImplementable()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest
            {
                static abstract int operator -(ITest x);
            }
            class C : {|CS0535:ITest|}
            {
            }
            """,
            FixedCode = """
            interface ITest
            {
                static abstract int operator -(ITest x);
            }
            class C : ITest
            {
                static int ITest.operator -(ITest x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_all_members_explicitly, codeAction.Title),
        }.RunAsync();

    [Fact]
    public Task TestStaticAbstractInterfaceUnsigneRightShiftOperator_OnlyExplicitlyImplementable()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest
            {
                static abstract int operator >>>(ITest x, int y);
            }
            class C : {|CS0535:ITest|}
            {
            }
            """,
            FixedCode = """
            interface ITest
            {
                static abstract int operator >>>(ITest x, int y);
            }
            class C : ITest
            {
                static int ITest.operator >>>(ITest x, int y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_all_members_explicitly, codeAction.Title),
        }.RunAsync();

    [Theory, CombinatorialData]
    public Task TestInstanceIncrementOperator_ImplementExplicitly([CombinatorialValues("++", "--")] string op)
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.Preview,
            TestCode = $$$"""
                interface ITest
                {
                    abstract void operator {{{op}}}();
                }
                class C : {|CS0535:ITest|}
                {
                }
                """ + CompilerFeatureRequiredAttribute,
            FixedCode = $$$"""
                interface ITest
                {
                    abstract void operator {{{op}}}();
                }
                class C : ITest
                {
                    void ITest.operator {{{op}}}()
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """ + CompilerFeatureRequiredAttribute,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_all_members_explicitly, codeAction.Title),
            CodeActionIndex = 1,
        }.RunAsync();

    [Theory, CombinatorialData]
    public Task TestInstanceCompoundAssignmentOperator_ImplementExplicitly([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.Preview,
            TestCode = $$"""
                interface ITest
                {
                    abstract void operator {{op}}(int y);
                }
                class C : {|CS0535:ITest|}
                {
                }
                """ + CompilerFeatureRequiredAttribute,
            FixedCode = $$"""
                interface ITest
                {
                    abstract void operator {{op}}(int y);
                }
                class C : ITest
                {
                    void ITest.operator {{op}}(int y)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """ + CompilerFeatureRequiredAttribute,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_all_members_explicitly, codeAction.Title),
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53927")]
    public Task TestStaticAbstractInterfaceOperator_ImplementImplicitly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int operator -(T x);
                static abstract int operator -(T x, int y);
            }
            class C : {|CS0535:{|CS0535:ITest<C>|}|}
            {
            }
            """,
            FixedCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int operator -(T x);
                static abstract int operator -(T x, int y);
            }
            class C : ITest<C>
            {
                public static int operator -(C x)
                {
                    throw new System.NotImplementedException();
                }

                public static int operator -(C x, int y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_interface, codeAction.Title),
        }.RunAsync();

    [Fact]
    public Task TestStaticAbstractInterfaceUnsignedRightShiftOperator_ImplementImplicitly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int operator >>>(T x, int y);
            }
            class C : {|CS0535:ITest<C>|}
            {
            }
            """,
            FixedCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int operator >>>(T x, int y);
            }
            class C : ITest<C>
            {
                public static int operator >>>(C x, int y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_interface, codeAction.Title),
        }.RunAsync();

    [Theory]
    [CombinatorialData]
    public Task TestInstanceIncrementOperator_ImplementImplicitly([CombinatorialValues("++", "--")] string op)
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.Preview,
            TestCode = $$"""
            interface ITest<T> where T : ITest<T>
            {
                void operator {{op}}();
            }
            class C : {|CS0535:ITest<C>|}
            {
            }
            """ + CompilerFeatureRequiredAttribute,
            FixedCode = $$"""
            interface ITest<T> where T : ITest<T>
            {
                void operator {{op}}();
            }
            class C : ITest<C>
            {
                public void operator {{op}}()
                {
                    throw new System.NotImplementedException();
                }
            }
            """ + CompilerFeatureRequiredAttribute,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_interface, codeAction.Title),
            CodeActionEquivalenceKey = "False;False;True:global::ITest<global::C>;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            CodeActionIndex = 0,
        }.RunAsync();

    [Theory]
    [CombinatorialData]
    public Task TestInstanceCompoundAssignmentOperator_ImplementImplicitly([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.Preview,
            TestCode = $$"""
            interface ITest<T> where T : ITest<T>
            {
                void operator {{op}}(int y);
            }
            class C : {|CS0535:ITest<C>|}
            {
            }
            """ + CompilerFeatureRequiredAttribute,
            FixedCode = $$"""
            interface ITest<T> where T : ITest<T>
            {
                void operator {{op}}(int y);
            }
            class C : ITest<C>
            {
                public void operator {{op}}(int y)
                {
                    throw new System.NotImplementedException();
                }
            }
            """ + CompilerFeatureRequiredAttribute,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_interface, codeAction.Title),
            CodeActionEquivalenceKey = "False;False;True:global::ITest<global::C>;Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction;",
            CodeActionIndex = 0,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53927")]
    public Task TestStaticAbstractInterfaceOperator_ImplementExplicitly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int operator -(T x);
            }
            class C : {|CS0535:ITest<C>|}
            {
            }
            """,
            FixedCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int operator -(T x);
            }
            class C : ITest<C>
            {
                static int ITest<C>.operator -(C x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_all_members_explicitly, codeAction.Title),
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53927")]
    public Task TestStaticAbstractInterfaceOperator_ImplementAbstractly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int operator -(T x);
            }
            abstract class C : {|CS0535:ITest<C>|}
            {
            }
            """,
            FixedCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int operator -(T x);
            }
            abstract class C : ITest<C>
            {
                public static int operator -(C x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_interface_abstractly, codeAction.Title),
            CodeActionIndex = 1,

        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53927")]
    public Task TestStaticAbstractInterface_Explicitly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest
            {
                static abstract int M(ITest x);
            }
            class C : {|CS0535:ITest|}
            {
            }
            """,
            FixedCode = """
            interface ITest
            {
                static abstract int M(ITest x);
            }
            class C : ITest
            {
                static int ITest.M(ITest x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_all_members_explicitly, codeAction.Title),
            CodeActionIndex = 1,

        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53927")]
    public Task TestStaticAbstractInterface_Implicitly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest
            {
                static abstract int M(ITest x);
            }
            class C : {|CS0535:ITest|}
            {
            }
            """,
            FixedCode = """
            interface ITest
            {
                static abstract int M(ITest x);
            }
            class C : ITest
            {
                public static int M(ITest x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_interface, codeAction.Title),
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53927")]
    public Task TestStaticAbstractInterface_ImplementImplicitly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int M(T x);
            }
            class C : {|CS0535:ITest<C>|}
            {
            }
            """,
            FixedCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int M(T x);
            }
            class C : ITest<C>
            {
                public static int M(C x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_interface, codeAction.Title),
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53927")]
    public Task TestStaticAbstractInterface_ImplementExplicitly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int M(T x);
            }
            class C : {|CS0535:ITest<C>|}
            {
            }
            """,
            FixedCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int M(T x);
            }
            class C : ITest<C>
            {
                static int ITest<C>.M(C x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_all_members_explicitly, codeAction.Title),
            CodeActionIndex = 1,

        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53927")]
    public Task TestStaticAbstractInterface_ImplementAbstractly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int M(T x);
            }
            abstract class C : {|CS0535:ITest<C>|}
            {
            }
            """,
            FixedCode = """
            interface ITest<T> where T : ITest<T>
            {
                static abstract int M(T x);
            }
            abstract class C : ITest<C>
            {
                public static int M(C x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_interface_abstractly, codeAction.Title),
            CodeActionIndex = 1,

        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60214")]
    public Task TestImplementCheckedOperators_Explicitly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface I1<T> where T : I1<T>
            {
                abstract static explicit operator checked string(T x);
                abstract static explicit operator string(T x);

                abstract static T operator checked -(T x);
                abstract static T operator -(T x);

                abstract static T operator checked +(T x, T y);
                abstract static T operator +(T x, T y);
            }

            class C3 : {|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:I1<C3>|}|}|}|}|}|}
            {
            }
            """,
            FixedCode = """
            interface I1<T> where T : I1<T>
            {
                abstract static explicit operator checked string(T x);
                abstract static explicit operator string(T x);

                abstract static T operator checked -(T x);
                abstract static T operator -(T x);

                abstract static T operator checked +(T x, T y);
                abstract static T operator +(T x, T y);
            }

            class C3 : I1<C3>
            {
                static C3 I1<C3>.operator checked +(C3 x, C3 y)
                {
                    throw new System.NotImplementedException();
                }

                static C3 I1<C3>.operator +(C3 x, C3 y)
                {
                    throw new System.NotImplementedException();
                }

                static C3 I1<C3>.operator checked -(C3 x)
                {
                    throw new System.NotImplementedException();
                }

                static C3 I1<C3>.operator -(C3 x)
                {
                    throw new System.NotImplementedException();
                }

                static explicit I1<C3>.operator checked string(C3 x)
                {
                    throw new System.NotImplementedException();
                }

                static explicit I1<C3>.operator string(C3 x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_all_members_explicitly, codeAction.Title),
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60214")]
    public Task TestImplementCheckedOperators_Implicitly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface I1<T> where T : I1<T>
            {
                abstract static explicit operator checked string(T x);
                abstract static explicit operator string(T x);

                abstract static T operator checked -(T x);
                abstract static T operator -(T x);

                abstract static T operator checked +(T x, T y);
                abstract static T operator +(T x, T y);
            }

            class C3 : {|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:I1<C3>|}|}|}|}|}|}
            {
            }
            """,
            FixedCode = """
            interface I1<T> where T : I1<T>
            {
                abstract static explicit operator checked string(T x);
                abstract static explicit operator string(T x);

                abstract static T operator checked -(T x);
                abstract static T operator -(T x);

                abstract static T operator checked +(T x, T y);
                abstract static T operator +(T x, T y);
            }

            class C3 : I1<C3>
            {
                public static C3 operator checked +(C3 x, C3 y)
                {
                    throw new System.NotImplementedException();
                }

                public static C3 operator +(C3 x, C3 y)
                {
                    throw new System.NotImplementedException();
                }

                public static C3 operator checked -(C3 x)
                {
                    throw new System.NotImplementedException();
                }

                public static C3 operator -(C3 x)
                {
                    throw new System.NotImplementedException();
                }

                public static explicit operator checked string(C3 x)
                {
                    throw new System.NotImplementedException();
                }

                public static explicit operator string(C3 x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_interface, codeAction.Title),
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60214")]
    public Task TestImplementCheckedOperators_Abstractly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            interface I1<T> where T : I1<T>
            {
                abstract static explicit operator checked string(T x);
                abstract static explicit operator string(T x);

                abstract static T operator checked -(T x);
                abstract static T operator -(T x);

                abstract static T operator checked +(T x, T y);
                abstract static T operator +(T x, T y);
            }

            abstract class C3 : {|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:I1<C3>|}|}|}|}|}|}
            {
            }
            """,
            FixedCode = """
            interface I1<T> where T : I1<T>
            {
                abstract static explicit operator checked string(T x);
                abstract static explicit operator string(T x);

                abstract static T operator checked -(T x);
                abstract static T operator -(T x);

                abstract static T operator checked +(T x, T y);
                abstract static T operator +(T x, T y);
            }

            abstract class C3 : I1<C3>
            {
                public static C3 operator checked +(C3 x, C3 y)
                {
                    throw new System.NotImplementedException();
                }

                public static C3 operator +(C3 x, C3 y)
                {
                    throw new System.NotImplementedException();
                }

                public static C3 operator checked -(C3 x)
                {
                    throw new System.NotImplementedException();
                }

                public static C3 operator -(C3 x)
                {
                    throw new System.NotImplementedException();
                }

                public static explicit operator checked string(C3 x)
                {
                    throw new System.NotImplementedException();
                }

                public static explicit operator string(C3 x)
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_interface_abstractly, codeAction.Title),
            CodeActionIndex = 1,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34580")]
    public Task TestSupportedConstraints1()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp7_3,
            TestCode =
            """
            using System;

            public interface ITest
            {
                void TestEnum<T>(T value) where T : Enum;
            }

            public abstract class BaseTest : {|CS0535:ITest|}
            {
            }
            """,
            FixedCode =
            """
            using System;

            public interface ITest
            {
                void TestEnum<T>(T value) where T : Enum;
            }

            public abstract class BaseTest : ITest
            {
                public void TestEnum<T>(T value) where T : Enum
                {
                    throw new NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34580")]
    public Task TestSupportedConstraints2()
        => new VerifyCS.Test
        {
            LanguageVersion = LanguageVersion.CSharp7_2,
            TestCode =
            """
            using System;

            public interface ITest
            {
                void TestEnum<T>(T value) where T : {|CS8320:Enum|};
            }

            public abstract class BaseTest : {|CS0535:ITest|}
            {
            }
            """,
            FixedCode =
            """
            using System;

            public interface ITest
            {
                void TestEnum<T>(T value) where T : {|CS8320:Enum|};
            }

            public abstract class BaseTest : ITest
            {
                void ITest.TestEnum<T>(T value)
                {
                    throw new NotImplementedException();
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58136")]
    public Task TestStaticAbstractMembers1()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            using System;

            internal interface I
            {
                internal static abstract int P { get; }

                internal static abstract event Action E;

                internal static abstract void M();
            }

            class C : {|CS0535:{|CS0535:{|CS0535:I|}|}|}
            {
            }
            """,
            FixedCode = """
            using System;

            internal interface I
            {
                internal static abstract int P { get; }

                internal static abstract event Action E;

                internal static abstract void M();
            }

            class C : I
            {
                static int I.P => throw new NotImplementedException();

                static event Action I.E
                {
                    add
                    {
                        throw new NotImplementedException();
                    }

                    remove
                    {
                        throw new NotImplementedException();
                    }
                }

                static void I.M()
                {
                    throw new NotImplementedException();
                }
            }
            """,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_all_members_explicitly, codeAction.Title),
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37374")]
    public Task TestRefReadonly1()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
            TestCode =
            """
            using System.Collections;
            using System.Collections.Generic;

            public interface IRefReadOnlyList<T> : IReadOnlyList<T> where T : struct
            {
                new ref readonly T this[int ix] { get; }
            }

            public struct A { };

            public class Class : {|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:IRefReadOnlyList<A>|}|}|}|}|}
            {
            }
            """,
            FixedCode =
            """
            using System.Collections;
            using System.Collections.Generic;

            public interface IRefReadOnlyList<T> : IReadOnlyList<T> where T : struct
            {
                new ref readonly T this[int ix] { get; }
            }

            public struct A { };

            public class Class : IRefReadOnlyList<A>
            {
                public ref readonly A this[int ix] => throw new System.NotImplementedException();

                A IReadOnlyList<A>.this[int index] => throw new System.NotImplementedException();

                public int Count => throw new System.NotImplementedException();

                public IEnumerator<A> GetEnumerator()
                {
                    throw new System.NotImplementedException();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70232")]
    public Task TestMissingWhenAlreadyContainingImpl()
        => new VerifyCS.Test
        {
            TestCode = """
            interface I
            {
                event System.EventHandler Click;
            }

            class C : I
            {
                event System.EventHandler I.Click { add { } remove { } }

                event System.EventHandler I.Click

            }
            """,
            //LanguageVersion = LanguageVersion.CSharp12,
            ExpectedDiagnostics =
            {
                DiagnosticResult.CompilerError("CS8646").WithSpan(6, 7, 6, 8),
                DiagnosticResult.CompilerError("CS0071").WithSpan(10, 32, 10, 33),
                DiagnosticResult.CompilerError("CS0102").WithSpan(10, 33, 10, 38)
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61263")]
    public Task ImplementStaticConversionsExplicitly()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
                interface I11<T11> where T11 : I11<T11>
                {
                    static abstract implicit operator long(T11 x);
                    static abstract explicit operator int(T11 x);
                }

                class C11 : {|CS0535:{|CS0535:I11<C11>|}|}
                {
                }
                """,
            FixedCode = """
                interface I11<T11> where T11 : I11<T11>
                {
                    static abstract implicit operator long(T11 x);
                    static abstract explicit operator int(T11 x);
                }
            
                class C11 : I11<C11>
                {
                    static implicit I11<C11>.operator long(C11 x)
                    {
                        throw new System.NotImplementedException();
                    }
                
                    static explicit I11<C11>.operator int(C11 x)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """,
            CodeActionIndex = 1,
            CodeActionVerifier = (codeAction, verifier) => verifier.Equal(CodeFixesResources.Implement_all_members_explicitly, codeAction.Title),
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67023")]
    public Task TestIEnumerable1()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Class : {|CS0535:{|CS0535:IEnumerable<int>|}|}
            {
            }
            """,
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Class : IEnumerable<int>
            {
                public IEnumerator<int> GetEnumerator()
                {
                    throw new NotImplementedException();
                }
            
                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67023")]
    public Task TestIEnumerable2()
        => TestWithAllCodeStyleOptionsOffAsync(
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Class : {|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:IEnumerator<int>|}|}|}|}|}
            {
            }
            """,
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Class : IEnumerator<int>
            {
                public int Current
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }
            
                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            
                public bool MoveNext()
                {
                    throw new NotImplementedException();
                }
            
                public void Reset()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67023")]
    public Task TestIEnumerable3()
        => TestWithAllCodeStyleOptionsOnAsync(
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Class : {|CS0535:{|CS0535:{|CS0535:{|CS0535:{|CS0535:IEnumerator<int>|}|}|}|}|}
            {
            }
            """,
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Class : IEnumerator<int>
            {
                public int Current => throw new NotImplementedException();
            
                object IEnumerator.Current => Current;
            
                public void Dispose() => throw new NotImplementedException();
                public bool MoveNext() => throw new NotImplementedException();
                public void Reset() => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72380")]
    public Task TestImplementProtectedAbstract_CSharp9()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                public interface I
                {
                    public abstract void Another();
                    protected abstract void Method();
                }

                public class CI : {|CS0535:{|CS0535:I|}|}
                {
                }
                """,
            FixedCode = """
                using System;

                public interface I
                {
                    public abstract void Another();
                    protected abstract void Method();
                }
            
                public class CI : I
                {
                    public void Another()
                    {
                        throw new NotImplementedException();
                    }
                
                    void I.Method()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72380")]
    public Task TestImplementProtectedAbstract_CSharp10()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                public interface I
                {
                    public abstract void Another();
                    protected abstract void Method();
                }

                public class CI : {|CS0535:{|CS0535:I|}|}
                {
                }
                """,
            FixedCode = """
                using System;

                public interface I
                {
                    public abstract void Another();
                    protected abstract void Method();
                }
            
                public class CI : I
                {
                    public void Another()
                    {
                        throw new NotImplementedException();
                    }
                
                    public void Method()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19721")]
    public Task TestMatchPropertyAgainstPropertyWithMoreAccessors1()
        => TestWithAllCodeStyleOptionsOnAsync(
            """
            interface ImmutableView
            {
                int Prop1 { get; }
            }
            interface MutableView : ImmutableView
            {
                new int Prop1 { get; set; }
            }
            class Implementation : {|CS0535:{|CS0535:MutableView|}|} { }
            """,
            """
            interface ImmutableView
            {
                int Prop1 { get; }
            }
            interface MutableView : ImmutableView
            {
                new int Prop1 { get; set; }
            }
            class Implementation : MutableView
            {
                public int Prop1 { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78281")]
    public Task TestImplementInstanceAssignmentOperator1()
        => new VerifyCS.Test
        {
            TestCode = """
                interface I1
                {
                   void operator ++();

                   void operator -=(I1 i);
                }

                class C1 : {|CS0535:{|CS0535:I1|}|}
                {
                }
                """,
            FixedCode = """
                interface I1
                {
                   void operator ++();
                
                   void operator -=(I1 i);
                }
                
                class C1 : I1
                {
                    public void operator -=(I1 i)
                    {
                        throw new System.NotImplementedException();
                    }
                
                    public void operator ++()
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78281")]
    public Task TestImplementInstanceAssignmentOperator2()
        => new VerifyCS.Test
        {
            TestCode = """
                interface I1
                {
                   void operator ++();

                   void operator -=(I1 i);
                }

                class C1 : {|CS0535:{|CS0535:I1|}|}
                {
                }
                """,
            FixedCode = """
                interface I1
                {
                   void operator ++();
                
                   void operator -=(I1 i);
                }
                
                class C1 : I1
                {
                    void I1.operator -=(I1 i)
                    {
                        throw new System.NotImplementedException();
                    }
                
                    void I1.operator ++()
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """,
            CodeActionIndex = 1,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79584")]
    public Task TestImplementIDisposable_DisposePattern_LF_EndOfLine()
         => new VerifyCS.Test
         {
             TestCode = """
                using System;
                class C : {|CS0535:IDisposable|}{|CS1513:|}{|CS1514:|}
                """.Replace("\r\n", "\n"),
             FixedCode = $$"""
                using System;
                class C : IDisposable
                {
                    private bool disposedValue;

                {{DisposePattern("protected virtual ", "C", "public void ")}}
                }
                """.Replace("\r\n", "\n"),
             CodeActionIndex = 1,
             ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
             LanguageVersion = LanguageVersion.CSharp14,
             Options = { { FormattingOptions2.NewLine, "\n" } },
         }.RunAsync();
}
